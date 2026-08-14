# Data Integrity in Less3

Object storage has one job that matters above all others: when you write bytes and later read them back, you get exactly what you wrote — not a stale copy, not a half-written blob, not the loser of a race between two clients. Everything else Less3 does (the S3 API surface, versioning, tagging, the dashboard, the metrics) is in service of that promise.

This document explains what Less3 does, concretely, to keep your data coherent and correct. It is written for the person deciding whether to trust this system with their objects. The short version: data integrity is not a feature that was added late — it is the design constraint that the v4.0.0 architecture was built around, and it holds in both deployment topologies.

## Two topologies, one guarantee

Less3 runs from a single binary in two shapes:

- **Standalone single-node** — the default when you run the binary natively. SQLite control plane, local disk for blobs, an in-process lock manager. This is the zero-dependency, one-process deployment.
- **Multi-node scale-out cluster** — the default in Docker. A PostgreSQL control plane, shared storage mounted identically on every node, a database-backed distributed lock manager, and an nginx load balancer in front.

The important thing is that **the correctness guarantee does not weaken as you scale out**. The single-node path and the multi-node path run the *same* object-mutation code through the *same* lock-manager abstraction. Going from one node to many changes which lock provider is in play and where the bytes live — it does not change the invariant that every read-modify-write on object state is serialized and fencing-checked. Scale-out never costs data integrity; that was the whole point of the v4.0.0 work.

## The core invariant

Every mutation of durable object state runs under a distributed lock and is gated by a fencing token that is re-checked at the moment of the guarded database commit. That covers:

- creating a new object version,
- overwriting an object when versioning is off,
- deleting an object or writing a delete marker,
- completing or aborting a multipart upload,
- deleting a superseded blob.

There is deliberately **no code path that mutates object data or metadata without going through the lock manager**, and no cache is allowed to serve a stale answer to a mutating decision. If you take one thing from this document, take that sentence.

## How correctness is enforced

### 1. A distributed lock serializes every read-modify-write

Object writes are a read-modify-write: read the current latest version, decide the next version (or decide whether an unversioned overwrite is legal), then write. Done naively across two nodes, that is a classic lost-update race — both nodes read version *N*, both write version *N+1*, and one write silently disappears.

Less3 closes that race by holding an **exclusive Write lock on the object key** (`obj:{tenant}:{bucket}:{key}`) across the entire read-modify-write. The version is computed *inside* the locked section. Behind the lock sits a database-enforced backstop: a **UNIQUE index on `(tenant_id, bucket_id, key, version)`** makes it physically impossible for two rows to claim the same version. If the lock were ever wrong — two writers both computing the same next version — the second insert is rejected by the database and fails; it cannot corrupt. You can see the shape of this in `BucketClient.AddObject`: acquire the Write lock, do the work, validate, commit. A rejected insert there is counted as a fencing/version conflict rather than surfaced as an opaque error, so the backstop firing is visible.

The lock manager sits behind a single abstraction, `ILockManager` (`src/Less3/Locking/`), with three providers:

| Provider | Where it runs | Authority | Used by |
|---|---|---|---|
| `Local` | In-process fair queue | In-memory per-key monotonic counter | Single-node / SQLite |
| `Postgres` | PL/pgSQL grant function under a per-key `pg_advisory_xact_lock` | PostgreSQL | Multi-node (in-database, no extra service) |
| `Clutch` | Persistent WebSocket to a Clutch server (BYOD → same Postgres) | PostgreSQL, via Clutch | Multi-node (Docker default) |

All three enforce the *same* semantics and the *same* integrity guarantees. The provider is chosen by configuration (`ClusterSettings.LockProvider`); the code that performs mutations does not know or care which one is active.

**Fair read/write/delete ordering.** Reads take a shared lock and run concurrently with no cap. Writes and deletes are exclusive and are granted only after every request that arrived before them has released — ordering is by arrival, so a steady stream of reads can never starve a queued writer or deleter. Because object blobs are immutable (see below), reads never actually contend with writes for the *bytes*; the exclusivity that protects integrity is between writers and deleters.

### 2. Fencing tokens make a lapsed lease safe

Distributed locks have a hard problem: what if a node acquires a lock, stalls (GC pause, slow disk, network hiccup), its lease expires, a second node is legitimately granted the lock — and then the first node wakes up and tries to commit as though it still held it?

Less3 solves this with **fencing tokens**. Every lock acquisition carries a per-key, monotonically increasing token (`LockHandle.FencingToken`). A larger token supersedes a smaller one. Before any guarded mutation commits, the code calls `ILockManager.ValidateAsync(handle)` — which confirms the handle still owns the key with a live lease and a *current* fencing token. If the lease lapsed and another holder took over, the token is stale and the commit is refused. The stalled node's write is rejected; the data is never corrupted.

The check runs immediately before the guarded database mutation (`ValidateAsync`), so in the ordinary case a superseded holder is rejected before it can write. For the create path there is a second, stronger line of defense that is fully atomic: the UNIQUE `(tenant_id, bucket_id, key, version)` index. Even in the razor-thin window between the validate and the commit, a stale holder cannot insert a duplicate version — the database rejects it. Lease expiry is always computed with the **database's clock** (`now()` server-side), never a node's local clock, so clock skew between nodes cannot cause two nodes to disagree about when a lease ended.

This is why **split-brain is not a failure mode**: there is exactly one authority for lock state (the database), one serialized decision per key, and a fencing token that makes any stale holder's mutation a no-op. Killing a node mid-request fails *that request* and nothing else — its lease lapses and another node proceeds cleanly. Nodes are stateless with respect to durable truth.

### 3. Blobs are immutable and content-addressed

Less3 never overwrites a byte in place. `DiskStorageDriver` writes each blob to a file named by an immutable, freshly generated object ID — a new write produces a new file, always. A superseded blob is deleted **only after** its replacement's metadata has committed, **only** for the specific object ID being retired, and **only** under the object's Write lock. There is no window in which a reader can see a torn or partially-overwritten object, and no ordering in which a row and its blob can disagree.

The content hash is computed in a **single streaming pass** during the write (`IncrementalHash`, MD5), not by re-opening the file afterward. That matters on shared filesystems: re-reading a just-written file over NFS/SMB can return attribute-cached stale bytes and yield a wrong hash. Computing the hash inline removes that hazard entirely. The write is flushed to stable storage with `fs.Flush(true)` (an fsync) before the handle closes, so a reader on another node sees complete, durable bytes.

### 4. Metadata is never served stale

Object and bucket metadata live in exactly one place — the control-plane database — and object metadata is **never cached** with a lifetime that could serve a stale answer to a mutating operation. Every read goes to the authority. A read on node B immediately after a write on node A sees the write, because there is no intermediate cache to be behind.

Two caches exist, and both are bounded so they cannot compromise correctness:

- **`BucketManager`** caches only `BucketClient` *instances* (the machinery to talk to a bucket), revalidated against a bucket `Epoch`/`LastModifiedUtc` on a short TTL. Bucket create, delete, and config changes bump the epoch, so other nodes converge. The object-write path re-reads bucket config (versioning flag, public-access flags) **fresh**, so an overwrite or versioning decision never runs on a stale flag. A deleted bucket is authoritative: a cached client whose row is gone is discarded and the request correctly sees `NoSuchBucket`.
- **Authentication/authorization results** may be cached, but only under a short TTL (`AuthCacheSettings`, default 15 seconds) with epoch invalidation on any credential, role, or session change. A revoked credential stops working within one TTL; a cached "allow" is never extended past its expiry.

### 5. Multipart uploads are safe across nodes

Multipart is where a lot of storage systems quietly lose data. In Less3:

- Parts stage to **shared** storage (`PartsDirectory`), so any node can complete or abort any upload — the work is not stranded on the node that received part 3.
- `UploadPart` is **idempotent** per `(uploadId, partNumber)` — it stages to a temp name and renames atomically, so a retried part (including a load-balancer retry) overwrites cleanly and never corrupts.
- `CompleteMultipartUpload` and `AbortMultipartUpload` hold a **distributed Write lock on the upload ID**. Complete re-reads the part rows from the database (the authority), assembles from shared storage, verifies the multipart ETag, and commits the final object under the object Write lock. A duplicate Complete is a no-op or a clean error — you get exactly one object, never two.

### 6. Singleton work runs exactly once per cluster

Background maintenance that must not run concurrently — temp/part cleanup and schema migration — is elected to a single node via well-known lock leases (`cluster:cleanup`, `cluster:maintenance`/migration). Cleanup deletes only files whose upload row is Completed or Aborted **and** whose file is older than a grace TTL; in-flight staging is never touched. Schema migration at boot is serialized with a Postgres advisory lock, so N nodes starting together migrate exactly once instead of racing on DDL.

## What the load balancer does and does not retry

nginx is configured with `proxy_next_upstream` set to **exclude non-idempotent methods**. A timed-out `PUT` or `POST` is *not* silently replayed against a second node, because a blind retry of a non-idempotent write is a data-duplication hazard. Retries are limited to connection errors on idempotent `GET`/`HEAD`. Combined with idempotent `UploadPart` and lock-guarded `Complete`, this means the retry behavior of the front door cannot manufacture duplicate or double-written data.

## Guardrails that stop unsafe deployments

Correctness depends on the topology being coherent, so Less3 refuses to start in a configuration that cannot be safe:

- **Cluster mode on SQLite is refused.** A shared SQLite file cannot back multiple writers without corruption, so cluster mode (or a Postgres lock provider) on a SQLite database is a fatal startup error, not a silent degradation.
- **Shared storage is required for multi-node.** Blobs, object-write staging, and multipart parts must live on storage mounted identically on every node. `MULTINODE_SETUP.md` documents the required close-to-open consistency and mount semantics, and states explicitly that a shared block device without a cluster filesystem is not supported.

## You can watch it hold

Integrity is not a claim you have to take on faith — it is instrumented. Library code emits BCL `Meter`/`ActivitySource` telemetry under `Less3.*` names, and the Docker stack ships Prometheus, Grafana, Loki, and Tempo pre-provisioned. Among the boards is a **Locks & Data Integrity** dashboard whose **fencing-conflict counter is the signal that matters**: under normal operation it sits at zero. A non-zero value means a stale holder was *caught and rejected* at commit — the mutation was refused, the data was protected, and you have a visible record of it. A spike is an alarm you can see, not silent corruption you discover later.

The `/api/v1/cluster/*` and `/api/v1/locks` endpoints expose live membership, health, the current cleanup leader, and the set of active locks and their holders, so an operator can see exactly what is coordinating at any moment.

## The design principle behind all of it

The cluster keeps **exactly one authority for each kind of truth**: the database for all metadata, lock state, and membership; shared storage for immutable blobs. Nodes are interchangeable and hold no durable truth of their own. Every decision that could corrupt or desynchronize data is made by one serialized transaction per key, protected by a fencing token that is re-checked at the point of commit. Where caching is genuinely worthwhile it is bounded by a short TTL and epoch invalidation, and it is never permitted to turn a stale value into a wrong write.

When there is a tension between throughput and correctness, Less3 chooses correctness. That is the trade the architecture makes on your behalf, deliberately and everywhere.

## Where to look in the code

For readers who want to verify these claims directly:

- `src/Less3/Locking/` — the lock-manager abstraction (`ILockManager`, `LockHandle`, `LockMode`, `LockKeys`) and the `Local`, `Postgres`, and `Clutch` providers.
- `src/Less3/Classes/BucketClient.cs` — object write/overwrite/delete acquiring a Write lock and calling `ValidateAsync` before the guarded commit; a unique-constraint rejection is counted as a fencing/version conflict.
- `src/Less3/Database/*/Queries/MigrationQueries.cs` — the UNIQUE `idx_objects_tenant_bucket_key_version_unique` index (all four dialects) that backstops the write lock.
- `src/Less3/Database/SqlErrorClassifier.cs` — dialect-agnostic detection of a unique-constraint violation.
- `src/Less3/Storage/DiskStorageDriver.cs` — single-pass streaming hash and `fs.Flush(true)` on write.
- `src/Less3/Api/S3/ObjectHandler.cs` — multipart initiate/upload/complete/abort with shared-storage parts and upload-ID locking.
- `archive/MULTINODE_PLAN.md` — the full plan, including the 17-row data-integrity risk register (each row: failure mode, remedy, test).
- `MULTINODE_SETUP.md` — the operator guide, including shared-storage requirements and the plain-language locks-and-fencing explainer.
