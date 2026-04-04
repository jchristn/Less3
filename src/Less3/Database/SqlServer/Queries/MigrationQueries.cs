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

            return migrations;
        }
    }
}
