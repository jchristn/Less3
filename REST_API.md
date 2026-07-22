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
- bucket-tags
- object-tags
- bucket-acls
- object-acls
- users
- credentials
- roles
- permissions
- role-assignments
- auth-sessions
- authorization-audit
- request-history

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

Route handlers use typed request and response models, set HTTP status codes explicitly, and pass cancellation tokens through service and database layers.

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

## OpenAPI

Less3 exposes one combined OpenAPI document:

```text
GET /openapi.json
```

The document includes S3, Less3 REST, and administrative APIs so the dashboard API Explorer can work from a single source.
