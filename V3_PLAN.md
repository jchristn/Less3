# Less3 v3.0.0 Implementation Plan

Branch: `feature/v3.0.0`

This plan is the working checklist for Less3 v3.0.0. It incorporates `V3.md`, the requirements in `C:\code\agents\requirements`, and the implementation decisions confirmed after reviewing the brief.

## Fixed Decisions

- Use PrettyID string IDs everywhere. Do not use GUIDs for identifiers.
- Generate K-sortable PrettyID values with stable prefixes and maximum total length of 32 characters.
- Keep the database primary key column name as `id`.
- Resolve S3 tenant context from the access key alone.
- Enforce globally unique access keys.
- Make bucket names unique per tenant, not globally unique across the server.
- Implement full RBAC: roles, permissions, assignments, sessions, authorization audit, server-side authorization checks, and dashboard management pages.
- Seed first boot with tenant ID `default`, user `admin@less3`, password `password`, and credential access key `default` / secret key `default`.
- Document v2 to v3 manual migration for SQLite, MySQL, PostgreSQL, and SQL Server.
- Expose one combined OpenAPI document for S3, Less3 REST, and administrative APIs.
- Include the pre-existing dirty worktree contents in the next commit unless a later instruction changes that scope.

## External Requirement Sources

- [ ] Backend implementation follows `C:\code\agents\requirements\BACKEND_ARCHITECTURE.md`.
- [ ] Authentication and authorization follows `C:\code\agents\requirements\AUTHENTICATION.md`.
- [ ] Tests follow `C:\code\agents\requirements\BACKEND_TEST_ARCHITECTURE.md`.
- [ ] Dashboard architecture and usability follows `C:\code\agents\requirements\FRONTEND_ARCHITECTURE.md` and `C:\code\agents\requirements\DASHBOARD_STYLE_AND_USABILITY.md`.
- [ ] Repository and document work follows `C:\code\agents\requirements\REPOSITORY_REQUIREMENTS.md`, `CODE_STYLE.md`, and `WRITING_DOCUMENTS.md`.

## Phase 1: Version, Branch, and Package Baseline

- [x] Create and switch to `feature/v3.0.0`.
- [x] Update `src/Less3/Less3.csproj` version metadata to `3.0.0`.
- [x] Add the PrettyID NuGet package to the server project.
- [x] Update package release notes for v3.0.0.
- [x] Update Docker image tags and compose assets from v2.2.0 to v3.0.0.
- [x] Update dashboard package metadata to v3.0.0.
- [x] Review root repository requirements and add missing publication files only if required.

## Phase 2: Identifier Foundation

- [x] Add a central ID prefix registry in `Constants`.
- [x] Add `IdGenerator` helpers using PrettyID K-sortable IDs with 32-character maximum total length.
- [x] Define prefixes for at least:
  - [x] `ten_` tenant IDs
  - [x] `usr_` user IDs
  - [x] `crd_` credential IDs
  - [x] `bkt_` bucket IDs
  - [x] `obj_` object IDs
  - [x] `upl_` multipart upload IDs
  - [x] `prt_` upload part IDs
  - [x] `btg_` bucket tag IDs
  - [x] `otg_` object tag IDs
  - [x] `bac_` bucket ACL IDs
  - [x] `oac_` object ACL IDs
  - [x] `rol_` role IDs
  - [x] `per_` permission IDs
  - [x] `asn_` role or permission assignment IDs
  - [x] `ses_` auth session IDs
  - [x] `aud_` authorization audit IDs
  - [x] `req_` request history IDs
- [x] Convert domain models from GUID-backed public identifiers to string `Id` fields.
- [x] Remove legacy random identifier generation from `src/Less3`.
- [x] Remove legacy GUID-named public contracts.
- [x] Keep compatibility shims out of v3 public API unless required by an external S3 protocol detail.
- [x] Update JSON serialization and dashboard type definitions to use string IDs and tenant IDs.
- [x] Update tests so no test expects GUID-form identifiers.

## Phase 3: Database Schema and Migrations

- [x] Create `tenants` table for all four providers.
- [x] Add `tenant_id` columns to tenant-owned tables:
  - [x] `buckets`
  - [x] `objects`
  - [x] `buckettags`
  - [x] `objecttags`
  - [x] `bucketacl`
  - [x] `objectacl`
  - [x] `users`
  - [x] `credentials`
  - [x] `uploads`
  - [x] `uploadparts`
  - [x] `requesthistory`
  - [x] RBAC and session tables
- [x] Rename `credential` table to `credentials`.
- [x] Convert application identity columns to string IDs while retaining provider primary keys.
- [x] Keep table and column names consistent across SQLite, MySQL, PostgreSQL, and SQL Server.
- [x] Add tenant ID indexes to every tenant-owned table.
- [x] Add compound indexes that match query patterns:
  - [x] buckets: `(tenant_id, name)`
  - [x] objects: `(tenant_id, bucket_id, key)`, `(tenant_id, bucket_id, key, version)`, `(tenant_id, bucket_id, createdutc)`
  - [x] tags: `(tenant_id, bucket_id)`, `(tenant_id, object_id)`
  - [x] ACLs: `(tenant_id, bucket_id)`, `(tenant_id, object_id)`
  - [x] users: `(tenant_id, email)`
  - [x] credentials: `(accesskey)` globally unique, `(tenant_id, user_id)`
  - [x] request history: `(tenant_id, createdutc)`, `(tenant_id, statuscode, createdutc)`, `(tenant_id, method, createdutc)`, `(tenant_id, sourceip, createdutc)`, `(tenant_id, requesttype, createdutc)`, `(tenant_id, user_id, createdutc)`, `(tenant_id, accesskey, createdutc)`
  - [x] RBAC: indexes for active role, permission, and assignment lookups by tenant, principal, resource, and operation
- [ ] Add idempotent migrations and first-boot initialization for all four providers.
- [x] Seed tenant `default`, admin user, default credential, built-in roles, built-in permissions, and admin assignments.
- [x] Ensure empty databases initialize without requiring old v2 artifacts.

## Phase 4: Database Method Interfaces

- [x] Add `ITenantMethods`.
- [x] Update existing database interfaces to be tenant-aware.
- [x] Add RBAC interfaces:
  - [x] `IRoleMethods`
  - [x] `IPermissionMethods`
  - [x] `IRoleAssignmentMethods`
  - [x] `IAuthSessionMethods`
  - [x] `IAuthorizationAuditMethods`
- [x] Update implementations for SQLite, MySQL, PostgreSQL, and SQL Server.
- [x] Ensure tenant-owned read, enumerate, update, and delete methods scope by tenant.
- [ ] Ensure admin-capable methods explicitly distinguish global admin access from tenant admin access.
- [ ] Add async methods with `CancellationToken` for cancellable work.
- [x] Keep provider-specific SQL under provider-specific `Queries` and `Implementations` folders.

## Phase 5: Authentication and Authorization

- [x] Add a typed request context with tenant ID, user ID, credential ID, session ID, principal, scopes, `IsAdmin`, `IsTenantAdmin`, and authentication state.
- [x] Resolve S3 requests by globally unique access key and derive tenant from the credential record.
- [x] Reject S3 requests when the credential or user is inactive, or when user and credential tenants differ.
- [x] Reject S3 requests when the tenant is inactive.
  - [x] Add live temporary-instance inactive-tenant S3 credential rejection coverage.
- [x] Add user login that creates revocable tenant-bound sessions.
- [x] Add session token validation and revocation.
  - [x] Add live temporary-instance inactive-tenant login rejection coverage.
- [ ] Add direct credential authentication only where required for dashboard/admin workflows.
- [ ] Implement RBAC checks with explicit deny semantics where modeled.
  - [x] Enforce explicit deny precedence for bearer-authenticated Less3 REST sessions.
  - [x] Enforce RBAC for S3 credential storage operations.
  - [x] Add live temporary-instance permit, deny, and authorization-audit coverage.
  - [ ] Complete admin API bypass rules, sensitive admin operation auditing, and effective-permission inspection endpoints.
- [x] Seed immutable built-in roles:
  - [x] `TenantAdmin`
  - [x] `SecurityAdmin`
  - [x] `Auditor`
  - [x] `Operator`
  - [x] `TenantMember`
  - [x] `Custom`
- [ ] Add resource and operation permission mapping for S3, Less3 REST, and admin APIs.
  - [x] Map S3 storage operations to RBAC storage permissions.
  - [x] Map Less3 REST session operations to RBAC resource/operation permissions.
  - [ ] Complete admin API permission mapping.
- [ ] Add authorization audit capture for failures and sensitive admin operations.
  - [x] Capture RBAC authorization failures in authorization audit.
  - [ ] Capture every sensitive admin mutation.
- [ ] Redact secrets in logs and request history.
  - [x] Redact session tokens and passwords in request history.
  - [ ] Complete log redaction guarantees.

## Phase 6: S3 API Tenant Integration

- [x] Update S3 request validation to load credentials by globally unique access key.
- [x] Attach credential tenant context to S3 request metadata and request history.
- [x] Bind every S3 operation to the credential tenant before executing storage logic.
- [x] Scope bucket lookup by `(tenant_id, name)`.
- [x] Scope object lookup by tenant and bucket.
- [x] Scope multipart uploads and upload parts by tenant.
- [x] Scope bucket tags, object tags, bucket ACLs, and object ACLs by tenant.
- [x] Reserve route-colliding bucket names (`api`, `admin`, `openapi.json`, `favicon.ico`, and `robots.txt`) across S3, Less3 REST, and admin bucket creation.
- [ ] Verify no S3 route can access another tenant's bucket, object, tag, ACL, upload, or version.
  - [x] Add live temporary-instance proof that bucket and object reads/writes fail across tenant credentials.
  - [x] Add live temporary-instance proof for multipart upload ID and version-specific cross-tenant denial.
- [ ] Preserve S3 protocol compatibility where possible without keeping GUID identifiers.

## Phase 7: Less3 REST API

- [ ] Add Watson 7 typed route registrars under `/api/v1`.
- [x] Use route schema `/api/v1/{type}/{id}` for standard CRUD.
- [x] Use route schema `/api/v1/{type}/{operation}` for logical operations that use `POST`.
- [ ] Implement typed create, read, enumerate, update, delete, and exists APIs for:
  - [x] tenants
  - [x] buckets
  - [x] objects
  - [x] bucket tags
  - [x] object tags
  - [x] bucket ACLs
  - [x] object ACLs
  - [x] users
  - [x] credentials
  - [x] roles
  - [x] permissions
  - [x] role assignments
  - [x] auth sessions
  - [x] authorization audit
  - [x] request history
- [x] Define `EnumerationQuery` with typed filters, page size, continuation or offset, sort field, sort direction, and tenant-aware filter semantics.
- [x] Define `EnumerationResult<T>` with items, total count where available, page metadata, continuation cursor where used, and filter echo where useful.
- [x] Use `EnumerationQuery` and `EnumerationResult<T>` for administrative and Less3 REST enumeration.
- [ ] Avoid fixed-contract `JsonElement`/DOM request processing.
- [x] Set response status codes explicitly in route handlers.
- [ ] Pass cancellation tokens through route, service, and database layers.
- [ ] Register CORS preflight and post-routing hooks through Watson 7.
- [x] Register one combined OpenAPI skeleton document that covers S3, Less3 REST, and admin API families.
- [ ] Expand the combined OpenAPI document to the full route surface and typed schemas.
  - [x] Add explicit implemented admin and Less3 REST paths to the combined document.
  - [x] Add live temporary-instance assertions for explicit object, role assignment, and auth session paths.
  - [x] Add shared v3 resource schemas with `Id` and `TenantId` contract fields.
  - [x] Add explicit Less3 REST paths for buckets, tags, ACLs, users, and credentials.
  - [ ] Add full typed schemas for every request and response.

## Phase 8: Admin Health, Settings, and Maintenance API

- [x] Add `/admin/health` for compact node status:
  - [x] server version
  - [x] uptime
  - [x] database type and reachability
  - [x] storage path writability
  - [x] free disk
  - [x] temp upload count
  - [x] request-history retention
  - [x] last cleanup run
- [x] Add server-side dashboard enumeration APIs for request history and other large lists.
- [x] Add request-history filters: `limit`, `offset` or cursor, `startUtc`, `endUtc`, `method`, `status`, `success`, `sourceIp`, `requestType`, `userId`, and `accessKey`.
- [ ] Add summary/reporting APIs for requests per minute, failure rate, p50/p95 latency, top buckets by bytes, top buckets by request count, top failed request types, and top access keys.
- [ ] Add maintenance APIs for request-history retention, purge history, cleanup temp uploads, object database vs blob verification, config summary export, migration status, and runtime settings updates.
- [ ] Mark settings that require restart before taking effect.

## Phase 9: Dashboard Navigation and Shell

- [x] Reorganize navigation into:
  - [x] HOME
  - [x] MANAGE: Buckets, Objects
  - [x] CONFIGURE: Tenants, Users, Credentials, Roles, Role Assignments, Permissions
  - [x] OPERATE: Request History, API Explorer
- [x] Use compact grouped navigation with visible section labels.
- [ ] Show tenant, user, role, endpoint, and version context in the shell.
- [x] Change the upper-right logout control to icon only with accessible label and tooltip.
- [x] Preserve light/dark theme behavior.
- [x] Keep shell layout usable at desktop, tablet, and mobile widths.

## Phase 10: Dashboard Product Pages

- [x] Add initial tenant management dashboard page with create, read, enumerate, update, and delete UI flows.
- [x] Add initial RBAC dashboard pages for roles, role assignments, and permissions.
- [x] Add API-backed tenant management page with exists and JSON detail flows.
- [x] Add API-backed RBAC pages for roles, role assignments, and permissions.
- [ ] Update users page for tenant-scoped identities, tenant admin flags, sessions, and role assignment links.
- [ ] Update credentials page:
  - [ ] generate access and secret keys server-side
  - [ ] show secret once on create
  - [ ] support rotate, disable, delete
  - [x] hide secrets from normal metadata views
  - [ ] show last-used and last-failed data from request history
  - [x] support session-only dashboard storage for admin key/session data
- [x] Update Home with 4 to 8 KPI cards. Suggested cards:
  - [x] Tenants
  - [x] Buckets
  - [x] Objects
  - [x] Storage used
  - [ ] Requests per minute
  - [x] Failure rate
  - [ ] p95 latency
  - [x] Active credentials
- [x] Add the node status band backed by `/admin/health`.
- [x] Add request activity chart and recent failure links.
- [ ] Add bucket detail pages with tabs:
  - [ ] Overview
  - [ ] Objects
  - [ ] Activity
  - [ ] Tags
  - [ ] ACL
  - [ ] Versioning
  - [ ] Settings
- [ ] Add object explorer prefix-aware pagination.
- [ ] Add object version browsing, delete-marker visibility, and restore/copy version workflows.
- [ ] Upgrade Request History to include saved filters, failed-only toggle, status family filters, slow request filter, copy as cURL, CSV export, and grouped views.
- [ ] Add dashboard report cards for the reporting API.
- [ ] Add Settings/Maintenance page with well-designed forms, not raw JSON editing.
- [ ] Add API Explorer polish:
  - [ ] bucket/user/credential dropdown injection
  - [ ] saved request collections
  - [ ] environment export/import
  - [ ] generated examples from current server config
  - [ ] Less3 REST API operations
- [x] Normalize endpoint naming in dashboard code, including request history naming.

## Phase 11: Documentation

- [x] Create `S3_API.md`.
- [x] Create `REST_API.md`.
- [x] Create `MIGRATING_V2_TO_V3.md`.
- [x] Document tenant model, ID prefixes, default seed credentials, and RBAC behavior.
- [ ] Document all four provider migration paths:
  - [x] SQLite
  - [x] MySQL
  - [x] PostgreSQL
  - [x] SQL Server
- [x] Document Docker usage for v3.0.0.
- [x] Update README and CHANGELOG where v3 behavior changes public usage.
- [x] Ensure docs do not instruct users to rely on Ids.

## Phase 12: Touchstone Test Architecture

- [x] Convert `test/Test.Shared` into the Touchstone.Core source of truth.
- [x] Keep shared test code free of console output.
- [x] Add `test/Test.Automated` Touchstone CLI runner.
- [x] Add `test/Test.Xunit` Touchstone xUnit adapter.
- [x] Add `test/Test.Nunit` Touchstone NUnit adapter.
- [x] Ensure every runner consumes the same descriptors from `Test.Shared`.
- [x] Add exhaustive backend Touchstone descriptor inventory covering identifiers, tenants, schema/migrations, auth/session, RBAC, S3, Less3 REST, admin APIs, health, reporting, provider matrix, security, concurrency/reliability, Docker, and bootstrap behavior.
  - [x] Current automated Touchstone run: 405 descriptors, 227 active passing live/static assertions, 178 planned/skipped descriptors, 0 failures.
- [x] Add reusable temporary Less3 server fixture that:
  - [x] creates isolated temp database and storage paths
  - [x] starts Less3 on a temporary port
  - [x] validates the container bootstrap seed for tenant `default`, user `admin@less3`, and credential `default` / `default`
  - [x] tears down temp resources
- [ ] Add S3 API coverage for bucket, object, tags, ACL, multipart, versioning, and isolation behavior.
  - [x] Add live temporary-instance S3 `ListBuckets` authentication smoke coverage.
  - [x] Add live temporary-instance S3 same-bucket-name-different-tenants coverage.
  - [x] Add live temporary-instance S3 cross-tenant bucket/object denial coverage.
  - [x] Add active repository-scan assertions for identifier/public-contract S3-adjacent invariants.
  - [x] Add live temporary-instance S3 service, bucket, object, multipart, ACL/tagging, and versioning assertions for currently implemented behavior.
  - [x] Add live temporary-instance S3 duplicate bucket, idempotent missing delete, canonical-user ACL, cross-tenant ACL, and tag validation assertions.
  - [ ] Convert remaining planned S3 protocol-compatibility, pagination, conditional, legal-hold/retention, and error-shape descriptors into active assertions.
- [ ] Add Less3 REST API coverage for CRUD, exists, enumeration, pagination, and authorization.
  - [x] Add live temporary-instance tenant CRUD/enumeration/exists coverage.
  - [x] Add live temporary-instance bucket CRUD/enumeration/exists coverage.
  - [x] Add live temporary-instance user and credential CRUD/enumeration/exists coverage.
  - [x] Add live temporary-instance bucket tag, object tag, bucket ACL, and object ACL CRUD/enumeration/exists coverage.
  - [x] Add live temporary-instance RBAC CRUD/enumeration/exists coverage.
  - [x] Add live temporary-instance auth session login/validate/revoke coverage.
  - [x] Add live temporary-instance auth session read/enumerate/revoke/exists coverage.
  - [x] Add live temporary-instance authorization audit read/enumerate/delete/exists coverage.
  - [x] Add live temporary-instance request history read/enumerate/delete/exists coverage.
  - [x] Add live temporary-instance object metadata CRUD/enumeration/exists coverage.
- [ ] Add RBAC coverage for built-in roles, custom roles, assignments, denial paths, and admin bypass rules.
  - [x] Add live temporary-instance built-in role/permission seed coverage.
  - [x] Add live temporary-instance custom role, permission, assignment, explicit deny, and denial-audit coverage.
  - [ ] Add admin bypass, built-in immutability, scoped bucket/object-prefix assignment, and read-only/operator role behavior coverage.
- [ ] Add database provider matrix coverage for migrations, first boot, tenant CRUD, user CRUD, credential CRUD, tenant-scoped enumeration, authorization-sensitive reads, and concurrent write paths.
- [x] Add request history and health endpoint coverage.
  - [x] Add live temporary-instance `/admin/health` smoke coverage.
  - [x] Add live temporary-instance `/admin/health` version, uptime, database, storage, temp, retention, and cleanup field coverage.
  - [x] Add live temporary-instance S3 request-history capture, pagination, tenant-scope, delete, and filter coverage.
  - [ ] Convert planned request-history, reporting, health degradation, and maintenance descriptors into active assertions.
- [ ] Add Docker/bootstrap smoke tests.
  - [x] Add live container-bootstrap default seed smoke coverage without a checked-in `system.json`.
  - [ ] Add active Docker image, compose, dashboard, volume persistence, and secret-redaction smoke coverage.

## Phase 13: Dashboard Tests

- [ ] Add Playwright smoke tests for:
  - [x] login
  - [x] Home
  - [x] tenants
  - [x] buckets
  - [ ] bucket detail
  - [x] objects
  - [x] request history
  - [x] API Explorer
  - [x] users
  - [x] credentials
  - [x] roles
  - [x] role assignments
  - [x] permissions
  - [ ] settings/maintenance
- [ ] Add smoke tests for empty, loading, and error states.
- [ ] Add tests for table pagination, filtering, sorting, row actions, and destructive confirmations.
- [x] Verify dashboard responsiveness at 1280px, 768px, and 390px.
- [x] Verify logout is icon-only but accessible.

## Phase 14: Compliance Review and Release Gate

- [x] Run repository-wide scan for forbidden GUID identifier generation.
- [x] Run repository-wide scan for legacy GUID-named contracts.
- [x] Run repository-wide scan for non-tenant-scoped database queries.
- [x] Run repository-wide scan for `JsonElement`, `JsonNode`, `JsonObject`, and fixed-contract DOM parsing.
  - [x] Confirm server code is clear; remaining `JsonDocument`/`JsonElement` usage is limited to tests.
- [x] Run repository-wide scan for C# style violations:
  - [ ] `var`
  - [ ] tuples
  - [ ] using directives outside namespace blocks
  - [ ] missing `ConfigureAwait(false)` in library/server awaits
  - [ ] missing XML docs on public surface
- [x] Run all backend builds.
- [x] Run Touchstone automated tests.
  - [x] Latest run: 405 total, 227 passed, 0 failed, 178 skipped/planned.
- [x] Run xUnit adapter tests.
- [x] Run NUnit adapter tests.
- [x] Run dashboard production build.
- [x] Run dashboard Jest unit tests to completion.
- [x] Run dashboard lint and Playwright smoke tests.
- [x] Build Docker images.
- [x] Start Docker compose and validate health, OpenAPI, dashboard login, S3 default credential access, and Less3 REST default session access.
- [x] Evaluate the completed codebase against every requirement file in `C:\code\agents\requirements` and record any justified deviations before release.
  - [x] Record current deviations in `V3_REQUIREMENTS_REVIEW.md`.
- [x] Confirm `V3_PLAN.md` checkboxes accurately reflect completed and remaining work.

## Release Completion Criteria

- [ ] No GUID identifiers remain in public contracts, storage models, database schema, dashboard types, or tests.
- [ ] Tenant isolation is enforced in the HTTP layer, service logic, database methods, and indexes.
- [ ] S3, Less3 REST, admin APIs, and dashboard flows are tenant-aware.
- [ ] RBAC is enforced and manageable through APIs and dashboard pages.
- [ ] One combined OpenAPI document exposes the full route surface.
- [ ] Migration documentation covers all supported providers.
- [x] Touchstone test runners all execute the same shared suites.
- [ ] Docker assets, project metadata, README, and CHANGELOG describe v3.0.0.
- [ ] The dashboard behaves like a compact operator console with usable diagnostics, not a raw CRUD scaffold.
