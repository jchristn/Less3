namespace Less3.Database.MySql.Queries
{
    using System.Collections.Generic;

    internal static class MigrationQueries
    {
        internal static List<string> GetMigrations()
        {
            List<string> migrations = new List<string>();

            // v2.2.0 to v2.3.0: add request/response body columns
            migrations.Add("ALTER TABLE requesthistory ADD COLUMN requestbody MEDIUMTEXT;");
            migrations.Add("ALTER TABLE requesthistory ADD COLUMN responsebody MEDIUMTEXT;");

            return migrations;
        }
    }
}
