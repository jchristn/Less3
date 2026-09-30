# Change Log

## Current Version

v4.1.0 (2026-09-29)

S3 compatibility and data-integrity fixes. Schema migrations run automatically on startup for every database provider.

- Fixed a data-loss bug: suspending versioning (or disabling it through the REST API) and then overwriting a key with several versions deleted the latest version's metadata, failed the write on the unique version index, and orphaned the blob. Versioning can now be `Enabled` or `Suspended` (never returned to unversioned, as in Amazon S3); suspended writes replace only the key's `null` version and keep every other version. `GetBucketVersioning` reports `Suspended`, and `PutBucketVersioning` with any other status returns `MalformedXML`. Changing versioning now updates the bucket in place instead of deleting and re-creating its row.
- Implemented Amazon S3 delete semantics. Without a version ID, a versioned bucket gets a new delete marker on top of the latest version (previously version 1 was marked, so a multi-version key stayed visible); with a version ID, that version and its ACL, tags, and blob are permanently removed (previously it was only marked, so storage was never reclaimed). GET of a delete marker's version returns `405`. Version ID `null` addresses the unversioned object. A malformed version ID returns `400 InvalidArgument` everywhere.
- DeleteObjects now reports missing keys and versions as deleted (was `NoSuchKey`), removes ACL and tag rows, reports per-key errors (a malformed version ID or a lost lock no longer fails the whole request), honors `Quiet`, omits `VersionId` unless one was requested, reports `DeleteMarker`/`DeleteMarkerVersionId`, rejects more than 1,000 keys, validates `Content-MD5`, and authorizes each key as a DeleteObject would, so an object-scoped RBAC deny can no longer be bypassed through the batch API.
- Fixed DeleteObject and version replacement leaving ACL and tag rows behind (they were looked up after the object row was already deleted, and outside the object lock). Tag and ACL writes now run under the object write lock and target the resolved version instead of assuming version 1.
- Implemented CopyObject (including source version, metadata and tagging directives, copy-source conditions, and source read authorization) and UploadPartCopy with ranges. Previously a server-side copy was stored as a 0-byte object, so `aws s3 mv` between keys lost the data.
- Fixed listing: keys are returned in binary key order across pages (they were in creation order); v1 `marker` is a key, not a row offset; pages no longer drop keys when a delimiter roll-up spans a page; common prefixes count toward `MaxKeys`/`KeyCount`; `start-after`, `NextMarker`, echoed `ContinuationToken`, `encoding-type=url`, and `fetch-owner` are supported; prefixes containing `_` or `%` match literally and case-sensitively; a key whose latest version is a delete marker no longer resurfaces older versions; invalid tokens and parameters return `400` instead of `500`.
- Fixed ListObjectVersions: `key-marker`/`version-id-marker` pagination with `NextKeyMarker`/`NextVersionIdMarker`, common prefixes, correct `IsLatest`, versions and delete markers interleaved in order, and no `500` for anonymously written objects.
- Fixed range reads: a range past the end is clamped, suffix ranges (`bytes=-N`) return `206`, the last byte is readable with `bytes=N-`, empty objects return `416`, and ranges stream from disk instead of being buffered in memory.
- Added conditional requests (`If-Match`, `If-None-Match`, `If-Modified-Since`, `If-Unmodified-Since`) for GET and HEAD, and conditional writes (`If-None-Match: *`, `If-Match`) checked atomically under the write lock.
- Added `Content-MD5` validation (`BadDigest`/`InvalidDigest`), `IncompleteBody` detection, streaming of unsigned PUT bodies to disk, storage of `Cache-Control`/`Content-Disposition`/`Content-Encoding`/`Content-Language`/`Expires`, `response-*` overrides, `x-amz-tagging` on PUT, lowercase user metadata keys, and a `binary/octet-stream` default content type.
- GET and ranged GET now hold the object's shared read lock for the whole response and re-resolve the latest version under it, so a read racing an overwrite returns one complete version instead of `404`/`500`. Blob files are opened read-only and shareable, so concurrent reads of one object no longer conflict.
- Fixed `x-amz-grant-*` headers (quoted `id="..."`/`emailAddress="..."` values were silently ignored), added the `bucket-owner-read`/`bucket-owner-full-control` canned ACLs, and rejected unknown canned ACLs and unknown grantees before anything is written.
- Security: canned ACL writes (`PUT ?acl` with no body) were answered before AWS signature validation, so a request carrying only a valid access key could change a bucket's or object's ACL. They now go through signature validation. The `?metadata` diagnostic is served only for the admin API key and redacts secrets.
- Object keys are now case-sensitive on MySQL and SQL Server (their default collations made `Photo.jpg` and `photo.jpg` the same key), keys that differ only by trailing spaces (`a` and `a `) are distinct (MySQL `utf8mb4_bin` and SQL Server's `=` ignore trailing spaces, so writing one could replace the other), and non-ASCII keys are stored correctly on SQL Server. On SQL Server the unique version index now uses the SHA-256 of the key.
- Upgrading: buckets whose versioning was suspended by an earlier release (which recorded it as "versioning off") are detected by the migration and marked `Suspended`, so the next write cannot delete one of their versions. Rows of buckets that never had versioning become their key's `null` version.
- Object writes decide versioning behavior from one snapshot taken under the object lock (read from the database in cluster mode), apply ACL and tag headers atomically with the new version, and never delete the newly committed blob if cleanup of the replaced version fails. Conditional GET/copy headers are re-checked against the version actually served, so a resumed download cannot splice bytes of a different version.
- MySQL: fresh installations failed to start because an index exceeded InnoDB's 3,072-byte key limit. Key indexes now use prefixes, and version uniqueness is enforced on the SHA-256 of the key, so the unique-version backstop now exists on MySQL.
- PostgreSQL: timestamps read back from the database were shifted by the server's UTC offset (a UTC value was converted to UTC a second time), so `Last-Modified`, listing `LastModified`, and conditional-request comparisons were wrong on hosts not running in UTC. `Last-Modified` headers are also now always emitted in UTC.
- `CreateBucket` on a bucket you already own returns `BucketAlreadyOwnedByYou`; invalid names return `InvalidBucketName`; `DeleteBucket` refuses while any version or delete marker remains. ListParts and ListMultipartUploads paginate, and ListMultipartUploads applies `delimiter`. CompleteMultipartUpload rejects an upload ID that belongs to another key.
- Upgraded S3Server from 7.3.2 to 8.0.1, whose responses were verified against Amazon S3. Client-visible changes: `HEAD` honors `Range` (206 with `Content-Range`, or 416); `Content-Range` describes the bytes returned (`bytes=0-100` on a 10-byte object is `bytes 0-9/10`); a suffix range on an empty object returns 200 with no body; a malformed ACL body returns `MalformedACLError`; a negative `max-keys` returns 400 `InvalidArgument` (it was a 500); query parameters are validated only by the operations that use them, and `partNumber=0` is rejected; `LastModified` and other timestamps have millisecond precision; every response body except a top-level `Error` is in the S3 XML namespace; errors carry `Key`, `BucketName`, `ArgumentName`/`ArgumentValue`, `RangeRequested` and `ActualObjectSize` where they apply, and errors to `HEAD` have no body; responses no longer echo `Host`, `Accept`, `Accept-Language` or `Accept-Charset`, or add a blanket `Cache-Control: no-cache`. Less3's own CORS headers are unchanged.
- ListObjects, ListObjectVersions, DeleteObjects, the ACL writes, CopyObject and UploadPartCopy now go through S3Server's native callbacks instead of responses Less3 wrote itself, and conditional `304`s and suffix ranges no longer need special handling. Delete markers in version listings carry only `Key`, `VersionId`, `IsLatest`, `LastModified` and `Owner`, and DeleteObjects per-key errors list `Key`, `VersionId`, `Code` and `Message`, as Amazon S3 does.
- Test servers no longer outlive the test process that started them: on Windows they run in a kill-on-close job object, a process-exit hook stops any still registered, and each run sweeps and stops servers (and removes temp directories) whose owning test process is gone.
- Added 158 S3 compatibility tests (positive and negative) in `test/Test.Shared/S3Compatibility`, including a protocol-details suite for the S3Server 8 behaviors above, plus 7 test-harness cleanup tests. They are part of the Touchstone catalog (`Test.Automated`, `--suite S3Compat` to run only them, and the xUnit theory adapter) and of the shared suite catalog (xUnit, NUnit, and `Test.Automated --legacy`). `LESS3_TEST_DB_TYPE`/`_HOST`/`_PORT`/`_USER`/`_PASSWORD`/`_NAME` run the tests against PostgreSQL, MySQL, or SQL Server, and `LESS3_TEST_DLL` runs them against another server build.
- `AwsCliTest.bat` and `MinioClientTest.bat` now assert S3 compatibility with real clients: DeleteObjects (missing keys, Quiet, per-key errors), delete markers and version reads, Suspended versioning, listing order and pagination, delimiter/start-after/prefix, case-sensitive keys, suffix and clamped ranges, conditional GET/PUT, Content-MD5, stored metadata and tags, CopyObject, large server-side moves, HEAD ranges, empty-object ranges, keys with spaces and plus signs in both listings, and delete-marker fields (and, for mc, `mirror` + `diff`, `undo`, and `rm --versions`). Both take `LESS3_ENDPOINT` (and credentials: `AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY`, or `LESS3_ACCESS_KEY`/`LESS3_SECRET_KEY` for mc) so they run unattended, and both now clean up every object version.

## Previous Versions

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
