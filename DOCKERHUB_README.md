![Less3 logo](https://raw.githubusercontent.com/jchristn/less3/main/assets/logo.png)

# Less3

Less3 is an S3-compatible object storage server for local development, private deployments, and environments where you need S3-style APIs without handing storage placement to a public cloud. As of v4.0.0 the same image runs two ways: as a single durable node, or as a multi-node scale-out cluster.

![Less3 architecture](https://raw.githubusercontent.com/jchristn/less3/main/assets/diagram.png)

## Two Ways to Run

**Single node on Postgres** is the simple option: one Less3 container, one PostgreSQL control plane, local-style storage on a mounted volume. No load balancer, no coordination overhead.

**Multi-node cluster** is the Docker default. Several Less3 containers sit behind an nginx load balancer, all backed by one PostgreSQL control plane and one shared storage volume, coordinating every object mutation through a native Postgres-backed distributed lock. The cluster keeps one authority for each kind of truth — Postgres for metadata, lock state, and membership; shared storage for immutable blobs that are never overwritten in place. Every read-modify-write on an object runs under an exclusive distributed lock and a fencing token re-checked before the database commit, so a lease that lapses mid-operation is fenced out at commit rather than allowed to corrupt data. Cluster mode requires Postgres and refuses to start on SQLite. The full operator guide is [`MULTINODE_SETUP.md`](https://github.com/jchristn/less3/blob/main/MULTINODE_SETUP.md).

## Images

- `jchristn77/less3:v4.0.0` - Less3 server
- `jchristn77/less3-ui:v4.0.0` - Less3 dashboard

## Quick Start (multi-node cluster)

The repository ships a Docker Compose file in `Docker/compose.yaml` that stands up the full cluster: PostgreSQL, two Less3 nodes sharing a storage volume, nginx on port `8000`, the dashboard on port `3000`, and the observability stack.

```bash
git clone https://github.com/jchristn/less3
cd less3/Docker
docker compose up -d --build
```

Point an S3 client at `http://localhost:8000` and requests round-robin across both nodes. Open the dashboard at `http://localhost:3000`, point it at `http://localhost:8000`, and sign in with the admin API key. The default bootstrap creates tenant `default`, user `admin@less3`, password `password`, and an S3 credential with access key `default` and secret key `default`. The admin API key is `less3admin`.

For one durable node on Postgres without the load balancer:

```bash
docker compose -f compose.single.yaml up -d --build
```

## Cluster Configuration

Cluster nodes read `system.node.json`, which enables the cluster, selects the lock provider (Clutch by default in the Docker stack; `Postgres` for in-database locking with no extra service), and points storage at the shared `/less3` mount. The essential blocks:

```json
{
  "Database": {
    "Type": "Postgresql",
    "Hostname": "postgres",
    "Port": 5432,
    "Username": "postgres",
    "Password": "postgres",
    "DatabaseName": "less3"
  },
  "Cluster": {
    "Enabled": true,
    "NodeId": null,
    "LockProvider": "Clutch",
    "Clutch": {
      "Endpoint": "http://clutch:8080",
      "AccessKey": "clutch-default-access-key"
    }
  },
  "Storage": {
    "DiskDirectory": "/less3/disk/",
    "TempDirectory": "/less3/temp/",
    "PartsDirectory": "/less3/temp/parts/"
  }
}
```

Every node must mount the shared storage at the same absolute path — in the demo, a single Docker named volume (`less3-data`) at `/less3`. A real multi-host cluster needs a network filesystem (NFS, SMB/CIFS, or a cluster filesystem) reachable from every host at the same path; a raw shared block device without a cluster filesystem is not supported. `NodeId` is left `null` and resolved from `LESS3_NODE_ID`, which the compose file sets per node.

## Observability

Each node exposes Watson's native Prometheus `/metrics` endpoint on its main port (scraped directly for the `http.server.*` metrics) and pushes its `Less3.*` domain metrics and traces to the bundled OpenTelemetry collector over OTLP; every S3, REST, and admin API is metered, and object operations record per-stage timings. Application logs are bridged from SyslogLogging through the same OTLP pipeline into Loki, so each node's logs are queryable in Grafana. The Docker stack ships Prometheus (`9090`), Grafana (`3001`, anonymous admin), Loki (`3100`), Tempo (`3200`), and the Clutch lock server (whose own metrics are scraped too). Grafana comes up with six dashboards already provisioned: "Less3 — Overview", "Less3 — Locks & Data Integrity" (whose fencing-conflict count should stay at zero), "Less3 — Cluster", "Less3 — API Operations", "Less3 — Clutch Lock Server", and "Less3 — Logs".

## Health and Cluster Endpoints

Every node answers an unauthenticated `GET /healthz` returning `{status, nodeId, version}`, used for container health checks and load-balancer probes. Admin-gated endpoints expose cluster state:

```bash
curl -H "x-api-key: less3admin" http://localhost:8000/api/v1/cluster/nodes
curl -H "x-api-key: less3admin" http://localhost:8000/api/v1/cluster/health
curl -H "x-api-key: less3admin" http://localhost:8000/api/v1/cluster/leader
curl -H "x-api-key: less3admin" http://localhost:8000/api/v1/locks
```

## Dashboard

The dashboard is a compact operator console for buckets, objects, tenants, users, credentials, RBAC, request history, maintenance, and the API Explorer. Set the server URL with `LESS3_SERVER_URL` when building or running the dashboard image; in the cluster it points at the nginx front end.

## S3 Compatibility

Less3 is designed for AWS SDKs, AWS CLI, MinIO Client, and direct S3-compatible HTTP calls. Supported APIs include bucket create/delete/list, object put/get/head/delete, range reads, tags, ACLs, versioning, multipart upload, and request history capture. Path-style URLs work out of the box:

```text
http://localhost:8000/my-bucket/path/to/object.txt
```

Virtual-hosted style can be enabled with `BaseDomain` and a wildcard listener when DNS and OS privileges are available.

## More Information

- https://github.com/jchristn/less3
- https://github.com/jchristn/less3/blob/main/README.md
- https://github.com/jchristn/less3/blob/main/MULTINODE_SETUP.md
- https://github.com/jchristn/less3/blob/main/CHANGELOG.md
