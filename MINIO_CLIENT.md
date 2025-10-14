# Testing with MinIO Client (mc)

Use the following commands to test a fresh installation of Less3 with the MinIO Client (`mc`).

MinIO Client is a modern alternative to AWS CLI with support for filesystems, S3-compatible object storage, and more.

**Important**: If you encounter any issues, please provide detailed output when filing an issue.

## Install MinIO Client

### Linux/macOS
```bash
curl https://dl.min.io/client/mc/release/linux-amd64/mc \
  --create-dirs \
  -o $HOME/minio-binaries/mc

chmod +x $HOME/minio-binaries/mc
export PATH=$PATH:$HOME/minio-binaries/

# Verify installation
mc --version
```

### Windows (PowerShell)
```powershell
Invoke-WebRequest -Uri "https://dl.min.io/client/mc/release/windows-amd64/mc.exe" -OutFile "C:\mc.exe"
setx path "%path%;C:\"
```

### Alternative: Binary Downloads
Download from: https://min.io/download#/client/mc

## Install and Start Less3

```
> less3


   _           ____
  | |___ _____|__ /
  | / -_|_-<_-<|_ \
  |_\___/__/__/___/



<3 :: Less3 :: S3-Compatible Object Storage

Thank you for using Less3!  We're putting together a basic system configuration
so you can be up and running quickly.  You'll want to modify the system.json
file after to ensure a more secure operating environment.


Less3 requires access to a database and supports Sqlite, Microsoft SQL Server,
MySQL, and PostgreSQL.  Please provide access details for your database.  The
user account supplied must have the ability to CREATE and DROP tables along
with issue queries containing SELECT, INSERT, UPDATE, and DELETE.  Setup will
attempt to create tables on your behalf if they dont exist.

Database type [sqlite|sqlserver|mysql|postgresql]: [sqlite]
Filename: [./less3.db]

IMPORTANT: Using Sqlite in production is not recommended if deploying within a
containerized environment and the database file is stored within the container.
Store the database file in external storage to ensure persistence.


All finished!

If you ever want to return to this setup wizard, just re-run the application
from the terminal with the 'setup' argument.

We created a bucket containing a few sample files for you so that you can see
your node in action.  Access these files in the 'default' bucket using the
AWS SDK or your favorite S3 browser tool.

  http://localhost:8000/default/hello.html
  http://localhost:8000/default/hello.txt
  http://localhost:8000/default/hello.json

  Access key  : default
  Secret key  : default
  Bucket name : default (public read enabled!)
  S3 endpoint : http://localhost:8000

IMPORTANT: be sure to supply a hostname in the system.json Webserver.Hostname
field if you wish to allow access from other machines.  Your node is currently
only accessible via localhost.  Do not use an IP address for this value.



Less3 | S3-Compatible Object Storage | v2.1.17
```

## Configure MinIO Client Alias

Add Less3 as an alias to MinIO Client:

```bash
mc alias set less3 http://localhost:8000 default default
```

**Output:**
```
Added `less3` successfully.
```

Verify the configuration:
```bash
mc alias list less3
```

**Output:**
```
less3
  URL       : http://localhost:8000
  AccessKey : default
  SecretKey : default
  API       : s3v4
  Path      : auto
```

## List Buckets

```bash
mc ls less3
```

**Output:**
```
[2024-01-15 10:30:00 PST]     0B default/
```

## List Objects in Default Bucket

```bash
mc ls less3/default
```

**Output:**
```
[2024-01-15 10:30:00 PST] 1.2KiB STANDARD hello.html
[2024-01-15 10:30:00 PST]  217B STANDARD hello.txt
[2024-01-15 10:30:00 PST]  152B STANDARD hello.json
```

## Download an Object

```bash
mc cp less3/default/hello.json ./hello.json
```

**Output:**
```
...default/hello.json: 152 B / 152 B ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100.00% 1.5 KiB/s 0s
```

## Upload an Object

**Note**: Versioning is disabled by default on buckets. Re-uploading an existing object will fail. Rename the object first or use a different filename.

```bash
mc cp ./hello.json less3/default/hello-copy.json
```

**Output:**
```
...ello-copy.json: 152 B / 152 B ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100.00% 1.5 KiB/s 0s
```

## Delete an Object

```bash
mc rm less3/default/hello-copy.json
```

**Output:**
```
Removing `less3/default/hello-copy.json`.
```

## Create a Bucket

```bash
mc mb less3/mybucket
```

**Output:**
```
Bucket created successfully `less3/mybucket`.
```

## Remove a Bucket

The bucket must be empty before removal.

```bash
mc rb less3/mybucket
```

**Output:**
```
Bucket removed successfully `less3/mybucket`.
```

## Check Bucket Existence

```bash
mc stat less3/default
```

**Output:**
```
Name      : default/
Type      : folder
```

## Check Object Metadata

```bash
mc stat less3/default/hello.txt
```

**Output:**
```
Name      : hello.txt
Date      : 2024-01-15 10:30:00 PST
Size      : 217 B
ETag      : 626A3F7A01F364E917A5088E4856CADD
Type      : file
Metadata  :
  Content-Type: application/octet-stream
```

## Copy Object Between Buckets

First create a second bucket:
```bash
mc mb less3/bucket2
```

Copy an object:
```bash
mc cp less3/default/hello.txt less3/bucket2/hello.txt
```

**Output:**
```
...default/hello.txt: 217 B / 217 B ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100.00% 2.1 KiB/s 0s
```

## Recursive Operations

Upload multiple files:
```bash
mc cp --recursive ./local-directory/ less3/mybucket/
```

Download an entire bucket:
```bash
mc cp --recursive less3/mybucket/ ./local-directory/
```

Remove all objects in a bucket:
```bash
mc rm --recursive --force less3/mybucket/
```

## Mirror Local Directory to Bucket

Synchronize a local directory to a bucket:
```bash
mc mirror ./local-directory/ less3/mybucket/
```

## Set Bucket Policy (Public Read)

```bash
mc anonymous set download less3/mybucket
```

**Output:**
```
Access permission for `less3/mybucket` is set to `download`
```

Get current policy:
```bash
mc anonymous get less3/mybucket
```

Remove public access:
```bash
mc anonymous set none less3/mybucket
```

## Bucket Versioning

Enable versioning:
```bash
mc version enable less3/mybucket
```

**Output:**
```
less3/mybucket versioning is enabled
```

Check versioning status:
```bash
mc version info less3/mybucket
```

Suspend versioning:
```bash
mc version suspend less3/mybucket
```

## Object Tagging

Set tags on an object:
```bash
mc tag set less3/mybucket/myfile.txt "key1=value1&key2=value2"
```

Get tags from an object:
```bash
mc tag list less3/mybucket/myfile.txt
```

Remove tags:
```bash
mc tag remove less3/mybucket/myfile.txt
```

## Multipart Upload Testing

For large files, MinIO Client automatically uses multipart uploads. Test with a large file:

```bash
# Create a 100MB test file
dd if=/dev/zero of=largefile.bin bs=1M count=100

# Upload (will use multipart automatically)
mc cp largefile.bin less3/mybucket/largefile.bin
```

## Find Objects

Search for objects by name pattern:
```bash
mc find less3/mybucket --name "*.json"
```

Search for objects larger than 1MB:
```bash
mc find less3/mybucket --larger 1M
```

## Additional Features

### Get Help
```bash
mc --help
mc cp --help
```

### Enable JSON Output
```bash
mc --json ls less3/default
```

### Watch for Changes
Monitor bucket for changes in real-time:
```bash
mc watch less3/mybucket
```

## Troubleshooting

### Connection Issues
If you encounter connection errors, verify:
1. Less3 is running: `curl http://localhost:8000`
2. Endpoint is correct: `mc alias list less3`
3. Access credentials match system.json configuration

### Debug Mode
For detailed error information:
```bash
mc --debug cp myfile.txt less3/mybucket/myfile.txt
```

## Performance Testing

Use `mc` built-in speedtest (requires MinIO server, but useful for comparison):
```bash
mc support perf less3/mybucket --duration 10s
```

## Additional Resources

- MinIO Client Documentation: https://min.io/docs/minio/linux/reference/minio-mc.html
- Less3 GitHub: https://github.com/jchristn/less3
- Report Issues: https://github.com/jchristn/less3/issues
