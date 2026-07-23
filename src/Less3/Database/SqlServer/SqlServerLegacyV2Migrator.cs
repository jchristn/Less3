namespace Less3.Database.SqlServer
{
    using System;
    using Microsoft.Data.SqlClient;
    using SyslogLogging;
    using Less3.Database.SqlServer.Queries;

    internal static class SqlServerLegacyV2Migrator
    {
        internal static void RunIfNeeded(string connectionString, LoggingModule logging, string logHeader)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                if (!IsLegacyV2Schema(conn)) return;

                logging?.Info(logHeader + "detected legacy SQL Server v2 schema; migrating to v3 control plane schema");

                using (SqlTransaction txn = conn.BeginTransaction())
                {
                    try
                    {
                        RenameLegacyTables(conn, txn);
                        Execute(conn, txn, SetupQueries.CreateTablesAndIndices());
                        Execute(conn, txn, LegacyV2MigrationSql.Build(LegacyV2MigrationDialect.SqlServer));
                        txn.Commit();
                    }
                    catch
                    {
                        txn.Rollback();
                        throw;
                    }
                }

                logging?.Info(logHeader + "legacy SQL Server v2 schema migration completed");
            }
        }

        private static bool IsLegacyV2Schema(SqlConnection conn)
        {
            return TableExists(conn, "users")
                && ColumnExists(conn, "users", "guid")
                && !ColumnExists(conn, "users", "tenant_id");
        }

        private static void RenameLegacyTables(SqlConnection conn, SqlTransaction txn)
        {
            foreach (var tableRename in LegacyV2MigrationSql.TableRenames)
            {
                RenameIfExists(conn, txn, tableRename.Key, tableRename.Value);
            }
        }

        private static void RenameIfExists(SqlConnection conn, SqlTransaction txn, string currentName, string legacyName)
        {
            if (!TableExists(conn, currentName, txn)) return;
            if (TableExists(conn, legacyName, txn)) throw new InvalidOperationException("Legacy migration target table already exists: " + legacyName);
            Execute(conn, txn, "EXEC sp_rename '" + EscapeSqlLiteral(currentName) + "', '" + EscapeSqlLiteral(legacyName) + "';");
        }

        private static bool TableExists(SqlConnection conn, string tableName, SqlTransaction txn = null)
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.objects WHERE object_id = OBJECT_ID(@name) AND type = 'U';",
                conn,
                txn))
            {
                cmd.Parameters.AddWithValue("@name", tableName);
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result) > 0;
            }
        }

        private static bool ColumnExists(SqlConnection conn, string tableName, string columnName)
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(@table) AND name = @column;",
                conn))
            {
                cmd.Parameters.AddWithValue("@table", tableName);
                cmd.Parameters.AddWithValue("@column", columnName);
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result) > 0;
            }
        }

        private static string EscapeSqlLiteral(string value)
        {
            return value.Replace("'", "''");
        }

        private static void Execute(SqlConnection conn, SqlTransaction txn, string sql)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn, txn))
            {
                cmd.CommandTimeout = 120;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
