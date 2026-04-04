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

            return migrations;
        }
    }
}
