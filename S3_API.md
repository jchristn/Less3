# Less3 S3 API

Less3 v3.0.0 keeps S3 compatibility as the data-plane API. Requests authenticate with S3 access keys, and the access key resolves the tenant. Access keys are globally unique, so S3 clients do not send a tenant header.

## Endpoint

The S3 endpoint is the Less3 server root:

```text
http://localhost:8000
```

The Docker default exposes the same endpoint on port `8000`.

## Default Development Credential

Fresh v3 deployments seed a default tenant and credential for local use:

```text
Tenant ID: default
Access Key: default
Secret Key: default
```

Change or remove this credential before exposing a node outside a trusted development environment.

## Tenant Resolution

S3 requests are tenant-scoped by credential lookup:

1. Less3 extracts the access key from the S3 authorization material.
2. Less3 loads the credential by globally unique access key.
3. The credential identifies its tenant and owning user.
4. The tenant, user, and credential must all be active.
5. Bucket, object, tag, ACL, multipart, and version operations execute only inside that tenant.

Bucket names are unique per tenant. Two tenants may each own a bucket named `photos`, but one tenant cannot create two buckets with the same name.

## Wire Format Conventions

S3 request and response bodies use the AWS S3 protocol shapes: XML for control-plane structures and raw bytes for object and part payloads. Empty successful responses have no body. Error responses are XML:

```xml
<Error>
  <Code>NoSuchKey</Code>
  <Message>The specified key does not exist.</Message>
</Error>
```

Common response headers include:

| Header | Meaning |
| --- | --- |
| `ETag` | MD5-style object or part entity tag, quoted |
| `Content-Length` | Object byte length |
| `Content-Type` | Stored object content type |
| `x-amz-version-id` | Object version when bucket versioning is enabled |
| `x-amz-request-id` | Server request identifier, when emitted by the S3 stack |
| `x-amz-bucket-region` | Bucket region on bucket-exists/head responses |

## Supported Operation Families

Less3 v3.0.0 documents and tests these S3 operation families:

- Service operations: list buckets.
- Bucket operations: create, delete, exists/head, list objects, list versions, read/write versioning, read/write/delete tags, read/write ACLs, and location.
- Object operations: put, get, head, ranged get (including suffix ranges), conditional get/head, copy, delete, delete many, read/write/delete tags, and read/write ACLs.
- Multipart operations: create upload, upload part, upload part copy, complete upload, abort upload, list uploads, and list upload parts.
- Versioning operations: enable and suspend versioning, retrieve specific versions (including the `null` version), permanently delete versions, list versions, and delete markers.

Less3-owned identifiers use PrettyId string IDs internally. S3 protocol fields that require bucket names, object keys, ETags, upload IDs, and version IDs keep their S3 meanings.

## Service Operations

### List Buckets

```text
GET /
```

Request body: none.

Response body:

```xml
<ListAllMyBucketsResult>
  <Owner>
    <ID>default</ID>
    <DisplayName>Default</DisplayName>
  </Owner>
  <Buckets>
    <Bucket>
      <Name>photos</Name>
      <CreationDate>2026-01-01T00:00:00.000Z</CreationDate>
    </Bucket>
  </Buckets>
</ListAllMyBucketsResult>
```

`Owner.ID` is the resolved tenant ID. The bucket list contains buckets visible to the authenticated credential within that tenant/account scope, matching AWS S3 account-scoped `ListBuckets` behavior.

## Bucket Operations

### Create Bucket

```text
PUT /{bucket}
```

Request body: none. Less3 uses the configured server region and ignores location configuration XML for bucket creation.

Optional ACL headers follow S3 conventions:

```text
x-amz-acl: private | public-read | public-read-write | authenticated-read
x-amz-grant-read: id="{user-id}",uri="http://acs.amazonaws.com/groups/global/AllUsers"
x-amz-grant-write: id="{user-id}"
x-amz-grant-read-acp: id="{user-id}"
x-amz-grant-write-acp: id="{user-id}"
x-amz-grant-full-control: id="{user-id}"
```

Response body: empty. Success status is `200` and the response includes:

```text
Location: /{bucket}
```

Bucket names must be valid S3-style names and must not collide with reserved Less3 route names such as `api`, `admin`, or `openapi.json`.

### Head Bucket

```text
HEAD /{bucket}
```

Request body: none.

Response body: empty. Success status is `200`; missing buckets return `404`.

### Delete Bucket

```text
DELETE /{bucket}
```

Request body: none.

Response body: empty. Success status is `204`. Non-empty buckets return `409 BucketNotEmpty`.

### List Objects

```text
GET /{bucket}?list-type=2&prefix={prefix}&delimiter=/&max-keys=100&continuation-token={token}&fetch-owner=true
GET /{bucket}?prefix={prefix}&delimiter=/&marker={key}&max-keys=100
```

Request body: none.

Response body:

```xml
<ListBucketResult>
  <Name>photos</Name>
  <Prefix>albums/</Prefix>
  <Marker>albums/2026/cover.jpg</Marker>
  <MaxKeys>100</MaxKeys>
  <Delimiter>/</Delimiter>
  <IsTruncated>false</IsTruncated>
  <NextContinuationToken>bGVzczM6YWxidW1zLzIwMjYvY292ZXIuanBn</NextContinuationToken>
  <KeyCount>1</KeyCount>
  <Contents>
    <Key>albums/2026/cover.jpg</Key>
    <LastModified>2026-01-01T00:00:00.000Z</LastModified>
    <ETag>"9a0364b9e99bb480dd25e1f0284c8555"</ETag>
    <Size>12345</Size>
    <StorageClass>STANDARD</StorageClass>
    <ContentType>image/jpeg</ContentType>
    <Owner>
      <ID>usr_default_admin</ID>
      <DisplayName>Admin</DisplayName>
    </Owner>
  </Contents>
  <CommonPrefixes>
    <Prefix>albums/2026/</Prefix>
  </CommonPrefixes>
</ListBucketResult>
```

Keys are returned in ascending UTF-8 byte order across all pages, as in Amazon S3. `prefix` is matched exactly and
case-sensitively. With a `delimiter`, keys are rolled up into `CommonPrefixes`, which count toward `max-keys` and
`KeyCount`. `max-keys` is capped at 1000; a negative or non-numeric value returns `400 InvalidArgument`.

- ListObjectsV2 (`list-type=2`) pages with the opaque `continuation-token` from `NextContinuationToken` (echoed back as
  `ContinuationToken`) and honors `start-after` (echoed as `StartAfter`). An invalid token returns `400 InvalidArgument`.
- ListObjects v1 pages with `marker`, which is a key: the next page starts after it. Truncated responses that use a
  delimiter include `NextMarker`.
- `encoding-type=url` encodes keys, prefixes, delimiters, and markers as Amazon S3 does (a space becomes `+`, `/` is
  kept, and other reserved bytes become `%XX`), so clients decode them with form decoding. It applies to
  ListObjectVersions and ListMultipartUploads as well.
- v1 responses always include `Marker` and never `KeyCount`; v2 responses include `KeyCount` and never `Marker`.
- `Owner` is included in v1 responses, and in v2 responses only when `fetch-owner=true` is supplied.

List Versions pages with `key-marker` and `version-id-marker` (returned as `NextKeyMarker` and `NextVersionIdMarker`),
returns versions and delete markers interleaved in key order with the newest version first, flags exactly one
`IsLatest` entry per key, and reports `null` as the version ID of versions written while versioning was not enabled.

### Get Bucket Location

```text
GET /{bucket}?location
```

Request body: none.

Response body:

```xml
<LocationConstraint>us-west-1</LocationConstraint>
```

### Get Bucket Tagging

```text
GET /{bucket}?tagging
```

Request body: none.

Response body:

```xml
<Tagging>
  <TagSet>
    <Tag>
      <Key>Environment</Key>
      <Value>Production</Value>
    </Tag>
  </TagSet>
</Tagging>
```

### Put Bucket Tagging

```text
PUT /{bucket}?tagging
```

Request body:

```xml
<Tagging>
  <TagSet>
    <Tag>
      <Key>Environment</Key>
      <Value>Production</Value>
    </Tag>
    <Tag>
      <Key>Component</Key>
      <Value>Less3</Value>
    </Tag>
  </TagSet>
</Tagging>
```

Response body: empty. Success status is `200`.

### Delete Bucket Tagging

```text
DELETE /{bucket}?tagging
```

Request body: none.

Response body: empty. Success status is `204`.

### Get Bucket ACL

```text
GET /{bucket}?acl
```

Request body: none.

Response body:

```xml
<AccessControlPolicy>
  <Owner>
    <ID>usr_default_admin</ID>
    <DisplayName>Admin</DisplayName>
  </Owner>
  <AccessControlList>
    <Grant>
      <Grantee>
        <ID>usr_default_admin</ID>
        <DisplayName>Admin</DisplayName>
      </Grantee>
      <Permission>FULL_CONTROL</Permission>
    </Grant>
  </AccessControlList>
</AccessControlPolicy>
```

### Put Bucket ACL

```text
PUT /{bucket}?acl
```

Request body:

```xml
<AccessControlPolicy>
  <Owner>
    <ID>usr_default_admin</ID>
    <DisplayName>Admin</DisplayName>
  </Owner>
  <AccessControlList>
    <Grant>
      <Grantee>
        <ID>usr_reader</ID>
        <DisplayName>Reader</DisplayName>
      </Grantee>
      <Permission>READ</Permission>
    </Grant>
  </AccessControlList>
</AccessControlPolicy>
```

Response body: empty. Success status is `200`. Canned ACL and grant headers are also accepted for bucket creation and ACL writes. A body that is not a valid ACL returns `400 MalformedACLError`.

### Get Bucket Versioning

```text
GET /{bucket}?versioning
```

Request body: none.

Response body when enabled:

```xml
<VersioningConfiguration>
  <Status>Enabled</Status>
</VersioningConfiguration>
```

Response body when disabled may omit `Status`.

### Put Bucket Versioning

```text
PUT /{bucket}?versioning
```

Request body:

```xml
<VersioningConfiguration>
  <Status>Enabled</Status>
</VersioningConfiguration>
```

`Status` must be `Enabled` or `Suspended`; any other value returns `400 MalformedXML`. As in Amazon S3, a bucket
that has had versioning enabled never returns to the unversioned state: suspending keeps every existing version, and
new writes (and deletes) while suspended replace the key's single `null` version. `GET ?versioning` returns
`Suspended` for such a bucket and no `Status` for a bucket that has never had versioning enabled.

Response body: empty. Success status is `200`.

### List Versions

```text
GET /{bucket}?versions&prefix={prefix}&delimiter=/&max-keys=100&key-marker={key}
```

Request body: none.

Response body:

```xml
<ListVersionsResult>
  <Name>photos</Name>
  <Prefix>albums/</Prefix>
  <KeyMarker>albums/2026/cover.jpg</KeyMarker>
  <MaxKeys>100</MaxKeys>
  <IsTruncated>false</IsTruncated>
  <Version>
    <Key>albums/2026/cover.jpg</Key>
    <VersionId>2</VersionId>
    <IsLatest>true</IsLatest>
    <LastModified>2026-01-01T00:00:00.000Z</LastModified>
    <ETag>"9a0364b9e99bb480dd25e1f0284c8555"</ETag>
    <Size>12345</Size>
    <StorageClass>STANDARD</StorageClass>
    <Owner>
      <ID>usr_default_admin</ID>
      <DisplayName>Admin</DisplayName>
    </Owner>
  </Version>
  <DeleteMarker>
    <Key>albums/2026/deleted.jpg</Key>
    <VersionId>3</VersionId>
    <IsLatest>true</IsLatest>
    <LastModified>2026-01-01T00:00:00.000Z</LastModified>
    <Owner>
      <ID>usr_default_admin</ID>
      <DisplayName>Admin</DisplayName>
    </Owner>
  </DeleteMarker>
</ListVersionsResult>
```

## Object Operations

### Put Object

```text
PUT /{bucket}/{key}
```

Request body: raw object bytes.

Useful request headers:

```text
Content-Type: text/plain
x-amz-meta-{name}: {value}
x-amz-acl: private | public-read | public-read-write | authenticated-read
x-amz-grant-read: id="{user-id}"
x-amz-grant-full-control: id="{user-id}"
```

Response body: empty. Success status is `200`; response headers include `ETag` and, when bucket versioning is enabled
or suspended, `x-amz-version-id`.

- A `Content-MD5` that does not match the body returns `400 BadDigest`; a malformed one returns `400 InvalidDigest`.
  No object is written.
- `If-None-Match: *` writes only when the key does not exist, and `If-Match` only when the current ETag matches;
  otherwise `412 PreconditionFailed` (`404 NoSuchKey` for `If-Match` on a missing key). The check and the write are
  atomic under the key's write lock.
- `Cache-Control`, `Content-Disposition`, `Content-Encoding`, `Content-Language`, and `Expires` are stored and
  returned on `GET`/`HEAD`. User metadata keys are stored in lowercase.
- `x-amz-tagging: key1=value1&key2=value2` tags the new object.
- `x-amz-acl` also accepts `bucket-owner-read` and `bucket-owner-full-control`; an unknown canned ACL returns `400`.
  `x-amz-grant-*` headers accept `id="..."`, `emailAddress="..."`, and `uri="..."` entries separated by commas.
- An object written without `Content-Type` is served as `binary/octet-stream`.
- A body shorter than its declared length returns `400 IncompleteBody`.

### Copy Object

```text
PUT /{bucket}/{key}
x-amz-copy-source: /{source-bucket}/{source-key}[?versionId={version-id}]
```

Request body: none. The caller must be allowed to read the source (as for `GET`) and write the destination.

Optional headers: `x-amz-metadata-directive: COPY | REPLACE`, `x-amz-tagging-directive: COPY | REPLACE` (with
`x-amz-tagging`), `x-amz-copy-source-if-match`, `x-amz-copy-source-if-none-match`,
`x-amz-copy-source-if-modified-since`, `x-amz-copy-source-if-unmodified-since`, and the ACL headers of `PUT Object`.

Response body:

```xml
<CopyObjectResult>
  <LastModified>2026-01-01T00:00:00.000Z</LastModified>
  <ETag>"9a0364b9e99bb480dd25e1f0284c8555"</ETag>
</CopyObjectResult>
```

Response headers include `x-amz-version-id` for a versioned destination and `x-amz-copy-source-version-id` for a
versioned source. Copying an object onto itself requires `x-amz-metadata-directive: REPLACE` in an unversioned
bucket (`400 InvalidRequest` otherwise). A failed copy-source condition returns `412 PreconditionFailed`. Copying a
key whose latest version is a delete marker returns `404`. `UploadPart` with `x-amz-copy-source` (and optionally
`x-amz-copy-source-range: bytes={first}-{last}`) copies into a multipart upload and returns a `CopyPartResult`.

### Head Object

```text
HEAD /{bucket}/{key}
HEAD /{bucket}/{key}?versionId={version-id}
```

Request body: none.

Response body: empty. Errors to `HEAD` have no body either.

Response headers:

```text
Content-Length: 12345
Content-Type: text/plain
ETag: "9a0364b9e99bb480dd25e1f0284c8555"
x-amz-version-id: 2
x-amz-meta-color: blue
```

`HEAD` honors `Range` as `GET` does: `206` with `Content-Range` and the length of the range, or `416` when the range
cannot be satisfied.

### Get Object

```text
GET /{bucket}/{key}
GET /{bucket}/{key}?versionId={version-id}
```

Request body: none.

Response body: raw object bytes. Response headers match `HEAD Object`.

Ranged reads use the standard `Range` header:

```text
Range: bytes=0-1023
```

Suffix ranges (`Range: bytes=-500`) and open-ended ranges (`Range: bytes=1024-`) are supported. Successful ranged
responses are `206` and include:

```text
Content-Range: bytes 0-1023/12345
Accept-Ranges: bytes
```

A range whose last byte is past the end of the object is clamped to the last byte, and `Content-Range` describes the
bytes returned (`bytes=5-100` on a 10-byte object is `bytes 5-9/10`). A suffix range larger than the object returns the
whole object. A range whose first byte is at or past the end of the object returns `416 InvalidRange` with
`RangeRequested` and `ActualObjectSize` in the error body. On an empty object a range is `416`, but a suffix range
returns `200` with an empty body, as in Amazon S3. A `Range` header Less3 cannot use (another unit, several ranges, or
unparseable bounds) is ignored and the whole object is returned.

Conditional requests: `If-Match` and `If-Unmodified-Since` return `412 PreconditionFailed` when they fail;
`If-None-Match` and `If-Modified-Since` return `304 Not Modified`. The same applies to `HEAD`.

Response overrides: the `response-content-type`, `response-content-disposition`, `response-content-encoding`,
`response-content-language`, `response-cache-control`, and `response-expires` query parameters override the
corresponding response headers.

Versions: when the latest version is a delete marker, `GET`/`HEAD` return `404 NoSuchKey` with
`x-amz-delete-marker: true`; naming a delete marker's version ID returns `405 MethodNotAllowed`. A version ID that
does not exist returns `404 NoSuchVersion`, and a malformed one `400 InvalidArgument`. `versionId=null` selects the
version written while versioning was not enabled.

### Delete Object

```text
DELETE /{bucket}/{key}
DELETE /{bucket}/{key}?versionId={version-id}
```

Request body: none.

Response body: empty. Success status is `204`.

- Unversioned bucket: the object is removed; deleting a missing key succeeds.
- Versioning enabled: without `versionId` a new delete marker is added on top of the latest version (older versions
  remain readable by version ID), and the response carries `x-amz-delete-marker: true` and the marker's
  `x-amz-version-id`. This happens even when the key does not exist.
- Versioning suspended: without `versionId` the key's `null` version is replaced by a `null` delete marker.
- With `versionId`: that version is permanently removed, together with its ACL, tags, and data. Removing a delete
  marker's version makes the previous version current again. A version that does not exist succeeds; a malformed
  version ID returns `400 InvalidArgument`.

### Delete Multiple Objects

```text
POST /{bucket}?delete
```

Request body:

```xml
<Delete>
  <Quiet>false</Quiet>
  <Object>
    <Key>multi-1.txt</Key>
  </Object>
  <Object>
    <Key>multi-2.txt</Key>
    <VersionId>2</VersionId>
  </Object>
</Delete>
```

Response body:

```xml
<DeleteResult>
  <Deleted>
    <Key>multi-1.txt</Key>
  </Deleted>
  <Deleted>
    <Key>multi-2.txt</Key>
    <VersionId>2</VersionId>
  </Deleted>
  <Error>
    <Key>protected.txt</Key>
    <Code>AccessDenied</Code>
    <Message>Access denied.</Message>
  </Error>
</DeleteResult>
```

Each key follows the `DELETE Object` semantics above and is authorized individually, exactly as a `DELETE Object` on
that key would be; one key failing never stops the others. A key or version that does not exist is reported as
`Deleted`, not as an error. When a delete marker is created the entry includes `<DeleteMarker>true</DeleteMarker>` and
`<DeleteMarkerVersionId>`. `VersionId` appears only when the request named one. With `<Quiet>true</Quiet>` only
`Error` entries are returned. A request must name between 1 and 1,000 keys (`400 MalformedXML` otherwise), and a
`Content-MD5` that does not match the body returns `400 BadDigest`.

### Get Object Tagging

```text
GET /{bucket}/{key}?tagging
GET /{bucket}/{key}?tagging&versionId={version-id}
```

Request body: none.

Response body:

```xml
<Tagging>
  <TagSet>
    <Tag>
      <Key>Type</Key>
      <Value>Image</Value>
    </Tag>
  </TagSet>
</Tagging>
```

### Put Object Tagging

```text
PUT /{bucket}/{key}?tagging
PUT /{bucket}/{key}?tagging&versionId={version-id}
```

Request body:

```xml
<Tagging>
  <TagSet>
    <Tag>
      <Key>Type</Key>
      <Value>Image</Value>
    </Tag>
    <Tag>
      <Key>Owner</Key>
      <Value>Less3</Value>
    </Tag>
  </TagSet>
</Tagging>
```

Response body: empty. Success status is `200`; versioned buckets include `x-amz-version-id`.

### Delete Object Tagging

```text
DELETE /{bucket}/{key}?tagging
DELETE /{bucket}/{key}?tagging&versionId={version-id}
```

Request body: none.

Response body: empty. Success status is `204` or `200`; versioned buckets include `x-amz-version-id`.

### Get Object ACL

```text
GET /{bucket}/{key}?acl
GET /{bucket}/{key}?acl&versionId={version-id}
```

Request body: none.

Response body is `AccessControlPolicy`, matching the bucket ACL shape.

### Put Object ACL

```text
PUT /{bucket}/{key}?acl
PUT /{bucket}/{key}?acl&versionId={version-id}
```

Request body:

```xml
<AccessControlPolicy>
  <Owner>
    <ID>usr_default_admin</ID>
    <DisplayName>Admin</DisplayName>
  </Owner>
  <AccessControlList>
    <Grant>
      <Grantee>
        <ID>usr_reader</ID>
        <DisplayName>Reader</DisplayName>
      </Grantee>
      <Permission>READ</Permission>
    </Grant>
  </AccessControlList>
</AccessControlPolicy>
```

Response body: empty. Success status is `200`; versioned buckets include `x-amz-version-id`.

## Multipart Operations

### Create Multipart Upload

```text
POST /{bucket}/{key}?uploads
```

Request body: none. Object metadata can be supplied with `Content-Type` and `x-amz-meta-*` headers.

Response body:

```xml
<InitiateMultipartUploadResult>
  <Bucket>photos</Bucket>
  <Key>large.bin</Key>
  <UploadId>upl_0123456789abcdef</UploadId>
</InitiateMultipartUploadResult>
```

### Upload Part

```text
PUT /{bucket}/{key}?partNumber=1&uploadId=upl_0123456789abcdef
```

Request body: raw part bytes.

Response body: empty. Success status is `200`; response headers include:

```text
ETag: "part-md5"
```

Uploading the same part number again replaces the previously stored part for completion purposes.

### List Parts

```text
GET /{bucket}/{key}?uploadId=upl_0123456789abcdef
```

Request body: none.

Response body:

```xml
<ListPartsResult>
  <Bucket>photos</Bucket>
  <Key>large.bin</Key>
  <UploadId>upl_0123456789abcdef</UploadId>
  <Initiator>
    <ID>usr_default_admin</ID>
    <DisplayName>Admin</DisplayName>
  </Initiator>
  <Owner>
    <ID>usr_default_admin</ID>
    <DisplayName>Admin</DisplayName>
  </Owner>
  <StorageClass>STANDARD</StorageClass>
  <Part>
    <PartNumber>1</PartNumber>
    <LastModified>2026-01-01T00:00:00.000Z</LastModified>
    <ETag>"part-md5"</ETag>
    <Size>5242880</Size>
  </Part>
  <IsTruncated>false</IsTruncated>
  <NextPartNumberMarker>1</NextPartNumberMarker>
</ListPartsResult>
```

### Complete Multipart Upload

```text
POST /{bucket}/{key}?uploadId=upl_0123456789abcdef
```

Request body:

```xml
<CompleteMultipartUpload>
  <Part>
    <PartNumber>1</PartNumber>
    <ETag>"part-one-md5"</ETag>
  </Part>
  <Part>
    <PartNumber>2</PartNumber>
    <ETag>"part-two-md5"</ETag>
  </Part>
</CompleteMultipartUpload>
```

Response body:

```xml
<CompleteMultipartUploadResult>
  <Location>/photos/large.bin</Location>
  <Bucket>photos</Bucket>
  <Key>large.bin</Key>
  <ETag>"combined-md5-2"</ETag>
  <VersionId>1</VersionId>
</CompleteMultipartUploadResult>
```

Every requested part must exist and have a matching ETag. Part numbers do not have to be contiguous in the current implementation.

### Abort Multipart Upload

```text
DELETE /{bucket}/{key}?uploadId=upl_0123456789abcdef
```

Request body: none.

Response body: empty. Success status is `204` or `200`.

### List Multipart Uploads

```text
GET /{bucket}?uploads&prefix={prefix}
```

Request body: none.

Response body:

```xml
<ListMultipartUploadsResult>
  <Bucket>photos</Bucket>
  <Prefix>large</Prefix>
  <Delimiter>/</Delimiter>
  <MaxUploads>1000</MaxUploads>
  <IsTruncated>false</IsTruncated>
  <Upload>
    <Key>large.bin</Key>
    <UploadId>upl_0123456789abcdef</UploadId>
    <Initiator>
      <ID>usr_default_admin</ID>
      <DisplayName>Admin</DisplayName>
    </Initiator>
    <Owner>
      <ID>usr_default_admin</ID>
      <DisplayName>Admin</DisplayName>
    </Owner>
    <StorageClass>STANDARD</StorageClass>
    <Initiated>2026-01-01T00:00:00.000Z</Initiated>
  </Upload>
  <NextKeyMarker>large.bin</NextKeyMarker>
  <NextUploadIdMarker>upl_0123456789abcdef</NextUploadIdMarker>
</ListMultipartUploadsResult>
```

Expired multipart uploads are hidden from this list and cleaned up by maintenance.

## OpenAPI

Less3 exposes one combined OpenAPI document for S3, Less3 REST, and administrative APIs:

```text
GET /openapi.json
```

The dashboard API Explorer consumes this document.
