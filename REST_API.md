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

Operations that are logical actions rather than simple CRUD use `POST`:

```text
/api/v1/{type}/{operation}
```

## Resource Types

The v3 REST API covers these resource families:

- tenants
- buckets
- objects
- buckettags
- objecttags
- bucketacls
- objectacls
- users
- credentials
- roles
- permissions
- roleassignments
- authsessions
- authorizationaudit
- requesthistory

## Standard CRUD Shape

Each resource family should expose these operations unless the resource is intentionally read-only:

```text
POST   /api/v1/{type}
GET    /api/v1/{type}/{id}
POST   /api/v1/{type}/enumerate
PUT    /api/v1/{type}/{id}
DELETE /api/v1/{type}/{id}
GET    /api/v1/{type}/{id}/exists
```

Implemented handlers set HTTP status codes explicitly. Cancellation-token propagation is partial in the current v3 branch and is tracked separately from the public route contract.

## EnumerationQuery

Enumeration APIs use a shared request shape:

```json
{
  "tenantId": "default",
  "limit": 100,
  "offset": 0,
  "continuationToken": null,
  "sortField": "createdUtc",
  "sortDirection": "desc",
  "startUtc": "2026-01-01T00:00:00Z",
  "endUtc": "2026-01-02T00:00:00Z",
  "filters": {
    "status": "500",
    "method": "GET"
  }
}
```

Tenant-scoped routes derive the tenant from the authenticated request context. Global administrators may use explicit tenant filters only on routes that allow platform-wide administration.

## EnumerationResult

Enumeration APIs return a shared response shape:

```json
{
  "items": [],
  "total": 0,
  "limit": 100,
  "offset": 0,
  "nextContinuationToken": null,
  "hasMore": false
}
```

The server owns filtering, sorting, pagination, and gap-filling for time buckets. The dashboard renders the shape returned by the server instead of loading unbounded tables and filtering client-side.

## Authentication and Authorization

Authenticated requests resolve to a typed request context containing tenant ID, user ID, credential ID, session ID, principal name, admin flags, and scopes. Full RBAC is enforced for admin and REST operations:

- system admins can operate across tenants where routes permit it
- tenant admins can administer only their tenant
- roles and permissions authorize ordinary users and credentials
- explicit deny rules win over permit rules
- authorization failures and sensitive admin operations are audited

REST requests accept either the configured `x-api-key` header or an `x-less3-session-token` header for routes that require an authenticated session. Direct credential login derives the tenant from the globally unique access key.

## Auth Session Operations

```text
POST /api/v1/authsessions/login
POST /api/v1/authsessions/credential-login
POST /api/v1/authsessions/validate
POST /api/v1/authsessions/revoke
```

`credential-login` accepts an access key and secret key. The tenant is resolved from the credential record, not from a caller-supplied tenant ID.

## Credential Operations

Credential create requests may omit access and secret key material. The server generates PrettyId-compatible string credential IDs where needed, globally unique `ak_` access keys with a 32-character maximum, and random secret keys. Secret keys are returned only on create and rotate responses; normal read, list, update, and disable responses hide the secret.

```text
POST /api/v1/credentials/{id}/rotate
POST /api/v1/credentials/{id}/disable
```

## Admin APIs

Admin APIs live outside the versioned REST prefix:

```text
/admin
```

They accept either the configured `x-api-key` header or an RBAC-authorized `x-less3-session-token` header. Admin API key calls retain platform-level administrative access. Session-token calls are authorized through RBAC.

### Reporting

```text
GET /admin/reports/requests
```

The request report supports tenant-scoped filtering and returns request count, success/failure count, failure rate, requests per minute, p50/p95 latency, top buckets by bytes, top buckets by request count, top failed request types, and top access keys.

### Maintenance

```text
GET  /admin/maintenance/status
POST /admin/maintenance/settings
POST /admin/maintenance/purge-request-history
POST /admin/maintenance/cleanup-temp-uploads
POST /admin/maintenance/run-cleanup
POST /admin/maintenance/verify-objects
GET  /admin/maintenance/migration-status
```

Maintenance status marks runtime-editable settings separately from settings that require restart. Runtime settings currently include request-history retention and cleanup interval.

### Effective Permissions

```text
GET /admin/effectivepermissions
```

The effective-permissions endpoint evaluates principal, resource, and operation inputs against role assignments, permissions, and admin bypass rules, then returns the matching assignments and permissions used to reach the decision.

## OpenAPI

Less3 exposes one combined OpenAPI document:

```text
GET /openapi.json
```

The document includes S3, Less3 REST, and administrative APIs so the dashboard API Explorer can work from a single source.
