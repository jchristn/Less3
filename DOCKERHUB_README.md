![Less3 logo](https://raw.githubusercontent.com/jchristn/less3/main/assets/logo.png)

# Less3

Less3 is an S3-compatible object storage server for local development, private deployments, and environments where you need S3-style APIs without handing storage placement to a public cloud.

The v3 release adds the tenant and RBAC foundation: tenant-aware buckets, users, credentials, sessions, roles, permissions, role assignments, authorization audit, request history, reporting, and dashboard management pages. Public identifiers use PrettyId K-sortable strings with stable prefixes instead of GUID contracts.

![Less3 architecture](https://raw.githubusercontent.com/jchristn/less3/main/assets/diagram.png)

## Images

- `jchristn77/less3:v3.0.0` - Less3 server
- `jchristn77/less3-ui:v3.0.0` - Less3 dashboard

## Quick Start

The repository ships a Docker Compose file in `Docker/compose.yaml` that starts the server on port `8000` and the dashboard on port `3000`.

```bash
git clone https://github.com/jchristn/less3
cd less3/Docker
docker compose -f compose.yaml up --build
```

Open the dashboard at `http://localhost:3000`, point it at `http://localhost:8000`, and sign in with the configured admin API key. The default container bootstrap creates tenant `default`, user `admin@less3`, password `password`, and an S3 credential with access key `default` and secret key `default`.

## Server Configuration

Less3 reads `system.json` from `/app/system.json`. The compose file mounts the sample configuration from `Docker/system.json` and persists data under these directories:

- `/app/db`
- `/app/disk`
- `/app/logs`
- `/app/temp`

Key settings:

```json
{
  "Webserver": {
    "Hostname": "localhost",
    "Port": 8000
  },
  "Database": {
    "Type": "Sqlite",
    "Filename": "./db/less3.db"
  },
  "Storage": {
    "DiskDirectory": "./disk/",
    "TempDirectory": "./temp/"
  },
  "AdminApiKey": "less3admin",
  "ValidateSignatures": true
}
```

SQLite is the default. The server code includes provider initialization paths for SQLite, MySQL, PostgreSQL, and SQL Server.

## Dashboard

The dashboard is a compact operator console for buckets, objects, tenants, users, credentials, RBAC, request history, maintenance, and the API Explorer. Set the server URL with `LESS3_SERVER_URL` when building or running the dashboard image.

## S3 Compatibility

Less3 is designed for AWS SDKs, AWS CLI, MinIO Client, and direct S3-compatible HTTP calls. Supported APIs include bucket create/delete/list, object put/get/head/delete, range reads, tags, ACLs, versioning, multipart upload, and request history capture.

Path-style URLs work out of the box:

```text
http://localhost:8000/my-bucket/path/to/object.txt
```

Virtual-hosted style can be enabled with `BaseDomain` and a wildcard listener when DNS and OS privileges are available.

## More Information

Source, issue tracking, API notes, and release notes are available at:

- https://github.com/jchristn/less3
- https://github.com/jchristn/less3/blob/main/README.md
- https://github.com/jchristn/less3/blob/main/CHANGELOG.md
