namespace Less3.Database.SqlServer.Queries
{
    using System.Collections.Generic;

    internal static class MigrationQueries
    {
        internal static List<string> GetMigrations()
        {
            List<string> migrations = new List<string>();

            // WatsonORM to custom driver: rename columns and add missing columns
            migrations.Add("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('objects') AND name = 'expirationutc') ALTER TABLE objects ADD expirationutc NVARCHAR(64);");
            migrations.Add("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('bucketacls') AND name = 'permitfullcontrol') EXEC sp_rename 'bucketacls.permitfullcontrol', 'fullcontrol', 'COLUMN';");
            migrations.Add("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('objectacls') AND name = 'permitfullcontrol') EXEC sp_rename 'objectacls.permitfullcontrol', 'fullcontrol', 'COLUMN';");
            migrations.Add("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('buckettags') AND name = 'tagkey') EXEC sp_rename 'buckettags.tagkey', 'key', 'COLUMN';");
            migrations.Add("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('buckettags') AND name = 'tagvalue') EXEC sp_rename 'buckettags.tagvalue', 'value', 'COLUMN';");
            migrations.Add("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('objecttags') AND name = 'tagkey') EXEC sp_rename 'objecttags.tagkey', 'key', 'COLUMN';");
            migrations.Add("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('objecttags') AND name = 'tagvalue') EXEC sp_rename 'objecttags.tagvalue', 'value', 'COLUMN';");
            migrations.Add("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('uploadparts') AND name = 'md5') EXEC sp_rename 'uploadparts.md5', 'md5hash', 'COLUMN';");
            migrations.Add("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('uploadparts') AND name = 'sha1') EXEC sp_rename 'uploadparts.sha1', 'sha1hash', 'COLUMN';");
            migrations.Add("IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('uploadparts') AND name = 'sha256') EXEC sp_rename 'uploadparts.sha256', 'sha256hash', 'COLUMN';");

            // v2.2.0 to v2.3.0: add request/response body columns
            migrations.Add("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('requesthistory') AND name = 'requestbody') ALTER TABLE requesthistory ADD requestbody NVARCHAR(MAX);");
            migrations.Add("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('requesthistory') AND name = 'responsebody') ALTER TABLE requesthistory ADD responsebody NVARCHAR(MAX);");

            // v4.0.0: enforce object version uniqueness as the data-integrity backstop behind the
            // distributed write lock. A single (tenant, bucket, key, version) must resolve to exactly
            // one row, so two writers that both computed the same next version can never both commit.
            // Replaces the earlier non-unique index of the same columns.
            migrations.Add("IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_tenant_bucket_key_version_unique') CREATE UNIQUE INDEX idx_objects_tenant_bucket_key_version_unique ON objects (tenant_id, bucket_id, [key], version);");
            migrations.Add("IF EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_tenant_bucket_key_version') DROP INDEX idx_objects_tenant_bucket_key_version ON objects;");

            // v4.1.0: S3 versioning compatibility. A bucket whose versioning is suspended keeps its
            // versions; the null version identifies the single row a suspended write or delete replaces.
            migrations.Add("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('buckets') AND name = 'versioningsuspended') ALTER TABLE buckets ADD versioningsuspended BIT NOT NULL DEFAULT 0;");
            migrations.Add("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('objects') AND name = 'nullversion') ALTER TABLE objects ADD nullversion BIT NOT NULL DEFAULT 0;");

            // v4.1.0: S3 object keys are case-sensitive byte strings. The server default collation is
            // usually case-insensitive, which made "Photo.jpg" and "photo.jpg" the same key. Changing the
            // column collation requires dropping and recreating the indexes that include it.
            migrations.Add(
                "IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('objects') AND name = 'key' AND collation_name <> 'Latin1_General_100_BIN2') "
                + "BEGIN "
                + "IF EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_key') DROP INDEX idx_objects_key ON objects; "
                + "IF EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_tenant_bucket_key') DROP INDEX idx_objects_tenant_bucket_key ON objects; "
                + "IF EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_tenant_bucket_key_version_unique') DROP INDEX idx_objects_tenant_bucket_key_version_unique ON objects; "
                + "ALTER TABLE objects ALTER COLUMN [key] NVARCHAR(1024) COLLATE Latin1_General_100_BIN2; "
                + "CREATE INDEX idx_objects_key ON objects ([key]); "
                + "CREATE INDEX idx_objects_tenant_bucket_key ON objects (tenant_id, bucket_id, [key]); "
                + "END");

            // v4.1.0: '=' and unique indexes ignore trailing spaces, so a unique index on [key] would treat "a"
            // and "a " as the same key. The uniqueness backstop uses the SHA-256 of the key instead.
            migrations.Add("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('objects') AND name = 'keyhash') ALTER TABLE objects ADD keyhash AS CAST(HASHBYTES('SHA2_256', [key]) AS BINARY(32)) PERSISTED;");
            migrations.Add("IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_tenant_bucket_keyhash_version_unique') CREATE UNIQUE INDEX idx_objects_tenant_bucket_keyhash_version_unique ON objects (tenant_id, bucket_id, keyhash, version);");
            migrations.Add("IF EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_tenant_bucket_key_version_unique') DROP INDEX idx_objects_tenant_bucket_key_version_unique ON objects;");


            // v4.1.0: before 4.1, suspending versioning simply turned it off, so a bucket with versioning off
            // but with numbered versions (version > 1, not null versions) was suspended; mark it so. Then mark
            // the rows of buckets that never had versioning as null versions. Both statements are idempotent:
            // rows written by 4.1 in unversioned buckets are always null versions.
            migrations.Add("UPDATE buckets SET versioningsuspended = 1 WHERE enableversioning = 0 AND versioningsuspended = 0 AND EXISTS (SELECT 1 FROM objects o WHERE o.bucket_id = buckets.id AND o.version > 1 AND o.nullversion = 0);");
            migrations.Add("UPDATE objects SET nullversion = 1 WHERE nullversion = 0 AND bucket_id IN (SELECT b.id FROM buckets b WHERE b.enableversioning = 0 AND b.versioningsuspended = 0);");

            return migrations;
        }
    }
}
