# Bug: DeleteObjects (multi-object delete) reports missing keys as `NoSuchKey` errors

| | |
|---|---|
| **Component** | S3 API, `POST /{bucket}?delete` (`S3RequestType.ObjectDeleteMultiple`) |
| **Code** | `src/Less3/Api/S3/ObjectHandler.cs`, `ObjectHandler.DeleteMultiple` (lines 106-171) |
| **Found in** | Docker image `jchristn77/less3:v4.0.0` (SQLite, single node); source in this folder |
| **Found by** | Blobject 6.0.0 contract tests (`Blobject.AmazonS3` / AWSSDK.S3 4.0.103.4) |
| **Severity** | Medium: breaks S3 clients that treat any `<Error>` in a DeleteObjects response as a failure |
| **Status** | Resolved (unreleased, see CHANGELOG.md). Blobject.AmazonS3 6.0.0 works around it (see [Client workaround](#client-workaround)); the workaround can be removed once this ships |

## Resolution

Fixed together with every related defect listed below. The implementation differs from the sketch in this document in four places:

- **ACL/tag cleanup** happens inside `BucketClient.DeleteObject`, under the object lock and keyed by object ID. Calling `DeleteObjectVersionAcl`/`DeleteObjectVersionTags` after the delete, as sketched, would have done nothing for unversioned buckets (the row is already gone when they look it up) and could delete the ACLs of a key re-created concurrently. `DeleteObject` had the same defect and is fixed too.
- **No `VersionId` on a versioned bucket** creates a delete marker on top of the latest version. The sketch kept version 1 as the default, which marked the oldest version and left the object visible.
- **`VersionId` = `"null"`** addresses the null version (the unversioned object) instead of returning `NoSuchVersion`. A well-formed version ID that does not exist is reported as `Deleted`, matching single-object DeleteObject; a malformed one is a per-key `NoSuchVersion` error.
- **Response XML** is written by Less3 rather than S3Server, whose `Deleted` type serializes a null `VersionId` as `xsi:nil` and always writes `DeleteMarker`.

Each key is also authorized individually, exactly as DeleteObject on that key would be, so an object-scoped RBAC deny can no longer be bypassed through the batch API. Tests: `test/Test.Shared/S3Compatibility/DeleteObjectsCompatibilityCases.cs` (22 cases, passing on SQLite, PostgreSQL, MySQL, and SQL Server; 19 of them fail against the original code; the other 3 are positive controls).

## Summary

When a `DeleteObjects` request names a key that does not exist, Less3 returns an `<Error>` with code `NoSuchKey` for that key. Amazon S3 reports a missing key as **successfully deleted** (a `<Deleted>` entry), because deleting a missing object is an idempotent no-op. MinIO does the same.

Less3's own single-object `DeleteObject` already does the right thing (`ObjectHandler.Delete`, lines 82-86: *"object does not exist, returning idempotent delete success"*). The multi-object path is inconsistent with it.

> AWS `DeleteObjects` API reference: *"If the object specified in the request is not found, Amazon S3 returns the result as deleted."*

## Reproduction

### 1. Start Less3

```bash
docker run --rm -p 8000:8000 -v /less3 \
  -v "$PWD/system.json:/app/system.json" jchristn77/less3:v4.0.0
```

`system.json` is `Docker/system.json` with the SQLite database and storage moved onto the volume. The image has no `./db/` directory, so the default `./db/less3.db` fails to open:

```json
"Database": { "Type": "Sqlite", "Filename": "/less3/less3.db", ... },
"Storage":  { "TempDirectory": "/less3/temp/", "PartsDirectory": "/less3/temp/parts/", "DiskDirectory": "/less3/disk/", ... }
```

This uses the seeded defaults: access key `default`, secret key `default`, bucket `default`.

### 2. Delete one existing and one missing key in a single request

AWS CLI:

```bash
export AWS_ACCESS_KEY_ID=default AWS_SECRET_ACCESS_KEY=default AWS_DEFAULT_REGION=us-east-1
aws --endpoint-url http://localhost:8000 s3api put-object --bucket default --key exists.txt --body README.md
aws --endpoint-url http://localhost:8000 s3api delete-objects --bucket default \
  --delete '{"Objects":[{"Key":"exists.txt"},{"Key":"missing.txt"}]}'
```

Or through Blobject (`C:\Code\Blobject`), which fails `Bulk.DeleteManyMissingKeys` and `Bulk.DeleteManyMixedExistingMissing` when the workaround is removed:

```bash
dotnet run --project src/Test.Automated -f net10.0 -- --provider s3 \
  --s3-access-key default --s3-secret-key default --s3-region us-east-1 --s3-bucket default \
  --s3-endpoint http://127.0.0.1:8000 --s3-ssl false \
  --s3-base-url "http://127.0.0.1:8000/{bucket}/{key}" --filter DeleteMany
```

### 3. Actual response (Less3)

```xml
<DeleteResult>
  <Deleted><Key>exists.txt</Key><VersionId>1</VersionId></Deleted>
  <Error><Key>missing.txt</Key><VersionId>1</VersionId><Code>NoSuchKey</Code><Message>...</Message></Error>
</DeleteResult>
```

AWSSDK.S3 then throws `DeleteObjectsException` (partial failure), and the caller sees `missing.txt` as a failed delete.

### 4. Expected response (Amazon S3 / MinIO)

```xml
<DeleteResult>
  <Deleted><Key>exists.txt</Key></Deleted>
  <Deleted><Key>missing.txt</Key></Deleted>
</DeleteResult>
```

## Root cause

`ObjectHandler.DeleteMultiple` reports a missing object as an error in two places:

```csharp
// lines 123-142: key not found before deleting
Obj obj = md.BucketClient.GetObjectVersionMetadata(curr.Key, versionId);
if (obj == null)
{
    ...
    else { error = new Error(ErrorCode.NoSuchKey); }        // <-- should be a Deleted entry
    ...
    deleteResult.Errors.Add(error);
    continue;
}

// lines 144-159: key deleted concurrently between the check above and the delete
if (!md.BucketClient.DeleteObjectVersion(curr.Key, versionId))
{
    ...
    else { error = new Error(ErrorCode.NoSuchKey); }        // <-- should also be a Deleted entry
    deleteResult.Errors.Add(error);
}
```

`BucketClient.DeleteObjectVersion` returns `false` only when the object is not found (under the object lock), so both branches mean "already gone".

## Fix

### Required: report missing keys as deleted

For a request **without** a `VersionId`, a missing key must produce a `<Deleted>` entry. For an explicit `VersionId` that does not exist, the sketch below keeps `NoSuchVersion`. Verify Amazon S3's behavior for that case against a real versioned bucket before settling on it, then document the choice.

```csharp
foreach (S3ServerLibrary.S3Objects.Object curr in dm.Objects)
{
    bool explicitVersion = !String.IsNullOrEmpty(curr.VersionId);
    long versionId = 1;

    if (explicitVersion && !Int64.TryParse(curr.VersionId, out versionId))
    {
        deleteResult.Errors.Add(new Error(ErrorCode.NoSuchVersion) { Key = curr.Key, VersionId = curr.VersionId });
        continue;
    }

    Obj obj = md.BucketClient.GetObjectVersionMetadata(curr.Key, versionId);
    bool deleted = obj != null && md.BucketClient.DeleteObjectVersion(curr.Key, versionId);

    if (!deleted && explicitVersion)
    {
        deleteResult.Errors.Add(new Error(ErrorCode.NoSuchVersion) { Key = curr.Key, VersionId = curr.VersionId });
        continue;
    }

    if (deleted)
    {
        md.BucketClient.DeleteObjectVersionAcl(curr.Key, versionId);
        md.BucketClient.DeleteObjectVersionTags(curr.Key, versionId);
    }

    // missing (no explicit version) or deleted: both are success
    if (!dm.Quiet)
    {
        Deleted entry = new Deleted { Key = curr.Key };
        if (md.Bucket.EnableVersioning) entry.VersionId = versionId.ToString();
        deleteResult.DeletedObjects.Add(entry);
    }
}
```

Adjust property names and initializers to the `S3ServerLibrary` types. The point is the control flow.

### Related defects in the same method (fix together)

These surfaced while reviewing the handler. Each is a divergence from `ObjectHandler.Delete` or from the S3 API.

1. **ACLs and tags are not deleted.**
   - **Problem:** the single-object path deletes the object's ACL and tag rows (`ObjectHandler.Delete`, lines 92-93: `DeleteObjectVersionAcl`, `DeleteObjectVersionTags`). The batch path doesn't, so every object deleted through `DeleteObjects` leaves orphaned ACL and tag rows. Whether a re-created key picks them up depends on how they are keyed; test 11 below checks it.
   - **Fix:** call both after a successful delete, as in the sketch above.
2. **A non-numeric `VersionId` aborts the whole request.**
   - **Problem:** `Convert.ToInt64(curr.VersionId)` (line 121) throws `FormatException` for values such as `"null"` (which S3 clients send for unversioned objects) or any opaque ID. The exception escapes the loop, so the whole batch fails as an unhandled exception and keys later in the request are never processed, instead of producing a per-key error.
   - **Fix:** use `Int64.TryParse` and report `NoSuchVersion` for that key only. This matches `RequestValidator.ParseVersionId`, lines 121-135.
3. **`Quiet` mode is ignored.**
   - **Problem:** `DeleteMultiple.Quiet` (S3ServerLibrary `S3Objects/DeleteMultiple.cs`, line 21) is never read. With `<Quiet>true</Quiet>`, S3 returns only the `<Error>` entries. Less3 returns every `<Deleted>` entry, which inflates responses for large batches (up to 1,000 keys).
   - **Fix:** skip `<Deleted>` entries when `dm.Quiet` is true.
4. **`VersionId` is returned for unversioned buckets.**
   - **Problem:** every `<Deleted>` entry carries `<VersionId>1</VersionId>`, even when versioning is disabled. The single-object path only emits `x-amz-version-id` when `md.Bucket.EnableVersioning` (lines 96-97).
   - **Fix:** emit `VersionId` only for versioned buckets.
5. **A lost lock fails the whole batch.**
   - **Problem:** `DeleteObjectVersion` can throw `LockLostException` (`BucketClient.cs`, around line 505). In `DeleteMultiple` that escapes the loop and fails the whole request, even though earlier keys were already deleted. The client then cannot tell which keys succeeded.
   - **Fix:** catch per key and add an `<Error>` with code `InternalError` for that key. Keep processing the rest.
6. **No telemetry.**
   - **Problem:** `ObjectHandler.Delete` records `DeleteObject` activity and stage timings; `DeleteMultiple` records none.
   - **Fix:** wrap it the same way, e.g. a `DeleteObjects` activity with a per-key count.

## Tests to add

Add these to the Less3 test suite, parameterized over versioning enabled and disabled where relevant.

| # | Scenario | Expected |
|---|---|---|
| 1 | Delete one missing key, no `VersionId` | 200, one `<Deleted>` for the key, no `<Error>` |
| 2 | Delete existing + missing keys in one request | 200, two `<Deleted>`, no `<Error>`; existing object gone |
| 3 | Delete the same key twice in one request | 200, both entries `<Deleted>` |
| 4 | Key deleted by a concurrent `DeleteObject` during the batch | reported `<Deleted>`, not `NoSuchKey` |
| 5 | Explicit `VersionId` that does not exist | the documented choice (`NoSuchVersion` error or `<Deleted>`), consistently |
| 6 | `VersionId` = `"null"` or non-numeric | per-key result; the other keys in the batch still processed; no 500 |
| 7 | `<Quiet>true</Quiet>` with only successes | 200, empty `<DeleteResult/>` |
| 8 | `<Quiet>true</Quiet>` with one failing key | only that key's `<Error>` returned |
| 9 | Unversioned bucket | `<Deleted>` entries have no `<VersionId>` |
| 10 | Object with an ACL and tags deleted via `DeleteObjects` | ACL and tag rows removed (query the database), same as `DeleteObject` |
| 11 | Re-create a key after batch delete (unversioned) | new object has no ACL or tags from the old one |
| 12 | 1,000 keys in one request | all processed; response well-formed |

For an external check, run Blobject's contract suite against Less3 with the `NoSuchKey` workaround removed from `Blobject.AmazonS3`. `Bulk.DeleteManyMissingKeys` and `Bulk.DeleteManyMixedExistingMissing` must pass. Temporarily delete the `NoSuchKey` branch in `AmazonS3BlobClient.DeleteManyAsync` to do this.

## Client workaround

Blobject.AmazonS3 6.0.0 (`AmazonS3BlobClient.DeleteManyAsync`) treats a `DeleteError` with code `NoSuchKey` as a successful deletion. This matches Blobject's documented contract that deleting a missing key succeeds, so Blobject users are unaffected. Other S3 clients still see the non-compliant behavior until Less3 is fixed.

## Acceptance criteria

- `DeleteObjects` responses for missing keys match Amazon S3 (reported as deleted).
- Batch deletes remove ACLs and tags, exactly like `DeleteObject`.
- A malformed or non-numeric `VersionId` never fails the whole batch.
- `Quiet` mode is honored.
- `VersionId` appears in results only for versioned buckets.
- A per-key failure, including a lost lock, is reported per key; other keys still get a result.
- The tests above pass on SQLite and PostgreSQL, single node and cluster.
