namespace Less3.Database.Sqlite
{
    using System;
    using Microsoft.Data.Sqlite;
    using SyslogLogging;
    using Less3.Database.Sqlite.Queries;

    internal static class SqliteLegacyV2Migrator
    {
        internal static void RunIfNeeded(string connectionString, LoggingModule logging, string logHeader)
        {
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                conn.Open();

                if (!IsLegacyV2Schema(conn)) return;

                logging?.Info(logHeader + "detected legacy SQLite v2 schema; migrating to v3 control plane schema");

                using (SqliteTransaction txn = conn.BeginTransaction())
                {
                    try
                    {
                        RenameLegacyTables(conn, txn);
                        Execute(conn, txn, SetupQueries.CreateTablesAndIndices());
                        Execute(conn, txn, LegacyDataMigrationSql());
                        txn.Commit();
                    }
                    catch
                    {
                        txn.Rollback();
                        throw;
                    }
                }

                logging?.Info(logHeader + "legacy SQLite v2 schema migration completed");
            }
        }

        private static bool IsLegacyV2Schema(SqliteConnection conn)
        {
            return TableExists(conn, "users")
                && ColumnExists(conn, "users", "guid")
                && !ColumnExists(conn, "users", "tenant_id");
        }

        private static void RenameLegacyTables(SqliteConnection conn, SqliteTransaction txn)
        {
            RenameIfExists(conn, txn, "users", "users_legacy_v2");
            RenameIfExists(conn, txn, "credential", "credential_legacy_v2");
            RenameIfExists(conn, txn, "buckets", "buckets_legacy_v2");
            RenameIfExists(conn, txn, "objects", "objects_legacy_v2");
            RenameIfExists(conn, txn, "bucketacls", "bucketacls_legacy_v2");
            RenameIfExists(conn, txn, "objectacls", "objectacls_legacy_v2");
            RenameIfExists(conn, txn, "buckettags", "buckettags_legacy_v2");
            RenameIfExists(conn, txn, "objecttags", "objecttags_legacy_v2");
            RenameIfExists(conn, txn, "uploads", "uploads_legacy_v2");
            RenameIfExists(conn, txn, "uploadparts", "uploadparts_legacy_v2");
            RenameIfExists(conn, txn, "requesthistory", "requesthistory_legacy_v2");
        }

        private static void RenameIfExists(SqliteConnection conn, SqliteTransaction txn, string currentName, string legacyName)
        {
            if (!TableExists(conn, currentName, txn)) return;
            if (TableExists(conn, legacyName, txn)) throw new InvalidOperationException("Legacy migration target table already exists: " + legacyName);
            Execute(conn, txn, "ALTER TABLE " + currentName + " RENAME TO " + legacyName + ";");
        }

        private static bool TableExists(SqliteConnection conn, string tableName, SqliteTransaction txn = null)
        {
            using (SqliteCommand cmd = new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name;", conn, txn))
            {
                cmd.Parameters.AddWithValue("@name", tableName);
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result) > 0;
            }
        }

        private static bool ColumnExists(SqliteConnection conn, string tableName, string columnName)
        {
            using (SqliteCommand cmd = new SqliteCommand("PRAGMA table_info(" + tableName + ");", conn))
            using (SqliteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (String.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }

            return false;
        }

        private static void Execute(SqliteConnection conn, SqliteTransaction txn, string sql)
        {
            using (SqliteCommand cmd = new SqliteCommand(sql, conn, txn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static string LegacyDataMigrationSql()
        {
            return @"
                INSERT OR IGNORE INTO tenants (id, parent_id, name, active, createdutc, lastupdateutc)
                VALUES ('default', NULL, 'Default', 1, strftime('%Y-%m-%d %H:%M:%f', 'now'), strftime('%Y-%m-%d %H:%M:%f', 'now'));

                INSERT OR IGNORE INTO users (id, tenant_id, name, email, passwordhash, isadmin, istenantadmin, active, createdutc)
                SELECT
                    CASE WHEN guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || id END,
                    'default',
                    CASE WHEN name IS NULL OR name = '' THEN 'Migrated user' ELSE name END,
                    CASE WHEN guid = 'default' THEN 'admin@less3' ELSE email END,
                    CASE WHEN guid = 'default' THEN 'password' ELSE '' END,
                    CASE WHEN guid = 'default' THEN 1 ELSE 0 END,
                    CASE WHEN guid = 'default' THEN 1 ELSE 0 END,
                    1,
                    createdutc
                FROM users_legacy_v2;

                INSERT OR IGNORE INTO credentials (id, tenant_id, user_id, description, accesskey, secretkey, isbase64, active, lastusedutc, lastfailedutc, createdutc)
                SELECT
                    CASE WHEN accesskey = 'default' THEN 'crd_default' ELSE 'crd_legacy_' || c.id END,
                    'default',
                    CASE
                        WHEN c.userguid = 'default' THEN 'usr_default_admin'
                        ELSE COALESCE((SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || u.id END FROM users_legacy_v2 u WHERE u.guid = c.userguid LIMIT 1), 'usr_default_admin')
                    END,
                    c.description,
                    c.accesskey,
                    c.secretkey,
                    c.isbase64,
                    1,
                    NULL,
                    NULL,
                    c.createdutc
                FROM credential_legacy_v2 c;

                INSERT OR IGNORE INTO buckets (id, tenant_id, owner_id, name, regionstring, storagetype, diskdirectory, enableversioning, enablepublicwrite, enablepublicread, createdutc)
                SELECT
                    CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE 'bkt_legacy_' || b.id END,
                    'default',
                    CASE
                        WHEN b.ownerguid = 'default' THEN 'usr_default_admin'
                        ELSE COALESCE((SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || u.id END FROM users_legacy_v2 u WHERE u.guid = b.ownerguid LIMIT 1), 'usr_default_admin')
                    END,
                    b.name,
                    b.regionstring,
                    CASE WHEN b.storagetype = '0' THEN 'Disk' ELSE b.storagetype END,
                    b.diskdirectory,
                    b.enableversioning,
                    b.enablepublicwrite,
                    b.enablepublicread,
                    b.createdutc
                FROM buckets_legacy_v2 b;

                INSERT OR IGNORE INTO bucketacls (id, tenant_id, usergroup, bucket_id, user_id, issued_by_user_id, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc)
                SELECT
                    'bac_legacy_' || a.id,
                    'default',
                    a.usergroup,
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE 'bkt_legacy_' || b.id END FROM buckets_legacy_v2 b WHERE b.guid = a.bucketguid LIMIT 1), 'bkt_default'),
                    CASE WHEN a.userguid = 'default' THEN 'usr_default_admin' ELSE (SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || u.id END FROM users_legacy_v2 u WHERE u.guid = a.userguid LIMIT 1) END,
                    CASE WHEN a.issuedbyuserguid = 'default' THEN 'usr_default_admin' ELSE (SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || u.id END FROM users_legacy_v2 u WHERE u.guid = a.issuedbyuserguid LIMIT 1) END,
                    a.permitread,
                    a.permitwrite,
                    a.permitreadacp,
                    a.permitwriteacp,
                    a.fullcontrol,
                    a.createdutc
                FROM bucketacls_legacy_v2 a;

                INSERT OR IGNORE INTO buckettags (id, tenant_id, bucket_id, key, value, createdutc)
                SELECT
                    'btg_legacy_' || t.id,
                    'default',
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE 'bkt_legacy_' || b.id END FROM buckets_legacy_v2 b WHERE b.guid = t.bucketguid LIMIT 1), 'bkt_default'),
                    t.""key"",
                    t.""value"",
                    t.createdutc
                FROM buckettags_legacy_v2 t;

                INSERT OR IGNORE INTO objects (id, tenant_id, bucket_id, owner_id, author_id, key, contenttype, contentlength, version, etag, retention, blobfilename, isfolder, deletemarker, md5, createdutc, lastupdateutc, lastaccessutc, metadata, expirationutc)
                SELECT
                    'obj_legacy_' || o.id,
                    'default',
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE 'bkt_legacy_' || b.id END FROM buckets_legacy_v2 b WHERE b.guid = o.bucketguid LIMIT 1), 'bkt_default'),
                    CASE WHEN o.ownerguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || u.id END FROM users_legacy_v2 u WHERE u.guid = o.ownerguid LIMIT 1), 'usr_default_admin') END,
                    CASE WHEN o.authorguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || u.id END FROM users_legacy_v2 u WHERE u.guid = o.authorguid LIMIT 1), 'usr_default_admin') END,
                    o.key,
                    o.contenttype,
                    o.contentlength,
                    o.version,
                    o.etag,
                    o.retention,
                    o.blobfilename,
                    o.isfolder,
                    o.deletemarker,
                    o.md5,
                    o.createdutc,
                    o.lastupdateutc,
                    o.lastaccessutc,
                    o.metadata,
                    o.expirationutc
                FROM objects_legacy_v2 o;

                INSERT OR IGNORE INTO objectacls (id, tenant_id, usergroup, user_id, issued_by_user_id, bucket_id, object_id, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc)
                SELECT
                    'oac_legacy_' || a.id,
                    'default',
                    a.usergroup,
                    CASE WHEN a.userguid = 'default' THEN 'usr_default_admin' ELSE (SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || u.id END FROM users_legacy_v2 u WHERE u.guid = a.userguid LIMIT 1) END,
                    CASE WHEN a.issuedbyuserguid = 'default' THEN 'usr_default_admin' ELSE (SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || u.id END FROM users_legacy_v2 u WHERE u.guid = a.issuedbyuserguid LIMIT 1) END,
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE 'bkt_legacy_' || b.id END FROM buckets_legacy_v2 b WHERE b.guid = a.bucketguid LIMIT 1), 'bkt_default'),
                    COALESCE((SELECT 'obj_legacy_' || o.id FROM objects_legacy_v2 o WHERE o.guid = a.objectguid LIMIT 1), 'obj_legacy_' || a.objectguid),
                    a.permitread,
                    a.permitwrite,
                    a.permitreadacp,
                    a.permitwriteacp,
                    a.fullcontrol,
                    a.createdutc
                FROM objectacls_legacy_v2 a;

                INSERT OR IGNORE INTO objecttags (id, tenant_id, bucket_id, object_id, key, value, createdutc)
                SELECT
                    'otg_legacy_' || t.id,
                    'default',
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE 'bkt_legacy_' || b.id END FROM buckets_legacy_v2 b WHERE b.guid = t.bucketguid LIMIT 1), 'bkt_default'),
                    COALESCE((SELECT 'obj_legacy_' || o.id FROM objects_legacy_v2 o WHERE o.guid = t.objectguid LIMIT 1), 'obj_legacy_' || t.objectguid),
                    t.""key"",
                    t.""value"",
                    t.createdutc
                FROM objecttags_legacy_v2 t;

                INSERT OR IGNORE INTO uploads (id, tenant_id, bucket_id, owner_id, author_id, key, createdutc, lastaccessutc, expirationutc, contenttype, metadata)
                SELECT
                    'upl_legacy_' || u.id,
                    'default',
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE 'bkt_legacy_' || b.id END FROM buckets_legacy_v2 b WHERE b.guid = u.bucketguid LIMIT 1), 'bkt_default'),
                    CASE WHEN u.ownerguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT CASE WHEN usr.guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || usr.id END FROM users_legacy_v2 usr WHERE usr.guid = u.ownerguid LIMIT 1), 'usr_default_admin') END,
                    CASE WHEN u.authorguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT CASE WHEN usr.guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || usr.id END FROM users_legacy_v2 usr WHERE usr.guid = u.authorguid LIMIT 1), 'usr_default_admin') END,
                    u.key,
                    u.createdutc,
                    u.lastaccessutc,
                    u.expirationutc,
                    u.contenttype,
                    u.metadata
                FROM uploads_legacy_v2 u;

                INSERT OR IGNORE INTO uploadparts (id, tenant_id, bucket_id, owner_id, upload_id, partnumber, partlength, md5hash, sha1hash, sha256hash, lastaccessutc, createdutc)
                SELECT
                    'prt_legacy_' || p.id,
                    'default',
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE 'bkt_legacy_' || b.id END FROM buckets_legacy_v2 b WHERE b.guid = p.bucketguid LIMIT 1), 'bkt_default'),
                    CASE WHEN p.ownerguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE 'usr_legacy_' || u.id END FROM users_legacy_v2 u WHERE u.guid = p.ownerguid LIMIT 1), 'usr_default_admin') END,
                    COALESCE((SELECT 'upl_legacy_' || u.id FROM uploads_legacy_v2 u WHERE u.guid = p.uploadguid LIMIT 1), 'upl_legacy_' || p.uploadguid),
                    p.partnumber,
                    p.partlength,
                    p.md5hash,
                    p.sha1hash,
                    p.sha256hash,
                    p.lastaccessutc,
                    p.createdutc
                FROM uploadparts_legacy_v2 p;

                INSERT OR IGNORE INTO requesthistory (id, tenant_id, httpmethod, requesturl, sourceip, statuscode, success, durationms, requesttype, user_id, accesskey, requestcontenttype, requestbodylength, responsecontenttype, responsebodylength, requestbody, responsebody, createdutc)
                SELECT
                    'req_legacy_' || id,
                    'default',
                    httpmethod,
                    requesturl,
                    sourceip,
                    statuscode,
                    success,
                    durationms,
                    requesttype,
                    CASE WHEN userguid = 'default' THEN 'usr_default_admin' ELSE NULL END,
                    accesskey,
                    requestcontenttype,
                    requestbodylength,
                    responsecontenttype,
                    responsebodylength,
                    requestbody,
                    responsebody,
                    createdutc
                FROM requesthistory_legacy_v2;
                ";
        }
    }
}
