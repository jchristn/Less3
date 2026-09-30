namespace Less3.Database.Sqlite.Queries
{
    using System.Collections.Generic;

    internal static class MigrationQueries
    {
        internal static List<string> GetMigrations()
        {
            List<string> migrations = new List<string>();

            // WatsonORM to custom driver: rename columns and add missing columns
            migrations.Add("ALTER TABLE objects ADD COLUMN expirationutc VARCHAR(64);");
            migrations.Add("ALTER TABLE bucketacls RENAME COLUMN permitfullcontrol TO fullcontrol;");
            migrations.Add("ALTER TABLE objectacls RENAME COLUMN permitfullcontrol TO fullcontrol;");
            migrations.Add("ALTER TABLE buckettags RENAME COLUMN tagkey TO key;");
            migrations.Add("ALTER TABLE buckettags RENAME COLUMN tagvalue TO value;");
            migrations.Add("ALTER TABLE objecttags RENAME COLUMN tagkey TO key;");
            migrations.Add("ALTER TABLE objecttags RENAME COLUMN tagvalue TO value;");
            migrations.Add("ALTER TABLE uploadparts RENAME COLUMN md5 TO md5hash;");
            migrations.Add("ALTER TABLE uploadparts RENAME COLUMN sha1 TO sha1hash;");
            migrations.Add("ALTER TABLE uploadparts RENAME COLUMN sha256 TO sha256hash;");

            // v2.2.0 to v2.3.0: add request/response body columns
            migrations.Add("ALTER TABLE requesthistory ADD COLUMN requestbody TEXT;");
            migrations.Add("ALTER TABLE requesthistory ADD COLUMN responsebody TEXT;");

            // v4.0.0: enforce object version uniqueness as the data-integrity backstop behind the
            // distributed write lock. A single (tenant, bucket, key, version) must resolve to exactly
            // one row, so two writers that both computed the same next version can never both commit.
            // Replaces the earlier non-unique index of the same columns.
            migrations.Add("CREATE UNIQUE INDEX IF NOT EXISTS idx_objects_tenant_bucket_key_version_unique ON objects (tenant_id, bucket_id, key, version);");
            migrations.Add("DROP INDEX IF EXISTS idx_objects_tenant_bucket_key_version;");

            // v4.1.0: S3 versioning compatibility. A bucket whose versioning is suspended keeps its
            // versions; the null version identifies the single row a suspended write or delete replaces.
            migrations.Add("ALTER TABLE buckets ADD COLUMN versioningsuspended INT NOT NULL DEFAULT 0;");
            migrations.Add("ALTER TABLE objects ADD COLUMN nullversion INT NOT NULL DEFAULT 0;");


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
