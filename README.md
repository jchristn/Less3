![alt tag](https://github.com/jchristn/less3/blob/main/assets/logo.png)

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

v2.2.0

- Updated to `S3Server v7.0.3`
- Expanded native `AWSSDK.S3` compatibility validation across ACLs, tagging, versioning, multipart upload, and signature enforcement
- Fixed object overwrite behavior for unversioned buckets, including multipart completion
- Improved S3 protocol handling around range reads, version enumeration, and signature validation
- Expanded the dashboard with object upload/view/edit workflows, row-click detail modals, standardized copy actions, and API Explorer credential selection plus pretty-print tools
- Added admin dashboard statistics for total buckets, total objects, total storage, and per-bucket object count/size visibility in the Buckets table
- See `CHANGELOG.md` for release details

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

Less3 includes a web-based dashboard for managing buckets, objects, users, and credentials. After starting the Less3 server, you can start the dashboard:

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
Admin APIs require the `x-api-key` header with a value matching `AdminApiKey` in system.json (default: `less3admin`).

### Endpoint Format
```
http://hostname:port/admin/{resource}/{operation}
```

### Available Resources
- **users** - Manage user accounts
- **credentials** - Manage access keys and secret keys
- **buckets** - Manage buckets and bucket configuration
- **stats** - Retrieve aggregate bucket, object, and storage metrics for dashboard and admin views

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
- `compose.image.yaml` - Docker Compose configuration that uses the published `jchristn77/less3:v2.2.0` image
- `system.json` - Pre-configured Less3 settings for the local-build compose path
- `db/less3.db` - SQLite database file created inside the mounted `db/` directory
- `factory/less3.db` - Factory-reset seed used by the reset scripts

### Fresh Pull: Run the Published Image

If you want to validate startup from the published image instead of building locally:

```bash
cd Docker
docker compose -f compose.image.yaml up -d
```

`compose.image.yaml` mounts the repository's `system.json` into the container so the current published `jchristn77/less3:v2.2.0` image starts cleanly on a fresh checkout.

You can also run the image directly from the `Docker` directory:

```bash
docker run --rm -p 8000:8000 \
  -v ./system.json:/app/system.json \
  -v ./db:/app/db \
  -v ./logs:/app/logs \
  -v ./temp:/app/temp \
  -v ./disk:/app/disk \
  jchristn77/less3:v2.2.0
```

### Default Configuration
- **Port**: 8000
- **Access Key**: `default`
- **Secret Key**: `default`
- **Protocol**: HTTP (no SSL)
- **URL Style**: Path-style (`http://localhost:8000/bucket/key`)
- **Hostname**: `*` (accepts all incoming requests)

On the first Docker startup, Less3 detects an empty configuration database and seeds the default `default` access key, `default` secret key, and `default` bucket automatically.

When rebuilt from this source, Less3 can also generate a default container configuration if `/app/system.json` is not mounted, then seed the default data set into an empty database. The current repository compose files still mount `system.json` so the published `v2.2.0` image works cleanly today.

### Volume Mounts
The Docker deployment maps the following directories for persistence:
- `compose.yaml` mounts `./system.json` -> `/app/system.json`
- `compose.image.yaml` mounts `./system.json` -> `/app/system.json` so the published image follows the same startup path as the local-build compose file
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
