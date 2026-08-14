# Migrating Less3 v3 to v4

The good news for most people upgrading from v3 is that there is almost nothing to do. Less3 v4.0.0 targets `net10.0` and introduces the multi-node cluster, but a standalone single-node deployment behaves exactly as it did in v3 — same SQLite database, same local storage, same setup wizard, same on-disk layout. The cluster is opt-in. If you are running one node on SQLite and you want to keep running one node on SQLite, you upgrade the binary and move on.

The larger task, moving a single node onto a PostgreSQL cluster, is a real project rather than a switch you flip, and there is no automatic tool that does it for you. That path is described honestly below: what it involves, and the manual steps to get there.

## Staying single-node

A v3 single-node install carries forward with no schema change and no configuration change beyond the version of the binary. The v4 code reads your existing `system.json` and your existing `less3.db`, applies any pending schema migrations on first boot the same way v3 did, and comes up single-node with the in-process lock manager. You do not need a `Cluster` block; its absence (or `Cluster.Enabled` set to false) is the single-node default.

Back up your database file and your storage directory before the upgrade, as you would before any version bump. Then replace the binary — or pull the new image, or rebuild from source — and start it. The setup wizard still defaults to SQLite, so a fresh native install is unchanged too. If you were happy on v3, you will not notice v4 until you decide you want a cluster.

One thing to know even if you never build a cluster: v4 will refuse to start in cluster mode on SQLite. That guard only fires when `Cluster.Enabled` is true, so it will never surprise a single-node user. It exists so that nobody accidentally points two processes at one SQLite file, which corrupts it.

## Moving a single node onto a Postgres cluster

Going from one SQLite node to a Postgres-backed cluster changes all three of the things a cluster relocates: the database, the storage, and the locking. There is no export-and-import utility that carries a live v3 SQLite dataset into a clustered Postgres deployment for you. Plan the move as a deliberate migration with a maintenance window, not an in-place flip.

Before you begin, read `MULTINODE_SETUP.md` end to end. It covers the mount rules, the settings blocks, and the data-integrity model in the depth this guide assumes. What follows is the ordered path from a working v3 node to a working v4 cluster.

**Stand up PostgreSQL first, and prove the nodes can reach it.** Create the database and a role that owns it. Do not pre-load a schema — Less3 creates its own tables, including the lock and membership tables, on first boot, and serializes that creation across nodes with an advisory lock so a simultaneous boot migrates exactly once. Confirm connectivity from every host that will run a node before you go further.

**Relocate storage to a shared mount.** Object blobs, object-write staging, and multipart parts all have to live on storage that every node mounts at the same absolute path. Choose NFS, SMB/CIFS, or a cluster filesystem — the setup guide explains the close-to-open consistency requirement and the mount options that make NFS behave. Copy your v3 object data from the old local `DiskDirectory` onto the new shared volume, preserving the layout, so the blobs your metadata references are actually present at the new location. A shared block device without a cluster filesystem on top is not supported and will corrupt data; do not take that shortcut.

**Move the metadata into Postgres.** This is the step with no turnkey tool. Your bucket, object, credential, tenant, and RBAC rows live in SQLite and need to exist in the new Postgres database, with the blob references still pointing at objects that now sit on the shared mount. For a small dataset the pragmatic path is to recreate it against the running Postgres node through the admin and S3 APIs — create the tenants, credentials, and buckets, then re-put the objects — which sidesteps any schema-shape mismatch between the two engines entirely. For a larger dataset you are looking at an offline data-transfer script that reads the SQLite tables and writes the Postgres equivalents; treat identifier formats and per-engine column types carefully, and validate a copy before you cut over. Whichever route you take, verify object reads against the new deployment before you retire the old one.

**Write the cluster configuration.** Give every node a `system.json` with the Postgres connection, `Cluster.Enabled` set to true, `LockProvider` set to `Postgres`, `Observability` configured for your metrics stack, and `DiskDirectory`/`TempDirectory`/`PartsDirectory` all pointing at the shared mount. The only per-node difference is identity — set `LESS3_NODE_ID` (or `Cluster.NodeId`) to something unique so logs, metrics, and lock records attribute correctly. `Docker/system.node.json` is a complete example to copy from.

**Bring up the nodes behind a load balancer.** Start the nodes; each registers in the membership table and begins heartbeating, and the first to reach the schema step runs migrations while the others wait. Put nginx (or an equivalent) in front, and carry over the one rule that matters: retry idempotent reads if you like, but never replay a POST, PUT, or DELETE across nodes, or a timed-out write can be duplicated. The setup guide has the exact nginx directives.

**Verify, then cut over.** Confirm every node reports healthy on `/healthz`, that `GET /api/v1/cluster/nodes` shows the full membership, and that an S3 client pointed at the balancer can read the objects you migrated. Watch the Locks & Data Integrity dashboard hold at zero fencing conflicts under a load test. Only once reads and writes check out against the cluster should you redirect production traffic and retire the old single-node install.

## What does not change

The S3 API surface is identical. Every bucket and object operation that worked against a v3 node works against a v4 cluster with the same request and response shapes; the only difference is internal — mutations now pass through the distributed lock. The admin REST API keeps its v3 endpoints and gains the cluster and lock endpoints under `/api/v1/cluster/*` and `/api/v1/locks`. Your existing clients, scripts, and SDK integrations do not need changes to talk to a cluster; they point at the load-balancer endpoint instead of a single node and otherwise behave the same.

If the cluster is more than you need, the honest recommendation is to stay single-node. The upgrade to v4 costs you nothing there, and the cluster will still be waiting the day one node genuinely stops being enough.
