# S3 API Compatibility Gaps

Comprehensive analysis of differences between Less3 responses and the AWS S3 API specification.
Testing performed against Less3 running at `http://localhost:8000/` with access key `default` / secret key `default`.

Each gap is attributed to the responsible codebase:
- **Less3** (`c:\code\less3\less3-2.1`) - Application-level logic, handlers, database operations
- **S3Server** (`c:\code\less3\s3server-6.0`) - Library providing scaffolding, request parsing, XML serialization, response handling
- **Both** - Root cause spans both codebases

Each gap also includes a **Fix Strategy** indicating what changes are needed:
- **Less3 only**: Fix entirely within Less3 application code. No S3Server changes needed.
- **S3Server internal**: Requires S3Server behavioral change (status codes, default values, XML attributes) but **no public API break**. Existing consumers recompile without changes.
- **S3Server additive**: Requires adding new public members to S3Server (e.g., `ShouldSerialize*()` methods, new properties, setters). **No existing API breaks** - purely additive.

**All 30 gaps can be fixed without breaking the S3Server public API.**

---

## Table of Contents

1. [Cross-Cutting / Global Issues](#1-cross-cutting--global-issues)
2. [Service APIs](#2-service-apis)
3. [Bucket APIs](#3-bucket-apis)
4. [Object APIs](#4-object-apis)
5. [Multipart Upload APIs](#5-multipart-upload-apis)
6. [Summary of Severity](#6-summary-of-severity)
7. [Fix Strategy Summary](#7-fix-strategy-summary)

---

## 1. Cross-Cutting / Global Issues

These issues affect multiple or all API responses.

### 1.1 XML Namespace Declaration

**Affects:** Every XML response body
**Attribution:** **S3Server** - All model classes in `S3Objects/` have the S3 namespace commented out (e.g., `// Namespace = "http://s3.amazonaws.com/doc/2006-03-01/"`). `SerializationHelper.cs` uses standard .NET `XmlSerializer` which emits default `xsi`/`xsd` namespaces.
**Fix Strategy:** **S3Server internal** - Uncomment/add `Namespace` parameter on `[XmlRoot]` attributes across all model classes. No public API change; property types, names, and method signatures remain identical.

**Less3 returns:**
```xml
<ElementName xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
```

**AWS S3 returns:**
```xml
<ElementName xmlns="http://s3.amazonaws.com/doc/2006-03-01/">
```

Less3 uses the .NET default XML serialization namespaces (`xmlns:xsi` and `xmlns:xsd`) instead of the S3-specific namespace `http://s3.amazonaws.com/doc/2006-03-01/`. This affects every XML response. Most S3 client libraries are lenient about this, but strict XML-namespace-aware parsers may fail.

### 1.2 Null Elements Serialized with `xsi:nil="true"`

**Affects:** ListObjectVersions, ListMultipartUploads, ListParts, CompleteMultipartUpload, Error responses, ACL responses
**Attribution:** **S3Server** - Model classes use `[XmlElement(IsNullable = true)]` on properties, causing .NET `XmlSerializer` to emit `xsi:nil="true"` for null values instead of omitting the element.
**Fix Strategy:** **S3Server internal** - Change `IsNullable = true` to `IsNullable = false` (or remove) on `[XmlElement]` attributes. Properties remain identical in C#; only XML serialization output changes.

**Less3 returns:**
```xml
<KeyMarker xsi:nil="true"></KeyMarker>
<VersionIdMarker xsi:nil="true"></VersionIdMarker>
```

**AWS S3 behavior:** Omits null/empty optional elements entirely, or includes them as empty elements (`<KeyMarker></KeyMarker>` or `<KeyMarker/>`). AWS S3 never uses the `xsi:nil` attribute.

### 1.3 Response Headers Echo Request Headers

**Affects:** Every response
**Attribution:** **Both**
- **Less3** (`Program.cs` lines 440-442): Adds `Accept: */*`, `Accept-Language: en-US, en`, `Accept-Charset: utf8` as response headers.
- **S3Server** (`S3Response.cs` `SetResponseHeaders()` lines 332-348): Adds `Host`, `X-Amz-Date` as response headers.

**Fix Strategy:** **Both Less3 only + S3Server internal**. Less3: remove 3 header lines in `Program.cs`. S3Server: remove `Host` and `X-Amz-Date` additions from `SetResponseHeaders()`. No public API changes.

Less3 responses include several headers that echo the incoming request values. These are not present in AWS S3 responses:

| Header | Less3 Value | AWS S3 |
|--------|-------------|--------|
| `Host` | `localhost` | Not present in response |
| `Accept` | `*/*` | Not present in response |
| `Accept-Language` | `en-US, en` | Not present in response |
| `Accept-Charset` | `utf8` | Not present in response |

These appear to be request headers being reflected back in the response, which is non-standard behavior.

### 1.4 `Server` Header Value

**Attribution:** **S3Server** - `S3Response.cs` `SetResponseHeaders()` sets the `Server` header.
**Fix Strategy:** **S3Server internal** - Change the string literal from `"S3Server"` to `"AmazonS3"` (or make configurable). No public API change.

**Less3:** `Server: S3Server Microsoft-HTTPAPI/2.0`
**AWS S3:** `Server: AmazonS3`

Minor cosmetic difference but notable for clients that check this header.

### 1.5 `X-Amz-Date` Response Header

**Attribution:** **S3Server** - `S3Response.cs` `SetResponseHeaders()` adds `X-Amz-Date` to every response.
**Fix Strategy:** **S3Server internal** - Remove the `X-Amz-Date` addition from `SetResponseHeaders()`. No public API change.

**Less3:** Returns `X-Amz-Date` as a response header (e.g., `X-Amz-Date: Fri, 06 Feb 2026 03:29:01 GMT`)
**AWS S3:** Does not include `X-Amz-Date` in response headers. Uses standard `Date` header only.

### 1.6 ETag Case

**Affects:** ListObjectsV2, ListObjectVersions, GetObject, HeadObject
**Attribution:** **Both**
- **Less3** (`BucketClient.cs` `AddObject`): Stores ETag as uppercase MD5 hash.
- **S3Server**: `ObjectMetadata.ETag` setter does not normalize case.

**Fix Strategy:** **Less3 only** - Call `.ToLowerInvariant()` on MD5 hash before storing in `BucketClient.AddObject`. No S3Server change needed; the setter already accepts any case.

**Less3:** ETags use uppercase hexadecimal: `"7999C2537483E984D7ED4FF602151D3B"`
**AWS S3:** ETags use lowercase hexadecimal: `"7999c2537483e984d7ed4ff602151d3b"`

While ETags should be treated as opaque strings, some clients do case-sensitive comparisons.

### 1.7 Missing `x-amz-version-id` Header on Versioned Buckets

**Affects:** GetObject, HeadObject, PutObject, DeleteObject, PutObjectTagging, DeleteObjectTagging, PutObjectAcl
**Attribution:** **Both**
- **Less3**: Object handlers do not add `x-amz-version-id` to response headers in their callbacks.
- **S3Server**: Does not add `x-amz-version-id` after callbacks complete either.

**Fix Strategy:** **Less3 only** - Add `ctx.Response.Headers.Add("x-amz-version-id", versionId)` in each object handler callback. S3Server's `S3Response.Headers` is a public `NameValueCollection` that callbacks can freely modify, and headers set in callbacks survive through to the response.

When a bucket has versioning enabled, AWS S3 includes `x-amz-version-id` in the response headers for all object operations. Less3 never includes this header on any response.

### 1.8 Error Response Format

**Affects:** All error responses (NoSuchBucket, NoSuchKey, NoSuchVersion, BucketAlreadyExists, BucketNotEmpty, AccessDenied, etc.)
**Attribution:** **S3Server** - `S3Objects/Error.cs`: The `Message` property has a getter (with a switch statement mapping `ErrorCode` to messages) but NO setter, so `XmlSerializer` cannot round-trip it. All fields use `IsNullable = true` producing `xsi:nil`. Element ordering is determined by property declaration order in the model class.
**Fix Strategy:** **S3Server additive + internal** - Add a setter to `Message` (with backing field; getter falls back to switch when not explicitly set). Add `HostId` property. Reorder property declarations to put `Code` first. Change `IsNullable` attributes. The existing constructor and getter still work identically - purely additive.

**Less3 returns:**
```xml
<Error xmlns:xsi="..." xmlns:xsd="...">
  <Key xsi:nil="true"></Key>
  <VersionId xsi:nil="true"></VersionId>
  <RequestId xsi:nil="true"></RequestId>
  <Resource xsi:nil="true"></Resource>
  <Code>NoSuchKey</Code>
</Error>
```

**AWS S3 returns:**
```xml
<Error>
  <Code>NoSuchKey</Code>
  <Message>The specified key does not exist.</Message>
  <Key>nonexistent.txt</Key>
  <RequestId>EXAMPLE-REQUEST-ID</RequestId>
  <HostId>EXAMPLE-HOST-ID</HostId>
</Error>
```

Differences:
- **Missing `<Message>` element**: AWS S3 always includes a human-readable error message. Less3 omits it entirely.
- **Non-standard elements**: Less3 includes `<Key>`, `<VersionId>`, `<Resource>` as null elements in every error. AWS S3 includes context-specific elements (e.g., `<Key>` only for key-related errors, `<BucketName>` for bucket errors).
- **`<RequestId>` is null**: Less3 sets `RequestId` to `xsi:nil="true"` despite having a valid request ID in the `x-amz-request-id` response header. AWS S3 populates this with the actual request ID.
- **Missing `<HostId>`**: AWS S3 always includes `<HostId>`. Less3 omits it.
- **Element ordering**: Less3 puts `<Code>` last; AWS S3 puts it first.

---

## 2. Service APIs

### 2.1 ListBuckets

**Attribution:** **Less3** - `ServiceHandler.cs` line 89 sets `Owner.ID = md.User.Name` instead of `md.User.GUID`, causing `ID` and `DisplayName` to both be the display name.
**Fix Strategy:** **Less3 only** - Change `md.User.Name` to `md.User.GUID` for the Owner.ID assignment. One-line fix.

**Less3 response:**
```xml
<ListAllMyBucketsResult xmlns:xsi="..." xmlns:xsd="...">
  <Owner>
    <ID>Default user</ID>
    <DisplayName>Default user</DisplayName>
  </Owner>
  <Buckets>
    <Bucket>
      <Name>default</Name>
      <CreationDate>2026-02-06T03:08:22.636486Z</CreationDate>
    </Bucket>
  </Buckets>
</ListAllMyBucketsResult>
```

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| Namespace | `xmlns:xsi`/`xmlns:xsd` | `xmlns="http://s3.amazonaws.com/doc/2006-03-01/"` |
| `Owner.ID` | `"Default user"` (display name) | Canonical user ID (64-char hex string) |
| `Owner.ID` equals `Owner.DisplayName` | Both are `"Default user"` | `ID` is a canonical ID; `DisplayName` is human-readable name |

---

## 3. Bucket APIs

### 3.1 CreateBucket

**Attribution:**
- Missing `Location` header: **S3Server** - `S3Server.cs` line 490 sets `StatusCode = 200` but does not add a `Location` header.
- Duplicate bucket 409: **Less3** - `BucketHandler.Write` returns `BucketAlreadyExists` for same-owner duplicate.

**Fix Strategy:**
- Location header: **Less3 only** - Add `ctx.Response.Headers.Add("Location", "/" + bucketName)` in the `BucketHandler.Write` callback. Headers set in callbacks persist through S3Server's response.
- Duplicate bucket: **Less3 only** - Change `BucketHandler.Write` to return success for same-owner duplicate.

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| Requires `Content-Length: 0` header | Returns `411 Length Required` without it | Accepts requests without Content-Length for empty bodies |
| Missing `Location` header in response | Not present | Returns `Location: /<bucket-name>` |
| Duplicate bucket (same owner) | Returns `409 BucketAlreadyExists` | Returns `200 OK` (or `BucketAlreadyOwnedByYou` depending on region) |

### 3.2 HeadBucket

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| Missing `x-amz-bucket-region` header | Not present | Returns region (e.g., `us-east-1`) |
| Content-Type | `text/plain` | No Content-Type (or `application/xml`) |

HeadBucket on non-existent bucket correctly returns 404.

### 3.3 ListObjectsV2

**Critical bugs and gaps:**

| Issue | Less3 | AWS S3 Expected | Severity | Attribution | Fix Strategy |
|-------|-------|-----------------|----------|-------------|--------------|
| **Returns all versions, not just latest** | Lists every version of each key as a separate entry | Only returns the latest non-delete-marked version of each unique key | **Critical** | **Less3** - `BucketClient.cs` `Enumerate` | **Less3 only** - Add version/delete-marker filtering to `Enumerate` query |
| **Deleted objects still appear** | Objects deleted via DeleteObject/DeleteObjects still show in listing | Objects with only delete markers are excluded from ListObjectsV2 | **Critical** | **Less3** - `BucketClient.cs` `Enumerate` | **Less3 only** - Filter out delete markers in `Enumerate` |
| **Owner always included** | `<Owner>` present in every `<Contents>` entry | Only included when `fetch-owner=true` is specified | Medium | **Both** - S3Server `ObjectMetadata` always has `Owner` field (default `new Owner()`); Less3 always populates it | **Less3 only** - Set `Owner = null` when `fetch-owner` not specified. S3Server `ObjectMetadata.Owner` has `IsNullable = true` so null won't serialize once gap 1.2 is fixed; or **S3Server additive** - add `ShouldSerializeOwner()` (pattern already used in `ListBucketResult` for other fields) |
| **`<ContentType>` element** | Included in each `<Contents>` entry | Not part of the ListObjectsV2 response schema | Medium | **S3Server** - `ObjectMetadata.ContentType` defaults to `"application/octet-stream"` and has no `[XmlIgnore]` | **S3Server internal** - Add `[XmlIgnore]` to `ContentType` property. Property remains fully accessible in C# (used by HeadObject/GetObject for HTTP Content-Type header); only suppressed from XML. |
| **`<Delimiter>` always present** | `<Delimiter>/</Delimiter>` in every response | Only included when `delimiter` parameter is sent in request | Low | **S3Server** - `ListBucketResult.cs` line 98: setter forces null/empty to `"/"` | **S3Server internal + additive** - Remove null-to-`"/"` guard in setter, change default to `null`, add `ShouldSerializeDelimiter()` method (pattern already used in this class). |
| **`<EncodingType>` always present** | `<EncodingType>url</EncodingType>` always | Only included when `encoding-type=url` is sent in request | Low | **S3Server** - `ListBucketResult.cs` line 107: defaults to `"url"` | **S3Server internal + additive** - Change default to `null`, add `ShouldSerializeEncodingType()`. |
| **Continuation token not honored** | `continuation-token` parameter appears to be ignored; all results returned | Should resume listing from the position indicated by the token | **High** | **Less3** - `BucketClient.cs` `Enumerate` pagination logic | **Less3 only** - Fix pagination logic in `Enumerate` |
| **ETag case** | Uppercase hex | Lowercase hex | Low | **Both** (see 1.6) | **Less3 only** (see 1.6) |

### 3.4 ListObjectVersions

**Less3 response (abbreviated):**
```xml
<ListVersionsResult>
  <Name>testbucket1</Name>
  <Prefix></Prefix>
  <KeyMarker>testobj.txt</KeyMarker>
  ...
  <Version>
    <Key>testobj.txt</Key>
    <VersionId>1</VersionId>
    ...
  </Version>
  <NextKeyMarker xsi:nil="true"/>
  <NextVersionIdMarker xsi:nil="true"/>
  <Delimiter xsi:nil="true"/>
  <CommonPrefixes></CommonPrefixes>
  <EncodingType xsi:nil="true"/>
</ListVersionsResult>
```

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| `<KeyMarker>` populated when not provided in request | Contains `testobj.txt` (last key) | Empty string or omitted when not provided in request |
| `<VersionId>` is integer | `1`, `2`, etc. | Opaque string (e.g., `3sL4kqtJlcpXroDTDmJ+rmSpXd3dIbrHY+MTRCxf3vjVBH40Nr8X8gdRQBpUMLUo`) |
| Null elements with `xsi:nil` | Multiple elements | Omitted or empty |
| `<CommonPrefixes>` always present | Empty element present | Only present when delimiter is used and there are common prefixes |
| Delete markers not shown | No `<DeleteMarker>` entries | Objects deleted in versioned buckets should show as `<DeleteMarker>` entries |
| Element ordering | `NextKeyMarker`, `NextVersionIdMarker`, etc. after `<Version>` elements | These metadata elements appear before `<Version>` elements in AWS |

### 3.5 GetBucketAcl

**Attribution:** **S3Server** - `Grantee.cs` line 41 uses `[XmlElement(ElementName = "Type")]` on `GranteeType` which serializes as a child element instead of `xsi:type` attribute. Null elements from `IsNullable = true`.
**Fix Strategy:** **S3Server internal** - Add `[XmlIgnore]` to the `GranteeType` property on `Grantee`. The `[XmlInclude(typeof(CanonicalUser))]` and `[XmlInclude(typeof(Group))]` attributes already exist on `Grantee` (lines 10-11), so XmlSerializer will automatically emit `xsi:type` when a derived-class instance is assigned. The `GranteeType` property remains accessible in C# - only its XML serialization is suppressed. Less3 must also change to use `new CanonicalUser()` / `new Group()` instead of `new Grantee()` (Less3 internal change).

**Less3 response (with grant):**
```xml
<AccessControlPolicy>
  <Owner><ID>default</ID><DisplayName>Default user</DisplayName></Owner>
  <AccessControlList>
    <Grant>
      <Grantee>
        <ID>default</ID>
        <DisplayName>Default user</DisplayName>
        <URI xsi:nil="true"></URI>
        <Type>CanonicalUser</Type>
        <EmailAddress xsi:nil="true"></EmailAddress>
      </Grantee>
      <Permission>FULL_CONTROL</Permission>
    </Grant>
  </AccessControlList>
</AccessControlPolicy>
```

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| Grantee type encoding | `<Type>CanonicalUser</Type>` as child element | `xsi:type="CanonicalUser"` as attribute on `<Grantee>` element |
| Null elements in Grantee | `<URI xsi:nil="true"/>`, `<EmailAddress xsi:nil="true"/>` present | Omitted entirely for CanonicalUser grantees |
| No grants returned by default | Empty `<AccessControlList>` for new buckets | AWS S3 always returns at least a FULL_CONTROL grant for the bucket owner |

### 3.6 PutBucketAcl

**Canned ACL behavior (`x-amz-acl: public-read`):**
**Attribution:**
- Owner grant replaced: **Less3** - `AclConverter.cs` `GrantsFromHeaders` (lines 317-323) only creates AllUsers READ grant; does not also create owner FULL_CONTROL.
- Group grantee type wrong: **Less3** - `AclConverter.cs` `AddGroupGrantsToList` (lines 534-608) creates `new Grantee()` (base class) instead of `new Group()`, does not set `GranteeType = "Group"`.

**Fix Strategy:** **Less3 only** - In `GrantsFromHeaders`, add owner FULL_CONTROL grant alongside the canned ACL grants. In `AddGroupGrantsToList`, use `new Group()` instead of `new Grantee()`. Both are internal Less3 changes.

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| Replaces owner grant | Only creates the AllUsers READ grant; loses owner FULL_CONTROL | Creates both: owner FULL_CONTROL + AllUsers READ |
| Group grantee type | `<Type>CanonicalUser</Type>` for AllUsers group | Should be `xsi:type="Group"` |
| Group grantee DisplayName | `<DisplayName>http://acs.amazonaws.com/groups/global/AllUsers</DisplayName>` | No `<DisplayName>` for group grantees; only `<URI>` |

### 3.7 GetBucketTagging

**Attribution:** **Less3** - `BucketHandler.ReadTags` returns 200 with empty `<TagSet>` instead of throwing `NoSuchTagSetError`.
**Fix Strategy:** **Less3 only** - Throw `S3Exception(new Error(ErrorCode.NoSuchTagSetError))` when no tags exist, instead of returning an empty TagSet.

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| No tags returns 200 with empty TagSet | `200 OK` with `<Tagging><TagSet></TagSet></Tagging>` | `404` with error code `NoSuchTagSetError` when no tags are configured |

When tags ARE set, the response structure is correct (Tag/Key/Value elements).

### 3.8 PutBucketTagging

**Attribution:** **S3Server** - `S3Server.cs` line 562 sets `StatusCode = 200` instead of `204`.
**Fix Strategy:** **S3Server internal** - Change the literal `200` to `204` at line 562. S3Server sets status code AFTER the callback, so Less3 cannot work around this. One-line fix, no public API change.

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| HTTP status code | `200 OK` | `204 No Content` |

### 3.9 DeleteBucketTagging

Returns `204 No Content`. **Correct.** Matches AWS S3.

### 3.10 GetBucketVersioning

**Attribution:**
- Suspended for never-enabled: **Less3** - `BucketHandler.cs` `ReadVersioning` (lines 395-410) returns `Suspended` for all non-versioning-enabled buckets, not distinguishing "never enabled" from "explicitly suspended".
- MfaDelete always present: **Both** - S3Server `VersioningConfiguration` model has non-nullable `MfaDeleteStatusEnum` (always serializes); Less3 always sets it to `Disabled`.

**Fix Strategy:**
- Suspended for never-enabled: **S3Server additive + Less3** - `VersioningConfiguration.Status` is non-nullable `VersioningStatusEnum` defaulting to `Suspended`, so it always serializes. Fix: add `ShouldSerializeStatus()` method and a new `[XmlIgnore] bool IncludeStatus` flag to `VersioningConfiguration` (additive, no existing API breaks). Less3 sets `IncludeStatus = false` for never-enabled buckets.
- MfaDelete always present: **S3Server additive** - Same pattern: add `ShouldSerializeMfaDelete()` and `[XmlIgnore] bool IncludeMfaDelete` flag (additive). Less3 sets `IncludeMfaDelete = false` when MFA Delete was never configured.

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| Never-enabled buckets show `Suspended` | Returns `<Status>Suspended</Status>` for buckets where versioning was never enabled | Returns empty `<VersioningConfiguration/>` with NO `<Status>` element |
| `<MfaDelete>` always present | `<MfaDelete>Disabled</MfaDelete>` always included | Only included if MFA Delete was explicitly configured |

When versioning is enabled: `<Status>Enabled</Status>` is correctly returned.
When versioning is suspended (after being enabled): `<Status>Suspended</Status>` is correct.

### 3.11 PutBucketVersioning

Returns `200 OK`. **Correct.** Matches AWS S3.

### 3.12 GetBucketLocation

**Less3 response:**
```xml
<LocationConstraint xmlns:xsi="..." xmlns:xsd="...">us-west-1</LocationConstraint>
```

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| Namespace | xsi/xsd namespaces | `xmlns="http://s3.amazonaws.com/doc/2006-03-01/"` |

The element name and value are correct. For the `us-east-1` region, AWS S3 returns an empty `<LocationConstraint/>` but Less3 would return the value, which is a minor difference for non-us-east-1 regions.

### 3.13 ListMultipartUploads

**Less3 response:**
```xml
<ListMultipartUploadsResult>
  <Bucket>testbucket1</Bucket>
  <KeyMarker xsi:nil="true"/>
  <UploadIdMarker xsi:nil="true"/>
  <NextKeyMarker xsi:nil="true"/>
  <Prefix xsi:nil="true"/>
  <Delimiter xsi:nil="true"/>
  <NextUploadIdMarker xsi:nil="true"/>
  <MaxUploads>1000</MaxUploads>
  <IsTruncated>false</IsTruncated>
  <Upload>
    <ChecksumAlgorithm>CRC32</ChecksumAlgorithm>
    <Initiated>...</Initiated>
    <Key>multipart-test.bin</Key>
    ...
  </Upload>
  <CommonPrefixes></CommonPrefixes>
  <EncodingType xsi:nil="true"/>
</ListMultipartUploadsResult>
```

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| `xsi:nil` null elements | Multiple null elements with `xsi:nil="true"` | Omitted or empty strings |
| `<CommonPrefixes>` always present | Empty element present | Only present when delimiter produces common prefixes |
| `<ChecksumAlgorithm>` always `CRC32` | Present in every Upload even if no checksum was requested | Only present when a checksum algorithm was specified at initiation |
| Upload element order | `ChecksumAlgorithm` first | AWS order: `Key`, `UploadId`, `Initiator`, `Owner`, `StorageClass`, `Initiated` |
| Empty listing: null markers | All markers are `xsi:nil` | Should be empty strings or omitted |

### 3.14 DeleteBucket

Returns `204 No Content` for empty bucket. **Correct.** Matches AWS S3.

Non-empty bucket correctly returns `409` with `BucketNotEmpty`. **Correct.** (Error format issues per section 1.8 apply.)

---

## 4. Object APIs

### 4.1 PutObject

**Attribution:**
- Missing ETag header: **Both** - Less3 `ObjectHandler.Write` does not add ETag to response headers; S3Server `S3Server.cs` line 896 does not add ETag after the callback.
- User metadata not stored: **Less3** - `ObjectHandler` does not store or return `x-amz-meta-*` headers for regular PutObject.

**Fix Strategy:**
- Missing ETag: **Less3 only** - Add `ctx.Response.Headers.Add("ETag", etag)` in the `ObjectHandler.Write` callback. S3Server's `S3Response.Headers` is a public `NameValueCollection` that callbacks can freely modify.
- User metadata: **Less3 only** - Store `x-amz-meta-*` request headers in a new DB table or JSON column in `Obj`, and return them on GET/HEAD. Internal Less3 schema change.

| Issue | Less3 | AWS S3 Expected | Severity |
|-------|-------|-----------------|----------|
| **Missing `ETag` response header** | No ETag returned | Returns `ETag: "md5hash"` | **High** |
| Missing `x-amz-version-id` header | Not present (on versioned buckets) | Returns version ID when versioning enabled | Medium |
| User metadata not stored | `x-amz-meta-*` headers ignored | Stored and returned on GET/HEAD | **High** |
| HTTP status code | `200 OK` | `200 OK` | Correct |

### 4.2 GetObject

**Attribution:** **Less3** - `ObjectHandler.cs` line 197 passes `md.Obj.Etag` to `S3Object`, but `ObjectHandler.Write` (lines 416-441) reuses the `obj` record from the previous version without clearing `Etag`, and `BucketClient.cs` `AddObject` (line 143) only sets `Etag = Md5` when `Etag` is empty, causing new versions to inherit the old version's ETag.
**Fix Strategy:** **Less3 only** - Clear `obj.Etag` (set to `null` or `""`) before calling `BucketClient.AddObject` when creating a new version, so `AddObject` will correctly set `Etag = Md5` from the new content.

| Issue | Less3 | AWS S3 Expected | Severity |
|-------|-------|-----------------|----------|
| **Wrong ETag for versioned objects** | Returns version 1's ETag regardless of which version is served | Returns the ETag of the specific version being returned | **Critical** |
| Missing `x-amz-version-id` header | Not present | Present when bucket has/had versioning enabled | Medium |
| Missing user metadata headers | `x-amz-meta-*` not returned | Returns all user metadata set during PutObject | **High** |
| ETag case | Uppercase hex | Lowercase hex | Low |

**Specific evidence:** GetObject for `testobj.txt` version 2 returned content "Hello World Test Content Version 2" (34 bytes, correct) but ETag `"2BE093118606E0D4BB375C281E2C797A"` (which is version 1's ETag; version 2's ETag should be `"AA050B53738D196C71C8EF8D4958D899"`).

### 4.3 GetObject with Range

**Attribution:** **S3Server** - `S3Server.cs` line 819 sets `StatusCode = 200` for `ObjectReadRange` instead of `206`. Lines 815-825 do not add a `Content-Range` header.
**Fix Strategy:** **S3Server internal** - Change `200` to `206` at line 819. Add `Content-Range` header computation using the returned `S3Object.Size` and original range from `S3Request`. S3Server sets the status code AFTER the callback returns, so Less3 cannot work around this - it requires an S3Server fix. No public API changes; the `S3Object` return type and callback signature remain identical.

| Issue | Less3 | AWS S3 Expected | Severity |
|-------|-------|-----------------|----------|
| **HTTP status code** | `200 OK` | `206 Partial Content` | **Critical** |
| **Missing `Content-Range` header** | Not present | `Content-Range: bytes 0-9/20` (for a `Range: bytes=0-9` request on a 20-byte object) | **Critical** |
| Missing `Accept-Ranges` header | Not present in range response | `Accept-Ranges: bytes` | Low |
| Missing `ETag` header in range response | Not present | ETag of the full object | Medium |
| Missing `Last-Modified` header in range response | Not present | Last-Modified timestamp | Medium |

The actual content returned is correct (proper byte range), but the response headers are wrong. Many S3 clients rely on the `206` status code and `Content-Range` header to properly handle partial responses.

### 4.4 HeadObject

**Attribution:** **Both**
- **Less3** (`ObjectHandler.cs` line 177): `Exists` handler does not pass `ContentType` to the `ObjectMetadata` constructor.
- **S3Server** (`ObjectMetadata.cs` line 58): `ContentType` defaults to `"application/octet-stream"` when not provided.

**Fix Strategy:** **Less3 only** - Set `ContentType` on the returned `ObjectMetadata` object (e.g., `md.ContentType = obj.ContentType`). S3Server already reads `md.ContentType` and uses it to set the HTTP `Content-Type` header (line 738). The `ObjectMetadata.ContentType` property is public and settable. No S3Server changes needed.

| Issue | Less3 | AWS S3 Expected | Severity |
|-------|-------|-----------------|----------|
| **Content-Type always `application/octet-stream`** | Returns `application/octet-stream` regardless of stored type | Returns the Content-Type set during PutObject | **Critical** |
| Missing `x-amz-version-id` header | Not present | Present when bucket has/had versioning | Medium |
| Missing user metadata | `x-amz-meta-*` not returned | Returns all user metadata set during PutObject | **High** |

**Evidence:** Object uploaded with `Content-Type: text/plain`. GetObject returns `Content-Type: text/plain` (correct). HeadObject returns `Content-Type: application/octet-stream` (wrong).

### 4.5 DeleteObject

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| Missing `x-amz-delete-marker` header | Not present (on versioned buckets) | `x-amz-delete-marker: true` when deleting from versioned bucket |
| Missing `x-amz-version-id` header | Not present | Returns version ID of the delete marker created |

HTTP status `204 No Content` is correct.

### 4.6 DeleteObjects (Multi-Object Delete)

**Attribution:** **Less3** - `BucketClient` delete logic does not properly create delete markers when versioning is enabled; instead performs hard deletes and reports incorrect DeleteMarker/VersionId values.
**Fix Strategy:** **Less3 only** - Modify `BucketClient` delete logic to create delete marker records (new `Obj` with `DeleteMarker = true`) when versioning is enabled, instead of hard-deleting. Return the new delete marker's version ID in the response.

**Less3 response:**
```xml
<DeleteResult>
  <Deleted>
    <Key>todelete2.txt</Key>
    <VersionId>1</VersionId>
    <DeleteMarker>false</DeleteMarker>
    <DeleteMarkerVersionId xsi:nil="true"/>
  </Deleted>
</DeleteResult>
```

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| `<DeleteMarker>false</DeleteMarker>` on versioned bucket | Reports no delete marker was created | Should be `true` (a delete marker is created when deleting from a versioned bucket without specifying versionId) |
| `<VersionId>1</VersionId>` in response | Reports version 1 | Should report the version ID of the newly created delete marker |
| Object still appears in listings after delete | `todelete2.txt` visible in ListObjectsV2 | Should be hidden (only visible via ListObjectVersions) |

### 4.7 GetObjectAcl

**Attribution:** **S3Server** (Grantee model) + **Less3** (ACL population logic). Same structural issues as GetBucketAcl (section 3.5):
- Grantee type as child element instead of attribute
- Null elements with `xsi:nil`
- Empty ACL for objects that should have default owner FULL_CONTROL

**Fix Strategy:** Same as 3.5 (**S3Server internal** for `[XmlIgnore]` on `GranteeType`) + **Less3 only** for populating default owner FULL_CONTROL.

### 4.8 PutObjectAcl

Returns `200 OK`. **Correct** status code.

### 4.9 GetObjectTagging

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| Missing `x-amz-version-id` header | Not present | Present when bucket has/had versioning |

When tags are set, the TagSet/Tag/Key/Value structure is correct. When no tags are set, returns 200 with empty TagSet, which is correct for objects (unlike bucket tagging).

### 4.10 PutObjectTagging

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| Missing `x-amz-version-id` header | Not present | Present when bucket has/had versioning |

HTTP status `200 OK` is correct.

### 4.11 DeleteObjectTagging

Returns `204 No Content`. **Correct.** Missing `x-amz-version-id` header (same as other object operations).

---

## 5. Multipart Upload APIs

### 5.1 CreateMultipartUpload (InitiateMultipartUpload)

**Attribution:** **Less3** - Uses `Guid.NewGuid()` for UploadId format.
**Fix Strategy:** **Less3 only** - Cosmetic. Could change to a base64-encoded GUID or random bytes if desired, but GUID format is valid and not breaking.

**Less3 response:**
```xml
<InitiateMultipartUploadResult>
  <Bucket>testbucket1</Bucket>
  <Key>multipart-test.bin</Key>
  <UploadId>a76504ff-a617-43fb-b0c9-a3ae72fb9890</UploadId>
</InitiateMultipartUploadResult>
```

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| UploadId format | UUID/GUID format (e.g., `a76504ff-a617-43fb-b0c9-a3ae72fb9890`) | Opaque base64-like string. Not a breaking difference, but noticeable. |
| Namespace | xsi/xsd | S3 namespace |

Structure is otherwise correct.

### 5.2 UploadPart

Returns `200 OK` with `ETag` header. **Largely correct.** The ETag is in lowercase with quotes, matching AWS S3 format.

### 5.3 CompleteMultipartUpload

**Attribution:**
- ETag format: **Less3** - `ObjectHandler.CompleteMultipartUpload` does not compute the multipart ETag with `-N` suffix (where N is the number of parts).
- Location includes uploadId: **Less3** - Sets Location URL with query parameters.

**Fix Strategy:** **Less3 only** - Compute multipart ETag correctly: MD5 of concatenated part MD5 hashes, formatted as `"hex-N"`. Fix Location URL to omit query parameters.

**Less3 response:**
```xml
<CompleteMultipartUploadResult>
  <Location>http://localhost:8000/testbucket1/multipart-test.bin?uploadId=...</Location>
  <Bucket>testbucket1</Bucket>
  <Key>multipart-test.bin</Key>
  <ChecksumCRC32 xsi:nil="true"/>
  <ChecksumCRC32C xsi:nil="true"/>
  <ChecksumSHA1 xsi:nil="true"/>
  <ChecksumSHA256 xsi:nil="true"/>
  <ETag>"423CBA5C8A781BAF67DAEB88CE59AF1A"</ETag>
</CompleteMultipartUploadResult>
```

| Issue | Less3 | AWS S3 Expected | Severity |
|-------|-------|-----------------|----------|
| **`<Location>` includes uploadId query param** | `http://...?uploadId=...` | `http://bucket.s3.region.amazonaws.com/key` (no query params) | Medium |
| **ETag format** | Plain MD5 hash: `"423CBA5C8A781BAF67DAEB88CE59AF1A"` | Multipart ETag format: `"hash-N"` where N is the number of parts (e.g., `"abc123-2"` for 2 parts) | **High** |
| Null checksum elements | Four `xsi:nil` checksum elements present | Omitted when no checksum algorithm was used | Low |
| ETag case | Uppercase | Lowercase | Low |

### 5.4 AbortMultipartUpload

Returns `204 No Content`. **Correct.** Matches AWS S3.

### 5.5 ListParts

**Attribution:** **S3Server** - Model class property ordering determines XML element order. `Initiator` and `Owner` metadata placed after `<Part>` elements in the serialized output.
**Fix Strategy:** **S3Server internal** - Reorder property declarations in the model class so metadata fields come before the `Part` list. No public API change; the properties and their types remain identical.

**Less3 response (abbreviated):**
```xml
<ListPartsResult>
  <Bucket>testbucket1</Bucket>
  <Key>multipart-test.bin</Key>
  <UploadId>...</UploadId>
  <PartNumberMarker>0</PartNumberMarker>
  <NextPartNumberMarker>0</NextPartNumberMarker>
  <MaxParts>1000</MaxParts>
  <IsTruncated>false</IsTruncated>
  <Part>
    <ChecksumCRC32 xsi:nil="true"/>
    <ChecksumCRC32C xsi:nil="true"/>
    <ChecksumSHA1 xsi:nil="true"/>
    <ChecksumSHA256 xsi:nil="true"/>
    <ETag>"..."</ETag>
    <LastModified>...</LastModified>
    <PartNumber>1</PartNumber>
    <Size>27</Size>
  </Part>
  ...
  <Initiator>
    <ID xsi:nil="true"/>
    <DisplayName xsi:nil="true"/>
  </Initiator>
  <Owner>...</Owner>
  <StorageClass>STANDARD</StorageClass>
  <ChecksumAlgorithm>CRC32</ChecksumAlgorithm>
</ListPartsResult>
```

| Issue | Less3 | AWS S3 Expected |
|-------|-------|-----------------|
| `<NextPartNumberMarker>` is `0` | Always `0` | Should be the highest part number listed (e.g., `2` when 2 parts are shown) |
| `<Initiator>` has null ID/DisplayName | Both are `xsi:nil` | Should be populated with the user who initiated the upload |
| Null checksum elements in each `<Part>` | Four null checksum elements per part | Omitted when no checksum was used |
| `<ChecksumAlgorithm>` always present | `CRC32` | Only present when a checksum algorithm was specified |
| Element ordering | `Initiator`, `Owner`, `StorageClass` come AFTER `<Part>` elements | In AWS, these metadata elements come BEFORE the `<Part>` elements |

---

## 6. Summary of Severity

### Critical (Breaks S3 client compatibility)

| # | Gap | Attribution | Fix Strategy |
|---|-----|-------------|--------------|
| 1 | **ListObjectsV2 returns all versions instead of latest only** - Causes duplicate entries and incorrect object counts | **Less3** - `BucketClient.cs` `Enumerate` | **Less3 only** |
| 2 | **ListObjectsV2 shows deleted objects** - Objects removed via DeleteObject still appear in listings | **Less3** - `BucketClient.cs` `Enumerate` | **Less3 only** |
| 3 | **GetObject returns wrong ETag for versioned objects** - Always returns version 1's ETag regardless of version served | **Less3** - `ObjectHandler.cs` `Write` + `BucketClient.cs` `AddObject` | **Less3 only** |
| 4 | **Range reads return HTTP 200 instead of 206** - Missing `Content-Range` header; breaks partial download clients | **S3Server** - `S3Server.cs` line 819 | **S3Server internal** (status set after callback; Less3 cannot work around) |
| 5 | **HeadObject always returns `Content-Type: application/octet-stream`** - Ignores the actual stored content type | **Both** - Less3 `ObjectHandler.Exists` + S3Server `ObjectMetadata` default | **Less3 only** (set `ContentType` on returned `ObjectMetadata`) |

### High (Significant functional gaps)

| # | Gap | Attribution | Fix Strategy |
|---|-----|-------------|--------------|
| 6 | **PutObject does not return ETag header** - Many S3 clients use the ETag from PutObject for verification | **Both** - Less3 `ObjectHandler.Write` + S3Server `S3Server.cs` line 896 | **Less3 only** (add header via `ctx.Response.Headers`) |
| 7 | **User metadata (`x-amz-meta-*`) not stored/returned** - Metadata sent with PutObject is silently lost | **Less3** - `ObjectHandler` | **Less3 only** (DB schema + handler changes) |
| 8 | **CompleteMultipartUpload ETag format wrong** - Missing `-N` part count suffix that identifies multipart objects | **Less3** - `ObjectHandler.CompleteMultipartUpload` | **Less3 only** |
| 9 | **Continuation token not honored in ListObjectsV2** - Pagination does not work | **Less3** - `BucketClient.cs` `Enumerate` | **Less3 only** |
| 10 | **Versioned delete behavior incorrect** - DeleteObject/DeleteObjects do not properly create delete markers | **Less3** - `BucketClient` delete logic | **Less3 only** |

### Medium (Noticeable differences)

| # | Gap | Attribution | Fix Strategy |
|---|-----|-------------|--------------|
| 11 | Missing `x-amz-version-id` header on all object operations | **Both** - Less3 handlers + S3Server response handling | **Less3 only** (add header via `ctx.Response.Headers`) |
| 12 | GetBucketVersioning returns `Suspended` for buckets where versioning was never enabled | **Less3** - `BucketHandler.ReadVersioning` | **S3Server additive** (add `ShouldSerializeStatus()` + `IncludeStatus` flag) + **Less3** |
| 13 | GetBucketTagging returns 200 with empty TagSet instead of 404 `NoSuchTagSetError` | **Less3** - `BucketHandler.ReadTags` | **Less3 only** |
| 14 | PutBucketTagging returns 200 instead of 204 | **S3Server** - `S3Server.cs` line 562 | **S3Server internal** (status set after callback; Less3 cannot work around) |
| 15 | CreateBucket returns 409 for same-owner duplicate instead of 200 | **Less3** - `BucketHandler.Write` | **Less3 only** |
| 16 | CreateBucket missing `Location` response header | **S3Server** - `S3Server.cs` line 490 | **Less3 only** (add header via `ctx.Response.Headers` in callback) |
| 17 | ACL grantee type stored as child element instead of `xsi:type` attribute | **S3Server** - `Grantee.cs` model | **S3Server internal** (add `[XmlIgnore]` to `GranteeType`; `[XmlInclude]` already present) + **Less3** (use subclasses) |
| 18 | Canned ACL `public-read` replaces owner grant instead of adding to it | **Less3** - `AclConverter.cs` `GrantsFromHeaders` | **Less3 only** |
| 19 | Owner always included in ListObjectsV2 (should require `fetch-owner=true`) | **Both** - S3Server `ObjectMetadata` + Less3 population logic | **Less3 only** (set `Owner = null` when not requested) or **S3Server additive** (add `ShouldSerializeOwner()`) |
| 20 | `<ContentType>` non-standard element in ListObjectsV2 | **S3Server** - `ObjectMetadata.cs` (no `[XmlIgnore]`) | **S3Server internal** (add `[XmlIgnore]` to `ContentType`; property remains usable in C# for HTTP headers) |

### Low (Cosmetic / unlikely to cause issues)

| # | Gap | Attribution | Fix Strategy |
|---|-----|-------------|--------------|
| 21 | XML namespace declaration (xsi/xsd vs S3 namespace) | **S3Server** - all model classes, `SerializationHelper.cs` | **S3Server internal** (uncomment namespace on `[XmlRoot]`) |
| 22 | `xsi:nil="true"` on null elements | **S3Server** - `[XmlElement(IsNullable = true)]` on model properties | **S3Server internal** (change `IsNullable` to `false`) |
| 23 | ETag uppercase vs lowercase | **Both** - Less3 stores uppercase; S3Server doesn't normalize | **Less3 only** (lowercase before storing) |
| 24 | Server header value | **S3Server** - `S3Response.cs` `SetResponseHeaders` | **S3Server internal** (change string literal) |
| 25 | Response headers echoing request headers | **Both** - Less3 `Program.cs` lines 440-442 + S3Server `SetResponseHeaders` | **Both internal** (remove header additions) |
| 26 | `<Delimiter>` and `<EncodingType>` always present in listings | **S3Server** - `ListBucketResult.cs` defaults | **S3Server internal + additive** (change defaults, add `ShouldSerialize*()`) |
| 27 | Error response format differences (missing Message, extra elements) | **S3Server** - `Error.cs` (`Message` has no setter) | **S3Server additive** (add setter to `Message`, add `HostId` property) |
| 28 | UploadId format (GUID vs opaque string) | **Less3** - `Guid.NewGuid()` | **Less3 only** (cosmetic) |
| 29 | `<MfaDelete>Disabled</MfaDelete>` always present in versioning config | **Both** - S3Server model + Less3 always sets it | **S3Server additive** (add `ShouldSerializeMfaDelete()` + flag) |
| 30 | ListParts/ListMultipartUploads element ordering | **S3Server** - model class property ordering | **S3Server internal** (reorder property declarations) |

---

## 7. Fix Strategy Summary

**All 30 gaps can be fixed without breaking the S3Server public C# API.** No existing public types, methods, properties, or constructor signatures need to be removed or changed.

### Changes by category

#### Less3 Only (18 gaps - no S3Server changes required)

These can be fixed entirely within Less3 application code:

| # | Gap | Complexity |
|---|-----|-----------|
| 1 | ListObjectsV2 returns all versions | Moderate - add filtering to `BucketClient.Enumerate` |
| 2 | ListObjectsV2 shows deleted objects | Moderate - filter delete markers in `Enumerate` |
| 3 | Wrong ETag for versioned objects | Simple - clear `Etag` before `AddObject` |
| 5 | HeadObject Content-Type always octet-stream | Simple - set `ContentType` on returned `ObjectMetadata` |
| 6 | PutObject missing ETag header | Simple - add header via `ctx.Response.Headers` |
| 7 | User metadata not stored/returned | Moderate - DB schema change + handler changes |
| 8 | CompleteMultipartUpload ETag format | Moderate - compute multipart ETag with `-N` suffix |
| 9 | Continuation token not honored | Moderate - fix pagination in `Enumerate` |
| 10 | Versioned delete behavior | Moderate - implement delete marker creation |
| 11 | Missing x-amz-version-id | Simple - add header via `ctx.Response.Headers` |
| 13 | GetBucketTagging 200 vs 404 | Simple - throw error when no tags |
| 15 | CreateBucket 409 for same owner | Simple - return success for same-owner dup |
| 16 | CreateBucket missing Location header | Simple - add header via `ctx.Response.Headers` |
| 18 | Canned ACL replaces owner | Simple - add owner FULL_CONTROL to canned ACL |
| 19 | Owner always in ListObjectsV2 | Simple - set Owner to null when not requested |
| 23 | ETag uppercase | Simple - `.ToLowerInvariant()` before storing |
| 25 | Echoed request headers (Less3 portion) | Simple - remove 3 lines in `Program.cs` |
| 28 | UploadId format | Simple - cosmetic format change |

#### S3Server Internal (no public API change, 10 gaps)

These require S3Server changes but only modify internal behavior, default values, or XML serialization attributes. All existing consumers recompile without changes:

| # | Gap | Change needed |
|---|-----|--------------|
| 4 | Range reads return 200 not 206 | Change `200` → `206` at line 819; add `Content-Range` header |
| 14 | PutBucketTagging returns 200 not 204 | Change `200` → `204` at line 562 |
| 17 | ACL grantee `<Type>` element | Add `[XmlIgnore]` to `GranteeType` property |
| 20 | ContentType in ListObjectsV2 XML | Add `[XmlIgnore]` to `ObjectMetadata.ContentType` |
| 21 | XML namespace wrong | Uncomment namespace on `[XmlRoot]` attributes |
| 22 | `xsi:nil` on null elements | Change `IsNullable = true` → `false` on `[XmlElement]` |
| 24 | Server header value | Change string literal in `SetResponseHeaders` |
| 25 | Echoed headers (S3Server portion) | Remove `Host`/`X-Amz-Date` from `SetResponseHeaders` |
| 30 | Element ordering in XML | Reorder property declarations in model classes |

**Note on gaps #4 and #14:** S3Server sets the HTTP status code AFTER the callback returns (`s3ctx.Response.StatusCode = 200`), so Less3 cannot work around these. They require S3Server-side fixes.

#### S3Server Additive (new public members, 5 gaps)

These require adding new public members to S3Server model classes. No existing members are removed or changed:

| # | Gap | New member(s) needed |
|---|-----|---------------------|
| 12 | Versioning shows Suspended for never-enabled | Add `ShouldSerializeStatus()` + `[XmlIgnore] bool IncludeStatus` to `VersioningConfiguration` |
| 26 | Delimiter/EncodingType always present | Add `ShouldSerializeDelimiter()` and `ShouldSerializeEncodingType()` to `ListBucketResult` (class already uses this pattern for `Marker`, `NextContinuationToken`, `CommonPrefixes`) |
| 27 | Error Message not serialized | Add setter to `Error.Message` property (with backing field; getter falls back to switch). Add `HostId` property. |
| 29 | MfaDelete always present | Add `ShouldSerializeMfaDelete()` + `[XmlIgnore] bool IncludeMfaDelete` to `VersioningConfiguration` |

Note: Gap #26's `Delimiter` setter currently forces null to `"/"`. This behavioral guard would also need to be relaxed (allow null). While this changes the setter's behavior, the property type, name, and accessibility remain identical - it is not a signature-level API break, but callers that relied on the null-to-"/" coercion would see different behavior.

### Gaps that CANNOT be worked around in Less3

Two gaps **require** S3Server changes because S3Server sets the HTTP status code after the callback returns, overwriting any value set by Less3:

- **Gap #4** (Range reads 200 → 206) - `S3Server.cs` line 819
- **Gap #14** (PutBucketTagging 200 → 204) - `S3Server.cs` line 562

All other S3Server-attributed gaps can either be worked around in Less3 (by setting headers in callbacks, using different model subclasses, etc.) or are cosmetic XML differences that require S3Server attribute/default changes.
