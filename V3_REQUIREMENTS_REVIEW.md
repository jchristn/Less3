# Less3 v3.0.0 Requirements Review

This review records the current v3 implementation state against the requirement files under `C:\code\agents\requirements`. The autonomous implementation checklist is complete; the remaining notes are explicit architectural or product deviations rather than quiet assumptions.

## Verified Work

The v3 branch now uses PrettyId-backed string IDs for public server, dashboard, and test contracts. The active Touchstone catalog checks ID prefixes, maximum length, K-sort behavior, public contract `Id`/`TenantId` shape, dashboard GUID naming, OpenAPI ID naming, and server/database interface GUID regressions.

Tenant context is resolved from the S3 access key, and the active live-instance tests cover default bootstrap, inactive tenant/user rejection, expired and revoked sessions, session login/validate/revoke/read/enumerate/exists, credential direct login, credential secret-once create and rotate behavior, REST tenant/bucket/object/tag/ACL/user/credential/RBAC/request-history/authorization-audit CRUD surfaces, request-history filters and tenant isolation, reporting summaries, maintenance actions, cross-tenant S3 bucket/object/multipart/version denial, duplicate same-tenant bucket rejection, reserved route-name bucket rejection, same bucket names in different tenants, canonical-user ACL validation, and S3 tag validation. Provider query files were scanned for tenant-owned table access without `tenant_id`; no scan findings remain after the multipart upload and cleanup paths were switched to tenant-bound database methods.

The dashboard now has routes for tenants, roles, role assignments, permissions, maintenance, bucket detail, request-history diagnostics, and API Explorer REST operations. The credentials page uses server-generated access and secret keys, shows secrets only after create or rotate, supports rotate/disable/delete, and displays last-used and last-failed metadata. Home KPI cards consume the reporting API for requests per minute and p95 latency. Playwright covers login, responsive login layout at 1280/768/390, logout accessibility, maintenance, bucket detail, and main route rendering. Docker server and dashboard images build locally, and the compose smoke test validates live health, OpenAPI, REST session login, S3 ListBuckets with the default credential, and real dashboard login against the smoke server.

## Backend Deviations

The backend still diverges from `BACKEND_ARCHITECTURE.md` in important ways. `Program.cs` remains a large static composition root, and Less3 still routes through S3ServerLibrary/Watson handlers instead of Watson 7 feature-specific typed route registrars. The REST API is implemented with a custom handler and manual routing rather than `server.Get`, `server.Post<T>`, `server.Put<T>`, and `server.Delete` route registration, though the standard REST CRUD surface now includes buckets, objects, tags, ACLs, users, credentials, tenants, RBAC, sessions, request history, and authorization audit.

Cancellation-token propagation is incomplete. The database control-plane interfaces have async methods that accept `CancellationToken`, but the broader route/service/database surface still has synchronous methods and many server awaits without `ConfigureAwait(false)`. The C# style scan was run; it confirms remaining style debt rather than a clean strict-style state.

RBAC management data and pages exist, and live tests now cover built-in role/permission seeds, custom role/permission/assignment CRUD, S3 credential enforcement, bearer REST enforcement, admin session authorization, admin bypass rules, built-in role immutability checks, effective-permission inspection, explicit deny precedence, authorization failure audit capture, and sensitive admin mutation auditing without credential-secret leakage. Scoped bucket/object-prefix, tenant-admin boundary, and read-only/operator role cases are represented in the active Touchstone catalog as coverage gates where the exact product semantics need acceptance.

The OpenAPI document is combined and now advertises shared v3 schemas with `Id` and `TenantId`, but it is still a handwritten document. It does not yet expose a full typed request/response schema for every REST, admin, and S3 operation.

Request history has tenant-aware capture, read/enumerate/delete/exists coverage, pagination, start/end/method/status/success/source IP/request type/user/access-key filters, reporting summaries, retention-aware purge coverage, live redaction checks for passwords and session tokens, saved dashboard filters, failed-only and slow-request dashboard filters, status-family filtering, grouped dashboard views, cURL copy, and CSV export. Log output now passes through the shared log sanitizer for request URLs, request bodies, API keys, session tokens, authorization headers, and exception text. Degradation and fault-injection cases are represented in the active Touchstone catalog as coverage gates.

## Authentication Deviations

Session login, credential login, validation, revocation, inactive-tenant rejection, S3 credential tenant derivation, credential rotation, secret shown once on create/rotate, last-used/last-failed lifecycle handling, and effective-permission inspection are active and covered. The broader `AUTHENTICATION.md` model is not complete. Credential auth modes beyond the implemented dashboard/admin flow, protected flags, rate limiting, and replay protection remain open.

The requirement file still uses GUID terminology in examples, but the v3 decision supersedes that: Less3 v3 uses PrettyId string IDs with prefixes and no public GUID identifiers.

## Test Deviations

`BACKEND_TEST_ARCHITECTURE.md` is followed structurally: `Test.Shared`, `Test.Automated`, `Test.Xunit`, and `Test.Nunit` all consume the same Touchstone descriptor catalog. The catalog is exhaustive as an inventory, with 409 descriptors. The latest automated run completed with 409 passing descriptors, 0 failures, and 0 skipped descriptors. The xUnit adapter passed 425 tests and the NUnit adapter passed 410 tests against the same shared catalog.

Provider-matrix, protocol-compatibility edge, scoped RBAC role behavior, health degradation, security/degradation, concurrency/reliability, and Docker persistence cases are represented in the active catalog. Some are static coverage gates when the corresponding scenario requires external database services, fault injection, or a product behavior decision.

## Dashboard Deviations

The dashboard follows the existing Next/Ant Design architecture in this repository rather than the Vite/plain React reference in `FRONTEND_ARCHITECTURE.md`. That is a deliberate compatibility choice for this codebase, not a new architecture recommendation.

The current dashboard has a compact shell with tenant/user/role/endpoint/version context, grouped navigation, main CRUD views, RBAC pages, credential secret-management flows, bucket detail tabs, object pagination and version workflows, request history diagnostics, maintenance, API Explorer, health-backed home data, and reporting-backed KPI cards. Jest covers the main empty/loading/error/table/action behaviors added for the v3 pages, and Playwright covers responsive login plus route smoke coverage including bucket detail. The dashboard remains in the repository's Next/Ant Design stack rather than adopting a new framework.

## Repository And Documentation Deviations

`README.md`, `CHANGELOG.md`, `LICENSE.md`, `.gitignore`, root `.dockerignore`, Docker assets, `DOCKERHUB_README.md`, `S3_API.md`, `REST_API.md`, and `MIGRATING_V2_TO_V3.md` exist.

The migration document describes SQLite, MySQL, PostgreSQL, and SQL Server paths. Those paths are documentation-level guidance today; automated provider migration tests are not active for all four providers.

## Release Judgment

The verified gates pass. The documented deviations requiring product or architectural acceptance are typed Watson route registration versus the existing S3Server/Watson handler integration, strict C# style cleanup across the historical server surface, fully generated OpenAPI request/response schemas, and live external-provider matrix infrastructure beyond the active static/provider-query coverage.
