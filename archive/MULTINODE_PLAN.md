# Less3 v4.0.0 — Multi-Node Scale-Out Plan

**Status:** IMPLEMENTED on `feature/v4.0.0` (2026-08-13). Backend, telemetry, Docker stack, docs, website, Postman, and unit tests are landed and the full existing test suite (458 tests) plus new lock/storage tests pass; Release build is clean. The genuinely multi-process integration tests (live-Postgres failover, cross-node multipart across two running nodes) are specified in §16 but require a running Postgres cluster to exercise and are left as CI/manual verification.
**Target version:** 4.0.0
**Work branch:** `feature/v4.0.0`
**Author of record:** Joel Christner
**Long pole:** DATA INTEGRITY. No component may be reachable in a state that lets two actors corrupt or desynchronize object data or metadata.

**What landed (summary):** pluggable `ILockManager` (`Local`/`Postgres`/`Clutch`) with per-key exclusive acquire and monotonic fencing tokens; object write/overwrite/delete and multipart complete/abort routed through the lock manager with a fencing re-check before every DB commit; superseded blobs deleted only after commit; `BucketManager` read-through with TTL revalidation; shared-storage relocation (blobs/staging/parts) with single-pass hashing; leader-elected cleanup with a cluster-mode grace TTL; SQLite-in-cluster startup guard; Postgres advisory-lock-guarded schema for lock/node tables; cluster membership + `/api/v1/cluster/*`, `/api/v1/locks`, and unauthenticated `/healthz`; Radiant/Watson OpenTelemetry with Prometheus scraping Watson's native `/metrics` on each node's main port (plus `Less3.*` domain metrics re-exported via the OTLP collector) and pre-provisioned Grafana boards (Overview, Locks & Data Integrity, Cluster, API Operations, Clutch); a multi-node Docker stack (nginx + 2 nodes + Postgres + Clutch + Clutch UI + Prometheus/Grafana/Loki/Tempo/otel-collector, no profiles — `compose.yaml` is the definitive deployment) plus a single-node overlay.

---

## 0. How to use this document

Every actionable item is a GitHub task checkbox. A developer works top to bottom, checks items as they land, and annotates blockers inline. The convention:

- `- [ ]` not started
- `- [x]` complete and verified (tests green, reviewed)
- `- [~]` in progress — append `(owner, note)` after the text
- `- [!]` blocked — append `(reason)` after the text

Do not check an item until its acceptance criteria (stated per phase) are met. Phases are ordered by dependency, but the risk register in §3 supersedes everything: an item that reopens a listed integrity risk is not "done," regardless of which phase it lives in.

A short glossary, because these terms recur:

- **Control plane** — the Postgres database. In a cluster it is the single source of truth for all metadata, lock state, and cluster membership.
- **Data plane** — object blobs and multipart parts on shared storage.
- **Node** — one Less3 server process. Nodes are interchangeable; any node can serve any request.
- **Fencing token** — a per-key monotonically increasing integer issued on lock acquire. It is re-checked at the moment of the guarded database mutation. A holder presenting a stale token is rejected, so a lease that expired mid-operation cannot corrupt data even if the holder never noticed.

---

## 1. What we are building and why

Less3 today is a single-node server. It keeps a node-local cache of `BucketClient` instances, writes object blobs and multipart parts to local disk and a local temp directory, and increments object versions with a non-atomic read-then-insert. None of that survives being run as two processes behind a load balancer — the second node cannot see the first node's buckets, parts, or in-flight writes, and two nodes racing on the same key will lose data.

v4.0.0 makes Less3 deployable two ways from the same binary:

1. **Standalone single-node** — the default when a user runs the binary natively. SQLite control plane, local storage, an in-process lock manager. Unchanged out-of-box experience.
2. **Multi-node scale-out cluster** — the default in Docker. Postgres control plane, shared storage mounted identically on every node, a Postgres-backed distributed lock manager, and an nginx front end that load-balances across nodes.

The database is the central authority for lock state in both topologies. In single-node that authority is a process-local lock; in multi-node it is a Postgres lock table guarded by row-level serialization and protected by fencing tokens.

Observability is a first-class deliverable, not an afterthought. Every node exposes Prometheus metrics and can export OTLP traces and logs, using the Radiant SDK as the host. The Docker stack ships Prometheus, Grafana (with dashboards pre-provisioned), Loki, Tempo, and an OpenTelemetry collector so an administrator can watch and manage the cluster from the first `compose up`.

### Design principles

The cluster keeps exactly one authority for each kind of truth. Object and bucket metadata live only in Postgres and are never cached with a lifetime that could serve a stale answer to a mutating operation. Lock decisions are made by one serialized transaction per key. Blobs live on shared storage addressed by immutable object IDs, so a write never overwrites a byte in place. Where caching is genuinely worth it — authentication and authorization results — it is bounded by a short TTL and invalidated on credential or role change, and it is never allowed to turn a revoked "deny" into an "allow" beyond that TTL.

Nodes are stateless with respect to durable truth. Killing a node mid-request must never corrupt data; at worst it fails that one request, and its lease lapses so another node can proceed. That property is what the failover tests in §16 exist to prove.

---

## 2. Target architecture

```
                          ┌────────────────────────────┐
                          │            nginx            │  load balancer
                          │  least_conn, no reton POST  │  :8000  (S3 + REST + dashboard API)
                          └───────┬───────────┬─────────┘
                                  │           │
                        ┌─────────▼──┐   ┌────▼───────┐        ... N nodes
                        │ less3-node1│   │ less3-node2│
                        │  :8000     │   │  :8000     │
                        │  :9464 /metrics (per node)   │
                        └──┬───┬─────┘   └────┬───┬────┘
                           │   │              │   │
        control plane      │   │  data plane  │   │
   ┌───────────────────────▼───┴──────────────▼───┴──────────┐
   │  PostgreSQL 17          │   Shared POSIX volume          │
   │  - all metadata         │   /less3/disk   (blobs)        │
   │  - lock table (authority)│  /less3/temp   (obj staging)  │
   │  - node membership      │   /less3/temp/parts (mpu parts)│
   └─────────────────────────┴───────────────────────────────┘

   observability:  each node ──OTLP──▶ otel-collector ──▶ Prometheus / Tempo / Loki ──▶ Grafana
                   Prometheus also scrapes each node :9464/metrics directly
   optional:       clutch-server (BYOD → same Postgres) as an alternative lock provider
```

Request handling stays where it is: `PreRequestHandler` authenticates and authorizes, the S3/REST handlers do the work. What changes is that any node-local assumption behind those handlers is replaced by a read-through/write-through path to the control plane, and every read-modify-write on object state is wrapped in a distributed lock plus a fencing-token check at commit.

---

## 3. Data-integrity risk register — the long pole

This is the heart of the plan. Each row names a component that could be misused to compromise integrity or coherency, the concrete failure, the remedy, and a checkbox for the remedy being implemented and covered by a test. No release ships with an unchecked row.

| # | Component / location | Failure mode under multi-node | Remedy | Done |
|---|---|---|---|---|
| R1 | `BucketManager._Buckets` cache (`BucketManager.cs:30`) | Bucket created/deleted on node A is invisible on node B; `GetClient` returns null → auth degrades, wrong bucket resolution | Make client cache read-through/write-through with a short TTL and a control-plane epoch/`LastModifiedUtc` check; lazily build a `BucketClient` from the DB on miss; treat delete as authoritative via a tombstone/existence recheck. Never answer a mutating request from a client whose bucket row is stale. | `- [ ]` |
| R2 | Version increment in `BucketClient.AddObject` (`BucketClient.cs:124`) | Two nodes read latest version N, both insert N+1 → lost update / two rows claim same version | Hold a **Write** lock on `tenant:bucket:key` for the whole read-modify-write; add a DB unique constraint on `(BucketId, Key, Version)`; compute next version inside the locked transaction; reject on fencing-token mismatch at commit | `- [ ]` |
| R3 | `ReplaceLatestUnversionedObject` (overwrite path, versioning off) | Node A deletes old row+blob while node B inserts new → orphaned or prematurely deleted blob, row/blob mismatch | Same Write lock as R2 held across delete+insert; delete the superseded blob **after** metadata commit; fencing check gates the mutation | `- [ ]` |
| R4 | Multipart parts in local `TempDirectory` (`ObjectHandler.cs:975,1181`) | Parts uploaded to node A are invisible to node B; Complete/Abort on another node loses data or builds a corrupt object | Relocate parts to the shared volume (`/less3/temp/parts`); hold a lock on the upload ID during Complete/Abort; parts addressed by `(uploadId, partNumber)` | `- [ ]` |
| R5 | Object-write staging temp (`ObjectHandler.cs:487`) | A retried PUT landing on a second node mid-stream leaves a partial staging file the other node can't finish | Relocate staging to shared volume; stage under a unique object ID; commit is atomic (rename after full write + hash verify); nginx does not retry PUT (R11) | `- [ ]` |
| R6 | `CleanupManager` timer runs on every node (`CleanupManager.cs:53,277`) | Each node independently deletes temp files by global DB rows, but part files exist on shared storage → one node deletes another's in-flight parts | Elect a single cleanup leader via a `cluster:cleanup` lock lease (heartbeat-renewed); only delete parts whose upload row is Completed/Aborted **and** older than a grace TTL; never delete `.tmp` staging younger than the TTL | `- [ ]` |
| R7 | SQLite driver + local file + process-local `ReaderWriterLockSlim` (`SqliteDatabaseDriver.cs:32`) | A shared SQLite file cannot back multiple writers → corruption; the RW lock coordinates nothing across processes | Startup guard: refuse to start in cluster mode on SQLite; Docker default is Postgres; document the constraint | `- [ ]` |
| R8 | Auth/authorization caching (to be added) | Cached credential or role permits access after revocation | Bounded TTL (default 15s, configurable); invalidate on credential/role/session change via a control-plane epoch; never extend a cached allow past TTL; deny decisions are not cached beyond TTL either | `- [ ]` |
| R9 | Lock lease expiry while holder still working | Lease lapses mid-write, a second writer is admitted → concurrent mutation | Fencing token re-checked in the guarded DB transaction; a mutation carrying a stale token is rejected; lease TTL exceeds max expected op time and is heartbeat-renewed; hard `maxHold` ceiling | `- [ ]` |
| R10 | Clock skew across nodes | Nodes disagree on when a lease expired | Lease expiry is computed with the database's clock (`now()` server-side), never node clocks; all TTL arithmetic done in SQL | `- [ ]` |
| R11 | nginx retrying non-idempotent methods | A timed-out PUT/POST retried on a second node duplicates a part or double-writes | `proxy_next_upstream` excludes POST/PUT (retry only on connect errors for idempotent GET/HEAD); UploadPart is idempotent per `(uploadId, partNumber)`; Complete is lock- and idempotency-guarded | `- [ ]` |
| R12 | Object metadata read-after-write | A read on node B right after a write on node A returns stale data | Object metadata is never cached (ConfigManager has zero cache today — keep it that way); all reads go to Postgres, the single authority | `- [ ]` |
| R13 | Bucket config (versioning flag, public read/write) cached in `BucketClient` | Versioning toggled on node A; node B uses stale flag → wrong overwrite behavior → integrity loss | Re-read the bucket row inside the object-write path (or gate the cached copy by the R1 epoch/TTL); the versioning decision must use fresh config | `- [ ]` |
| R14 | `DiskStorageDriver.Write` re-opens the file to compute MD5 (`DiskStorageDriver.cs:374`) | On NFS/SMB, attribute caching can make the re-read see stale bytes → wrong hash or corrupt read | Compute the hash streaming during the single write pass (removes the re-open); fsync before close; document required mount options (`sync`, low `actimeo`) in `MULTINODE_SETUP.md` | `- [ ]` |
| R15 | Blob deletion ordering | Deleting a superseded blob before its metadata row is gone, or while another version references it, loses data | Blobs are addressed by immutable object ID (fresh per write — already true); delete only after metadata commit, only the specific superseded blob, under the object lock | `- [ ]` |
| R16 | Schema migration races at boot | Every node runs migrations in its DB-driver constructor; N nodes race on DDL → deadlock or partial migration | Guard migrations with a Postgres advisory lock (`pg_advisory_lock`) so exactly one node migrates while others wait; idempotent DDL; version-stamped migration ledger | `- [ ]` |
| R17 | Request-history / audit writes from all nodes | Fine functionally, but rows are unattributed | Stamp every history/audit row with `NodeId`; no integrity risk, tracked for completeness | `- [ ]` |

**Acceptance for §3:** every row is `- [x]`, each backed by a named test in §16. The concurrency and failover suites must reproduce R2, R3, R4, R6, R9, and R11 and prove the remedy holds.

---

## 4. Phase 0 — Branch, versioning, and housekeeping

**Goal:** a clean `feature/v4.0.0` branch with version numbers, dependencies, and internal docs corrected before any behavior changes.

- [ ] Create and switch to branch `feature/v4.0.0` from `main`.
- [ ] Bump `<Version>` in `src/Less3/Less3.csproj` from `3.0.0` to `4.0.0`; update the release-notes property.
- [ ] Bump `dashboard/package.json` `version` to `4.0.0` (and `package-lock.json`).
- [ ] Update Docker image tags `:v3.0.0` → `:v4.0.0` in all compose files.
- [ ] Update version strings in `README.md`, `CHANGELOG.md`, `DOCKERHUB_README.md`, `REST_API.md`, `S3_API.md`, and the dashboard "about"/footer.
- [ ] **NuGet updates:**
  - [ ] Update `S3Server` to the latest release; confirm the Watson (`WatsonWebserver` / `WatsonWebserver.Core`) version it pulls.
  - [ ] Force Watson to the latest 7.x by adding an explicit top-level package reference if the S3Server transitive pin lags. Verify Watson 7.1+ exposes its native `Meter`/telemetry surface (the meter names to subscribe to in §11).
  - [ ] Add `Radiant` (telemetry host).
  - [ ] Add `Clutch.Sdk` **only** behind the optional Clutch lock provider (see §7); it must not be a hard dependency of the default build.
  - [ ] Review `SyslogLogging`, `Npgsql`/Postgres driver deps, and confirm `net10.0` target across projects.
- [ ] **Correct `CLAUDE.md` inaccuracies** (they will otherwise mislead every future change):
  - [ ] Target framework is `net10.0`, not .NET 8.
  - [ ] The persistence layer is a hand-rolled multi-dialect driver (`src/Less3/Database/`), **not** WatsonORM. Rewrite the "Database Schema (WatsonORM)" section accordingly.
  - [ ] Remove the "you are on branch feature/multipart" note; multipart is shipped.
  - [ ] Add a "Multi-node architecture" section (lock manager, shared storage, cluster membership, telemetry) once §6–§11 land.
- [ ] Add `MIGRATING_V3_TO_V4.md` stub (filled in §15).

**Acceptance:** solution builds with zero warnings on `net10.0`; `grep -r "3\.0\.0"` returns only historical/archive references; CLAUDE.md matches reality.

---

## 5. Phase 1 — Configuration, settings, and topology switch

**Goal:** one binary, two topologies, selected by configuration, with a default that differs between native and Docker.

- [ ] Add a `Cluster` settings block to `SettingsBase` (`src/Less3/Settings/`), one class per file per code style:
  - [ ] `ClusterSettings.cs` — `Enabled` (bool, default false), `NodeId` (string; default generated from hostname+PrettyId), `LockProvider` enum (`Local` | `Postgres` | `Clutch`, default `Local`), `NodeHeartbeatIntervalMs`, `NodeStaleAfterMs`.
  - [ ] `LockSettings.cs` — `DefaultLeaseMs` (default 30000), `HeartbeatIntervalMs` (default 10000), `MaxHoldMs` (default 3600000), `AcquireTimeoutMs`, `WaiterPollMs`.
  - [ ] `ClutchSettings.cs` — `Endpoint`, `AccessKey`, `TenantId` (only read when `LockProvider == Clutch`).
  - [ ] `AuthCacheSettings.cs` — `Enabled` (default true), `TtlMs` (default 15000), documented as R8's bound.
- [ ] Add a `LocalLockProvider` vs `PostgresLockProvider` default resolution: native binary defaults to `Local` + SQLite; container bootstrap defaults to `Postgres` lock provider + Postgres DB.
- [ ] Relocate storage paths for cluster mode: `StorageSettings.DiskDirectory`, `TempDirectory`, and the multipart parts subdirectory must be settable to a shared mount; add a `PartsDirectory` (default `TempDirectory/parts`) so parts and staging can be reasoned about separately.
- [ ] Update `Setup.cs` (native OOBE wizard): keep SQLite as the default; add prompts for cluster enablement only when the user opts in; write the `Cluster` block to `system.json`.
- [ ] Update `ContainerBootstrapSettingsFactory.cs` / `DefaultDataSeeder.cs`: Docker default `system.json` uses Postgres, `Cluster.Enabled = true`, `LockProvider = Postgres`, shared `/less3/disk` and `/less3/temp`.
- [ ] **Startup guards (R7):** if `Cluster.Enabled` and DB type is SQLite, log a fatal error and refuse to start. If `LockProvider == Postgres` but DB type is SQLite, same.
- [ ] Node identity is stamped into logs, metrics labels, lock holder IDs, request-history rows, and audit rows.

**Acceptance:** `dotnet run` with no `system.json` still produces a working single-node SQLite server (unchanged OOBE); the Docker bootstrap produces a Postgres cluster-mode config; the SQLite-in-cluster guard fires in a test.

---

## 6. Phase 2 — Distributed lock manager (`ILockManager`)

**Goal:** the abstraction chosen in the design review — a pluggable lock manager with a native-Postgres default, an in-process implementation for single-node, and an optional Clutch implementation. This phase is the spine of data integrity; it is prerequisite to §7–§9.

### 6.1 Abstraction

- [ ] `src/Less3/Locking/ILockManager.cs` — interface. Methods (async, each takes a `CancellationToken`):
  - `Task<LockHandle> AcquireAsync(string key, LockMode mode, AcquireOptions options, CancellationToken)`
  - `Task HeartbeatAsync(LockHandle handle, CancellationToken)`
  - `Task<bool> ReleaseAsync(LockHandle handle, CancellationToken)`
  - `Task<bool> ValidateAsync(LockHandle handle, CancellationToken)` — confirms the handle's fencing token is still current (used at commit time)
- [ ] `LockMode.cs` enum — `Read` | `Write` | `Delete` (shared / exclusive-among-writers / fully exclusive), matching S3 access semantics.
- [ ] `AcquireOptions.cs` — `Behavior` (`FailFast` | `Wait`), `TimeoutMs`, `LeaseMs`.
- [ ] `LockHandle.cs` — `Key`, `Mode`, `HolderId`, `FencingToken` (long), `LeaseExpiresUtc`, `Provider`. No tuples anywhere (code style).
- [ ] `LockDeniedException.cs`, `LockLostException.cs` (domain-specific, documented with `<exception>` tags).
- [ ] A background heartbeat loop owned by the lock manager renews all live handles at `HeartbeatIntervalMs`; on renewal failure it marks the handle lost so callers fail closed.

### 6.2 `LocalLockManager` (single-node / SQLite)

- [ ] `src/Less3/Locking/LocalLockManager.cs` — in-process, keyed locks over `Padlock<string>` (already a dependency) or `ReaderWriterLockSlim` per key with a `ConcurrentDictionary`. Fencing tokens are a per-key in-memory monotonic counter. Leases are irrelevant in-process but the API is honored (no-op heartbeat, immediate release). This preserves single-node behavior with zero external services.

### 6.3 `PostgresLockManager` (multi-node default)

- [ ] New control-plane tables (Postgres dialect; migration guarded by R16):
  - `less3_lock` — `LockKey` (PK), `Mode`, `HolderId`, `SessionId`, `FencingToken` (bigint), `AcquiredUtc`, `LeaseExpiresUtc`, `NodeId`, plus per-key policy columns (`ReadMaxHolders`, etc.) if we want Clutch-parity; a companion `less3_lock_holder` table if a Read lock must support multiple holders.
  - `less3_lock_sequence` — per-key fencing counter, or a `bigserial`-backed monotonic column incremented in the acquire transaction.
- [ ] Acquire is **one transaction serialized per key**: `SELECT ... FOR UPDATE` on the key row (Clutch's proven pattern), evaluate the mode against current holders and lease expiry (using DB `now()` per R10), increment and return the fencing token, write the holder, commit. Fail-fast returns denied; Wait polls at `WaiterPollMs` within `TimeoutMs`.
- [ ] Lease reclamation: an expired lease is reclaimable by the next acquirer inside the same serialized transaction (a lapsed holder never blocks forever). The reclaim bumps the fencing token, which is what invalidates the stale holder (R9).
- [ ] `ValidateAsync` re-reads the holder row `FOR SHARE` and confirms `HolderId` + `FencingToken` still match; callers invoke this (or fold the check into the guarded mutation's `WHERE`) at commit.
- [ ] Fencing-token enforcement is folded into the guarded write: object-metadata mutations carry `WHERE ... AND :fencingToken >= last_applied_token`, so a stale token cannot commit even if `ValidateAsync` raced.

### 6.4 `ClutchLockManager` (optional)

- [ ] `src/Less3/Locking/ClutchLockManager.cs` — wraps `Clutch.Sdk.ClutchLockClient` (WebSocket lock client; auto-heartbeats). Maps `LockMode`/`AcquireOptions`/`LockHandle` onto Clutch's `LockMode`/`AcquireOptions`/`AcquiredLock`; surfaces `LockDeniedException` from `Clutch.Sdk.LockDeniedException`. Reads `ClutchSettings`. Compiled and referenced only when the provider is selected; `Clutch.Sdk` stays out of the default dependency closure (conditional package reference or a thin plugin assembly).
- [ ] Document that Clutch shares Less3's Postgres via BYOD, so the DB remains the lock authority even with this provider.
- [ ] Note Clutch's alpha status (v0.2.0) in `MULTINODE_SETUP.md`; it is an opt-in, not the default.

### 6.5 Wiring

- [ ] Instantiate the selected `ILockManager` in `Program.cs` `InitializeGlobals`, after the database driver and before `BucketManager`/`ApiHandler`. Inject it into handlers that perform read-modify-write.
- [ ] Leader election helper on top of `ILockManager`: `cluster:cleanup` and `cluster:migration` are just well-known lock keys held with a renewed lease.

**Acceptance:** the lock suite (§16) passes for `LocalLockManager` and `PostgresLockManager`: mutual exclusion under contention, lease expiry reclamation, fencing-token rejection of a stale holder, Wait-with-timeout, and heartbeat renewal. `ClutchLockManager` passes the same suite when explicitly enabled.

---

## 7. Phase 3 — BucketManager and cache coherency

**Goal:** eliminate the node-local staleness in R1 and R13 without adding a metadata cache that could serve a stale mutating decision (R12).

- [ ] Convert `BucketManager._Buckets` to a read-through/write-through cache of `BucketClient` instances only. On `GetClient` miss, build the client from the live bucket row; on bucket create/delete anywhere in the cluster, other nodes must converge.
- [ ] Add a coherency signal: a `LastModifiedUtc`/monotonic `Epoch` column on the bucket row. `GetClient` revalidates a cached client against the current epoch on a short TTL (default a few seconds) and rebuilds on mismatch. Bucket delete is authoritative — a cached client whose row is gone is discarded and the request sees `NoSuchBucket`.
- [ ] Bucket create/delete/config-change bump the epoch inside their lock (create/delete already hold the per-bucket `Padlock`; extend to update paths that change versioning or public-access flags — R13).
- [ ] Object-write path re-reads bucket config (versioning flag, public read/write) fresh, or reads it through the epoch-validated client, so the overwrite/versioning decision never uses stale config (R13).
- [ ] Confirm and preserve that `ConfigManager` and object metadata remain **uncached** (R12). Do not add object-metadata caching.
- [ ] Auth caching (R8): add an optional, TTL-bounded cache for credential lookup and authorization results, keyed by access key / session hash, invalidated on credential/role/session mutation via a control-plane epoch. Wire `AuthCacheSettings`. Every cached authorization decision carries an expiry; on any credential or role write, bump the auth epoch so nodes drop stale entries within one TTL.

**Acceptance:** a two-node integration test creates a bucket on node A and immediately uses it on node B; deletes it on node A and confirms node B returns `NoSuchBucket` within one TTL; toggles versioning on node A and confirms node B honors the new setting on the next write. A revocation test proves a deleted credential stops working within `AuthCache.TtlMs`.

---

## 8. Phase 4 — Shared storage and object writes

**Goal:** blobs, object-write staging, and multipart parts live on shared storage reachable identically from every node, with writes that are safe on NFS/SMB (R5, R14, R15).

- [ ] Point `DiskDirectory`, `TempDirectory`, and `PartsDirectory` at the shared mount in cluster mode; keep local paths for single-node.
- [ ] Rework `DiskStorageDriver.Write` to compute the content hash in the single streaming write pass and remove the re-open-to-hash step (R14). `fsync`/flush before close so a reader on another node sees complete bytes.
- [ ] Object-write staging (`ObjectHandler.ObjectWrite`) stages to `TempDirectory` under a unique object ID, verifies the hash, then commits (rename into the blob store) — atomic publish, no partial blob visible (R5).
- [ ] Blob deletion of a superseded version happens only after the metadata commit, only for the specific object ID, under the object Write lock (R15).
- [ ] Document required shared-filesystem semantics in `MULTINODE_SETUP.md`: close-to-open consistency, mount options, and the explicit statement that a shared block device without a cluster filesystem is not supported.

**Acceptance:** an object written on node A is byte-identical when read from node B (hash verified); a killed write leaves no visible partial blob; the storage suite passes against a shared-volume harness.

---

## 9. Phase 5 — Multipart uploads across nodes

**Goal:** any node can serve any part of any upload's lifecycle (R4, R11).

- [ ] Relocate part staging (`GetPartFilePath`) to `PartsDirectory` on shared storage; parts addressed by `(bucketId, uploadId, partNumber)`.
- [ ] `UploadPart` is idempotent: re-uploading the same part number overwrites atomically (stage `.tmp` then rename), so an nginx-safe retry cannot corrupt (R11). Part hashes recorded in the DB row.
- [ ] `CompleteMultipartUpload` acquires a **Write** lock on the upload ID, re-reads part rows from the DB (authority), assembles from shared storage, verifies the multipart ETag, commits the object under the object Write lock, then deletes parts and rows. Idempotent/lock-guarded so a duplicate Complete is a no-op or a clean error.
- [ ] `AbortMultipartUpload` acquires the same lock, deletes parts and rows, and leaves no orphan.
- [ ] `ListParts` / `ListMultipartUploads` read from the DB and shared storage, so they are node-agnostic.

**Acceptance:** the cross-node multipart test uploads parts via node A and completes via node B, producing a byte-correct object; a concurrent double-Complete produces exactly one object; an abort mid-upload leaves no residual parts after cleanup.

---

## 10. Phase 6 — Cleanup, leader election, and singleton work

**Goal:** background maintenance runs once per cluster, never destroying live data (R6, R16).

- [ ] `CleanupManager` acquires the `cluster:cleanup` lease before each pass; only the leader runs. Non-leaders skip.
- [ ] Cleanup deletes only temp/part files whose upload row is Completed or Aborted **and** whose file mtime is older than a grace TTL; `.tmp` staging younger than the TTL is never touched (R6).
- [ ] Schema migration is guarded by a Postgres advisory lock so exactly one node migrates at boot; others wait, then verify (R16). Applies to every dialect's migration entry point, but the multi-node guard is Postgres-specific.
- [ ] Request-history retention/purge also runs leader-only.

**Acceptance:** with two nodes, only one performs a cleanup pass in a given interval (asserted via metrics/logs); a unit test proves in-flight parts survive a cleanup pass; concurrent boot of N nodes migrates exactly once.

---

## 11. Phase 7 — Observability (metrics, traces, logs)

**Goal:** every node is measurable and manageable through Prometheus/Grafana out of the box, using Radiant as the host and Watson's native meters, with logs shipped to the stack.

### 11.1 Instrumentation (BCL meters, no telemetry dependency in the instrumented code)

- [ ] Create `static readonly Meter` instruments per area (stable, namespaced names). Proposed meters and key instruments:
  - `Less3.Api` — request counter and latency histogram by operation and status; active-request up/down counter.
  - `Less3.Storage` — bytes read/written, blob put/get/delete counters, storage error counter, free-space gauge.
  - `Less3.Locks` — acquire/wait/deny/expire counters, hold-duration histogram, **fencing-conflict counter** (the integrity signal), active-lock gauge.
  - `Less3.Buckets` — client cache hit/miss, client build counter, epoch-revalidation counter.
  - `Less3.Multipart` — parts uploaded, completes, aborts, assembly-duration histogram.
  - `Less3.Cleanup` — files scanned/deleted, leader-pass counter.
  - `Less3.Db` — query latency histogram, error counter by dialect.
  - `Less3.Auth` — authn/authz result counters, auth-cache hit/miss.
- [ ] Add an `ActivitySource` per area for spans (request → auth → lock → storage → db).
- [ ] Keep label cardinality bounded: identifiers (bucket, key, upload ID) go on spans/logs, not metric labels.

### 11.2 Telemetry host (Radiant)

- [ ] At the composition root, start a `RadiantHost` from `RadiantSettings("less3")`:
  - [ ] Enable the Prometheus HttpListener on `:9464` path `/metrics` per node.
  - [ ] `Sources.AddMeter` for every `Less3.*` meter **and** Watson's native meters (e.g. the `Watson.*` webserver meters — confirm exact names after the Watson 7.x bump).
  - [ ] Enable runtime instrumentation and process gauges.
  - [ ] Configure OTLP export to the collector endpoint (from settings; disabled by default in native single-node, enabled in Docker).
- [ ] Stamp `service.instance.id = NodeId` on the resource so per-node series are distinguishable.
- [ ] Guard one Prometheus host per port per process.

### 11.3 Logs as a telemetry target

- [ ] Add a `LoggingBuilder.AddLess3(...)`-style extension (mirroring Radiant's `AddRadiant`) so `ILogger` output is exported OTLP→Loki with trace/log correlation, co-existing with the existing `SyslogLogging` sink (do not remove syslog).
- [ ] Route Watson 7.x's native logging into the same pipeline where it exposes one.
- [ ] Every log/trace/metric carries `NodeId`; logs carry `trace_id`/`span_id` for correlation.

### 11.4 Grafana dashboards (pre-provisioned)

- [ ] Ship dashboard JSON under `docker/grafana/dashboards/` and provisioning under `docker/grafana/provisioning/{datasources,dashboards}/` (adapted from Radiant's stack, PromQL renamed to Less3 metric names — remember the Prometheus exporter appends `_total`/unit suffixes and converts dots to underscores).
  - [ ] **Less3 Overview** — RPS, latency p50/p95/p99 by operation, error rate, active requests, per-node breakdown.
  - [ ] **Storage** — throughput, blob op rates, free space, storage errors.
  - [ ] **Locks & Data Integrity** — acquisitions/waits/denials, lease expirations, **fencing conflicts** (should be ~0; a spike is an integrity alarm), lock hold duration.
  - [ ] **Cluster** — node up/down (from membership + `up`), cleanup-leader identity, node versions.
  - [ ] **Multipart** — in-flight uploads, completes/aborts, assembly duration.
  - [ ] **Database** — query latency, errors, connection pool.
  - [ ] Datasources: Prometheus (default), Loki, Tempo, with trace↔log correlation wired as in Radiant's provisioning.

**Acceptance:** `curl node:9464/metrics` returns Less3 and Watson series; Grafana comes up with all dashboards populated against the running cluster; the Locks & Data Integrity board shows zero fencing conflicts under normal load and a non-zero count in the deliberate-conflict test.

---

## 12. Phase 8 — Docker, compose, and nginx

**Goal:** `docker compose up` yields a working multi-node Postgres cluster with load balancing and full observability; a single-node overlay remains available.

- [ ] Rewrite `Docker/compose.yaml` (`.yaml`, build contexts per repo requirements) as the **default multi-node Postgres** stack:
  - [ ] `postgres:17` with a named volume and healthcheck; seeded database.
  - [ ] `less3-node1`, `less3-node2` (built from `src/Less3/Dockerfile`), each with `NODE_ID`, Postgres connection env, `Cluster.Enabled=true`, `LockProvider=Postgres`, mounting the shared `less3-data` volume at `/less3/disk` and `/less3/temp`, exposing `:9464`.
  - [ ] `nginx` front end on `:8000`, `least_conn`, `proxy_next_upstream` excluding POST/PUT (R11), large `client_max_body_size` for uploads, upstream healthchecks, WebSocket upgrade headers (needed if Clutch/live features use WS).
  - [ ] `less3-ui` dashboard pointing at the nginx endpoint.
  - [ ] `otel-collector`, `prometheus` (scrapes both nodes' `:9464` and the collector), `grafana` (provisioned), `loki`, `tempo` — adapted from Radiant's `docker/`.
  - [ ] Optional `clutch-server` behind a compose **profile** (`--profile clutch`), sharing the same Postgres via BYOD, for users choosing the Clutch lock provider.
- [ ] Shared storage: a named Docker volume for the demo (single host). Document that real clusters use NFS/SMB/cluster-FS mounted at the same path on every node.
- [ ] Add `compose.single.yaml` overlay: one node, Postgres, no nginx — for users who want Postgres durability without scale-out.
- [ ] Add `.dockerignore` where appropriate (repo requirement); confirm `system.json` templates for each topology.
- [ ] Update `Docker/` helper scripts (`compose-up/down`, `run`, `update`) and the seed directory layout for shared volumes.
- [ ] Prometheus scrape config (`docker/prometheus.yaml`) lists each node; Grafana provisioning mounts as in §11.4.

**Acceptance:** `docker compose up` brings up Postgres, two nodes, nginx, and the full telemetry stack; an AWS CLI run against `:8000` round-robins across nodes and passes; Grafana dashboards populate; `--profile clutch` swaps in the Clutch provider and still passes the integrity suite.

---

## 13. Phase 9 — REST API and S3 API surface

**Goal:** expose cluster state for the dashboard and operators; keep the S3 surface unchanged in contract.

- [ ] Node membership table `less3_node` (`NodeId`, `Hostname`, `Version`, `StartedUtc`, `LastSeenUtc`, `Role`); nodes register on boot, heartbeat at `NodeHeartbeatIntervalMs`, and are considered stale after `NodeStaleAfterMs`.
- [ ] New REST endpoints under `/api/v1` (documented in `REST_API.md`):
  - [ ] `GET /api/v1/cluster/nodes` — membership with health and versions.
  - [ ] `GET /api/v1/cluster/health` — aggregate cluster health for the dashboard and for nginx/orchestrator probes.
  - [ ] `GET /api/v1/locks` and `GET /api/v1/locks/{key}` — active locks and holders (read-only, admin-gated).
  - [ ] `GET /api/v1/cluster/leader` — current holder of `cluster:cleanup` (operational visibility).
- [ ] A lightweight `GET /healthz` (or reuse the admin health probe) for nginx upstream checks; ensure it does not require auth and reflects DB + storage writability.
- [ ] S3 API operations are unchanged in contract; only their internals gain locking. Verify all existing S3 endpoints behave identically single-node.
- [ ] `REST_API.md` updated with the new resources, request/response DTOs (PascalCase JSON per existing convention), and auth requirements. `S3_API.md` reviewed for accuracy (no contract change expected).

**Acceptance:** the new endpoints return correct data in a two-node cluster; existing REST/S3 contract tests still pass; `REST_API.md` matches the implemented DTOs.

---

## 14. Phase 10 — Dashboard

**Goal:** the Next.js dashboard understands a cluster and gives an operator a coherent view, at v4.0.0.

- [ ] Bump to 4.0.0; update about/footer/version references.
- [ ] Point the dashboard's server URL at the nginx endpoint by default in Docker.
- [ ] Add a **Cluster** view: node list with health, version, uptime, leader badge (from `/api/v1/cluster/nodes` + `/cluster/leader`).
- [ ] Add a **Locks** view: active locks and holders (from `/api/v1/locks`), with a clear "fencing conflicts" indicator sourced from metrics or an audit endpoint.
- [ ] Add an **Observability** panel linking to the provisioned Grafana boards (and surfacing key numbers inline where cheap).
- [ ] Audit the dashboard for single-node assumptions (any client-side caching that assumes one server; any request that must be sticky). Confirm auth/session flows work through the load balancer (sessions are DB-backed, so no stickiness required — verify).
- [ ] Update dashboard `CHANGELOG`/README and its Playwright e2e for the new views.
- [ ] Follow the project's dashboard style/usability standards for the new views.

**Acceptance:** dashboard runs against the cluster through nginx, shows both nodes healthy, lists active locks during a load test, and its e2e suite passes.

---

## 15. Phase 11 — Documentation

**Goal:** an operator can stand up either topology from the docs alone, and the repo's document set is internally consistent at v4.0.0.

- [ ] **`MULTINODE_SETUP.md`** (new, the guided walkthrough): prerequisites; Postgres provisioning; shared-storage options (NFS/SMB/cluster-FS) with required mount semantics (R14) and the unsupported-configurations callout; `system.json` for cluster mode; nginx configuration and the no-retry-on-POST/PUT rationale (R11); bringing up nodes; verifying health via `/api/v1/cluster/*`; enabling the optional Clutch provider and its alpha caveat; the observability stack and where the dashboards live; a data-integrity section that explains locks + fencing in plain terms; troubleshooting (split-brain is impossible because the DB is the single authority — explain why); scaling up/down and draining a node. Written as real prose per the writing standards, not a bullet dump.
- [ ] **`README.md`**: reframe Less3 as deployable standalone **or** as a multi-node scale-out cluster; add an architecture diagram; link `MULTINODE_SETUP.md`; update the feature list, quick-starts (native SQLite vs Docker Postgres), and version.
- [ ] **`DOCKERHUB_README.md`**: mirror the README's key points (use cases, architecture, getting started) with explicit asset URLs per repo requirements; reflect the Postgres default in Docker.
- [ ] **`CHANGELOG.md`**: a 4.0.0 entry covering multi-node, distributed locking, shared storage, observability, Postgres-default-in-Docker, dashboard cluster views, and the breaking/behavioral notes.
- [ ] **`MIGRATING_V3_TO_V4.md`**: how a v3 single-node SQLite user upgrades (stays single-node with no change), and how to migrate a single node to a Postgres cluster (export/import or point-at-Postgres path, storage relocation to a shared mount, cutover).
- [ ] **`CLAUDE.md`**: finish the corrections from §4 and add the multi-node architecture section.
- [ ] Review `AWSCLI.md`, `MINIO_CLIENT.md`, `S3_API.md`, `AUTHENTICATION.md`-referenced flows for accuracy against the LB endpoint.
- [ ] Re-read every edited document as a whole for voice and accuracy per the writing standards.

**Acceptance:** a reviewer who has never seen the cluster can follow `MULTINODE_SETUP.md` to a healthy two-node deployment; all version and topology references are consistent.

---

## 16. Phase 12 — Tests

**Goal:** prove the integrity remedies, not just exercise happy paths. The concurrency, failover, and cross-node suites are the ones that justify shipping.

- [ ] **Lock manager unit suite** (Local + Postgres, and Clutch when enabled): mutual exclusion under contention; Read/Write/Delete semantics; Wait-with-timeout; lease expiry reclamation; heartbeat renewal; fencing-token rejection of a stale holder.
- [ ] **Concurrency / race suite** (reproduces R2, R3): N writers on one key produce a monotonic version history with no lost update and no orphan blob; concurrent overwrite (versioning off) yields exactly one surviving object with a matching blob.
- [ ] **Cross-node multipart suite** (R4, R11): parts on node A, complete on node B; idempotent part re-upload; double-Complete yields one object; abort leaves no residue.
- [ ] **Cleanup-leader suite** (R6): only one node cleans; in-flight parts survive; concurrent-boot migration runs once (R16).
- [ ] **Failover suite** (R9): kill a node mid-write; its lease lapses; another node proceeds; no corruption; the fencing conflict is counted, not applied.
- [ ] **Cache-coherency suite** (R1, R13): bucket create/delete/config-change converges across nodes within one TTL; versioning toggle honored on the next write.
- [ ] **Auth-cache suite** (R8): revoked credential/role/session stops working within `AuthCache.TtlMs`.
- [ ] **Startup-guard test** (R7): cluster mode + SQLite refuses to start.
- [ ] **nginx no-retry test** (R11): a timed-out POST is not replayed on a second node.
- [ ] **Storage integrity test** (R5, R14, R15): byte-identical read across nodes; no visible partial blob on a killed write.
- [ ] **Telemetry test**: `/metrics` exposes expected series; fencing-conflict metric increments in the deliberate-conflict test.
- [ ] Extend the shared "Touchstone" suite (`test/Test.Shared`) so xUnit, NUnit, and the automated runner all cover the above; add a docker-compose (or Testcontainers-Postgres) integration harness for the genuinely multi-process cases.
- [ ] **Performance** (`Test.PerformanceBenchmark`): throughput and latency through nginx across N nodes vs single-node baseline; lock overhead measured.

**Acceptance:** the full suite is green on `net10.0`; every risk-register row maps to at least one named passing test; the perf baseline is recorded in the PR.

---

## 17. Phase 13 — Postman collection

- [ ] Add a **Cluster** folder to `Less3.postman_collection` (schema v2.1.0): `GET cluster/nodes`, `cluster/health`, `cluster/leader`, `locks`, `locks/{key}`.
- [ ] Add environment variables for the nginx base URL and admin key; add a variable for a node's direct `:9464/metrics` for spot checks.
- [ ] Verify existing REST and S3 requests still pass through the LB endpoint.
- [ ] Update any embedded documentation/descriptions in the collection to reference v4.0.0 and the cluster endpoints.

**Acceptance:** the collection runs clean against a running cluster; the new Cluster folder returns expected shapes.

---

## 18. Phase 14 — Website (less3.ai)

**Goal:** modernize the design and tell the standalone-or-cluster story honestly. Location: `C:/code/Web Sites/less3.ai` (plain static HTML/CSS/JS on GitHub Pages).

- [ ] **Messaging:** update positioning to "Deploy as a standalone single-node system or a multi-node scale-out cluster." Add a topology section that contrasts the two and an architecture diagram (nodes + nginx + Postgres control plane + shared storage + observability). Lead with the data-integrity guarantee (distributed locks + fencing tokens, DB as single authority) — it is the differentiator and it is now true, so it can be claimed precisely.
- [ ] **Revise `PLAN.md`'s banned-claims list:** the current site deliberately forbids HA/scale/distribution language. Update it to permit accurate multi-node claims while still banning overreach ("infinitely scalable," "automatic failover" where not literally provided). Keep the trust-first, verifiable tone.
- [ ] **Design modernization** (inspiration: xenocloud.ai, x.ai, spacex.com): darker, high-contrast, bold typography, generous negative space, restrained motion, a strong hero with a real architecture visual. Keep developer credibility; evolve (don't abandon) the green accent. Honor `prefers-reduced-motion` and keep the zero-dependency static build.
- [ ] **New/updated sections:** hero rewrite; topology/deployment-modes; architecture diagram; observability (Grafana screenshots); data-integrity explainer; updated compatibility and quick-start (native vs Docker-Postgres).
- [ ] **Assets:** new architecture diagram (inline SVG preferred), Grafana screenshots under `public/screenshots/`, refreshed OG/social image, updated favicon set if the mark changes.
- [ ] **Tests:** update Playwright specs that hard-code current copy (the 4-line hero, section headings, tab labels, the `dotnet build` string) and keep the axe-core zero-violations bar; add checks for the new sections and no horizontal overflow at all breakpoints.
- [ ] **SEO/meta:** update `sitemap.xml`, JSON-LD, meta description; verify the GitHub Pages workflow still deploys only the intended files.
- [ ] Re-read the copy against the writing standards for voice; the site should read like the author, not a template.

**Acceptance:** the redesigned site builds and deploys via the existing Pages workflow; Playwright + axe suites pass; the topology and integrity messaging is present and accurate; no overclaiming.

---

## 19. Phase 15 — Release

- [ ] All §3 risk-register rows `- [x]` with named tests.
- [ ] Full backend suite green on `net10.0`, zero warnings.
- [ ] Dashboard unit + e2e green at 4.0.0.
- [ ] Website Playwright + axe green.
- [ ] `docker compose up` (default multi-node) and `compose.single.yaml` both verified end-to-end with AWS CLI and MinIO client smoke tests.
- [ ] Native `dotnet run` OOBE unchanged (SQLite single-node) and verified.
- [ ] Docs consistent at v4.0.0; `MULTINODE_SETUP.md` walked by a fresh reviewer.
- [ ] Postman collection verified.
- [ ] Grafana dashboards populate; Locks & Data Integrity board shows zero fencing conflicts under normal load.
- [ ] Images tagged `:v4.0.0`; `DOCKERHUB_README.md` publishable.
- [ ] `MIGRATING_V3_TO_V4.md` validated against a real v3→v4 upgrade.
- [ ] PR from `feature/v4.0.0` reviewed and merged.

---

## 20. Cross-cutting requirements (apply to every phase)

- **Code style** (`c:\code\agents\requirements\CODE_STYLE.md`): usings inside the namespace, system-first alphabetical; private fields `_PascalCase`; no `var`; no tuples; one class/enum per file; `.ConfigureAwait(false)`; every async method takes a `CancellationToken` (unless the class holds one); XML docs on all public surface, none on private; guard clauses; specific exception types with `<exception>` tags; nullable reference types enabled; no `Console.WriteLine` in library code — use the logger.
- **Repository requirements** (`REPOSITORY_REQUIREMENTS.md`): `.gitignore`, `.dockerignore` where relevant, README/DOCKERHUB_README/CHANGELOG/LICENSE present and accurate; source only under `src/`, `test/`, `dashboard/`, `sdk/`; Docker uses `.yaml` with build contexts.
- **Writing standards** (`WRITING_DOCUMENTS.md`): applies to `MULTINODE_SETUP.md`, README, CHANGELOG prose, and the website copy — human voice, varied rhythm, no formulaic AI openings, prose that carries the sections rather than bare lists.
- **Integrity gate:** any change that could reopen a §3 row is blocked until its remedy and test are back in place. When in doubt, prefer correctness over throughput — the whole point of v4.0.0 is that scale-out never costs data integrity.

---

## 21. Open items to confirm during implementation

A few things are decided in principle but need a concrete answer once code is in front of us:

- Exact Watson 7.x meter/source names to subscribe to (confirm after the NuGet bump in §4).
- Whether `PostgresLockManager` should also ship a SqlServer/MySql sibling now, or defer multi-node support for those engines to a later release (Postgres is the committed default; the abstraction leaves room either way).
- Whether Read locks need true multi-holder support (the `less3_lock_holder` table) for concurrent readers, or whether object reads can proceed lock-free against the immutable-blob model (likely lock-free reads, since blobs are never mutated in place — confirm against the versioning/delete-marker paths).
- The grace TTL values for cleanup and the auth-cache TTL default (15s proposed) — tune against the failover and revocation tests.
