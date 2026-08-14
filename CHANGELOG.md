# Change Log

## Current Version

v4.0.0 (2026-08-13)

- Added the multi-node scale-out cluster. The same binary now runs standalone (SQLite control plane, local disk, in-process lock) or as a cluster (PostgreSQL control plane, shared storage, distributed lock, nginx load balancer), selected by configuration. Native OOBE is unchanged; the Docker default is now the cluster.
- Targeted `net10.0`.
- Added a pluggable distributed lock manager (`ILockManager`) with `Local` (in-process, single-node), `Postgres` (in-database; the database is the lock authority, acquisition is one serialized transaction per key), and `Clutch` (alpha, over Clutch's native WebSocket lock protocol with one persistent connection per node — so a node crash auto-releases every lock it held instead of stranding it until the lease lapses — sharing the same Postgres via bring-your-own-database) providers. The Docker stack ships with `Clutch` as the default so the bundled Clutch server, its dashboard, and the "Manage Locks" action are live out of the box; both providers preserve the same integrity guarantees, and `Postgres` is a one-line switch for in-database locking with no extra service.
- Added fair FIFO read/write/delete lock semantics: reads take a shared lock and run concurrently with no cap; writes and deletes are exclusive and granted only after every request that arrived before them releases (a write lets pending reads flush; a delete drains everything). Ordering is by arrival, so a steady read stream cannot starve a queued writer or deleter.
- Added per-key monotonic fencing tokens re-checked at the guarded database commit, so a lock lease that lapses mid-operation cannot corrupt data: the stale holder is fenced out, its request fails, and another node proceeds.
- Wrapped every object read-modify-write (version increment, unversioned overwrite, delete, multipart complete/abort, blob delete) in an exclusive distributed lock.
- Moved object blobs, object-write staging, and multipart parts onto shared storage (`DiskDirectory`, `TempDirectory`, and the new `Storage.PartsDirectory`) mounted identically on every node, so any node can complete or abort any multipart upload. Blob writes compute the content hash in a single streaming pass and are addressed by an immutable object id — never overwritten in place.
- Made `UploadPart` idempotent per `(uploadId, partNumber)` and put `CompleteMultipartUpload`/`AbortMultipartUpload` under a distributed write lock on the upload id, so a cross-node or retried multipart lifecycle stays correct.
- Added cluster membership (nodes register and heartbeat in a membership table) and admin REST endpoints: `GET /api/v1/cluster/nodes`, `GET /api/v1/cluster/health`, `GET /api/v1/cluster/leader`, `GET /api/v1/locks`, and `GET /api/v1/locks/{key}`.
- Added an unauthenticated `GET /healthz` returning `{status, nodeId, version}`, reflecting database and storage writability, for load balancers and orchestrator probes.
- Made `CleanupManager` and schema migration leader-only — cleanup via a `cluster:cleanup` lock lease, migration via a Postgres advisory lock — so N nodes booting together migrate exactly once and background maintenance never deletes another node's in-flight parts.
- Added a bucket-client cache-coherency signal (`BucketClientCacheTtlMs` plus a bucket epoch) so a bucket create, delete, or config change on one node converges on the others within a bounded TTL; the object-write path reads bucket config fresh. Object and bucket metadata are never cached with a lifetime that could serve a stale mutating decision.
- Added an optional, TTL-bounded authentication/authorization cache (`Cluster.AuthCache`, disabled by default) with epoch invalidation on credential/role/session change.
- Added observability. Library code is instrumented with base-class-library `Meter`/`ActivitySource` under `Less3.*` names, plus Watson 7.1's native `http.server.*` metrics. Each node exposes Watson's Prometheus `/metrics` endpoint on its main port (Prometheus scrapes it directly), and a Radiant host at the composition root exports the `Less3.*` domain metrics, traces, and logs over OTLP to the collector, which re-exports them for Prometheus.
- Metered every S3, REST, and admin API operation (`less3.api.requests` / `less3.api.duration`, labeled by surface and operation), and added per-stage timestamps throughout every object operation — PutObject, GetObject, ranged GetObject, HeadObject, and DeleteObject each record `less3.object.stage.duration` for their lock-acquire, metadata-read, storage read/write, database-commit, and blob-delete stages.
- Bridged application logs into the OTLP pipeline: the SyslogLogging module's `MessageLogged` event (SyslogLogging 2.2.1) forwards every log line to a Radiant `ILogger`, which exports over OTLP to the collector and on to Loki, so each node's logs are queryable in Grafana (labeled by `service_instance_id`) and correlated with traces.
- Shipped the Docker observability stack (Prometheus, Grafana, Loki, Tempo, OpenTelemetry collector) with six pre-provisioned Grafana dashboards: "Less3 — Overview", "Less3 — Locks & Data Integrity" (whose fencing-conflict count should stay at zero), "Less3 — Cluster", "Less3 — API Operations", "Less3 — Clutch Lock Server", and "Less3 — Logs". The Clutch server's own metrics are exported over OTLP and scraped into Prometheus alongside Less3's.
- Added a `Cluster` and `Observability` settings block to `system.json`, and `PartsDirectory` to `Storage`.
- Added a startup guard: cluster mode refuses to start on SQLite, because a shared SQLite file cannot back multiple writers.
- Added `MULTINODE_SETUP.md`, `archive/MULTINODE_PLAN.md`, and `MIGRATING_V3_TO_V4.md`.

## Previous Versions

v3.0.0

- Added the v3 tenant and RBAC foundation, including tenant, role, permission, role assignment, session, authorization audit, and request context contracts
- Switched new identifier generation to PrettyID K-sortable string IDs with stable prefixes and a 32-character maximum
- Added tenant-aware schema setup and index definitions for SQLite, MySQL, PostgreSQL, and SQL Server
- Added default v3 bootstrap values: tenant `default`, user `admin@less3`, password `password`, access key `default`, and secret key `default`
- Added credential secret-once create/rotate flows, direct credential session login, credential disable, and hidden-secret metadata responses
- Added admin reporting, maintenance, effective-permission inspection, RBAC-authorized admin session tokens, and sensitive admin mutation audit coverage
- Added dashboard navigation and management pages for tenants, credentials, roles, permissions, reporting KPIs, and maintenance
- Added `S3_API.md`, `REST_API.md`, and `MIGRATING_V2_TO_V3.md`
- Added shared Touchstone descriptors and CLI, xUnit, and NUnit runners for v3 coverage expansion, with 407 descriptors and 267 active assertions passing in the latest automated run

v2.2.0

- Updated to `S3Server v7.0.3`
- Added broad native `AWSSDK.S3` integration coverage for bucket APIs, object APIs, ACLs, tagging, versioning, multipart upload, protocol/error shapes, and signature validation
- Fixed unversioned object overwrite behavior for both standard uploads and multipart completion
- Fixed version enumeration so `ListObjectVersions` returns the full object history
- Tightened range-read handling and validation against native AWS SDK behavior
- Expanded the dashboard with object upload/view/edit workflows, row-click detail modals, centered/full-screen content viewers, standardized copy-to-clipboard controls, and request/response pretty-print tools
- Added credential selection in API Explorer, improved request validation, and aligned dashboard bucket management with admin APIs and signed S3 object requests
- Added admin statistics APIs and dashboard summary cards for total buckets, total objects, total storage, plus per-bucket object count and total size in the Buckets table
- Added admin-side user and credential edit flows backed by update endpoints, with clearer dashboard error reporting during connectivity and admin operations

v2.1.x

- Dependency update and changes to improve compatibility with AWS CLI
- Testing with key AWS CLI capabilities, see AWSCLI.md

v2.0.0

- Dependency updates, internal refactor

v1.5.0

- Breaking change; signatures no longer being validated
- Dependency updates
- Folder fixes
- Owner information included in enumeration
- Better alerts on startup about request requirements (virtual hosting vs path style URLs)

v1.4.0

- Minor refactor
- Fixes to enumeration including folder support
- Request signature authentication

v1.3.0.1

- Migrate database layer to ORM
- Improved usability and console log messages
- Simplification of objects
- Centralized authentication and authorization
- Virtualized storage layer to support new backend storage options
- Updated Postman collection
- Dockerfile for containerized deployments

v1.2.0.2

- Minor cleanup, version from assembly, dependency update, XML documentation, Postman collection

v1.2.0

- Support for bucket in hostname or bucket in URL
- Dependency update

v1.1.0
 
- Dependency update with performance improvements, better async behavior
- Better support for large objects using streams instead of memory-intensive byte arrays
- Better support for chunked transfer-encoding
- Bugfixes
 
v1.0.x

- Added bucket location API
- Changed serializer to remove pretty print for Cyberduck compatibility (S3 Java SDK compatibility)
- Added ACL APIs
- Authentication header support for both v2 and v4
- Chunked transfer support
- Initial release; please see supported APIs below.
