namespace Less3.Database.SqlServer.Queries
{
    using System.Collections.Generic;

    internal static class MigrationQueries
    {
        internal static List<string> GetMigrations()
        {
            List<string> migrations = new List<string>();

            // v2.2.0 to v2.3.0: add request/response body columns
            migrations.Add("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('requesthistory') AND name = 'requestbody') ALTER TABLE requesthistory ADD requestbody NVARCHAR(MAX);");
            migrations.Add("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('requesthistory') AND name = 'responsebody') ALTER TABLE requesthistory ADD responsebody NVARCHAR(MAX);");

            return migrations;
        }
    }
}
