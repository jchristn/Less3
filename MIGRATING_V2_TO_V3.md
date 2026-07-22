# Migrating Less3 v2 to v3

Less3 v3.0.0 is designed for net-new deployments. Backward compatibility is not automatic. Use this guide only when you need to carry a v2 database forward manually.

Back up the database and object storage directory before running any SQL. The examples below show the structural work needed to move toward v3's tenant-aware schema. Review the generated PrettyID values before applying them to production data.

## Target Changes

v3 changes the persistence model in these ways:

- `tenants` is the top-level table.
- Tenant-owned tables have a `tenant_id` column.
- The old `credential` table is renamed `credentials`.
- Application identifiers use PrettyID string IDs with stable prefixes and a maximum length of 32 characters.
- No legacy identifier columns remain in the v3 target schema.
- Access keys are globally unique.
- Bucket names are unique per tenant.
- RBAC introduces roles, permissions, role assignments, auth sessions, and authorization audit records.

## Default Seed

For a single-tenant migration, assign existing rows to:

```text
tenant_id = default
admin user email = admin@less3
admin password = password
access key = default
secret key = default
```

Use a separate offline script or admin API to replace v2 identifier values with PrettyID values. The application generates PrettyID values through the `PrettyId` NuGet package; SQL alone cannot reproduce the exact K-sortable ID format safely.

## SQLite

SQLite has limited in-place column alteration. The safest manual migration is table-copy based:

```sql
PRAGMA foreign_keys = OFF;
BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS tenants (
  id TEXT PRIMARY KEY,
  parent_id TEXT NULL,
  name TEXT NOT NULL,
  active INTEGER NOT NULL DEFAULT 1,
  createdutc TEXT NOT NULL,
  lastupdateutc TEXT NOT NULL
);

INSERT OR IGNORE INTO tenants (id, parent_id, name, active, createdutc, lastupdateutc)
VALUES ('default', NULL, 'Default', 1, datetime('now'), datetime('now'));

ALTER TABLE credential RENAME TO credentials;
ALTER TABLE users ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE credentials ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE buckets ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE objects ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE bucketacls ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE objectacls ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE buckettags ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE objecttags ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE uploads ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE uploadparts ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';
ALTER TABLE requesthistory ADD COLUMN tenant_id TEXT NOT NULL DEFAULT 'default';

CREATE UNIQUE INDEX IF NOT EXISTS idx_credentials_accesskey_unique ON credentials (accesskey);
CREATE UNIQUE INDEX IF NOT EXISTS idx_buckets_tenant_name_unique ON buckets (tenant_id, name);
CREATE INDEX IF NOT EXISTS idx_users_tenant_email ON users (tenant_id, email);
CREATE INDEX IF NOT EXISTS idx_objects_tenant_bucket_key ON objects (tenant_id, bucket_id, key);
CREATE INDEX IF NOT EXISTS idx_requesthistory_tenant_createdutc ON requesthistory (tenant_id, createdutc);

COMMIT;
PRAGMA foreign_keys = ON;
```

After the structural migration, run an offline PrettyID rewrite for every application identifier column and update all references in dependent tables.

## MySQL

```sql
START TRANSACTION;

CREATE TABLE IF NOT EXISTS tenants (
  id VARCHAR(32) PRIMARY KEY,
  parent_id VARCHAR(32) NULL,
  name VARCHAR(256) NOT NULL,
  active TINYINT(1) NOT NULL DEFAULT 1,
  createdutc DATETIME(6) NOT NULL,
  lastupdateutc DATETIME(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO tenants (id, parent_id, name, active, createdutc, lastupdateutc)
VALUES ('default', NULL, 'Default', 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));

RENAME TABLE credential TO credentials;
ALTER TABLE users ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE credentials ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE buckets ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE objects ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE bucketacls ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE objectacls ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE buckettags ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE objecttags ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE uploads ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE uploadparts ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE requesthistory ADD COLUMN tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';

CREATE UNIQUE INDEX idx_credentials_accesskey_unique ON credentials (accesskey);
CREATE UNIQUE INDEX idx_buckets_tenant_name_unique ON buckets (tenant_id, name);
CREATE INDEX idx_users_tenant_email ON users (tenant_id, email);
CREATE INDEX idx_objects_tenant_bucket_key ON objects (tenant_id, bucket_id, `key`);
CREATE INDEX idx_requesthistory_tenant_createdutc ON requesthistory (tenant_id, createdutc);

COMMIT;
```

Run the PrettyID rewrite after this step, then update primary keys and foreign-key references in a single maintenance window.

## PostgreSQL

```sql
BEGIN;

CREATE TABLE IF NOT EXISTS tenants (
  id VARCHAR(32) PRIMARY KEY,
  parent_id VARCHAR(32) NULL,
  name VARCHAR(256) NOT NULL,
  active BOOLEAN NOT NULL DEFAULT TRUE,
  createdutc TIMESTAMP NOT NULL,
  lastupdateutc TIMESTAMP NOT NULL
);

INSERT INTO tenants (id, parent_id, name, active, createdutc, lastupdateutc)
VALUES ('default', NULL, 'Default', TRUE, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC')
ON CONFLICT (id) DO NOTHING;

ALTER TABLE credential RENAME TO credentials;
ALTER TABLE users ADD COLUMN IF NOT EXISTS tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE credentials ADD COLUMN IF NOT EXISTS tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE buckets ADD COLUMN IF NOT EXISTS tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE objects ADD COLUMN IF NOT EXISTS tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE bucketacls ADD COLUMN IF NOT EXISTS tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE objectacls ADD COLUMN IF NOT EXISTS tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE buckettags ADD COLUMN IF NOT EXISTS tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE objecttags ADD COLUMN IF NOT EXISTS tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE uploads ADD COLUMN IF NOT EXISTS tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE uploadparts ADD COLUMN IF NOT EXISTS tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';
ALTER TABLE requesthistory ADD COLUMN IF NOT EXISTS tenant_id VARCHAR(32) NOT NULL DEFAULT 'default';

CREATE UNIQUE INDEX IF NOT EXISTS idx_credentials_accesskey_unique ON credentials (accesskey);
CREATE UNIQUE INDEX IF NOT EXISTS idx_buckets_tenant_name_unique ON buckets (tenant_id, name);
CREATE INDEX IF NOT EXISTS idx_users_tenant_email ON users (tenant_id, email);
CREATE INDEX IF NOT EXISTS idx_objects_tenant_bucket_key ON objects (tenant_id, bucket_id, key);
CREATE INDEX IF NOT EXISTS idx_requesthistory_tenant_createdutc ON requesthistory (tenant_id, createdutc);

COMMIT;
```

PostgreSQL can perform the PrettyID rewrite in transactional batches. Build and verify an ID mapping table before changing dependent columns.

## SQL Server

```sql
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='tenants' AND xtype='U')
CREATE TABLE tenants (
  id NVARCHAR(32) PRIMARY KEY,
  parent_id NVARCHAR(32) NULL,
  name NVARCHAR(256) NOT NULL,
  active BIT NOT NULL DEFAULT 1,
  createdutc NVARCHAR(64) NOT NULL,
  lastupdateutc NVARCHAR(64) NOT NULL
);

IF NOT EXISTS (SELECT * FROM tenants WHERE id = 'default')
INSERT INTO tenants (id, parent_id, name, active, createdutc, lastupdateutc)
VALUES ('default', NULL, 'Default', 1, CONVERT(NVARCHAR(64), SYSUTCDATETIME(), 126), CONVERT(NVARCHAR(64), SYSUTCDATETIME(), 126));

IF EXISTS (SELECT * FROM sysobjects WHERE name='credential' AND xtype='U')
EXEC sp_rename 'credential', 'credentials';

ALTER TABLE users ADD tenant_id NVARCHAR(32) NOT NULL CONSTRAINT df_users_tenant_id DEFAULT 'default';
ALTER TABLE credentials ADD tenant_id NVARCHAR(32) NOT NULL CONSTRAINT df_credentials_tenant_id DEFAULT 'default';
ALTER TABLE buckets ADD tenant_id NVARCHAR(32) NOT NULL CONSTRAINT df_buckets_tenant_id DEFAULT 'default';
ALTER TABLE objects ADD tenant_id NVARCHAR(32) NOT NULL CONSTRAINT df_objects_tenant_id DEFAULT 'default';
ALTER TABLE bucketacls ADD tenant_id NVARCHAR(32) NOT NULL CONSTRAINT df_bucketacls_tenant_id DEFAULT 'default';
ALTER TABLE objectacls ADD tenant_id NVARCHAR(32) NOT NULL CONSTRAINT df_objectacls_tenant_id DEFAULT 'default';
ALTER TABLE buckettags ADD tenant_id NVARCHAR(32) NOT NULL CONSTRAINT df_buckettags_tenant_id DEFAULT 'default';
ALTER TABLE objecttags ADD tenant_id NVARCHAR(32) NOT NULL CONSTRAINT df_objecttags_tenant_id DEFAULT 'default';
ALTER TABLE uploads ADD tenant_id NVARCHAR(32) NOT NULL CONSTRAINT df_uploads_tenant_id DEFAULT 'default';
ALTER TABLE uploadparts ADD tenant_id NVARCHAR(32) NOT NULL CONSTRAINT df_uploadparts_tenant_id DEFAULT 'default';
ALTER TABLE requesthistory ADD tenant_id NVARCHAR(32) NOT NULL CONSTRAINT df_requesthistory_tenant_id DEFAULT 'default';

CREATE UNIQUE INDEX idx_credentials_accesskey_unique ON credentials (accesskey);
CREATE UNIQUE INDEX idx_buckets_tenant_name_unique ON buckets (tenant_id, name);
CREATE INDEX idx_users_tenant_email ON users (tenant_id, email);
CREATE INDEX idx_objects_tenant_bucket_key ON objects (tenant_id, bucket_id, [key]);
CREATE INDEX idx_requesthistory_tenant_createdutc ON requesthistory (tenant_id, createdutc);

COMMIT TRANSACTION;
```

SQL Server `ALTER TABLE ADD` statements fail if the column already exists. Check existing columns before rerunning the script in a partially migrated environment.

## PrettyID Rewrite

The structural SQL above does not complete the legacy identifier conversion. For each provider, perform an offline ID rewrite:

1. Stop Less3.
2. Generate PrettyID values for tenants, users, credentials, buckets, objects, tags, ACLs, uploads, upload parts, request history rows, roles, permissions, assignments, sessions, and audit rows.
3. Store old-to-new mappings in temporary mapping tables.
4. Update primary identifier columns.
5. Update every dependent reference column.
6. Verify all joins with the mapping tables.
7. Drop old legacy identifier columns only after the application has started and tests have passed against the v3 schema.

Do not expose the migrated node until S3 access, REST authentication, dashboard login, request history, and bucket/object isolation have been verified.
