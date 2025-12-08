![alt tag](https://github.com/jchristn/less3/blob/master/assets/logo.png)

# Less3 :: S3-Compatible Object Storage

Less3 is an S3-compatible object storage platform that you can run anywhere. 

![alt tag](https://github.com/jchristn/less3/blob/master/assets/diagram.png)

## Use Cases

Core use cases for Less3:

- Local object storage - S3-compatible storage on your laptop, virtual machine, container, or bare metal
- Private cloud object storage - use your existing private cloud hardware to create an S3-compatible storage pool
- Development and test - local devtest against S3-compatible storage
- Remote storage - deploy S3-compatible storage in environments where you must control data placement

## Current Version

v2.1.17

- **Multipart upload support** - InitiateMultipartUpload, UploadPart, CompleteMultipartUpload, AbortMultipartUpload, ListParts, ListMultipartUploads
- Enhanced AWS CLI compatibility
- Dependency updates for improved stability
- See `AWSCLI.md` for comprehensive testing commands

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
  "ValidateSignatures": true
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

### Example
```bash
curl -X GET http://localhost:8000/admin/users/list \
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

Less3 is available on [DockerHub](https://hub.docker.com/r/jchristn/less3).

### Quick Start with Docker Compose

1. Navigate to the `Docker` directory
2. Run the deployment:
   ```bash
   cd Docker
   docker compose up -d
   ```

The `Docker` directory contains:
- `compose.yaml` - Docker Compose configuration
- `system.json` - Pre-configured Less3 settings
- `less3.db` - SQLite database (will be created on first run)

### Default Configuration
- **Port**: 8000
- **Access Key**: `default`
- **Secret Key**: `default`
- **Protocol**: HTTP (no SSL)
- **URL Style**: Path-style (`http://localhost:8000/bucket/key`)
- **Hostname**: `*` (accepts all incoming requests)

### Volume Mounts
The Docker deployment maps the following directories for persistence:
- `./system.json` → `/app/system.json` - Configuration file
- `./less3.db` → `/app/less3.db` - Database
- `./logs/` → `/app/logs/` - Log files
- `./temp/` → `/app/temp/` - Temporary files during uploads
- `./disk/` → `/app/disk/` - Object storage data

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
