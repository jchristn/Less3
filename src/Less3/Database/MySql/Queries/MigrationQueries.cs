namespace Less3.Database.MySql.Queries
{
    using System.Collections.Generic;

    internal static class MigrationQueries
    {
        internal static List<string> GetMigrations()
        {
            List<string> migrations = new List<string>();

            // WatsonORM to custom driver: rename columns and add missing columns
            migrations.Add("ALTER TABLE objects ADD COLUMN expirationutc DATETIME(6);");
            migrations.Add("ALTER TABLE bucketacls RENAME COLUMN permitfullcontrol TO fullcontrol;");
            migrations.Add("ALTER TABLE objectacls RENAME COLUMN permitfullcontrol TO fullcontrol;");
            migrations.Add("ALTER TABLE buckettags RENAME COLUMN tagkey TO `key`;");
            migrations.Add("ALTER TABLE buckettags RENAME COLUMN tagvalue TO value;");
            migrations.Add("ALTER TABLE objecttags RENAME COLUMN tagkey TO `key`;");
            migrations.Add("ALTER TABLE objecttags RENAME COLUMN tagvalue TO value;");
            migrations.Add("ALTER TABLE uploadparts RENAME COLUMN md5 TO md5hash;");
            migrations.Add("ALTER TABLE uploadparts RENAME COLUMN sha1 TO sha1hash;");
            migrations.Add("ALTER TABLE uploadparts RENAME COLUMN sha256 TO sha256hash;");

            // v2.2.0 to v2.3.0: add request/response body columns
            migrations.Add("ALTER TABLE requesthistory ADD COLUMN requestbody MEDIUMTEXT;");
            migrations.Add("ALTER TABLE requesthistory ADD COLUMN responsebody MEDIUMTEXT;");

            // v4.0.0: enforce object version uniqueness as the data-integrity backstop behind the
            // distributed write lock. A single (tenant, bucket, key, version) must resolve to exactly
            // one row, so two writers that both computed the same next version can never both commit.
            // Replaces the earlier non-unique index of the same columns.
            migrations.Add("CREATE UNIQUE INDEX idx_objects_tenant_bucket_key_version_unique ON objects (tenant_id, bucket_id, `key`, version);");
            migrations.Add("DROP INDEX idx_objects_tenant_bucket_key_version ON objects;");

            return migrations;
        }
    }
}
