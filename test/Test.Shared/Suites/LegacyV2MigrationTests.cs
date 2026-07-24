namespace Test.Shared.Suites
{
    using System;
    using System.IO;
    using Less3.Database;
    using Less3.Database.Sqlite;
    using Microsoft.Data.Sqlite;
    using SyslogLogging;

    /// <summary>
    /// Tests v2-to-v3 database migration coverage.
    /// </summary>
    public class LegacyV2MigrationTests : TestSuite
    {
        /// <inheritdoc />
        public override string Name => "Legacy v2 Migration Tests";

        /// <inheritdoc />
        public override async System.Threading.Tasks.Task RunTestsAsync()
        {
            await RunTest("LegacyV2Migration_DriversRunMigratorBeforeSchemaSetup", DriversRunMigratorBeforeSchemaSetup).ConfigureAwait(false);
            await RunTest("LegacyV2Migration_ProviderMigratorsDetectAndCopyLegacySchema", ProviderMigratorsDetectAndCopyLegacySchema).ConfigureAwait(false);
            await RunTest("LegacyV2Migration_SharedSqlContainsProviderSpecificSyntax", SharedSqlContainsProviderSpecificSyntax).ConfigureAwait(false);
            await RunTest("LegacyV2Migration_SqliteDriverMigratesLegacySchema", SqliteDriverMigratesLegacySchema).ConfigureAwait(false);
        }

        private static void DriversRunMigratorBeforeSchemaSetup()
        {
            foreach ((string Path, string MigratorCall, string SetupCall) item in new[]
            {
                ("Sqlite/SqliteDatabaseDriver.cs", "SqliteLegacyV2Migrator.RunIfNeeded", "SetupQueries.CreateTablesAndIndices"),
                ("MySql/MySqlDatabaseDriver.cs", "MySqlLegacyV2Migrator.RunIfNeeded", "SetupQueries.CreateTables"),
                ("PostgreSql/PostgreSqlDatabaseDriver.cs", "PostgreSqlLegacyV2Migrator.RunIfNeeded", "SetupQueries.CreateTablesAndIndices"),
                ("SqlServer/SqlServerDatabaseDriver.cs", "SqlServerLegacyV2Migrator.RunIfNeeded", "SetupQueries.CreateTablesAndIndices")
            })
            {
                string source = ReadDatabaseFile(item.Path);
                int migratorIndex = source.IndexOf(item.MigratorCall, StringComparison.Ordinal);
                int setupIndex = source.IndexOf(item.SetupCall, StringComparison.Ordinal);

                Ensure(migratorIndex >= 0, item.Path + " must call " + item.MigratorCall);
                Ensure(setupIndex >= 0, item.Path + " must call " + item.SetupCall);
                Ensure(migratorIndex < setupIndex, item.Path + " must run legacy migration before v3 schema setup");
            }
        }

        private static void ProviderMigratorsDetectAndCopyLegacySchema()
        {
            foreach ((string Path, string Dialect) item in new[]
            {
                ("MySql/MySqlLegacyV2Migrator.cs", "LegacyV2MigrationDialect.MySql"),
                ("PostgreSql/PostgreSqlLegacyV2Migrator.cs", "LegacyV2MigrationDialect.PostgreSql"),
                ("SqlServer/SqlServerLegacyV2Migrator.cs", "LegacyV2MigrationDialect.SqlServer")
            })
            {
                string source = ReadDatabaseFile(item.Path);

                EnsureContains(source, "IsLegacyV2Schema", item.Path);
                EnsureContains(source, "\"users\"", item.Path);
                EnsureContains(source, "\"guid\"", item.Path);
                EnsureContains(source, "\"tenant_id\"", item.Path);
                EnsureContains(source, "LegacyV2MigrationSql.TableRenames", item.Path);
                EnsureContains(source, "LegacyV2MigrationSql.Build(" + item.Dialect + ")", item.Path);
            }
        }

        private static void SharedSqlContainsProviderSpecificSyntax()
        {
            string source = ReadDatabaseFile("LegacyV2MigrationSql.cs");

            EnsureContains(source, "INSERT OR IGNORE INTO", "sqlite conflict syntax");
            EnsureContains(source, "INSERT IGNORE INTO", "mysql conflict syntax");
            EnsureContains(source, "ON CONFLICT DO NOTHING", "postgres conflict syntax");
            EnsureContains(source, "MERGE", "sql server conflict syntax");
            EnsureContains(source, "`key`", "mysql key quoting");
            EnsureContains(source, "[key]", "sql server key quoting");
            EnsureContains(source, "CURRENT_TIMESTAMP", "sqlite timestamp");
            EnsureContains(source, "UTC_TIMESTAMP(6)", "mysql timestamp");
            EnsureContains(source, "SYSUTCDATETIME()", "sql server timestamp");
            EnsureContains(source, "CASE WHEN guid = 'default' THEN 1 ELSE 0 END", "default tenant migration");
        }

        private static void SqliteDriverMigratesLegacySchema()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "less3-sqlite-v2-migration-" + TestIds.Suffix());
            Directory.CreateDirectory(tempDirectory);

            try
            {
                string databasePath = Path.Combine(tempDirectory, "less3.db");
                CreateLegacyV2Database(databasePath);

                using (SqliteDatabaseDriver driver = CreateDriver(databasePath))
                {
                }

                using (SqliteDatabaseDriver driver = CreateDriver(databasePath))
                {
                }

                using SqliteConnection conn = new SqliteConnection("Data Source=" + databasePath + ";");
                conn.Open();

                Ensure(!ColumnExists(conn, "users", "guid"), "users.guid should be removed");
                Ensure(!ColumnExists(conn, "credentials", "guid"), "credentials.guid should be removed");
                Ensure(!ColumnExists(conn, "buckets", "guid"), "buckets.guid should be removed");
                Ensure(!ColumnExists(conn, "objects", "guid"), "objects.guid should be removed");

                EnsureEqual("admin@less3", ScalarString(conn, "SELECT email FROM users WHERE id = 'usr_default_admin';"), "default admin email");
                EnsureEqual("password", ScalarString(conn, "SELECT passwordhash FROM users WHERE id = 'usr_default_admin';"), "default admin password");
                EnsureEqual("default", ScalarString(conn, "SELECT accesskey FROM credentials WHERE id = 'crd_default';"), "default access key");
                EnsureEqual("default", ScalarString(conn, "SELECT secretkey FROM credentials WHERE id = 'crd_default';"), "default secret key");
                EnsureEqual("default", ScalarString(conn, "SELECT name FROM buckets WHERE id = 'bkt_default';"), "default bucket name");

                EnsureEqual(1L, ScalarLong(conn, "SELECT COUNT(*) FROM objects WHERE id = 'obj_legacy_1' AND bucket_id = 'bkt_default';"), "object migrated");
                EnsureEqual(1L, ScalarLong(conn, "SELECT COUNT(*) FROM bucketacls WHERE id = 'bac_legacy_1' AND bucket_id = 'bkt_default' AND user_id = 'usr_default_admin';"), "bucket ACL migrated");
                EnsureEqual(1L, ScalarLong(conn, "SELECT COUNT(*) FROM objectacls WHERE id = 'oac_legacy_1' AND object_id = 'obj_legacy_1';"), "object ACL migrated");
                EnsureEqual(1L, ScalarLong(conn, "SELECT COUNT(*) FROM buckettags WHERE id = 'btg_legacy_1' AND key = 'environment' AND value = 'test';"), "bucket tag migrated");
                EnsureEqual(1L, ScalarLong(conn, "SELECT COUNT(*) FROM objecttags WHERE id = 'otg_legacy_1' AND object_id = 'obj_legacy_1';"), "object tag migrated");
                EnsureEqual(1L, ScalarLong(conn, "SELECT COUNT(*) FROM uploads WHERE id = 'upl_legacy_1' AND bucket_id = 'bkt_default';"), "upload migrated");
                EnsureEqual(1L, ScalarLong(conn, "SELECT COUNT(*) FROM uploadparts WHERE id = 'prt_legacy_1' AND upload_id = 'upl_legacy_1';"), "upload part migrated");
                EnsureEqual(1L, ScalarLong(conn, "SELECT COUNT(*) FROM requesthistory WHERE id = 'req_legacy_1' AND user_id = 'usr_default_admin';"), "request history migrated");
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
            }
        }

        private static SqliteDatabaseDriver CreateDriver(string databasePath)
        {
            DatabaseSettings settings = new DatabaseSettings(databasePath);
            LoggingModule logging = new LoggingModule("127.0.0.1", 514, false);
            return new SqliteDatabaseDriver(settings, logging);
        }

        private static void CreateLegacyV2Database(string databasePath)
        {
            using SqliteConnection conn = new SqliteConnection("Data Source=" + databasePath + ";");
            conn.Open();
            Execute(conn, LegacySchemaSql());
            Execute(conn, LegacySeedSql());
        }

        private static bool ColumnExists(SqliteConnection conn, string tableName, string columnName)
        {
            using SqliteCommand cmd = new SqliteCommand("PRAGMA table_info(" + tableName + ");", conn);
            using SqliteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (String.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ScalarString(SqliteConnection conn, string sql)
        {
            object value = Scalar(conn, sql);
            return value?.ToString() ?? String.Empty;
        }

        private static long ScalarLong(SqliteConnection conn, string sql)
        {
            return Convert.ToInt64(Scalar(conn, sql));
        }

        private static object Scalar(SqliteConnection conn, string sql)
        {
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            return cmd.ExecuteScalar();
        }

        private static void Execute(SqliteConnection conn, string sql)
        {
            using SqliteCommand cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        private static string ReadDatabaseFile(string relativePath)
        {
            string root = LocateRepositoryRoot();
            string path = Path.Combine(root, "src", "Less3", "Database", relativePath.Replace('/', Path.DirectorySeparatorChar));
            return File.ReadAllText(path);
        }

        private static string LocateRepositoryRoot()
        {
            string current = AppContext.BaseDirectory;

            while (!String.IsNullOrEmpty(current))
            {
                if (Directory.Exists(Path.Combine(current, "src", "Less3", "Database")))
                {
                    return current;
                }

                DirectoryInfo parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            throw new DirectoryNotFoundException("Unable to locate repository root from " + AppContext.BaseDirectory);
        }

        private static string LegacySchemaSql()
        {
            return @"
                CREATE TABLE buckets (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid VARCHAR(64) NOT NULL, ownerguid VARCHAR(64) NOT NULL, name VARCHAR(256) NOT NULL, regionstring VARCHAR(32) NOT NULL, storagetype VARCHAR(16) NOT NULL, diskdirectory VARCHAR(256) NOT NULL, enableversioning TINYINT NOT NULL, enablepublicwrite TINYINT NOT NULL, enablepublicread TINYINT NOT NULL, createdutc TEXT NOT NULL);
                CREATE TABLE bucketacls (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid VARCHAR(64) NOT NULL, usergroup VARCHAR(256), bucketguid VARCHAR(64), userguid VARCHAR(64), issuedbyuserguid VARCHAR(64), permitread TINYINT NOT NULL, permitwrite TINYINT NOT NULL, permitreadacp TINYINT NOT NULL, permitwriteacp TINYINT NOT NULL, fullcontrol TINYINT NOT NULL, createdutc TEXT NOT NULL);
                CREATE TABLE buckettags (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid VARCHAR(64) NOT NULL, bucketguid VARCHAR(64) NOT NULL, key VARCHAR(256) NOT NULL, value VARCHAR(1024), createdutc TEXT NOT NULL);
                CREATE TABLE credential (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid VARCHAR(64) NOT NULL, userguid VARCHAR(64) NOT NULL, description VARCHAR(256), accesskey VARCHAR(256) NOT NULL, secretkey VARCHAR(256) NOT NULL, isbase64 TINYINT NOT NULL, createdutc TEXT NOT NULL);
                CREATE TABLE objects (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid VARCHAR(64) NOT NULL, bucketguid VARCHAR(64) NOT NULL, ownerguid VARCHAR(64) NOT NULL, authorguid VARCHAR(64) NOT NULL, key VARCHAR(256) NOT NULL, contenttype VARCHAR(128), contentlength BIGINT NOT NULL, version BIGINT NOT NULL, etag VARCHAR(64), retention VARCHAR(32), blobfilename VARCHAR(256) NOT NULL, isfolder TINYINT NOT NULL, deletemarker TINYINT NOT NULL, md5 VARCHAR(32), createdutc TEXT NOT NULL, lastupdateutc TEXT NOT NULL, lastaccessutc TEXT NOT NULL, metadata VARCHAR(4096), expirationutc VARCHAR(64));
                CREATE TABLE objectacls (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid VARCHAR(64) NOT NULL, usergroup VARCHAR(256), userguid VARCHAR(64), issuedbyuserguid VARCHAR(64), bucketguid VARCHAR(64) NOT NULL, objectguid VARCHAR(64) NOT NULL, permitread TINYINT NOT NULL, permitwrite TINYINT NOT NULL, permitreadacp TINYINT NOT NULL, permitwriteacp TINYINT NOT NULL, fullcontrol TINYINT NOT NULL, createdutc TEXT NOT NULL);
                CREATE TABLE objecttags (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid VARCHAR(64) NOT NULL, bucketguid VARCHAR(64) NOT NULL, objectguid VARCHAR(64) NOT NULL, key VARCHAR(256) NOT NULL, value VARCHAR(1024), createdutc TEXT NOT NULL);
                CREATE TABLE uploads (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid VARCHAR(64) NOT NULL, bucketguid VARCHAR(64) NOT NULL, ownerguid VARCHAR(64) NOT NULL, authorguid VARCHAR(64) NOT NULL, key VARCHAR(256) NOT NULL, createdutc TEXT NOT NULL, lastaccessutc TEXT NOT NULL, expirationutc TEXT NOT NULL, contenttype VARCHAR(256), metadata VARCHAR(4096));
                CREATE TABLE uploadparts (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid VARCHAR(64) NOT NULL, bucketguid VARCHAR(64) NOT NULL, ownerguid VARCHAR(64) NOT NULL, uploadguid VARCHAR(64) NOT NULL, partnumber INTEGER NOT NULL, partlength INTEGER NOT NULL, md5hash VARCHAR(32) NOT NULL, sha1hash VARCHAR(40) NOT NULL, sha256hash VARCHAR(64) NOT NULL, lastaccessutc TEXT NOT NULL, createdutc TEXT NOT NULL);
                CREATE TABLE users (id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, guid VARCHAR(64) NOT NULL, name VARCHAR(256) NOT NULL, email VARCHAR(256) NOT NULL, createdutc TEXT NOT NULL);
                CREATE TABLE requesthistory (id INTEGER PRIMARY KEY AUTOINCREMENT, guid VARCHAR(64) NOT NULL, httpmethod VARCHAR(16), requesturl VARCHAR(2048), sourceip VARCHAR(64), statuscode INT NOT NULL DEFAULT 0, success INT NOT NULL DEFAULT 1, durationms INTEGER NOT NULL DEFAULT 0, requesttype VARCHAR(128), userguid VARCHAR(64), accesskey VARCHAR(256), requestcontenttype VARCHAR(256), requestbodylength INTEGER NOT NULL DEFAULT 0, responsecontenttype VARCHAR(256), responsebodylength INTEGER NOT NULL DEFAULT 0, requestbody TEXT, responsebody TEXT, createdutc VARCHAR(64) NOT NULL);
            ";
        }

        private static string LegacySeedSql()
        {
            return @"
                INSERT INTO users (guid, name, email, createdutc) VALUES ('default', 'Default user', 'default@default.com', '2026-01-01T00:00:00Z');
                INSERT INTO credential (guid, userguid, description, accesskey, secretkey, isbase64, createdutc) VALUES ('credential-guid', 'default', 'My first access key', 'default', 'default', 0, '2026-01-01T00:00:00Z');
                INSERT INTO buckets (guid, ownerguid, name, regionstring, storagetype, diskdirectory, enableversioning, enablepublicwrite, enablepublicread, createdutc) VALUES ('bucket-guid', 'default', 'default', 'us-west-1', '0', './disk/default/Objects/', 0, 0, 1, '2026-01-01T00:00:00Z');
                INSERT INTO bucketacls (guid, usergroup, bucketguid, userguid, issuedbyuserguid, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc) VALUES ('bucket-acl-guid', NULL, 'bucket-guid', 'default', 'default', 1, 0, 0, 0, 0, '2026-01-01T00:00:00Z');
                INSERT INTO buckettags (guid, bucketguid, key, value, createdutc) VALUES ('bucket-tag-guid', 'bucket-guid', 'environment', 'test', '2026-01-01T00:00:00Z');
                INSERT INTO objects (guid, bucketguid, ownerguid, authorguid, key, contenttype, contentlength, version, etag, retention, blobfilename, isfolder, deletemarker, md5, createdutc, lastupdateutc, lastaccessutc, metadata, expirationutc) VALUES ('object-guid', 'bucket-guid', 'default', 'default', 'hello.txt', 'text/plain', 5, 1, 'etag', NULL, 'blob.dat', 0, 0, 'md5', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', '{}', NULL);
                INSERT INTO objectacls (guid, usergroup, userguid, issuedbyuserguid, bucketguid, objectguid, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc) VALUES ('object-acl-guid', NULL, 'default', 'default', 'bucket-guid', 'object-guid', 1, 0, 0, 0, 0, '2026-01-01T00:00:00Z');
                INSERT INTO objecttags (guid, bucketguid, objectguid, key, value, createdutc) VALUES ('object-tag-guid', 'bucket-guid', 'object-guid', 'kind', 'text', '2026-01-01T00:00:00Z');
                INSERT INTO uploads (guid, bucketguid, ownerguid, authorguid, key, createdutc, lastaccessutc, expirationutc, contenttype, metadata) VALUES ('upload-guid', 'bucket-guid', 'default', 'default', 'large.bin', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', '2026-01-02T00:00:00Z', 'application/octet-stream', '{}');
                INSERT INTO uploadparts (guid, bucketguid, ownerguid, uploadguid, partnumber, partlength, md5hash, sha1hash, sha256hash, lastaccessutc, createdutc) VALUES ('part-guid', 'bucket-guid', 'default', 'upload-guid', 1, 5, 'md5', 'sha1', 'sha256', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z');
                INSERT INTO requesthistory (guid, httpmethod, requesturl, sourceip, statuscode, success, durationms, requesttype, userguid, accesskey, requestcontenttype, requestbodylength, responsecontenttype, responsebodylength, requestbody, responsebody, createdutc) VALUES ('request-guid', 'GET', '/default/hello.txt', '127.0.0.1', 200, 1, 10, 'ObjectRead', 'default', 'default', NULL, 0, NULL, 5, NULL, NULL, '2026-01-01T00:00:00Z');
            ";
        }

        private static void Ensure(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void EnsureContains(string source, string expected, string message)
        {
            if (source == null || !source.Contains(expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(message + " must contain " + expected);
            }
        }

        private static void EnsureEqual<T>(T expected, T actual, string message)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(message + " expected [" + expected + "] but received [" + actual + "].");
            }
        }
    }
}
