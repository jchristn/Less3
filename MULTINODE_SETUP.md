# Running Less3 as a Multi-Node Cluster

Less3 v4.0.0 ships one binary that runs two ways. Left alone, it behaves exactly as it always has: a single process, a SQLite file, object blobs on local disk, locking handled in memory. That is the standalone topology, and for a laptop, a build agent, or a small private deployment it is still the right answer. The other way is a cluster — several Less3 processes sitting behind a load balancer, all backed by one PostgreSQL database and one shared storage volume, coordinating every object mutation through a distributed lock. This guide is about the second way: what it needs, how to stand it up, how to confirm it is healthy, and why it does not corrupt your data when a node dies mid-write.

Read the standalone-versus-cluster tradeoff before you provision anything. The cluster is more machinery, and you should only take it on when you actually need what it provides.

## Which topology you want

A single Less3 process saturates a surprising amount of traffic, and SQLite on a local disk is fast and durable enough for most private workloads. Reach for a cluster when one node is no longer enough — when you need more request throughput than a single process delivers, when you want to survive the loss of a node without losing the service, or when your storage already lives on a shared filesystem and you want several front ends serving it. Everything below assumes you have decided that a cluster earns its keep.

The cluster changes three things about how Less3 stores state. Metadata moves from SQLite to PostgreSQL, because a SQLite file cannot back multiple writers — put two processes on one SQLite file and you will corrupt it. Object blobs and multipart parts move from local disk to a shared volume that every node mounts at the same path, because a part uploaded to one node has to be readable by any other node that finishes the upload. And locking moves from an in-process lock to a distributed one that lives in Postgres, because the whole point of running more than one node is that they act on the same data, and two nodes racing on the same object key must not both win.

Less3 refuses to start a cluster on SQLite. If `Cluster.Enabled` is true and the database type is SQLite, the process logs a fatal error and exits rather than come up in a configuration that will eventually eat data. That guard is deliberate. Do not try to work around it.

## What you need before you start

Three pieces have to be in place: a PostgreSQL server the nodes can reach, a shared storage volume mounted identically on every node, and a load balancer in front. The Docker stack in this repo provides all three so you can see the shape of the thing in a few minutes. A real deployment usually supplies its own Postgres and its own shared filesystem, and this guide covers both the demo path and the hand-rolled path.

### PostgreSQL

Any PostgreSQL 14 or newer works; the Docker stack pins `postgres:17`. Create a database and a role that owns it, and make sure every node can open a connection. Less3 creates its own tables and indices on first boot, including the lock table and the node-membership table, so you do not pre-load a schema. When several nodes boot at once they would otherwise race each other running the same DDL, so schema creation and migration are serialized behind a Postgres advisory lock — exactly one node migrates while the others wait, then everyone proceeds against the finished schema.

The database is the authority for more than metadata here. Lock state lives in Postgres, cluster membership lives in Postgres, and lease expiry is computed with the database's own clock rather than any node's clock, so a node with a skewed clock cannot decide on its own that someone else's lease has expired. Give the database the durability and backup treatment you would give any system of record, because in a cluster that is precisely what it is.

A minimal connection block in `system.json`:

```json
"Database": {
  "Type": "Postgresql",
  "Hostname": "postgres",
  "Port": 5432,
  "Username": "postgres",
  "Password": "postgres",
  "DatabaseName": "less3",
  "RequireEncryption": false,
  "LogQueries": false
}
```

### Shared storage and the mount rules that matter

Every node writes blobs to `DiskDirectory`, stages in-progress object writes in `TempDirectory`, and stages multipart parts in `PartsDirectory` (which defaults to `TempDirectory/parts`). In a cluster all three must resolve to the same shared storage, mounted at the same absolute path on every node. If node 1 mounts the volume at `/less3` and node 2 mounts it at `/mnt/less3`, the paths recorded in Postgres will not resolve on both nodes and completions will fail. Pick one path and use it everywhere.

The filesystem underneath that mount has to give you close-to-open consistency: once a node finishes writing and closes a file, any other node that opens it afterward must see the complete, final bytes. NFS, SMB/CIFS, and purpose-built cluster filesystems (CephFS, GlusterFS, and the like) all provide this and all work. On NFS in particular, mount with `sync` and a low attribute-cache timeout (`actimeo`) so a reader on a second node does not serve stale cached bytes — Less3 flushes and fsyncs a blob before it commits the metadata row, but the reader still has to actually go to the server rather than trust a stale attribute cache.

One configuration is not supported and will corrupt data: a raw shared block device (a SAN LUN, an iSCSI target, an EBS volume) mounted by more than one node without a cluster filesystem on top. Ordinary filesystems like ext4 or XFS assume a single writer. Mount one on two machines at once and they will each cache and write metadata independently, and the on-disk structure will be destroyed. If you want to share a block device, you must run a real cluster filesystem over it. A single-writer filesystem shared by multiple nodes is not a supported deployment, full stop.

The Docker demo sidesteps all of this by using a single Docker named volume, `less3-data`, mounted at `/less3` in every node container. That is correct because every container runs on one host sharing one volume. It is not a template for a real multi-host cluster — for that you need a network filesystem reachable from every host.

## The settings blocks

Cluster mode adds a `Cluster` block and an `Observability` block to `system.json`, and it puts `PartsDirectory` alongside the existing storage paths. The node config the Docker stack ships (`Docker/system.node.json`) is a complete worked example; the important parts are below.

```json
"Cluster": {
  "Enabled": true,
  "NodeId": null,
  "LockProvider": "Clutch",
  "NodeHeartbeatIntervalMs": 10000,
  "NodeStaleAfterMs": 30000,
  "BucketClientCacheTtlMs": 5000,
  "Lock": {
    "DefaultLeaseMs": 30000,
    "HeartbeatIntervalMs": 10000,
    "MaxHoldMs": 3600000,
    "AcquireTimeoutMs": 15000,
    "WaiterPollMs": 250
  },
  "Clutch": {
    "Endpoint": "http://clutch:8080",
    "AccessKey": "clutch-default-access-key",
    "TenantId": null,
    "RequestTimeoutMs": 15000
  },
  "AuthCache": {
    "Enabled": false,
    "TtlMs": 15000
  }
}
```

`Enabled` turns the cluster on and arms the SQLite startup guard. `NodeId` names this node in logs, metrics labels, lock-holder records, membership rows, and audit trails; leave it `null` and Less3 resolves it from the machine name or the `LESS3_NODE_ID` environment variable, which is how the compose file gives each container a stable identity. `LockProvider` selects the coordination backend — the Docker stack ships with `Clutch` so the bundled lock server, its dashboard, and the "Manage Locks" action are live out of the box; `Postgres` is the in-database provider (no extra service, and the more battle-tested path); and `Local` is the in-process single-node manager. The `Clutch` sub-block is read only when `LockProvider` is `Clutch`: `Endpoint` is the Clutch server's http(s) URL (upgraded to a WebSocket internally), and `AccessKey` is its lock credential — the demo seeds `clutch-default-access-key` on first boot.

The heartbeat and staleness values govern membership: a node writes a heartbeat every `NodeHeartbeatIntervalMs`, and if it goes quiet for longer than `NodeStaleAfterMs` the cluster considers it stale. `BucketClientCacheTtlMs` bounds how long a node may reuse a cached bucket handle before it revalidates the bucket against the database, so a bucket created or reconfigured on one node becomes visible on the others within that window.

The `Lock` sub-block tunes the distributed lock. `DefaultLeaseMs` is how long an acquired lock is valid before it must be renewed; the holder heartbeats every `HeartbeatIntervalMs` to keep it alive. `MaxHoldMs` is a hard ceiling that stops a wedged operation from holding a lock forever. `AcquireTimeoutMs` and `WaiterPollMs` control how long and how often a waiter tries before giving up. The defaults are sized so a normal operation finishes well inside a lease; leave them alone unless a workload with unusually long operations forces the issue.

`AuthCache` is off by default, and that is the conservative choice — with it disabled, every request re-checks credentials and authorization against the database, so a revoked credential stops working immediately. Turn it on only if authentication lookups become a measured bottleneck. When enabled, an authorization decision is cached for at most `TtlMs`, and a credential or role change bumps a control-plane epoch that drops stale entries, so a revocation takes effect within one TTL rather than lingering indefinitely. A cache that can keep a revoked "allow" alive for fifteen seconds is a real, if bounded, tradeoff. Make it on purpose.

The `Observability` block wires up metrics and traces:

```json
"Observability": {
  "Enabled": true,
  "ServiceName": "less3",
  "PrometheusEnabled": true,
  "PrometheusPath": "/metrics",
  "OtlpEnabled": true,
  "OtlpEndpoint": "http://otel-collector:4317",
  "ExportLogs": true
}
```

When `PrometheusEnabled` is set, each node serves a Prometheus scrape endpoint at `PrometheusPath` (`/metrics`) **on its main webserver port** — the same port that serves S3 and REST (8000 in the Docker stack) — which binds all interfaces and is reachable across the network. This endpoint carries Watson's HTTP/server metrics. The `PrometheusHostname`/`PrometheusPort` fields are reserved for an OpenTelemetry in-process listener that is not usable on Linux, so they do not affect where `/metrics` is served. When `OtlpEnabled` is set, the node also pushes its `Less3.*` domain metrics, traces, and logs to the collector at `OtlpEndpoint`. In native single-node mode OTLP export is off; the Docker stack turns it on because it ships the collector to receive it.

Finally, storage points at the shared mount:

```json
"Storage": {
  "StorageType": "Disk",
  "DiskDirectory": "/less3/disk/",
  "TempDirectory": "/less3/temp/",
  "PartsDirectory": "/less3/temp/parts/"
}
```

## Bringing up the cluster

### The Docker path

If you just want a working cluster, the compose file is the fastest route. The compose files reference the published, tagged images (`jchristn77/less3:v4.0.0` and `jchristn77/less3-ui:v4.0.0`), so build and tag them first with the repo-root build scripts, then bring the stack up from the `Docker` directory:

```bash
build-all.bat v4.0.0
cd Docker
docker compose up -d
```

That starts PostgreSQL 17, brings up two nodes (`less3-node1` and `less3-node2`) that share the `less3-data` volume, puts nginx in front on port 8000, and starts the full observability stack: an OpenTelemetry collector, Prometheus, Grafana, Loki, and Tempo. The dashboard comes up on port 3000 pointed at the nginx endpoint. On first boot Less3 seeds the default tenant and bucket, so access key `default` / secret key `default` and admin API key `less3admin` work immediately.

Point an S3 client at `http://localhost:8000` and your requests round-robin across both nodes through nginx. Adding a third node is a matter of copying the `less3-node2` service block, giving it a new `LESS3_NODE_ID`, and mounting the same volume and config.

If you want a Postgres control plane without the scale-out (one durable node, no load balancer) use the single-node overlay instead:

```bash
docker compose -f compose.single.yaml up -d
```

### HTTP ports

The default Docker stack (`compose.yaml`) publishes these host ports. Everything an operator or client touches goes through them; PostgreSQL and the individual node listeners are not published, because nginx fronts the nodes and Prometheus reaches them over the internal Docker network.

| Host port | Service | Purpose |
|---|---|---|
| 8000 | nginx | The single entry point, load-balanced across the nodes: S3 API, Less3 REST API (`/api/v1/...`), admin API (`/admin/...`), `/healthz`, and each node's `/metrics`. Point your S3 client and the dashboard here. |
| 3000 | less3-ui | Less3 dashboard (web UI). |
| 9090 | prometheus | Prometheus UI and query API. |
| 3001 | grafana | Grafana (anonymous admin, no login); the three Less3 dashboards are pre-provisioned. |
| 3100 | loki | Loki log ingest and query API. |
| 3200 | tempo | Tempo trace query API. |
| 4317 | otel-collector | OTLP/gRPC ingest; every node pushes its `Less3.*` metrics, traces, and logs here. |
| 4318 | otel-collector | OTLP/HTTP ingest (same purpose as 4317). |
| 8080 | clutch | Clutch REST + lock WebSocket API. The lock socket is `ws://localhost:8080/v1.0/lock/connect`. |
| 3002 | clutch-ui | Clutch operator dashboard (the Less3 dashboard's "Manage Locks" action links here). |

Ports that stay internal to the Docker network (not published to the host):

| Container port | Service | Purpose |
|---|---|---|
| 5432 | postgres | The PostgreSQL control plane (metadata, lock state, membership). Reached only by the nodes and, if enabled, Clutch. |
| 8000 | less3-node1 / less3-node2 | Each node's S3/REST listener and its `/metrics` endpoint. nginx balances across them; Prometheus scrapes `less3-nodeN:8000/metrics` directly. |
| 8889 | otel-collector | The collector's Prometheus exporter, scraped by Prometheus for the `Less3.*` metrics. |

The single-node overlay (`compose.single.yaml`) publishes only **8000** (the Less3 server directly, no load balancer) and **3000** (the dashboard); PostgreSQL stays internal.

### The manual path: N processes behind nginx

Nothing about the cluster requires Docker. A cluster is just several Less3 processes that share a database and a storage mount, with a load balancer in front. To run it by hand:

Give every node the same `system.json` with `Cluster.Enabled` true, `LockProvider` set to `Postgres`, the same Postgres connection, and `DiskDirectory`/`TempDirectory`/`PartsDirectory` all pointing at the shared mount. The only thing that differs between nodes is identity — set `LESS3_NODE_ID` (or `Cluster.NodeId`) to something unique per node so logs, metrics, and lock records attribute correctly.

Mount the shared filesystem at the same absolute path on every host. Verify it independently of Less3 before you start anything: write a file from one node, read it from another, confirm the bytes match. If that round trip does not work at the filesystem level, Less3 cannot make it work.

Start the nodes. Each one registers in the `less3_node` membership table on boot and begins heartbeating. The first node to reach the schema step takes the advisory lock and runs migrations; the rest wait and then proceed.

Put a load balancer in front. nginx is what the Docker stack uses and what the next section assumes, but any balancer works as long as it follows the same retry discipline. The nodes are interchangeable — any node serves any request — so you do not need sticky sessions. Sessions are stored in the database, not in node memory, so a client can land on a different node on every request without noticing.

## Why the load balancer must not retry writes

The nginx config in `Docker/nginx/nginx.conf` balances with `least_conn` and, more importantly, retries only connection-level failures:

```nginx
proxy_next_upstream error timeout;
proxy_next_upstream_tries 2;
```

Because `non_idempotent` is not in that list, nginx will replay a request on a second node only when it never got a response — a connection error or timeout on a request it can safely assume was not processed. It will not replay a POST, PUT, or DELETE that may have already run. That restraint is the whole point. Picture a PUT that reaches node 1, commits the object, and then the response is lost to a network hiccup. If the balancer resent that PUT to node 2, you would get a duplicated write or a second version nobody asked for. By refusing to retry non-idempotent methods, nginx guarantees that a mutation runs on at most one node. A GET or HEAD, which changes nothing, is safe to retry, and those still are.

If you bring your own balancer, carry this rule across. Retrying idempotent reads is fine and helpful. Retrying writes across nodes is a data-integrity bug waiting to happen.

## Confirming the cluster is healthy

Every node answers an unauthenticated `GET /healthz` that returns its status, node id, and version:

```bash
curl http://localhost:8000/healthz
```

```json
{ "status": "healthy", "nodeId": "less3-node1", "version": "4.0.0" }
```

The probe reflects whether the node can reach the database and write to storage, which is why the compose file uses it for container health checks and startup ordering, and why it is the right target for any orchestrator or external load-balancer probe. It requires no credentials precisely so that a balancer can call it without secrets.

For a real picture of the cluster, the admin REST endpoints under `/api/v1/cluster` need the admin key or an admin session token:

```bash
curl -H "x-api-key: less3admin" http://localhost:8000/api/v1/cluster/nodes
curl -H "x-api-key: less3admin" http://localhost:8000/api/v1/cluster/health
curl -H "x-api-key: less3admin" http://localhost:8000/api/v1/cluster/leader
```

`cluster/nodes` lists every registered node with its health, version, and last-seen time, so you can see at a glance whether a node has gone stale. `cluster/health` aggregates that into one answer for a dashboard or an alerting probe. `cluster/leader` names the node currently holding the `cluster:cleanup` lease — the one running singleton background work — which is useful when you are trying to figure out where cleanup is actually happening.

The lock endpoints let you watch coordination live:

```bash
curl -H "x-api-key: less3admin" http://localhost:8000/api/v1/locks
curl -H "x-api-key: less3admin" http://localhost:8000/api/v1/locks/{key}
```

Under a load test you will see write locks appear and clear as object mutations pass through. A lock that never clears is a signal worth chasing.

## Observability and the dashboards

Each node instruments itself with plain base-class-library meters under `Less3.*` names — storage throughput, lock activity, fencing conflicts, cache behavior, multipart progress, cleanup passes — plus the Watson webserver's own `http.server.*` request-rate and latency metrics. The two sets take different reachable paths: Watson serves its HTTP metrics directly at `/metrics` on each node's main port, and a Radiant telemetry host pushes the `Less3.*` metrics (and traces and logs) over OTLP to the collector. Every series carries the node id, so per-node breakdowns and whole-cluster rollups are both available.

The Docker stack turns this into something you can watch without any setup. Prometheus (port 9090) scrapes each node's `/metrics` on its main port for the HTTP metrics and scrapes the collector for the `Less3.*` metrics, so there is no double counting. Grafana (port 3001, anonymous admin login, no password) comes up with three dashboards already provisioned from `Docker/grafana/dashboards`:

- **Less3 — Overview**: request rate, latency percentiles by operation, error rate, and a per-node breakdown.
- **Less3 — Locks & Data Integrity**: lock acquisitions, waits, denials, lease expirations, hold durations, and a fencing-conflict counter that should sit at zero. A nonzero fencing-conflict count is not noise — it is the cluster catching a stale lock holder before it could commit, and a sustained spike is the alarm you actually care about.
- **Less3 — Cluster**: node up/down state, versions, and which node holds the cleanup lease.

Loki (port 3100) collects logs and Tempo (port 3200) collects traces, both wired into Grafana with trace-to-log correlation, so you can pivot from a slow request span to the exact log lines that request produced. The existing `SyslogLogging` output stays in place; the OTLP pipeline runs alongside it rather than replacing it.

## The Clutch lock provider (Docker default)

The Docker stack routes Less3's locking through the bundled Clutch lock server by default, so the Clutch server, its dashboard, and the Less3 dashboard's "Manage Locks" action are all live the moment you `docker compose up`. Each node opens one persistent lock WebSocket to Clutch, which shows up as two connections on the "Less3 — Clutch Lock Server" Grafana board once both nodes have served a lock. The relevant `system.node.json` block:

```json
"Cluster": {
  "LockProvider": "Clutch",
  "Clutch": {
    "Endpoint": "http://clutch:8080",
    "AccessKey": "clutch-default-access-key",
    "TenantId": null
  }
}
```

Less3 talks to Clutch over its native WebSocket lock protocol (one persistent connection per node, so every lock the node holds is released automatically if the socket drops). There is no third-party SDK pulled into the default build. `Endpoint` is the Clutch server's http(s) URL, upgraded to a WebSocket internally; `AccessKey` is its lock credential — the demo seeds `clutch-default-access-key` on first boot. The connection is established lazily on a node's first lock, so an idle node shows no connection until it serves one. Clutch's dashboard is at `http://localhost:3002` (also reachable from the Less3 dashboard's "Manage Locks" action).

The Clutch server shares the same PostgreSQL database through bring-your-own-database, so even with this provider the database remains the single authority for lock state and fencing tokens. What changes is the front door to that authority, not the authority itself.

Clutch is alpha (v0.2.0). It passes the same coordination tests as the native provider, but it has less mileage, and it adds a service to run. For a leaner deployment — or a more battle-tested one — set `LockProvider` to `Postgres` instead: locking then runs entirely inside the database with no extra service, and the bundled Clutch server simply goes unused. Both providers preserve the same data-integrity guarantees; pick based on whether you want Clutch's centralized lock console or the minimal-dependency in-database path.

## How the cluster protects your data

The reason a Less3 cluster can scale out without corrupting objects comes down to two mechanisms working together: one authority per kind of truth, and a lock that carries a fencing token.

Start with the authority rule. Object and bucket metadata live in exactly one place — Postgres — and are never cached with a lifetime that could hand a stale answer to an operation that is about to mutate. Lock state lives in Postgres. Cluster membership lives in Postgres. Blobs live on shared storage, addressed by an immutable object id, and every write produces a new id; a blob is never overwritten in place. Because there is only ever one place that holds the real answer, two nodes cannot hold two different truths and then disagree.

Now the lock. Every read-modify-write on object state — incrementing a version, overwriting an unversioned object, deleting, completing or aborting a multipart upload — runs while the node holds an exclusive distributed lock on that object's key. Acquiring the lock is a single serialized transaction in Postgres: the node selects the lock row for update, checks it against current holders and lease expiry using the database clock, and if it wins, it is handed a fencing token — a per-key integer that only ever increases. When the node finally commits its change, the guarded write re-checks that token against what the database expects. A stale token cannot commit.

Follow that through the ugly case, because the ugly case is the whole reason the mechanism exists. A node acquires the lock and starts a long write. Something stalls it — a garbage-collection pause, a slow disk, a network partition to the database — long enough that its lease lapses. Postgres, computing expiry on its own clock, now considers the lock free, and a second node acquires it. That second acquisition bumps the fencing token. Two nodes now believe they hold the lock, which sounds exactly like split-brain. It is not, and here is why: the first node's commit carries the old, smaller token, and the database rejects any write that does not present the current token. The first node's mutation is refused at the last step. It fails its one request. It corrupts nothing. The second node, holding the current token, proceeds cleanly. The fencing-conflict counter ticks up by one — the cluster recording that it caught exactly the situation it was built to catch.

Split-brain requires two actors to both believe they are authoritative and both commit conflicting changes. In a Less3 cluster there is only one authority — the database — and it hands out a single monotonic token per key and refuses every write that does not match. Two nodes can briefly believe they hold a lock; only one can ever commit under it. Kill a node in the middle of a write and its lease simply lapses, its in-flight commit is fenced out if it ever wakes up, and another node picks up the work. The failure is bounded to a single request. Your data is not.

## Scaling and draining nodes

Adding a node is undramatic. Bring up another process with the same database connection, the same shared mount, and a unique node id, and put it in the balancer's upstream pool. It registers itself, starts heartbeating, and begins taking traffic. Because there is no per-node durable state, a new node needs no data migration and no warm-up beyond building its bucket-client cache on demand.

Removing a node is a matter of draining it first. Take it out of the balancer's upstream so no new requests arrive, let the in-flight requests finish, then stop the process. Any lock it still holds is released on a clean shutdown; if the process dies without releasing, the lease lapses on its own and another node reclaims the key. Either way, nothing is stranded. If the node happened to hold the `cluster:cleanup` lease, another node wins that lease at the next attempt and singleton work continues elsewhere.

There is no minimum node count the cluster enforces, but common sense applies: run at least two if surviving a node loss is the reason you built a cluster in the first place, and keep an odd sense of headroom so draining one for maintenance does not leave you at capacity.

## When something is wrong

A node that refuses to start and logs a fatal error about SQLite in cluster mode is the startup guard doing its job. `Cluster.Enabled` is true but the database is still SQLite; point it at Postgres.

A node that starts but never appears healthy usually cannot reach the database or cannot write to the shared mount. `/healthz` reflects both, so a node that answers `/healthz` at all has at least a listening web server; check its logs for the connection or path error. If the mount is the problem, verify it by hand — write from one node, read from another — before blaming Less3.

Multipart completions or overwrites that fail across nodes almost always trace back to the storage mount: different paths on different nodes, or a filesystem that is not giving you close-to-open consistency. Confirm `DiskDirectory`, `TempDirectory`, and `PartsDirectory` resolve to the same shared location on every node, and confirm the mount options match the recommendations above.

A climbing fencing-conflict count on the Locks & Data Integrity dashboard means leases are lapsing while holders still think they are working. A few under chaos testing are expected and healthy — that is the fence working. A sustained climb under normal load means operations are routinely outrunning the lease; either the workload has genuinely long operations that warrant a larger `DefaultLeaseMs`, or a node is struggling (GC pressure, slow storage, a flaky database link) and should be investigated rather than tuned around.

If cleanup does not seem to be running, check `/api/v1/cluster/leader` to see which node holds the lease. Only the leader runs cleanup and schema migration; the others deliberately skip it. If the named leader is unhealthy, the lease will move once it goes stale.

Stand the cluster up, watch the Locks & Data Integrity board hold at zero conflicts through a real load test, then kill a node mid-write and watch another node finish the work while that board ticks up by one and stops. That single observation — a bounded failure, a fenced-out zombie, no corruption — is the guarantee the whole architecture exists to make, and it is the thing worth verifying before you trust it with anything you care about.
