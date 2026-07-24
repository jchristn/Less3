# Less3 REST API

Less3 v3.0.0 adds a first-party REST API for management, administration, and product-specific operations that do not belong in the S3 protocol surface.

## Base URL

```text
/api/v1
```

Standard CRUD routes use:

```text
/api/v1/{type}/{id}
```

Logical actions use `POST`:

```text
/api/v1/{type}/{operation}
```

Admin/dashboard compatibility APIs live outside the versioned prefix:

```text
/admin
```

## Payload Conventions

REST request and response bodies are JSON unless a route explicitly returns no body. The server serializes .NET DTOs directly, so JSON property names are PascalCase, for example `TenantId`, `CreatedUtc`, and `RequestHistoryRetentionDays`. Query-string parameter names are lower camel case, for example `tenantId`, `bucketId`, and `objectId`.

Timestamps are ISO 8601 UTC strings. Less3 IDs are PrettyId string IDs with resource prefixes, k-sortable generated values, and a 32-character maximum. Create bodies may supply IDs explicitly or omit them and let the server generate defaults. Fields shown as `null` are optional unless the route description says otherwise.

Credentials are sanitized on normal reads and lists: `SecretKey` is `null` except on credential create and rotate responses. `AuthSession.TokenHash` is not serialized. Session login responses include the raw bearer `Token` once.

Invalid REST requests may return the shared S3-style error XML used by the HTTP stack:

```xml
<Error>
  <Code>InvalidRequest</Code>
  <Message>Your request is invalid.</Message>
</Error>
```

Other common status/body shapes:

| Status | Body |
| --- | --- |
| `200` | JSON response model, list, or action result |
| `201` | Created JSON model unless noted for legacy `/admin` compatibility routes |
| `204` | Empty body |
| `400` | Invalid request XML |
| `401` | Plain text authentication failure |
| `403` | Empty or plain text authorization failure |
| `404` | Empty body |
| `409` | Empty body |

## Authentication

REST requests accept either the configured admin key header or a session-token header:

```text
x-api-key: less3admin
x-less3-session-token: {session-token}
```

Admin API key calls have platform-level administrative access. Session-token calls are authorized through RBAC. Direct credential login derives the tenant from the globally unique access key.

## Resource Types

The v3 REST API covers these resource families:

| Type | Model | Scope notes |
| --- | --- | --- |
| `tenants` | `Tenant` | Platform-scoped |
| `users` | `User` | Tenant-scoped by `tenantId` or authenticated context |
| `credentials` | `Credential` | Tenant-scoped; access keys are globally unique |
| `roles` | `Role` | Tenant-scoped; built-in roles are protected |
| `permissions` | `Permission` | Tenant-scoped |
| `roleassignments` | `RoleAssignment` | Tenant-scoped |
| `authsessions` | `AuthSession` | Tenant-scoped |
| `authorizationaudit` | `AuthorizationAudit` | Tenant-scoped |
| `requesthistory` | `RequestHistory` | Tenant-scoped |
| `buckets` | `Bucket` | Tenant-scoped |
| `objects` | `Obj` | Requires `bucketId` when reading/enumerating by bucket |
| `buckettags` | `BucketTag` | Requires or derives `bucketId` |
| `objecttags` | `ObjectTag` | Requires or derives `bucketId` and `objectId` |
| `bucketacls` | `BucketAcl` | Requires or derives `bucketId` |
| `objectacls` | `ObjectAcl` | Requires or derives `bucketId` and `objectId` |

Aliases accepted by the route normalizer include `assignments` for `roleassignments`, `sessions` for `authsessions`, `audit` for `authorizationaudit`, and singular tag/ACL forms.

## Standard Operations

Most resource families expose this contract:

| Operation | Request body | Response body |
| --- | --- | --- |
| `POST /api/v1/{type}` | Resource model for `{type}` | `201` resource model |
| `GET /api/v1/{type}/{id}` | None | `200` resource model |
| `GET /api/v1/{type}` | None; query params build an `EnumerationQuery` | `200` `EnumerationResult<T>` |
| `POST /api/v1/{type}/enumerate` | `EnumerationQuery` | `200` `EnumerationResult<T>` |
| `PUT /api/v1/{type}/{id}` | Resource model for `{type}` | `200` resource model |
| `DELETE /api/v1/{type}/{id}` | None | `204` empty |
| `GET /api/v1/{type}/{id}/exists` | None | `200` `ExistsResponse` |
| `POST /api/v1/{type}/exists?id={id}` | None | `200` `ExistsResponse` |

Tenant-scoped requests can include `tenantId` in the query string when using the admin API key or a principal allowed to address that tenant. `objects`, `objecttags`, `bucketacls`, and `objectacls` also support `bucketId` and/or `objectId` query parameters.

## Shared Request Shapes

### EnumerationQuery

Used by `POST /api/v1/{type}/enumerate`. The same fields can be sent as query parameters for `GET /api/v1/{type}` except `ContinuationToken`, `StartUtc`, `EndUtc`, and `Filters` are only fully represented in the JSON body.

```json
{
  "TenantId": "default",
  "Limit": 100,
  "Offset": 0,
  "ContinuationToken": null,
  "SortField": "CreatedUtc",
  "SortDirection": "desc",
  "StartUtc": "2026-01-01T00:00:00Z",
  "EndUtc": "2026-01-02T00:00:00Z",
  "Filters": {
    "status": "500",
    "method": "GET",
    "requestType": "ListBuckets",
    "userId": "usr_default_admin",
    "accessKey": "default",
    "prefix": "logs/",
    "objectId": "obj_0123456789abcdef"
  }
}
```

`Limit` is clamped to `1..1000`. `Offset` is clamped to `0+`. `SortDirection` is `asc` unless the value is `desc`.

### EnumerationResult<T>

```json
{
  "Items": [],
  "Total": 0,
  "Limit": 100,
  "Offset": 0,
  "NextContinuationToken": null,
  "HasMore": false
}
```

### ExistsResponse

```json
{
  "Exists": true
}
```

## Resource Model Shapes

### Tenant

```json
{
  "Id": "default",
  "ParentId": null,
  "Name": "Default",
  "Active": true,
  "CreatedUtc": "2026-01-01T00:00:00Z",
  "LastUpdateUtc": "2026-01-01T00:00:00Z"
}
```

### User

`PasswordHash` is the persisted authentication value used by the current server model. Session login uses the separate `AuthSessionLoginRequest` shape.

```json
{
  "Id": "usr_default_admin",
  "TenantId": "default",
  "Name": "Admin",
  "Email": "admin@less3",
  "PasswordHash": "password",
  "IsAdmin": true,
  "IsTenantAdmin": true,
  "Active": true,
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

### Credential

`SecretKey` may be omitted on create, in which case the server generates one. `AccessKey` may also be omitted on create and will be generated as a globally unique `ak_` PrettyId. `SecretKey` is returned only on create and rotate responses.

```json
{
  "Id": "cred_0123456789abcdef",
  "TenantId": "default",
  "UserId": "usr_default_admin",
  "Description": "Default credential",
  "AccessKey": "default",
  "SecretKey": "default",
  "IsBase64": false,
  "Active": true,
  "LastUsedUtc": null,
  "LastFailedUtc": null,
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

### Role

```json
{
  "Id": "rol_builtin_tenantadmin",
  "TenantId": "default",
  "Name": "TenantAdmin",
  "Description": "Tenant administrator",
  "IsBuiltIn": true,
  "InheritsToChildren": true,
  "Active": true,
  "CreatedUtc": "2026-01-01T00:00:00Z",
  "LastUpdateUtc": "2026-01-01T00:00:00Z"
}
```

### Permission

`ResourceType` and `Operation` accept explicit resource/action names or `All`. `Permit: false` is an explicit deny and wins over permit rules.

```json
{
  "Id": "perm_0123456789abcdef",
  "TenantId": "default",
  "RoleId": "rol_builtin_tenantmember",
  "ResourceType": "Bucket",
  "Operation": "Read",
  "Permit": true,
  "Active": true,
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

### RoleAssignment

`PrincipalType` is usually `User` or `Credential`. `ResourceType` and `ResourceId` are optional for resource-scoped assignments.

```json
{
  "Id": "ra_0123456789abcdef",
  "TenantId": "default",
  "RoleId": "rol_builtin_tenantmember",
  "PrincipalType": "User",
  "PrincipalId": "usr_default_admin",
  "ResourceType": "Tenant",
  "ResourceId": "default",
  "Active": true,
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

### AuthSession

Raw token material is not part of this model. Use the login actions for token issuance and validation.

```json
{
  "Id": "sess_0123456789abcdef",
  "TenantId": "default",
  "PrincipalType": "User",
  "PrincipalId": "usr_default_admin",
  "Active": true,
  "CreatedUtc": "2026-01-01T00:00:00Z",
  "ExpirationUtc": "2026-01-01T08:00:00Z",
  "RevokedUtc": null,
  "SourceIp": "127.0.0.1"
}
```

### AuthorizationAudit

```json
{
  "Id": "aa_0123456789abcdef",
  "TenantId": "default",
  "UserId": "usr_default_admin",
  "CredentialId": "cred_0123456789abcdef",
  "ResourceType": "Bucket",
  "ResourceId": "bkt_0123456789abcdef",
  "Operation": "Read",
  "Permitted": true,
  "Reason": "RBAC permit from role rol_builtin_tenantmember permission perm_0123456789abcdef.",
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

### RequestHistory

```json
{
  "Id": "rh_0123456789abcdef",
  "TenantId": "default",
  "HttpMethod": "GET",
  "RequestUrl": "http://localhost:8000/",
  "SourceIp": "127.0.0.1",
  "StatusCode": 200,
  "Success": true,
  "DurationMs": 12,
  "RequestType": "ListBuckets",
  "UserId": "usr_default_admin",
  "AccessKey": "default",
  "RequestContentType": null,
  "RequestBodyLength": 0,
  "ResponseContentType": "application/xml",
  "ResponseBodyLength": 512,
  "RequestBody": null,
  "ResponseBody": "<ListAllMyBucketsResult>...</ListAllMyBucketsResult>",
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

### Bucket

`StorageType` is normally `Disk`. `DiskDirectory` is a server filesystem path and can usually be omitted on create when using S3 bucket creation. REST bucket creation must not use reserved route names such as `api`, `admin`, or `openapi.json` as bucket names.

```json
{
  "Id": "bkt_0123456789abcdef",
  "TenantId": "default",
  "OwnerId": "usr_default_admin",
  "Name": "photos",
  "RegionString": "us-west-1",
  "StorageType": "Disk",
  "DiskDirectory": "./disk/photos/Objects/",
  "EnableVersioning": false,
  "EnablePublicWrite": false,
  "EnablePublicRead": false,
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

### Obj

REST object models describe metadata rows. Object bytes are written and read through the S3 API.

```json
{
  "Id": "obj_0123456789abcdef",
  "TenantId": "default",
  "BucketId": "bkt_0123456789abcdef",
  "OwnerId": "usr_default_admin",
  "AuthorId": "usr_default_admin",
  "Key": "photos/cover.jpg",
  "ContentType": "image/jpeg",
  "ContentLength": 123456,
  "Version": 1,
  "Etag": "9a0364b9e99bb480dd25e1f0284c8555",
  "Retention": "NONE",
  "BlobFilename": "obj_0123456789abcdef",
  "IsFolder": false,
  "DeleteMarker": false,
  "Md5": "9a0364b9e99bb480dd25e1f0284c8555",
  "CreatedUtc": "2026-01-01T00:00:00Z",
  "LastUpdateUtc": "2026-01-01T00:00:00Z",
  "LastAccessUtc": "2026-01-01T00:00:00Z",
  "Metadata": "{\"color\":\"blue\"}",
  "ExpirationUtc": null
}
```

### BucketTag

```json
{
  "Id": "bt_0123456789abcdef",
  "TenantId": "default",
  "BucketId": "bkt_0123456789abcdef",
  "Key": "Environment",
  "Value": "Production",
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

### ObjectTag

```json
{
  "Id": "ot_0123456789abcdef",
  "TenantId": "default",
  "BucketId": "bkt_0123456789abcdef",
  "ObjectId": "obj_0123456789abcdef",
  "Key": "Type",
  "Value": "Image",
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

### BucketAcl

Use either `UserId` for canonical-user ACLs or `UserGroup` for group ACLs. Permission flags map to S3 `READ`, `WRITE`, `READ_ACP`, `WRITE_ACP`, and `FULL_CONTROL`.

```json
{
  "Id": "ba_0123456789abcdef",
  "TenantId": "default",
  "UserGroup": null,
  "BucketId": "bkt_0123456789abcdef",
  "UserId": "usr_default_admin",
  "IssuedByUserId": "usr_default_admin",
  "PermitRead": true,
  "PermitWrite": false,
  "PermitReadAcp": false,
  "PermitWriteAcp": false,
  "FullControl": false,
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

### ObjectAcl

```json
{
  "Id": "oa_0123456789abcdef",
  "TenantId": "default",
  "UserGroup": null,
  "UserId": "usr_default_admin",
  "IssuedByUserId": "usr_default_admin",
  "BucketId": "bkt_0123456789abcdef",
  "ObjectId": "obj_0123456789abcdef",
  "PermitRead": true,
  "PermitWrite": false,
  "PermitReadAcp": false,
  "PermitWriteAcp": false,
  "FullControl": false,
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

## Auth Session Operations

### User Login

```text
POST /api/v1/authsessions/login
```

Request body:

```json
{
  "TenantId": "default",
  "Email": "admin@less3",
  "Password": "password",
  "ExpirationMinutes": 480
}
```

Response body:

```json
{
  "Session": {
    "Id": "sess_0123456789abcdef",
    "TenantId": "default",
    "PrincipalType": "User",
    "PrincipalId": "usr_default_admin",
    "Active": true,
    "CreatedUtc": "2026-01-01T00:00:00Z",
    "ExpirationUtc": "2026-01-01T08:00:00Z",
    "RevokedUtc": null,
    "SourceIp": "127.0.0.1"
  },
  "Token": "raw-session-token"
}
```

### Credential Login

```text
POST /api/v1/authsessions/credential-login
```

Request body:

```json
{
  "AccessKey": "default",
  "SecretKey": "default",
  "ExpirationMinutes": 60
}
```

Response body is the same `AuthSessionLoginResponse` shape. The tenant is resolved from the credential record, not from a caller-supplied tenant ID.

### Validate Session

```text
POST /api/v1/authsessions/validate
```

Request body:

```json
{
  "Token": "raw-session-token"
}
```

Response body:

```json
{
  "Valid": true,
  "Session": {
    "Id": "sess_0123456789abcdef",
    "TenantId": "default",
    "PrincipalType": "User",
    "PrincipalId": "usr_default_admin",
    "Active": true,
    "CreatedUtc": "2026-01-01T00:00:00Z",
    "ExpirationUtc": "2026-01-01T08:00:00Z",
    "RevokedUtc": null,
    "SourceIp": "127.0.0.1"
  },
  "Reason": null
}
```

### Revoke Session

```text
POST /api/v1/authsessions/revoke
```

Request body:

```json
{
  "Token": "raw-session-token"
}
```

Response body:

```json
{
  "Valid": false,
  "Session": null,
  "Reason": "Session revoked."
}
```

## Credential Actions

### Rotate Credential

```text
POST /api/v1/credentials/rotate?tenantId=default&id=cred_0123456789abcdef
POST /admin/credentials/{id}/rotate
```

The versioned REST route reads the credential ID from the `id` query parameter. The admin compatibility route reads it from the path.

Request body: none.

Response body:

```json
{
  "Id": "cred_0123456789abcdef",
  "TenantId": "default",
  "UserId": "usr_default_admin",
  "Description": "Default credential",
  "AccessKey": "default",
  "SecretKey": "new-secret-key",
  "IsBase64": false,
  "Active": true,
  "LastUsedUtc": null,
  "LastFailedUtc": null,
  "CreatedUtc": "2026-01-01T00:00:00Z"
}
```

### Disable Credential

```text
POST /api/v1/credentials/disable?tenantId=default&id=cred_0123456789abcdef
POST /admin/credentials/{id}/disable
```

Request body: none.

Response body is a sanitized `Credential` with `Active: false` and `SecretKey: null`.

## Admin API Shapes

The `/admin/{resource}` compatibility routes use the same model shapes as `/api/v1/{type}` for buckets, users, credentials, tenants, roles, permissions, role assignments, auth sessions, authorization audit, and request history. Prefer `/api/v1` for new integrations because it returns the shared enumeration envelope. Legacy `/admin` list routes generally return arrays directly.

Direct `authsessions` CRUD is administrative. Normal callers should create, validate, and revoke usable session tokens through the login action routes because raw token material is not part of the persisted `AuthSession` resource model.

### Dashboard Statistics

```text
GET /admin/stats
```

Response body:

```json
{
  "BucketCount": 1,
  "TotalObjectCount": 10,
  "TotalBytes": 10240,
  "Buckets": [
    {
      "Name": "photos",
      "Id": "bkt_0123456789abcdef",
      "Objects": 10,
      "Bytes": 10240
    }
  ],
  "GeneratedUtc": "2026-01-01T00:00:00Z"
}
```

### Health

```text
GET /admin/health
```

Response body:

```json
{
  "ServerVersion": "3.0.0",
  "UptimeSeconds": 3600,
  "DatabaseType": "Sqlite",
  "DatabaseReachable": true,
  "StoragePath": "./disk/",
  "StoragePathWritable": true,
  "FreeDiskBytes": 123456789,
  "TempPath": "./temp/",
  "TempUploadCount": 0,
  "RequestHistoryRetentionDays": 30,
  "LastCleanupRunUtc": null,
  "GeneratedUtc": "2026-01-01T00:00:00Z"
}
```

### Request History Summary

```text
GET /admin/requesthistory/summary?tenantId=default&startUtc=2026-01-01T00:00:00Z&endUtc=2026-01-02T00:00:00Z&interval=hour
```

Response body:

```json
{
  "Data": [
    {
      "TimestampUtc": "2026-01-01T00:00:00Z",
      "SuccessCount": 100,
      "FailureCount": 5
    }
  ],
  "StartUtc": "2026-01-01T00:00:00Z",
  "EndUtc": "2026-01-02T00:00:00Z",
  "Interval": "hour",
  "TotalSuccess": 100,
  "TotalFailure": 5
}
```

Supported intervals are `minute`, `15minute`, `hour`, `6hour`, and `day`.

### Request Report

```text
GET /admin/reports/requests?tenantId=default&startUtc=2026-01-01T00:00:00Z&endUtc=2026-01-02T00:00:00Z
```

Response body:

```json
{
  "TenantId": "default",
  "StartUtc": "2026-01-01T00:00:00Z",
  "EndUtc": "2026-01-02T00:00:00Z",
  "RequestCount": 1000,
  "SuccessCount": 980,
  "FailureCount": 20,
  "RequestsPerMinute": 41.67,
  "FailureRate": 0.02,
  "P50LatencyMs": 12,
  "P95LatencyMs": 80,
  "TopBucketsByBytes": [
    {
      "Name": "photos",
      "Id": "bkt_0123456789abcdef",
      "Count": 10,
      "Bytes": 10240
    }
  ],
  "TopBucketsByRequestCount": [
    {
      "Name": "photos",
      "Id": null,
      "Count": 500,
      "Bytes": 0
    }
  ],
  "TopFailedRequestTypes": [
    {
      "Name": "GetObject",
      "Id": null,
      "Count": 20,
      "Bytes": 0
    }
  ],
  "TopAccessKeys": [
    {
      "Name": "default",
      "Id": null,
      "Count": 1000,
      "Bytes": 0
    }
  ],
  "GeneratedUtc": "2026-01-01T00:00:00Z"
}
```

### Maintenance Status

```text
GET /admin/maintenance/status
```

Response body:

```json
{
  "RequestHistoryRetentionDays": 30,
  "CleanupIntervalMs": 3600000,
  "LastCleanupRunUtc": null,
  "RuntimeEditableSettings": [
    "HeaderApiKey",
    "AdminApiKey",
    "RegionString",
    "RequestHistoryRetentionDays",
    "CleanupIntervalMs"
  ],
  "RestartRequiredSettings": [
    "EnableConsole",
    "ValidateSignatures",
    "Database",
    "Webserver",
    "Storage"
  ],
  "Configuration": {
    "EnableConsole": true,
    "ValidateSignatures": true,
    "BaseDomain": null,
    "HeaderApiKey": "x-api-key",
    "AdminApiKey": "[redacted]",
    "RegionString": "us-west-1",
    "RequestHistoryRetentionDays": 30,
    "CleanupIntervalMs": 3600000,
    "Database": {},
    "Webserver": {},
    "Storage": {},
    "Logging": {},
    "Debug": {}
  },
  "GeneratedUtc": "2026-01-01T00:00:00Z"
}
```

### Maintenance Settings Update

```text
POST /admin/maintenance/settings
```

Request body can either update selected runtime settings:

```json
{
  "RequestHistoryRetentionDays": 30,
  "CleanupIntervalMs": 3600000,
  "OlderThanUtc": null
}
```

Or persist a complete settings object under `Configuration`:

```json
{
  "Configuration": {
    "EnableConsole": true,
    "ValidateSignatures": true,
    "BaseDomain": null,
    "HeaderApiKey": "x-api-key",
    "AdminApiKey": "[redacted]",
    "RegionString": "us-west-1",
    "RequestHistoryRetentionDays": 30,
    "CleanupIntervalMs": 3600000,
    "Database": {
      "Type": "Sqlite",
      "Filename": "./less3.db"
    },
    "Webserver": {},
    "Storage": {},
    "Logging": {},
    "Debug": {}
  }
}
```

The nested `Configuration` object has the same shape returned by `/admin/maintenance/status` under `Configuration`; the example above is abbreviated to the main top-level groups. Submit the full edited object when changing arbitrary settings.

Response body:

```json
{
  "Success": true,
  "Action": "update-settings",
  "PurgedRequestHistoryCount": 0,
  "ExpiredUploadCount": 0,
  "DeletedTempFileCount": 0,
  "ObjectRowCount": 0,
  "MissingBlobFileCount": 0,
  "MissingBlobFiles": [],
  "CutoffUtc": null,
  "RuntimeAppliedSettings": [
    "RequestHistoryRetentionDays",
    "CleanupIntervalMs"
  ],
  "RestartRequiredSettings": [],
  "GeneratedUtc": "2026-01-01T00:00:00Z"
}
```

### Maintenance Actions

These routes return `MaintenanceActionResult`:

```text
POST /admin/maintenance/purge-request-history
POST /admin/maintenance/cleanup-temp-uploads
POST /admin/maintenance/run-cleanup
POST /admin/maintenance/verify-objects
```

`purge-request-history` accepts this optional request body:

```json
{
  "OlderThanUtc": "2026-01-01T00:00:00Z"
}
```

### Migration Status

```text
GET /admin/maintenance/migration-status
```

Response body:

```json
{
  "DatabaseType": "Sqlite",
  "MigrationsAppliedOnStartup": true,
  "IdempotentStartupMigrations": true,
  "DefaultTenantSeeded": true,
  "DefaultAdminUserSeeded": true,
  "DefaultCredentialSeeded": true,
  "GeneratedUtc": "2026-01-01T00:00:00Z"
}
```

### Effective Permissions

```text
GET /admin/effectivepermissions?tenantId=default&principalType=User&principalId=usr_default_admin&resourceType=Bucket&resourceId=bkt_0123456789abcdef&operation=Read
```

Request body: none. All inputs are query-string parameters.

Response body:

```json
{
  "TenantId": "default",
  "PrincipalType": "User",
  "PrincipalId": "usr_default_admin",
  "ResourceType": "Bucket",
  "ResourceId": "bkt_0123456789abcdef",
  "Operation": "Read",
  "HasDecision": true,
  "Permitted": true,
  "IsAdminBypass": false,
  "IsTenantAdminBypass": true,
  "Reason": "Principal is a tenant administrator.",
  "MatchingAssignments": [],
  "MatchingPermissions": [],
  "GeneratedUtc": "2026-01-01T00:00:00Z"
}
```

## OpenAPI

Less3 exposes one combined OpenAPI document:

```text
GET /openapi.json
```

The document includes S3, Less3 REST, and administrative APIs so the dashboard API Explorer can work from a single source.
