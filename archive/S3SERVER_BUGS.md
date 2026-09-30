# S3Server Bugs And Improvements Found While Expanding Less3 Coverage

This file captures issues that appear to belong in `S3Server` itself or in its default protocol handling, rather than in Less3-specific business logic.

Context:

- Less3 was moved to native `AWSSDK.S3`-driven integration tests.
- The expanded suite now exercises bucket ACLs, object ACLs, version listing, multipart, raw XML/error shapes, and negative signature-validation paths.
- Some failures were fixed in Less3 directly. Those are not listed here.
- The items below are the ones that should be considered upstream `S3Server` improvements.

## Status (S3Server 8.0.1)

All items are resolved: 1 to 7 in S3Server 7.4.0 and 8.0.0, and 8 in 8.0.1 (see the S3Server CHANGELOG). Less3 uses the native callbacks for all of them and no longer writes any S3 response body itself.

## 1. Signature validation can fail open when `EnableSignatures = true`

Where:

- `C:\Code\Less3\S3Server-6.0\src\S3Server\S3Server.cs`
- In `RequestHandler`, signature enforcement is effectively gated by both:
  - `_Settings.EnableSignatures`
  - `Service.GetSecretKey != null`

Current behavior:

- If an embedding application sets `EnableSignatures = true` but forgets to wire `Service.GetSecretKey`, `S3Server` does not reject requests.
- It silently skips the entire signature-validation block and continues processing the request.
- This is a fail-open configuration path.

Why this matters:

- An integrator can believe signed requests are being enforced when they are not.
- The failure mode is quiet.
- Negative tests like "wrong secret key must be rejected" will unexpectedly pass unless the embedding app separately notices the missing callback.
- This is a security footgun more than a simple ergonomics issue.

How it surfaced in Less3:

- Less3 had `ValidateSignatures` enabled in settings, but had not wired `S3Server.Service.GetSecretKey`.
- As a result, intentionally bad AWS signatures were accepted until Less3 added the missing callback.

Recommended fix:

- Fail fast at startup if `EnableSignatures` is `true` and `Service.GetSecretKey` is `null`.
- If startup validation is not desirable, then fail closed on request processing instead:
  - log a clear configuration error
  - reject the request with a deterministic server/configuration failure
- At minimum, emit a prominent warning once instead of silently bypassing validation.

Recommended upstream tests:

- `EnableSignatures = true` + no `GetSecretKey` callback should not allow requests through.
- Wrong secret should produce `SignatureDoesNotMatch`.
- Unknown access key should be rejected deterministically.
- Signature V2 should be rejected when signatures are enabled.

## 2. `GET` with `Range` is framed as `200 OK` instead of `206 Partial Content`

Where:

- `C:\Code\Less3\S3Server-6.0\src\S3Server\S3Server.cs`
- `RequestHandler`, `S3RequestType.ObjectReadRange`

Current behavior:

- After the callback returns an object for a range read, `S3Server` sets:
  - `StatusCode = 200`
  - `ContentType`
  - `ContentLength`
- It does not set `206 Partial Content`.
- It also does not mirror the richer header framing used in the normal object-read path.

Why this matters:

- Raw S3 clients expect successful range reads to return `206 Partial Content`.
- Returning `200` for a partial response is protocol-inaccurate and can break stricter clients, proxies, caches, or test harnesses.
- The embedding application cannot fully fix this from the callback because `S3Server` overwrites the response status after the callback returns.

Observed consequence:

- Less3 can set `Content-Range` in its callback, but `S3Server` still forces the final status code to `200`.
- That leaves the response internally inconsistent.

Recommended fix:

- Change the `ObjectReadRange` success path to send `206 Partial Content`.
- Preserve or add the standard headers expected on a partial object response:
  - `Content-Range`
  - `Accept-Ranges`
  - `ETag`
  - `Last-Modified`
  - version header when applicable
- Avoid hardcoding a success shape that the callback cannot refine.

Recommended upstream tests:

- Successful byte-range request returns `206`.
- `Content-Range` is present and matches the requested slice.
- `Accept-Ranges: bytes` is present.
- Range responses still include ETag and last-modified metadata.

## 3. Recognized but unwired operations fall through to generic `InvalidRequest`

Where:

- `C:\Code\Less3\S3Server-6.0\src\S3Server\S3Server.cs`
- Switch cases in `RequestHandler`

Current behavior:

- `S3Server` correctly recognizes many request types.
- If the matching callback is `null`, execution falls through the switch.
- The request eventually lands in:
  - `_Settings.DefaultRequestHandler`, if present, or
  - a generic `InvalidRequest` response

Why this matters:

- This makes supported-but-unimplemented operations hard to diagnose.
- A feature gap looks the same as a malformed request.
- It is especially confusing for operations like:
  - bucket website configuration
  - bucket logging configuration
  - object retention
  - legal hold
  - other optional S3 APIs that are recognized at the routing layer

Why this is worth fixing upstream:

- Once `S3Server` has already parsed the request into a specific `S3RequestType`, it has more information than `InvalidRequest` communicates.
- The library should distinguish:
  - malformed/unknown request
  - recognized request type with no callback implementation

Recommended fix:

- For recognized request types with no registered callback, return a more precise failure.
- A dedicated error such as `NotImplemented`, `UnsupportedOperation`, or a configurable "feature disabled" response would be better than `InvalidRequest`.
- Log the missing callback name when this path is taken.

Recommended upstream tests:

- A recognized request type with no callback should not return the same error as an unparseable request.
- The response body and status should clearly indicate missing implementation.

## 4. Range-response protocol details are split awkwardly between callback code and core server code

Where:

- `C:\Code\Less3\S3Server-6.0\src\S3Server\S3Server.cs`
- The object read and object range response paths are inconsistent.

Current behavior:

- `ObjectRead` adds several protocol headers in `S3Server` itself.
- `ObjectReadRange` leaves important response details to the embedding application callback, but still finalizes the response in the core server.
- This creates an awkward contract:
  - the callback must know protocol details
  - but the core server still owns the final status code and body send

Why this matters:

- It is easy for embedders to produce incomplete or inconsistent range responses.
- The library already knows it is handling `ObjectReadRange`, so it should own the S3 framing of that response just as it does for standard object reads.
- Today the contract is neither fully callback-driven nor fully server-driven.

Recommended fix:

- Normalize object read and object range handling so the protocol framing is owned in one place.
- Either:
  - let the callback fully control the response, or
  - have `S3Server` consistently add the correct S3 headers/status for both full and partial reads.

## Recommended S3Server Test Additions

Even if the implementation changes above are deferred, `S3Server` should add first-class integration coverage for these cases because they are easy to regress:

- Signature validation enabled but callback missing.
- Wrong secret key on a valid access key.
- Unknown access key.
- Signature V2 request rejection.
- Successful byte-range read returns `206`.
- Range response contains `Content-Range` and `Accept-Ranges`.
- Recognized but unwired request types do not collapse into generic malformed-request errors.


## 5. `PreRequestHandler` runs before signature validation

Where: `S3Server.cs`, `RequestHandler`: `Settings.PreRequestHandler` is invoked, and a `true` result ends the request, before the `EnableSignatures` block.

Current behavior: anything an embedding application answers from `PreRequestHandler` is never signature-checked. Less3 answered canned-ACL writes (`PUT ?acl` with no body, which the `ObjectWriteAcl`/`BucketWriteAcl` callbacks cannot accept because they require an XML body) from its pre-request handler, so a request carrying only a valid access key could change a bucket's or object's ACL.

How Less3 works around it: the ACL-write, ListObjects, ListObjectVersions and DeleteObjects callbacks are left unregistered, so S3Server routes those requests to `DefaultRequestHandler`, which runs after signature validation.

Recommended fix: validate signatures before `PreRequestHandler` (or offer a post-authentication hook), and let the ACL-write callbacks receive a null policy when the body is empty.

## 6. Response types cannot express several Amazon S3 responses exactly

- `Deleted.VersionId` and `Deleted.DeleteMarkerVersionId` are `IsNullable = true` without `ShouldSerialize*`, so a null value is written as `<VersionId xsi:nil="true"/>`, and `<DeleteMarker>false</DeleteMarker>` is always written. `Error` in a `DeleteResult` also writes a non-standard `HttpStatusCode` element.
- `ListBucketResult` has no `NextMarker`, `StartAfter` or `ContinuationToken`, so ListObjects v1 delimiter pagination and v2 token echo cannot be expressed.
- `ListVersionsResult` keeps versions and delete markers in separate lists, so they are serialized grouped rather than interleaved in key/version order.
- `ListMultipartUploadsResult` is serialized without the S3 XML namespace.
- `ObjectWrite` has no way to return a body, so CopyObject (`x-amz-copy-source`) cannot return `CopyObjectResult`, and without special handling a copy is treated as a PUT with an empty body. The same applies to `UploadPart` with `x-amz-copy-source` (`CopyPartResult`).
- Read callbacks cannot return `304 Not Modified`, and there is no `ErrorCode` for it.
- A suffix range (`Range: bytes=-N`) sets `RangeEnd` but not `RangeStart`, so the request is classified as `ObjectRead` and the response is framed as `200` instead of `206`.

How Less3 works around it: it writes these responses itself (from `DefaultRequestHandler`, or from inside the callback, returning a zero-length result so S3Server's follow-up send is a no-op).

## 7. Request parsing errors surface as `500`

- A negative `max-keys` makes the `S3Request.MaxKeys` setter throw `ArgumentOutOfRangeException` while the request context is being built, before any callback runs, so the client gets `500 InternalError` instead of `400 InvalidArgument`.
- `ctx.Http.Request.Query.Elements` holds percent-encoded values (e.g. `prefix=alpha%2F`) while `S3Request.Prefix` and friends are decoded; callers reading other query parameters must decode them themselves.

## Additional Recommended S3Server Test Additions

- A canned ACL write with a forged signature is rejected.
- `DeleteResult` for an unversioned key contains no `VersionId` or `DeleteMarker` elements.
- `max-keys=-1` returns `400`.
- `bytes=-N` returns `206` with the last `N` bytes.

## 8. Delete markers in ListObjectVersions carry a nil ETag and a StorageClass (8.0.0; resolved in 8.0.1)

Where: `S3Objects/VersionedEntity.cs` declares `ETag` with `IsNullable = true` and always writes `StorageClass`; `DeleteMarker` sets `ETag` to null. A version listing therefore contains:

```xml
<DeleteMarker><Key>k</Key><VersionId>2</VersionId><IsLatest>true</IsLatest><LastModified>...</LastModified>
  <ETag p3:nil="true" xmlns:p3="http://www.w3.org/2001/XMLSchema-instance"></ETag><StorageClass>STANDARD</StorageClass>
  <Owner>...</Owner></DeleteMarker>
```

Amazon S3 sends only `Key`, `VersionId`, `IsLatest`, `LastModified` and `Owner` for a delete marker. Clients that read the listing are handed an ETag element and a storage class that a delete marker does not have.

How Less3 worked around it until 8.0.1: `Bucket.ReadVersions` was left unregistered. `BucketHandler.ListObjectVersionsXml` built the same `ListVersionsResult`, applied `encoding-type=url` itself (S3Server's encoding overload is internal), serialized it with `SerializationHelper.SerializeXml`, and removed `ETag`, `Size` and `StorageClass` from each `DeleteMarker`. The workaround was removed when Less3 moved to 8.0.1.

Recommended fix: add `ShouldSerializeETag()` (non-empty) and `ShouldSerializeStorageClass()` (false for `DeleteMarker`) to the version entities, and add a Compatibility scenario that lists a delete marker. Less3's `S3CompatProtocol_VersionListing_DeleteMarkerShape` test and the AwsCliTest "Delete marker shape" check assert the Amazon S3 shape.
