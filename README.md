<img src="assets/heart.png" alt="Less3 logo" width="128" height="128">

# Less3 :: S3-Compatible Object Storage

Less3 is an S3-compatible object storage platform that you can run anywhere. 

![alt tag](https://github.com/jchristn/less3/blob/main/assets/diagram.png)

## Use Cases

Core use cases for Less3:

- Local object storage - S3-compatible storage on your laptop, virtual machine, container, or bare metal
- Private cloud object storage - use your existing private cloud hardware to create an S3-compatible storage pool
- Development and test - local devtest against S3-compatible storage
- Remote storage - deploy S3-compatible storage in environments where you must control data placement

## Current Version

v3.0.0

- Added the v3 tenant and RBAC foundation with tenant, role, permission, assignment, session, and authorization audit models
- Switched new identifier generation to PrettyID K-sortable string IDs with stable prefixes and a 32-character maximum
- Added tenant-aware schema initialization hooks and indexes for SQLite, MySQL, PostgreSQL, and SQL Server
- Added credential secret-once create/rotate flows, direct credential session login, and RBAC-authorized admin session tokens
- Added admin reporting, maintenance, and effective-permission inspection APIs
- Added v3 migration guidance, S3 API notes, Less3 REST API notes, and a shared Touchstone test descriptor baseline
- Added dashboard navigation and management pages for tenants, credentials, roles, permissions, reporting KPIs, and maintenance
- See `CHANGELOG.md` for release details

<details>
<summary><strong>Screenshots</strong></summary>

<details>
<summary>Dashboard home</summary>

The dashboard home summarizes tenant, bucket, object, storage, credential, request, failure-rate, and latency metrics, with quick actions and a request summary chart.

<a href="assets/ss1.png"><img src="assets/ss1.png" alt="Dashboard home with metrics, quick actions, and a request summary chart"></a>

</details>

<details>
<summary>Object details</summary>

The object browser exposes bucket contents and object metadata, including identifiers, size, storage class, delete marker state, and download URL.

<a href="assets/ss2.png"><img src="assets/ss2.png" alt="Object details modal showing metadata for README.md"></a>

</details>

<details>
<summary>Object contents</summary>

The object contents view lets users inspect and edit text objects directly from the dashboard.

<a href="assets/ss3.png"><img src="assets/ss3.png" alt="Object contents modal showing README.md text"></a>

</details>

<details>
<summary>Request history detail</summary>

Request history detail shows routing, tenant, status, timing, authentication, metadata, and request/response bodies, with cURL export support.

<a href="assets/ss4.png"><img src="assets/ss4.png" alt="Request detail view with request and response payload panels"></a>

</details>

<details>
<summary>Maintenance settings</summary>

Maintenance settings centralize core server, database, webserver, and IO configuration with restart-required indicators.

<a href="assets/ss5.png"><img src="assets/ss5.png" alt="Maintenance settings page with core, database, webserver, and IO fields"></a>

</details>

<details>
<summary>API explorer</summary>

The API explorer builds authenticated admin and S3 requests, sends them from the dashboard, and displays body, headers, cURL, and example responses.

<a href="assets/ss6.png"><img src="assets/ss6.png" alt="API explorer showing a list buckets request and JSON response"></a>

</details>

</details>

## Help and Feedback

First things first - do you need help or have feedback?  Please file an issue here.

## Special Thanks

Thanks to @iain-cyborn for helping make the platform better!

## Initial Setup

### Prerequisites

- .NET 8.0 SDK or runtime
- Supported databases: SQLite (default), SQL Server, MySQL, or PostgreSQL

### Quick Start

Clone, build, and run Less3:

```bash
git clone https://github.com/jchristn/less3
cd less3
dotnet build src/Less3.sln
cd src/Less3
dotnet run
```

On first launch, Less3 will run a setup wizard that creates:
- `system.json` - Server configuration
- `less3.db` - SQLite database (default)
- A sample "default" bucket with test files

To re-run the setup wizard at any time:
```bash
dotnet run setup
```

### Starting the Dashboard

Less3 includes a web-based dashboard for managing buckets, objects, tenants, users, credentials, RBAC, request history, and maintenance. After starting the Less3 server, you can start the dashboard:

```bash
cd dashboard
npm install
npm run build
npm run start
```

The dashboard will be available at `http://localhost:3000`.

By default, the dashboard expects the Less3 server to be available at `http://localhost:8000` and validates that the configured endpoint exposes the Less3 admin API before saving it.

For development, you can use:
```bash
npm run dev
```

**Note**: The dashboard requires Node.js v18.20.4 or later.

### Publishing for Deployment

```bash
dotnet publish src/Less3/Less3.csproj -c Release -o ./publish
cd publish
dotnet Less3.dll
```

### Configuration Requirements

**Webserver.Hostname**: MUST be set to a DNS hostname. IP addresses are not supported (parsing will fail). Incoming HTTP requests must have a HOST header matching this value, or you will receive `400/Bad Request`.

**Wildcard Listeners**: Using `*`, `+`, or `0.0.0.0` for `Webserver.Hostname` requires administrative/root privileges (OS requirement).

### Key Configuration Settings (system.json)

```json
{
  "Webserver": {
    "Hostname": "localhost",
    "Port": 8000
  },
  "BaseDomain": null,
  "Storage": {
    "DiskDirectory": "./disk/",
    "TempDirectory": "./temp/"
  },
  "Database": {
    "Type": "Sqlite",
    "Filename": "./less3.db"
  },
  "AdminApiKey": "less3admin",
  "ValidateSignatures": true,
  "RequestHistoryRetentionDays": 30,
  "CleanupIntervalMs": 3600000,
  "UseTcpServer": false
}
```

## S3 Client Compatibility

Less3 was designed to be consumed using the AWS SDK, AWS CLI, MinIO Client (mc), or direct RESTful integration in accordance with Amazon's official S3 API documentation (https://docs.aws.amazon.com/AmazonS3/latest/API/Welcome.html).

### Tested and Compatible Clients

- **AWS SDK** (C#, Python, Java, etc.)
- **AWS CLI** - See `AWSCLI.md` for comprehensive testing commands
- **MinIO Client (mc)** - See `MINIO.md` for comprehensive testing commands
- **CloudBerry Explorer for S3** (https://www.cloudberrylab.com/explorer/windows/amazon-s3.aspx)
- **S3 Browser** (http://s3browser.com/)

Should you encounter a discrepancy between how Less3 operates and how AWS S3 operates, please file an issue with details and supporting AWS documentation.

## Supported S3 APIs

Less3 implements the following AWS S3 APIs. For a complete compatibility matrix, refer to the 'assets' directory.

### Service APIs
- **ListBuckets** - List all buckets

### Bucket APIs
- **CreateBucket** (Write) - Create a new bucket
- **DeleteBucket** (Delete) - Delete an empty bucket
- **HeadBucket** (Exists) - Check if bucket exists
- **ListObjectsV2** (Read) - List objects in a bucket
- **ListObjectVersions** (ReadVersions) - List object versions
- **GetBucketAcl** (ReadAcl) - Get bucket access control list
- **PutBucketAcl** (WriteAcl) - Set bucket access control list
- **GetBucketTagging** (ReadTagging) - Get bucket tags
- **PutBucketTagging** (WriteTagging) - Set bucket tags
- **DeleteBucketTagging** (DeleteTagging) - Delete bucket tags
- **GetBucketVersioning** (ReadVersioning) - Get bucket versioning configuration
- **PutBucketVersioning** (WriteVersioning) - Set bucket versioning (no MFA delete support)
- **GetBucketLocation** (ReadLocation) - Get bucket location/region
- **ListMultipartUploads** (ReadMultipartUploads) - List in-progress multipart uploads

### Object APIs
- **PutObject** (Write) - Upload an object
- **GetObject** (Read) - Download an object
- **HeadObject** (Exists) - Check if object exists
- **DeleteObject** (Delete) - Delete an object or version
- **DeleteObjects** (DeleteMultiple) - Delete multiple objects
- **GetObjectAcl** (ReadAcl) - Get object access control list
- **PutObjectAcl** (WriteAcl) - Set object access control list
- **GetObjectTagging** (ReadTagging) - Get object tags
- **PutObjectTagging** (WriteTagging) - Set object tags
- **DeleteObjectTagging** (DeleteTagging) - Delete object tags
- **GetObject with Range** (ReadRange) - Download partial object content

### Multipart Upload APIs
- **CreateMultipartUpload** (InitiateMultipartUpload) - Start a multipart upload
- **UploadPart** - Upload a part of a multipart upload
- **CompleteMultipartUpload** - Finalize a multipart upload
- **AbortMultipartUpload** - Cancel a multipart upload
- **ListParts** (ReadParts) - List parts of a multipart upload

## Implementation Notes

Less3 aims to faithfully implement S3 API behavior. However, there are a few minor differences that should be inconsequential for most use cases:

- **Version IDs**: Stored as integers internally rather than opaque strings (e.g., `1`, `2`, `3` instead of AWS-style strings)
- **Region**: Defaults to `us-west-1` (configurable via `RegionString` in system.json)
- **Signature Validation**: Can be enabled/disabled via `ValidateSignatures` setting (enabled by default)

If you encounter incompatibilities or unexpected behavior with supported APIs, please file an issue with:
- Description of the expected behavior
- Link to AWS S3 documentation
- Steps to reproduce the issue

## URL Styles: Path-Style vs Virtual Hosted

Less3 supports both S3 URL styles for accessing buckets and objects:

### Path-Style URLs (Default)
- **Format**: `http://hostname:port/bucket/key`
- **Configuration**: Do NOT set `BaseDomain` in system.json (leave it null)
- **Example**: `http://localhost:8000/mybucket/myfile.txt`
- **Use Case**: Simple setup, local development, no DNS configuration needed

### Virtual Hosted-Style URLs
- **Format**: `http://bucket.hostname:port/key`
- **Configuration Requirements**:
  1. Set `BaseDomain` to your base domain (e.g., `.localhost` - note the leading period)
  2. Set `Webserver.Hostname` to `*` (wildcard listener)
  3. Run Less3 with administrative/root privileges
  4. Ensure DNS resolves bucket subdomains to your Less3 server (e.g., `mybucket.localhost`)
- **Example**: `http://mybucket.localhost:8000/myfile.txt`
- **Use Case**: Production environments, AWS S3-like URL structure

**Configuration Example (system.json for virtual hosted-style)**:
```json
{
  "BaseDomain": ".localhost",
  "Webserver": {
    "Hostname": "*",
    "Port": 8000
  }
}
```

## Administrative APIs

Less3 provides REST APIs for administrative operations such as managing users, credentials, and buckets.

### Authentication
Admin APIs accept either the `x-api-key` header with a value matching `AdminApiKey` in system.json (default: `less3admin`) or an RBAC-authorized `x-less3-session-token` header.

### Endpoint Format
```
http://hostname:port/admin/{resource}/{operation}
```

### Available Resources
- **users** - Manage user accounts
- **credentials** - Manage access keys and secret keys
- **buckets** - Manage buckets and bucket configuration
- **stats** - Retrieve aggregate bucket, object, and storage metrics for dashboard and admin views
- **reports** - Retrieve request reporting summaries including request rate, failure rate, latency, and top usage fields
- **maintenance** - Inspect cleanup status, update runtime maintenance settings, purge request history, clean temp uploads, verify objects, and inspect migration status
- **effectivepermissions** - Inspect how RBAC would decide a principal/resource/operation request

### Example
```bash
curl -X GET http://localhost:8000/admin/users/list \
  -H "x-api-key: less3admin"
```

```bash
curl -X GET http://localhost:8000/admin/stats \
  -H "x-api-key: less3admin"
```

For detailed API documentation, refer to the project wiki.

## Open Source Packages 

Less3 is built using a series of open-source packages, including:

- AWS SDK - https://github.com/aws/aws-sdk-net
- S3 Server - https://github.com/jchristn/s3server
- Watson Webserver - https://github.com/jchristn/WatsonWebserver
- WatsonORM - https://github.com/jchristn/watsonorm

## Docker Deployment

Less3 is available on [DockerHub](https://hub.docker.com/r/jchristn77/less3).

### Fresh Clone: Build from Source

1. Navigate to the `Docker` directory
2. Run the deployment:
   ```bash
   cd Docker
   docker compose up --build -d
   ```

The `Docker` directory contains:
- `compose.yaml` - Docker Compose configuration that builds from the local `src/` tree
- `system.json` - Pre-configured Less3 settings for the local-build compose path
- `db/less3.db` - SQLite database file created inside the mounted `db/` directory
- `factory/less3.db` - Factory-reset seed used by the reset scripts

### Fresh Pull: Run the Published Image

If you want to validate startup from the published image instead of building locally, run the image directly from the `Docker` directory:

```bash
docker run --rm -p 8000:8000 \
  -v ./system.json:/app/system.json \
  -v ./db:/app/db \
  -v ./logs:/app/logs \
  -v ./temp:/app/temp \
  -v ./disk:/app/disk \
  jchristn77/less3:v3.0.0
```

### Default Configuration
- **Port**: 8000
- **Access Key**: `default`
- **Secret Key**: `default`
- **Protocol**: HTTP (no SSL)
- **URL Style**: Path-style (`http://localhost:8000/bucket/key`)
- **Hostname**: `*` (accepts all incoming requests)

On the first Docker startup, Less3 detects an empty configuration database and seeds the default `default` access key, `default` secret key, and `default` bucket automatically.

When rebuilt from this source, Less3 can also generate a default container configuration if `/app/system.json` is not mounted, then seed the default v3 data set into an empty database.

### Volume Mounts
The Docker deployment maps the following directories for persistence:
- `compose.yaml` mounts `./system.json` -> `/app/system.json`
- Current repo layout: `./db/` -> `/app/db/` and `system.json` points SQLite at `./db/less3.db`
- `./db/` -> `/app/db/` - SQLite database directory
- `./logs/` -> `/app/logs/` - Log files
- `./temp/` -> `/app/temp/` - Temporary files during uploads
- `./disk/` -> `/app/disk/` - Object storage data

### Building Your Own Image
```bash
cd src
docker build -t less3:custom -f Less3/Dockerfile .
```

**Important**: For production deployments, always:
1. Change the default access key and secret key
2. Use persistent volume mounts for database and storage
3. Consider using a non-SQLite database (SQL Server, MySQL, or PostgreSQL)

## Version History

Refer to CHANGELOG.md for details.
