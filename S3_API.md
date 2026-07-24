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
- Object operations: put, get, head, ranged get, delete, delete many, read/write/delete tags, and read/write ACLs.
- Multipart operations: create upload, upload part, complete upload, abort upload, list uploads, and list upload parts.
- Versioning operations: retrieve specific versions, list versions, and show delete markers.

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
  <NextContinuationToken>MQ==</NextContinuationToken>
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

`Owner` is included only when `fetch-owner=true` is supplied and the owner can be resolved.

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

Response body: empty. Success status is `200`. Canned ACL and grant headers are also accepted for bucket creation and ACL writes.

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

Use any status other than `Enabled` to disable versioning in the current Less3 implementation.

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

Response body: empty. Success status is `200`; response headers include `ETag` and, when bucket versioning is enabled, `x-amz-version-id`.

### Head Object

```text
HEAD /{bucket}/{key}
HEAD /{bucket}/{key}?versionId={version-id}
```

Request body: none.

Response body: empty.

Response headers:

```text
Content-Length: 12345
Content-Type: text/plain
ETag: "9a0364b9e99bb480dd25e1f0284c8555"
x-amz-version-id: 2
x-amz-meta-color: blue
```

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

Successful ranged responses include:

```text
Content-Range: bytes 0-1023/12345
Accept-Ranges: bytes
```

### Delete Object

```text
DELETE /{bucket}/{key}
DELETE /{bucket}/{key}?versionId={version-id}
```

Request body: none.

Response body: empty. Success status is `204` or the S3 stack's successful empty response. Missing current-version deletes are idempotent; invalid explicit versions return an S3 error.

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
    <VersionId>1</VersionId>
  </Deleted>
  <Error>
    <Key>missing.txt</Key>
    <VersionId>1</VersionId>
    <Code>NoSuchKey</Code>
    <Message>The specified key does not exist.</Message>
  </Error>
</DeleteResult>
```

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
