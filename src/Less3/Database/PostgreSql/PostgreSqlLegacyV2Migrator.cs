namespace Less3.Database.PostgreSql
{
    using System;
    using Npgsql;
    using SyslogLogging;
    using Less3.Database.PostgreSql.Queries;

    internal static class PostgreSqlLegacyV2Migrator
    {
        internal static void RunIfNeeded(string connectionString, LoggingModule logging, string logHeader)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                if (!IsLegacyV2Schema(conn)) return;

                logging?.Info(logHeader + "detected legacy PostgreSQL v2 schema; migrating to v3 control plane schema");

                using (NpgsqlTransaction txn = conn.BeginTransaction())
                {
                    try
                    {
                        RenameLegacyTables(conn, txn);
                        Execute(conn, txn, SetupQueries.CreateTablesAndIndices());
                        Execute(conn, txn, LegacyV2MigrationSql.Build(LegacyV2MigrationDialect.PostgreSql));
                        txn.Commit();
                    }
                    catch
                    {
                        txn.Rollback();
                        throw;
                    }
                }

                logging?.Info(logHeader + "legacy PostgreSQL v2 schema migration completed");
            }
        }

        private static bool IsLegacyV2Schema(NpgsqlConnection conn)
        {
            return TableExists(conn, "users")
                && ColumnExists(conn, "users", "guid")
                && !ColumnExists(conn, "users", "tenant_id");
        }

        private static void RenameLegacyTables(NpgsqlConnection conn, NpgsqlTransaction txn)
        {
            foreach (var tableRename in LegacyV2MigrationSql.TableRenames)
            {
                RenameIfExists(conn, txn, tableRename.Key, tableRename.Value);
            }
        }

        private static void RenameIfExists(NpgsqlConnection conn, NpgsqlTransaction txn, string currentName, string legacyName)
        {
            if (!TableExists(conn, currentName, txn)) return;
            if (TableExists(conn, legacyName, txn)) throw new InvalidOperationException("Legacy migration target table already exists: " + legacyName);
            Execute(conn, txn, "ALTER TABLE " + QuoteIdentifier(currentName) + " RENAME TO " + QuoteIdentifier(legacyName) + ";");
        }

        private static bool TableExists(NpgsqlConnection conn, string tableName, NpgsqlTransaction txn = null)
        {
            using (NpgsqlCommand cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = @name;",
                conn,
                txn))
            {
                cmd.Parameters.AddWithValue("@name", tableName);
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result) > 0;
            }
        }

        private static bool ColumnExists(NpgsqlConnection conn, string tableName, string columnName)
        {
            using (NpgsqlCommand cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = @table AND column_name = @column;",
                conn))
            {
                cmd.Parameters.AddWithValue("@table", tableName);
                cmd.Parameters.AddWithValue("@column", columnName);
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result) > 0;
            }
        }

        private static string QuoteIdentifier(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static void Execute(NpgsqlConnection conn, NpgsqlTransaction txn, string sql)
        {
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn, txn))
            {
                cmd.CommandTimeout = 120;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
