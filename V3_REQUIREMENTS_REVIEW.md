# Less3 v3.0.0 Requirements Review

This review records the current v3 implementation state against the requirement files under `C:\code\agents\requirements`. It is intentionally direct: several release gates are implemented and verified, but some requirements remain as explicit deviations rather than quiet assumptions.

## Verified Work

The v3 branch now uses PrettyId-backed string IDs for public server, dashboard, and test contracts. The active Touchstone catalog checks ID prefixes, maximum length, K-sort behavior, public contract `Id`/`TenantId` shape, dashboard GUID naming, OpenAPI ID naming, and server/database interface GUID regressions.

Tenant context is resolved from the S3 access key, and the active live-instance tests cover default bootstrap, inactive tenant rejection, session login/validate/revoke/read/enumerate/exists, REST tenant/RBAC/object/request-history/authorization-audit CRUD surfaces, request-history filters and tenant isolation, cross-tenant S3 bucket/object/multipart/version denial, and same bucket names in different tenants. Provider query files were scanned for tenant-owned table access without `tenant_id`; no scan findings remain after the multipart upload and cleanup paths were switched to tenant-bound database methods.

The dashboard now has routes for tenants, roles, role assignments, and permissions; Playwright covers login, responsive login layout at 1280/768/390, logout accessibility, and main route rendering. Docker server and dashboard images build locally, and the compose smoke test validates live health, OpenAPI, REST session login, S3 ListBuckets with the default credential, and real dashboard login against the smoke server.

## Backend Deviations

The backend still diverges from `BACKEND_ARCHITECTURE.md` in important ways. `Program.cs` remains a large static composition root, and Less3 still routes through S3ServerLibrary/Watson handlers instead of Watson 7 feature-specific typed route registrars. The REST API is implemented with a custom handler and manual routing rather than `server.Get`, `server.Post<T>`, `server.Put<T>`, and `server.Delete` route registration.

Cancellation-token propagation is incomplete. The database control-plane interfaces have async methods that accept `CancellationToken`, but the broader route/service/database surface still has synchronous methods and many server awaits without `ConfigureAwait(false)`. The C# style scan was run; it confirms remaining style debt rather than a clean strict-style state.

RBAC management data and pages exist, and live tests now cover built-in role/permission seeds, custom role/permission/assignment CRUD, S3 credential enforcement, bearer REST enforcement, explicit deny precedence, and authorization failure audit capture. The remaining gaps are admin API permission mapping, admin bypass rules, sensitive admin mutation auditing, scoped bucket/object-prefix assignment behavior, built-in immutability checks, and effective-permission inspection endpoints.

The OpenAPI document is combined and now advertises shared v3 schemas with `Id` and `TenantId`, but it is still a handwritten document. It does not yet expose a full typed request/response schema for every REST, admin, and S3 operation.

Request history has tenant-aware capture, read/enumerate/delete/exists coverage, pagination, start/end/method/status/success/source IP/request type/user/access-key filters, and live redaction checks for passwords and session tokens. It is still not the full request-history subsystem described in the reference. Missing items include strongly typed filter DTOs for every route, server-side summary/reporting APIs, maintenance purge/retention APIs, full log redaction guarantees, and degradation tests.

## Authentication Deviations

Session login, validation, revocation, inactive-tenant rejection, and S3 credential tenant derivation are active and covered. The broader `AUTHENTICATION.md` model is not complete. Credential direct-auth dashboard flows, credential auth modes, protected flags, credential rotation, secret shown once on create, last-used/last-failed lifecycle handling, rate limiting, replay protection, and effective-permission endpoints remain open.

The requirement file still uses GUID terminology in examples, but the v3 decision supersedes that: Less3 v3 uses PrettyId string IDs with prefixes and no public GUID identifiers.

## Test Deviations

`BACKEND_TEST_ARCHITECTURE.md` is followed structurally: `Test.Shared`, `Test.Automated`, `Test.Xunit`, and `Test.Nunit` all consume the same Touchstone descriptor catalog. The catalog is exhaustive as an inventory, with 402 descriptors. The latest automated run completed with 186 active live/static assertions passing, 0 failures, and 216 planned/skipped descriptors.

Many descriptors remain skipped because the corresponding product behavior or infrastructure is not yet complete. The remaining planned groups are primarily provider-matrix migration/runtime tests, protocol-compatibility edge cases, reporting and maintenance APIs, health degradation cases, security/degradation tests, concurrency/reliability tests, and Docker persistence/image flows.

## Dashboard Deviations

The dashboard follows the existing Next/Ant Design architecture in this repository rather than the Vite/plain React reference in `FRONTEND_ARCHITECTURE.md`. That is a deliberate compatibility choice for this codebase, not a new architecture recommendation.

The current dashboard has a compact shell, grouped navigation, main CRUD views, RBAC pages, request history, API Explorer, and health-backed home data. It is not yet the full operator console described by `DASHBOARD_STYLE_AND_USABILITY.md`. Missing items include bucket detail tabs, settings/maintenance pages, saved filters, slow/failure status filters, CSV export, copy-as-cURL, report cards backed by reporting APIs, full empty/loading/error state coverage, and i18n infrastructure.

## Repository And Documentation Deviations

`README.md`, `CHANGELOG.md`, `LICENSE.md`, `.gitignore`, Docker assets, `S3_API.md`, `REST_API.md`, and `MIGRATING_V2_TO_V3.md` exist. The repository does not yet include a root `DOCKERHUB_README.md`. Docker ignore files are scoped to source/dashboard build contexts rather than a root `.dockerignore`.

The migration document describes SQLite, MySQL, PostgreSQL, and SQL Server paths. Those paths are documentation-level guidance today; automated provider migration tests are not active for all four providers.

## Release Judgment

The current tree is substantially stronger than the initial v3 worktree, and the verified gates pass. It is not yet a complete v3 release candidate under the supplied requirement files. The largest remaining release blockers are admin-side RBAC completion, typed Watson route registration, full OpenAPI schemas, reporting/maintenance APIs, provider-matrix migration tests, and dashboard product-depth pages.
