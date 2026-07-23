namespace Less3.Database.MySql
{
    using System;
    using global::MySql.Data.MySqlClient;
    using SyslogLogging;
    using Less3.Database.MySql.Queries;

    internal static class MySqlLegacyV2Migrator
    {
        internal static void RunIfNeeded(string connectionString, LoggingModule logging, string logHeader)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                if (!IsLegacyV2Schema(conn)) return;

                logging?.Info(logHeader + "detected legacy MySQL v2 schema; migrating to v3 control plane schema");

                RenameLegacyTables(conn);
                Execute(conn, SetupQueries.CreateTables());
                Execute(conn, LegacyV2MigrationSql.Build(LegacyV2MigrationDialect.MySql));

                logging?.Info(logHeader + "legacy MySQL v2 schema migration completed");
            }
        }

        private static bool IsLegacyV2Schema(MySqlConnection conn)
        {
            return TableExists(conn, "users")
                && ColumnExists(conn, "users", "guid")
                && !ColumnExists(conn, "users", "tenant_id");
        }

        private static void RenameLegacyTables(MySqlConnection conn)
        {
            foreach (var tableRename in LegacyV2MigrationSql.TableRenames)
            {
                RenameIfExists(conn, tableRename.Key, tableRename.Value);
            }
        }

        private static void RenameIfExists(MySqlConnection conn, string currentName, string legacyName)
        {
            if (!TableExists(conn, currentName)) return;
            if (TableExists(conn, legacyName)) throw new InvalidOperationException("Legacy migration target table already exists: " + legacyName);
            Execute(conn, "RENAME TABLE `" + currentName + "` TO `" + legacyName + "`;");
        }

        private static bool TableExists(MySqlConnection conn, string tableName)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @name;",
                conn))
            {
                cmd.Parameters.AddWithValue("@name", tableName);
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result) > 0;
            }
        }

        private static bool ColumnExists(MySqlConnection conn, string tableName, string columnName)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = @table AND column_name = @column;",
                conn))
            {
                cmd.Parameters.AddWithValue("@table", tableName);
                cmd.Parameters.AddWithValue("@column", columnName);
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result) > 0;
            }
        }

        private static void Execute(MySqlConnection conn, string sql)
        {
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 120;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
