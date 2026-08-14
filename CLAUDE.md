# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Less3 is an S3-compatible object storage platform written in C# (`net10.0`) that can be deployed anywhere. It implements AWS S3 APIs using the S3Server library and provides both path-style and virtual-hosted URL support for bucket access.

As of v4.0.0, Less3 runs in two topologies from the same binary: a **standalone single-node** deployment (the default when the binary is run natively — SQLite control plane, local storage, in-process locking) and a **multi-node scale-out cluster** (the default in Docker — Postgres control plane, shared storage mounted identically on every node, a Postgres-backed distributed lock manager, and an nginx load balancer). See `archive/MULTINODE_PLAN.md`, `MULTINODE_SETUP.md`, and the "Multi-Node Architecture" section below.

## Build and Run Commands

### Building the Project
```bash
# Build the solution
dotnet build src/Less3.sln

# Build specific configuration
dotnet build src/Less3.sln -c Release
dotnet build src/Less3.sln -c Debug

# Publish for deployment
dotnet publish src/Less3/Less3.csproj -c Release -o ./publish
```

### Running the Application
```bash
# Run from the project directory
cd src/Less3
dotnet run

# Run with setup wizard (creates system.json and less3.db)
dotnet run setup

# Run from published output
cd publish
dotnet Less3.dll
```

### Docker Deployment
```bash
# Using Docker Compose
cd Docker
docker compose up -d
docker compose down

# Or use the provided scripts
./Docker/compose-up.sh    # Linux/Mac
./Docker/compose-up.bat   # Windows
```

### Testing with AWS CLI
See `AWSCLI.md` for comprehensive AWS CLI testing commands. Key endpoints:
- Default access key: `default`
- Default secret key: `default`
- Default endpoint: `http://localhost:8000`

## Architecture Overview

### Core Architecture Layers

**Program.cs** (src/Less3/Program.cs)
- Entry point and initialization
- Creates all managers in specific order: Logging → Database → Config → Bucket → Auth → API Handler → Admin API Handler → Console → S3Server
- Hosts the S3Server and routes requests through PreRequestHandler → S3Server APIs → PostRequestHandler
- Handles authentication/authorization in PreRequestHandler before delegating to S3Server

**Manager Layer**
- `ConfigManager`: Manages users, credentials, buckets via the hand-rolled database driver layer (`src/Less3/Database/`)
- `BucketManager`: Manages bucket lifecycle and maintains BucketClient instances for each bucket
- `AuthManager`: Handles authentication and authorization, produces RequestMetadata with auth results
- `ConsoleManager`: Interactive console for administration when enabled

**API Layer** (src/Less3/Api/)
- `ApiHandler`: Primary S3 API facade that delegates to specialized handlers
- `ServiceHandler`: Service-level APIs (ListBuckets)
- `BucketHandler`: Bucket operations (Create, Delete, Read, Write, ACLs, Tags, Versioning)
- `ObjectHandler`: Object operations (Read, Write, Delete, Range reads, ACLs, Tags)
- `AdminApiHandler`: Administrative APIs accessed via x-api-key header

**Storage Layer** (src/Less3/Storage/)
- `StorageDriverBase`: Abstract base class for storage backends
- `DiskStorageDriver`: File system-based storage implementation
- Objects stored in `{bucket.DiskDirectory}/{obj.BlobFilename}`

**Data Models** (src/Less3/Classes/)
- `Bucket`, `Obj`, `User`, `Credential`: Core entities persisted through the database driver layer
- `BucketAcl`, `ObjectAcl`: Access control lists
- `BucketTag`, `ObjectTag`: Tagging support
- `RequestMetadata`: Contains authentication/authorization results, bucket/object references for each request

### Request Flow

1. HTTP request arrives → `PreRequestHandler` in Program.cs
2. Authentication: Extract access key from request, look up User and Credential
3. Authorization: Check bucket/object ownership, ACLs (AllUsers, AuthenticatedUsers, per-user), or bucket global config (EnablePublicRead/Write)
4. Store `RequestMetadata` in `ctx.Metadata`
5. Delegate to appropriate handler (Service/Bucket/Object)
6. Handler retrieves metadata via `ApiHelper.GetRequestMetadata(ctx)`
7. Handler performs operation, interacts with BucketClient
8. Response sent, `PostRequestHandler` logs metrics

### Authentication & Authorization Architecture

**Authentication Flow** (AuthManager.AuthenticateAndBuildMetadata)
- Extracts access key from Authorization header
- Looks up Credential and User
- Populates RequestMetadata with authentication result (Authenticated, NotAuthenticated, NoMaterialSupplied, AccessKeyNotFound, UserNotFound)
- Loads Bucket, Object, ACLs into RequestMetadata

**Authorization Flow** (AuthManager.Authorize* methods)
Authorization is checked in this order:
1. Admin API key (full access)
2. Bucket/Object global config (EnablePublicRead/Write)
3. AllUsers ACLs (anonymous access)
4. Authenticated user checks:
   - Bucket/Object ownership
   - AuthenticatedUsers ACLs
   - Per-user ACLs

**Authorization Results**
- `AdminAuthorized`: Admin API key used
- `PermitBucketOwnership` / `PermitObjectOwnership`: User owns the resource
- `PermitBucketGlobalConfig`: Bucket allows public access
- `PermitBucketAllUsersAcl` / `PermitObjectAllUsersAcl`: AllUsers ACL grants access
- `PermitBucketAuthUserAcl` / `PermitObjectAuthUserAcl`: AuthenticatedUsers ACL grants access
- `PermitBucketUserAcl` / `PermitObjectUserAcl`: Per-user ACL grants access
- `NotAuthorized`: Access denied

### Database Schema (hand-rolled driver layer)

Persistence is a hand-rolled, multi-dialect driver layer under `src/Less3/Database/` — **not** WatsonORM (WatsonORM appears only in historical migration comments). `DatabaseDriverBase` defines the typed method surface; `DatabaseDriverFactory` selects `SqliteDatabaseDriver`, `SqlServerDatabaseDriver`, `MySqlDatabaseDriver`, or `PostgreSqlDatabaseDriver` from `DatabaseTypeEnum`. Each driver has per-dialect `Implementations/` and `Queries/` folders; some SQL is hand-prepared on purpose (do not refactor it away without discussion). Tables and indices are created and migrated inside each driver's constructor (e.g. `SqliteDatabaseDriver` runs PRAGMAs, the legacy migrator, `SetupQueries.CreateTablesAndIndices()`, then `RunMigrations()`), not in `Program.cs`.

In multi-node mode, schema creation/migration must be serialized across nodes with a Postgres advisory lock so N nodes booting together migrate exactly once (see `archive/MULTINODE_PLAN.md` R16).

Core tables:
- `User`: Users in the system (GUID, Name, Email, etc.)
- `Credential`: Access keys and secret keys linked to users
- `Bucket`: Bucket metadata (Name, OwnerGUID, DiskDirectory, EnablePublicRead/Write, EnableVersioning)
- `BucketAcl`: Bucket-level access control
- `BucketTag`: Bucket tags
- `Obj`: Object metadata (Key, Version, BlobFilename, ContentLength, ContentType, OwnerGUID, DeleteMarker)
- `ObjectAcl`: Object-level access control
- `ObjectTag`: Object tags

Supports SQLite (default for native single-node), SQL Server, MySQL, and PostgreSQL (default and required engine for multi-node clusters) via the driver factory.

### Settings Structure (system.json)

Created by Setup wizard if not exists. Key settings:
- `Webserver.Hostname`: DNS hostname (must not be IP address, use `*` for wildcard)
- `Webserver.Port`: TCP port (default 8000)
- `BaseDomain`: For virtual hosted URLs (e.g., `.localhost`), null for path-style
- `Storage.DiskDirectory`: Root directory for object storage (default `./disk/`)
- `Storage.TempDirectory`: Temporary upload directory (default `./temp/`)
- `Database`: `DatabaseSettings` (SQLite default: `./less3.db`; Postgres in Docker)
- `ValidateSignatures`: Enable/disable AWS signature validation
- `AdminApiKey`: API key for admin endpoints (default `less3admin`)

### Path-Style vs Virtual Hosted URLs

**Path-Style** (default, BaseDomain = null)
- URL format: `http://hostname:port/bucket/key`
- Hostname is fixed (e.g., `localhost`)

**Virtual Hosted** (BaseDomain set, e.g., `.localhost`)
- URL format: `http://bucket.hostname:port/key`
- Requires wildcard hostname (`*`) and admin/root privileges
- Bucket name extracted from subdomain

## Key Development Patterns

### Working with Multipart Uploads

Multipart upload is shipped. `src/Less3/Classes/Upload.cs` tracks upload sessions and `src/Less3/Classes/UploadPart.cs` tracks individual parts; both are metadata POCOs persisted through the driver layer. The S3 operations (`InitiateMultipartUpload`, `UploadPart`, `CompleteMultipartUpload`, `AbortMultipartUpload`, `ListParts`, `ListMultipartUploads`) live in `ObjectHandler`.

In multi-node mode, part staging must land on **shared** storage (`PartsDirectory`, default `TempDirectory/parts`) so any node can complete or abort any upload; `UploadPart` is idempotent per `(uploadId, partNumber)`; and `CompleteMultipartUpload`/`AbortMultipartUpload` hold a distributed **Write** lock on the upload ID. See `archive/MULTINODE_PLAN.md` R4/R11.

### Adding New S3 APIs

1. Add handler method to appropriate handler class (ServiceHandler, BucketHandler, or ObjectHandler)
2. Wire up in ApiHandler (internal method that delegates)
3. Register callback in Program.cs InitializeGlobals with S3Server instance:
   ```csharp
   _S3Server.Object.YourNewMethod = _ApiHandler.ObjectYourNewMethod;
   ```
4. Handler pattern:
   ```csharp
   internal async Task YourMethod(S3Context ctx)
   {
       RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
       // Check authorization
       if (md.Authorization == AuthorizationResult.NotAuthorized)
           throw new S3Exception(new Error(ErrorCode.AccessDenied));
       // Perform operation using md.BucketClient
   }
   ```

### Working with BucketClient

BucketClient is the primary interface for bucket operations:
- Retrieved via `BucketManager.GetClient(bucketName)`
- Stored in `RequestMetadata.BucketClient`
- Key methods:
  - Object metadata: `GetObjectLatestMetadata`, `GetObjectVersionMetadata`, `GetObjectLatestVersion`
  - Object operations: `AddObject`, `DeleteObjectVersion`
  - ACLs: `GetBucketAcl`, `GetObjectAcl`, `AddObjectAcl`, `DeleteObjectVersionAcl`
  - Tags: `GetBucketTags`, `GetObjectTags`, `AddObjectVersionTags`, `DeleteObjectVersionTags`

### Versioning Behavior

- Versioning disabled by default on new buckets
- When versioning disabled: Overwriting existing object throws `InvalidBucketState`
- When enabled: New writes create new version (Version counter increments)
- Version IDs are integers (not strings like AWS S3)
- Delete creates delete marker when versioning enabled
- Version 1 is special: If version not specified, version 1 assumed

### Error Handling

Use S3Exception with ErrorCode:
```csharp
throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
throw new S3Exception(new Error(ErrorCode.AccessDenied));
throw new S3Exception(new Error(ErrorCode.NoSuchKey));
throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
```

Common error codes:
- `NoSuchBucket`, `NoSuchKey`, `NoSuchVersion`
- `AccessDenied`, `InvalidBucketState`
- `InternalError`, `InvalidRange`

## Coding Standards and Style Guidelines

**CRITICAL**: These standards MUST be followed strictly in all code. They ensure consistency and maintainability across the codebase.

### File Structure and Organization

**Namespace and Using Statements**
- Namespace declaration must be at the top of the file
- All `using` statements must be contained INSIDE the namespace block
- Microsoft and standard system library usings must be listed first, in alphabetical order
- Other using statements follow, also in alphabetical order

```csharp
namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using S3ServerLibrary;
    using SyslogLogging;

    public class Example { }
}
```

**File Organization**
- Limit each file to containing exactly ONE class or exactly ONE enum
- Do NOT nest multiple classes or multiple enums in a single file
- Regions are NOT required for files under 500 lines
- For larger files, use regions: `Public-Members`, `Private-Members`, `Constructors-and-Factories`, `Public-Methods`, `Private-Methods`

### Naming Conventions

**Private Member Variables**
- MUST start with underscore followed by Pascal case
- Correct: `_Settings`, `_Logging`, `_BucketManager`
- Incorrect: `_settings`, `_fooBar`, `settings`

**Example:**
```csharp
private Settings _Settings;
private LoggingModule _Logging;
private ConfigManager _Config;
```

### Documentation Requirements

**Public Members, Constructors, and Public Methods**
- MUST have XML code documentation
- Document parameters, return values, and exceptions
- Specify default values, minimum/maximum values where applicable
- Explain what different values mean or their effects

```csharp
/// <summary>
/// Maximum number of retry attempts for failed operations.
/// Default value is 3. Minimum value is 1. Maximum value is 10.
/// Higher values increase reliability but may impact performance.
/// </summary>
public int MaxRetries { get; set; } = 3;
```

**Private Members and Private Methods**
- MUST NOT have code documentation
- Keep private implementation details undocumented

**Exception Documentation**
```csharp
/// <summary>
/// Retrieves a user by their GUID.
/// </summary>
/// <param name="guid">User GUID.</param>
/// <returns>User object if found.</returns>
/// <exception cref="ArgumentNullException">Thrown when guid is null or empty.</exception>
/// <exception cref="InvalidOperationException">Thrown when database connection fails.</exception>
public User GetUserByGuid(string guid)
```

### Property Implementation

**Public Members with Validation**
- Use explicit getters and setters with backing variables when value requires range or null validation
- Validate in the setter

```csharp
private int _MaxConnections = 100;

/// <summary>
/// Maximum number of concurrent connections.
/// Default value is 100. Minimum value is 1. Maximum value is 10000.
/// </summary>
public int MaxConnections
{
    get { return _MaxConnections; }
    set
    {
        if (value < 1 || value > 10000)
            throw new ArgumentOutOfRangeException(nameof(value), "Must be between 1 and 10000.");
        _MaxConnections = value;
    }
}
```

### Variable Declaration

**No var Keyword**
- Do NOT use `var` when defining variables
- Always use the actual type

```csharp
// Correct
List<Bucket> buckets = new List<Bucket>();
string fileName = GetFileName();

// Incorrect
var buckets = new List<Bucket>();
var fileName = GetFileName();
```

### Async/Await Patterns

**CancellationToken Requirements**
- Every async method MUST accept a CancellationToken as an input parameter
- Exception: If the class has a CancellationToken or CancellationTokenSource as a class member
- Check for cancellation at appropriate places in long-running operations

```csharp
public async Task<List<Bucket>> GetBucketsAsync(CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();

    List<Bucket> buckets = await _ORM.SelectAsync<Bucket>(cancellationToken).ConfigureAwait(false);

    return buckets;
}
```

**ConfigureAwait Usage**
- Use `.ConfigureAwait(false)` on await calls where appropriate
- Prevents deadlocks in library code

```csharp
byte[] data = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
```

### IEnumerable Methods

**Sync and Async Variants**
- When implementing a method that returns `IEnumerable<T>`, also create an async variant
- Async variant must include a CancellationToken parameter

```csharp
public IEnumerable<Bucket> GetBuckets()
{
    // Synchronous implementation
}

public async Task<IEnumerable<Bucket>> GetBucketsAsync(CancellationToken cancellationToken = default)
{
    // Asynchronous implementation
}
```

### Exception Handling

**Specific Exception Types**
- Use specific exception types rather than generic `Exception`
- Always include meaningful error messages with context
- Consider custom exception types for domain-specific errors

```csharp
// Good
if (String.IsNullOrEmpty(bucketName))
    throw new ArgumentNullException(nameof(bucketName), "Bucket name cannot be null or empty.");

if (port < 1 || port > 65535)
    throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be between 1 and 65535.");

// Bad
if (String.IsNullOrEmpty(bucketName))
    throw new Exception("Invalid bucket name");
```

**Exception Filters**
```csharp
try
{
    // Database operation
}
catch (SqlException ex) when (ex.Number == 2601)
{
    // Handle duplicate key error specifically
}
```

### Resource Management

**IDisposable Pattern**
- Implement IDisposable/IAsyncDisposable when holding unmanaged resources or disposable objects
- Use `using` statements or `using` declarations for IDisposable objects
- Follow the full Dispose pattern with `protected virtual void Dispose(bool disposing)`
- Always call `base.Dispose()` in derived classes

```csharp
public class ResourceManager : IDisposable
{
    private bool _Disposed = false;
    private FileStream _Stream;

    protected virtual void Dispose(bool disposing)
    {
        if (_Disposed) return;

        if (disposing)
        {
            // Dispose managed resources
            if (_Stream != null)
            {
                _Stream.Dispose();
                _Stream = null;
            }
        }

        // Free unmanaged resources here if any

        _Disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
```

**Using Statements**
```csharp
using (FileStream fs = new FileStream(path, FileMode.Open))
{
    // Use fs
}

// Or with declaration (C# 8+)
using FileStream fs = new FileStream(path, FileMode.Open);
// fs automatically disposed at end of scope
```

### Nullable Reference Types and Input Validation

**Enable Nullable Reference Types**
- Use nullable reference types (enable `<Nullable>enable</Nullable>` in project files)
- Document nullability in XML comments

**Guard Clauses**
- Validate input parameters with guard clauses at method start
- Use `ArgumentNullException.ThrowIfNull()` for .NET 6+ or manual null checks
- Proactively identify and eliminate situations where null might cause exceptions

```csharp
public void ProcessBucket(Bucket bucket, string ownerGuid)
{
    ArgumentNullException.ThrowIfNull(bucket);

    if (String.IsNullOrEmpty(ownerGuid))
        throw new ArgumentNullException(nameof(ownerGuid), "Owner GUID cannot be null or empty.");

    // Method implementation
}
```

**Result Pattern**
- Consider using the Result pattern or Option/Maybe types for methods that can fail
- Avoids throwing exceptions for expected failure cases

### Thread Safety

**Documentation**
- Document thread safety guarantees in XML comments
- Clearly state if a class or method is thread-safe

```csharp
/// <summary>
/// Thread-safe bucket manager.
/// All public methods can be safely called from multiple threads.
/// </summary>
public class BucketManager
```

**Synchronization**
- Use `Interlocked` operations for simple atomic operations
- Prefer `ReaderWriterLockSlim` over `lock` for read-heavy scenarios
- Use proper locking for shared state

```csharp
private readonly object _BucketsLock = new object();
private List<BucketClient> _Buckets = new List<BucketClient>();

public void AddBucket(BucketClient client)
{
    lock (_BucketsLock)
    {
        _Buckets.Add(client);
    }
}
```

### LINQ Best Practices

**Prefer LINQ when readable**
- Use LINQ methods over manual loops when readability is not compromised
- Use `.Any()` instead of `.Count() > 0` for existence checks
- Use `.FirstOrDefault()` with null checks rather than `.First()` when element might not exist

```csharp
// Good - check for existence
if (buckets.Any(b => b.Name == "default"))

// Bad - counts entire collection
if (buckets.Count(b => b.Name == "default") > 0)

// Good - safe access
Bucket bucket = buckets.FirstOrDefault(b => b.Name == targetName);
if (bucket != null) { }

// Bad - throws if not found
Bucket bucket = buckets.First(b => b.Name == targetName);
```

**Multiple Enumeration**
- Be aware of multiple enumeration issues
- Consider `.ToList()` when enumerating multiple times

```csharp
IEnumerable<Bucket> query = GetBuckets().Where(b => b.EnableVersioning);

// If using query multiple times
List<Bucket> buckets = query.ToList();
int count = buckets.Count;
foreach (Bucket bucket in buckets) { }
```

### Avoid Tuples

**Tuples Should Be Avoided**
- Do NOT use tuples unless absolutely, absolutely necessary
- Create dedicated classes or structs instead
- Tuples reduce code readability and type safety

```csharp
// Bad
public (string Name, int Count) GetBucketInfo()

// Good
public BucketInfo GetBucketInfo()

public class BucketInfo
{
    public string Name { get; set; }
    public int Count { get; set; }
}
```

### Configuration Over Constants

**Configurable Values**
- Avoid using constant values for things developers may later want to configure or change
- Use public members with backing private members set to reasonable defaults
- Document the default values in XML comments

```csharp
// Good
private int _DefaultTimeout = 30000;

/// <summary>
/// Default timeout in milliseconds.
/// Default value is 30000 (30 seconds).
/// </summary>
public int DefaultTimeout
{
    get { return _DefaultTimeout; }
    set { _DefaultTimeout = value; }
}

// Bad
const int DEFAULT_TIMEOUT = 30000;
```

### Opaque Classes

**Don't Make Assumptions**
- Do NOT make assumptions about what class members or methods exist on opaque classes
- If a class implementation is not visible to you, ASK for the implementation
- Never guess at API surface or behavior

### SQL Statements

**Manual SQL Preparation**
- If code uses manually prepared strings for SQL statements, there is a good reason
- Assume the existing approach is correct
- Do not attempt to "fix" or refactor SQL statement construction without discussion

### Library Code Restrictions

**No Console Output**
- Ensure NO `Console.WriteLine` statements are added to library code
- Use the logging framework instead (`_Logging.Info()`, `_Logging.Debug()`, etc.)

```csharp
// Bad
Console.WriteLine("Processing bucket: " + bucketName);

// Good
_Logging.Info("Processing bucket: " + bucketName);
```

## Important Caveats

1. **Hostname Requirements**: `Server.DnsHostname` in system.json cannot be an IP address (parsing will fail). Use DNS names or `localhost`.

2. **Wildcard Listeners**: Using `*`, `+`, or `0.0.0.0` for hostname requires administrative/root privileges.

3. **Versioning**: Version IDs are integers internally, not opaque strings like AWS S3. This is a minor compatibility difference.

4. **Database**: SQLite default is convenient but not recommended for production containers without persistent volume mounts.

5. **Authorization Check Order**: Always check authorization before performing operations. The order in AuthManager determines precedence (admin → ownership → public → AllUsers → authenticated → per-user).

6. **File Paths**: Use forward slashes in paths. Code normalizes paths in Settings.

7. **Multi-node requires Postgres + shared storage**: Cluster mode refuses to start on SQLite (a shared SQLite file cannot back multiple writers). Blobs, object-write staging, and multipart parts must live on storage mounted identically on every node.

8. **Data integrity is the invariant**: Every read-modify-write on object state (version increment, unversioned overwrite, multipart complete/abort, blob delete) runs under a distributed lock and is gated by a fencing token re-checked at the guarded DB mutation. Do not add a code path that mutates object data or metadata without going through the lock manager. Do not cache object/bucket metadata in a way that could serve a stale answer to a mutating operation.

## Multi-Node Architecture

The v4.0.0 cluster keeps one authority for each kind of truth: Postgres for all metadata, lock state, and membership; shared storage for immutable blobs. Nodes are stateless with respect to durable truth — killing a node mid-request fails at most that request, never corrupts data. Reference designs: `archive/MULTINODE_PLAN.md` (full plan + 17-row data-integrity risk register) and `MULTINODE_SETUP.md` (operator guide).

**Distributed lock manager** (`src/Less3/Locking/`)
- `ILockManager` abstraction with `AcquireAsync`/`HeartbeatAsync`/`ReleaseAsync`/`ValidateAsync`, `LockMode` (Read/Write/Delete), `AcquireOptions`, and `LockHandle` (carries `HolderId`, `FencingToken`, `LeaseExpiresUtc`).
- **Fair FIFO read/write/delete semantics**: reads take a **shared** lock and run concurrently with no cap; writes and deletes are **exclusive** and are granted only after every request that arrived before them has released (a write lets pending reads flush; a delete drains everything). Ordering is by arrival, so a steady read stream cannot starve a queued writer/deleter. Object read paths (`BucketClient.GetObjectLatest*`) hold the shared read lock for the life of the streamed response via `LockReleasingStream`.
- `LocalLockManager`: in-process fair queue (`LocalLockQueue`/`LocalLockEntry`) for single-node/SQLite; fencing tokens are an in-memory per-key monotonic counter.
- `PostgresLockManager`: default in cluster mode. Each request is appended to a per-key FIFO queue (`less3_lock_queue`); the grant decision runs inside the `less3_lock_try_grant` PL/pgSQL function under a per-key `pg_advisory_xact_lock`, so exactly one node decides at a time. Lease expiry uses the DB clock; an expired lease/waiter is reclaimed; each grant bumps a per-key monotonic fencing token (`less3_lock_fence`) that a mutation re-checks before commit.
- `ClutchLockManager`: optional. Connects to a Clutch server over its native **WebSocket** lock protocol (`/v1.0/lock/connect`, `x-clutch-access-key` header) with one persistent connection per node — so every lock the node holds is auto-released by Clutch when the socket closes (a crash can't strand a lock until its lease lapses). No NuGet SDK dependency (raw `ClientWebSocket`). Clutch performs the fair read/write/delete drain server-side and shares the same Postgres via BYOD, so the DB stays authoritative. Alpha; opt-in only.
- Selection is driven by `ClusterSettings.LockProvider` (`Local` | `Postgres` | `Clutch`).

**Cache coherency**
- `BucketManager` caches only `BucketClient` instances, revalidated against a bucket `Epoch`/`LastModifiedUtc` on a short TTL; bucket create/delete/config-change bump the epoch so other nodes converge. The object-write path reads bucket config (versioning, public access) fresh.
- Object and bucket **metadata are never cached** with a lifetime that could serve a stale mutating decision. Authentication/authorization results may be cached, but only under a short TTL (`AuthCacheSettings`, default 15s) with epoch invalidation on credential/role/session change.

**Data plane**
- `DiskStorageDriver` writes blobs addressed by immutable object ID (fresh per write — no in-place overwrite), computing the content hash in a single streaming pass. In cluster mode `DiskDirectory`, `TempDirectory`, and `PartsDirectory` point at the shared mount.

**Singletons and membership**
- `CleanupManager` and schema migration run leader-only, elected via well-known lock leases (`cluster:cleanup`, `cluster:migration`). Nodes register in `less3_node` on boot and heartbeat; the dashboard and `/api/v1/cluster/*` expose membership, health, leader, and active locks.

**Observability**
- Library code is instrumented with plain BCL `Meter`/`ActivitySource` under `Less3.*` names (no telemetry-SDK dependency in the instrumented code). At the composition root a `RadiantHost` subscribes to the `Less3.*` meters and Watson's `Watson` meter, exposes Prometheus on `:9464`, and can export OTLP traces and logs. The Docker stack ships Prometheus, Grafana (dashboards pre-provisioned, including a Locks & Data Integrity board whose fencing-conflict count should stay at zero), Loki, Tempo, and an OpenTelemetry collector.

## Admin API

Admin APIs require `x-api-key` header with value matching `Settings.AdminApiKey` (default: `less3admin`).

Format: `http://hostname:port/admin/{resource}/{operation}`

Handlers in `src/Less3/Api/Admin/`:
- `GetHandler`: Retrieve resources
- `PostHandler`: Create/update resources
- `DeleteHandler`: Delete resources

Access granted via `AdminAuthorized` authorization result, bypassing normal ACL checks.
