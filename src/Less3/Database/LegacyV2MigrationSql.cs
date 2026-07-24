namespace Less3.Database
{
    using System;
    using System.Collections.Generic;

    internal enum LegacyV2MigrationDialect
    {
        Sqlite,
        MySql,
        PostgreSql,
        SqlServer
    }

    internal static class LegacyV2MigrationSql
    {
        internal static IReadOnlyDictionary<string, string> TableRenames { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "users", "users_legacy_v2" },
            { "credential", "credential_legacy_v2" },
            { "buckets", "buckets_legacy_v2" },
            { "objects", "objects_legacy_v2" },
            { "bucketacls", "bucketacls_legacy_v2" },
            { "objectacls", "objectacls_legacy_v2" },
            { "buckettags", "buckettags_legacy_v2" },
            { "objecttags", "objecttags_legacy_v2" },
            { "uploads", "uploads_legacy_v2" },
            { "uploadparts", "uploadparts_legacy_v2" },
            { "requesthistory", "requesthistory_legacy_v2" }
        };

        internal static string Build(LegacyV2MigrationDialect dialect)
        {
            if (dialect == LegacyV2MigrationDialect.SqlServer) return BuildSqlServer();
            return BuildStandard(dialect);
        }

        private static string BuildStandard(LegacyV2MigrationDialect dialect)
        {
            string insert = InsertPrefix(dialect);
            string conflictSuffix = ConflictSuffix(dialect);
            string now = NowExpression(dialect);
            string key = Identifier(dialect, "key");

            return $@"
                {insert} tenants (id, parent_id, name, active, createdutc, lastupdateutc)
                VALUES ('default', NULL, 'Default', {TrueValue(dialect)}, {now}, {now}){conflictSuffix};

                {insert} users (id, tenant_id, name, email, passwordhash, isadmin, istenantadmin, active, createdutc)
                SELECT
                    CASE WHEN guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "id"))} END,
                    'default',
                    CASE WHEN name IS NULL OR name = '' THEN 'Migrated user' ELSE name END,
                    CASE WHEN guid = 'default' THEN 'admin@less3' ELSE email END,
                    CASE WHEN guid = 'default' THEN 'password' ELSE '' END,
                    CASE WHEN guid = 'default' THEN 1 ELSE 0 END,
                    CASE WHEN guid = 'default' THEN 1 ELSE 0 END,
                    1,
                    createdutc
                FROM users_legacy_v2{conflictSuffix};

                {insert} credentials (id, tenant_id, user_id, description, accesskey, secretkey, isbase64, active, lastusedutc, lastfailedutc, createdutc)
                SELECT
                    CASE WHEN accesskey = 'default' THEN 'crd_default' ELSE {Concat(dialect, "'crd_legacy_'", TextValue(dialect, "c.id"))} END,
                    'default',
                    CASE
                        WHEN c.userguid = 'default' THEN 'usr_default_admin'
                        ELSE COALESCE((SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "u.id"))} END FROM users_legacy_v2 u WHERE u.guid = c.userguid LIMIT 1), 'usr_default_admin')
                    END,
                    c.description,
                    c.accesskey,
                    c.secretkey,
                    c.isbase64,
                    1,
                    NULL,
                    NULL,
                    c.createdutc
                FROM credential_legacy_v2 c{conflictSuffix};

                {insert} buckets (id, tenant_id, owner_id, name, regionstring, storagetype, diskdirectory, enableversioning, enablepublicwrite, enablepublicread, createdutc)
                SELECT
                    CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", TextValue(dialect, "b.id"))} END,
                    'default',
                    CASE
                        WHEN b.ownerguid = 'default' THEN 'usr_default_admin'
                        ELSE COALESCE((SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "u.id"))} END FROM users_legacy_v2 u WHERE u.guid = b.ownerguid LIMIT 1), 'usr_default_admin')
                    END,
                    b.name,
                    b.regionstring,
                    CASE WHEN b.storagetype = '0' THEN 'Disk' ELSE b.storagetype END,
                    b.diskdirectory,
                    b.enableversioning,
                    b.enablepublicwrite,
                    b.enablepublicread,
                    b.createdutc
                FROM buckets_legacy_v2 b{conflictSuffix};

                {insert} bucketacls (id, tenant_id, usergroup, bucket_id, user_id, issued_by_user_id, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc)
                SELECT
                    {Concat(dialect, "'bac_legacy_'", TextValue(dialect, "a.id"))},
                    'default',
                    a.usergroup,
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", TextValue(dialect, "b.id"))} END FROM buckets_legacy_v2 b WHERE b.guid = a.bucketguid LIMIT 1), 'bkt_default'),
                    CASE WHEN a.userguid = 'default' THEN 'usr_default_admin' ELSE (SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "u.id"))} END FROM users_legacy_v2 u WHERE u.guid = a.userguid LIMIT 1) END,
                    CASE WHEN a.issuedbyuserguid = 'default' THEN 'usr_default_admin' ELSE (SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "u.id"))} END FROM users_legacy_v2 u WHERE u.guid = a.issuedbyuserguid LIMIT 1) END,
                    a.permitread,
                    a.permitwrite,
                    a.permitreadacp,
                    a.permitwriteacp,
                    a.fullcontrol,
                    a.createdutc
                FROM bucketacls_legacy_v2 a{conflictSuffix};

                {insert} buckettags (id, tenant_id, bucket_id, {key}, value, createdutc)
                SELECT
                    {Concat(dialect, "'btg_legacy_'", TextValue(dialect, "t.id"))},
                    'default',
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", TextValue(dialect, "b.id"))} END FROM buckets_legacy_v2 b WHERE b.guid = t.bucketguid LIMIT 1), 'bkt_default'),
                    {Column(dialect, "t", "key")},
                    t.value,
                    t.createdutc
                FROM buckettags_legacy_v2 t{conflictSuffix};

                {insert} objects (id, tenant_id, bucket_id, owner_id, author_id, {key}, contenttype, contentlength, version, etag, retention, blobfilename, isfolder, deletemarker, md5, createdutc, lastupdateutc, lastaccessutc, metadata, expirationutc)
                SELECT
                    {Concat(dialect, "'obj_legacy_'", TextValue(dialect, "o.id"))},
                    'default',
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", TextValue(dialect, "b.id"))} END FROM buckets_legacy_v2 b WHERE b.guid = o.bucketguid LIMIT 1), 'bkt_default'),
                    CASE WHEN o.ownerguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "u.id"))} END FROM users_legacy_v2 u WHERE u.guid = o.ownerguid LIMIT 1), 'usr_default_admin') END,
                    CASE WHEN o.authorguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "u.id"))} END FROM users_legacy_v2 u WHERE u.guid = o.authorguid LIMIT 1), 'usr_default_admin') END,
                    {Column(dialect, "o", "key")},
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
                FROM objects_legacy_v2 o{conflictSuffix};

                {insert} objectacls (id, tenant_id, usergroup, user_id, issued_by_user_id, bucket_id, object_id, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc)
                SELECT
                    {Concat(dialect, "'oac_legacy_'", TextValue(dialect, "a.id"))},
                    'default',
                    a.usergroup,
                    CASE WHEN a.userguid = 'default' THEN 'usr_default_admin' ELSE (SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "u.id"))} END FROM users_legacy_v2 u WHERE u.guid = a.userguid LIMIT 1) END,
                    CASE WHEN a.issuedbyuserguid = 'default' THEN 'usr_default_admin' ELSE (SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "u.id"))} END FROM users_legacy_v2 u WHERE u.guid = a.issuedbyuserguid LIMIT 1) END,
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", TextValue(dialect, "b.id"))} END FROM buckets_legacy_v2 b WHERE b.guid = a.bucketguid LIMIT 1), 'bkt_default'),
                    COALESCE((SELECT {Concat(dialect, "'obj_legacy_'", TextValue(dialect, "o.id"))} FROM objects_legacy_v2 o WHERE o.guid = a.objectguid LIMIT 1), {Concat(dialect, "'obj_legacy_'", TextValue(dialect, "a.objectguid"))}),
                    a.permitread,
                    a.permitwrite,
                    a.permitreadacp,
                    a.permitwriteacp,
                    a.fullcontrol,
                    a.createdutc
                FROM objectacls_legacy_v2 a{conflictSuffix};

                {insert} objecttags (id, tenant_id, bucket_id, object_id, {key}, value, createdutc)
                SELECT
                    {Concat(dialect, "'otg_legacy_'", TextValue(dialect, "t.id"))},
                    'default',
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", TextValue(dialect, "b.id"))} END FROM buckets_legacy_v2 b WHERE b.guid = t.bucketguid LIMIT 1), 'bkt_default'),
                    COALESCE((SELECT {Concat(dialect, "'obj_legacy_'", TextValue(dialect, "o.id"))} FROM objects_legacy_v2 o WHERE o.guid = t.objectguid LIMIT 1), {Concat(dialect, "'obj_legacy_'", TextValue(dialect, "t.objectguid"))}),
                    {Column(dialect, "t", "key")},
                    t.value,
                    t.createdutc
                FROM objecttags_legacy_v2 t{conflictSuffix};

                {insert} uploads (id, tenant_id, bucket_id, owner_id, author_id, {key}, createdutc, lastaccessutc, expirationutc, contenttype, metadata)
                SELECT
                    {Concat(dialect, "'upl_legacy_'", TextValue(dialect, "u.id"))},
                    'default',
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", TextValue(dialect, "b.id"))} END FROM buckets_legacy_v2 b WHERE b.guid = u.bucketguid LIMIT 1), 'bkt_default'),
                    CASE WHEN u.ownerguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT CASE WHEN usr.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "usr.id"))} END FROM users_legacy_v2 usr WHERE usr.guid = u.ownerguid LIMIT 1), 'usr_default_admin') END,
                    CASE WHEN u.authorguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT CASE WHEN usr.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "usr.id"))} END FROM users_legacy_v2 usr WHERE usr.guid = u.authorguid LIMIT 1), 'usr_default_admin') END,
                    {Column(dialect, "u", "key")},
                    u.createdutc,
                    u.lastaccessutc,
                    u.expirationutc,
                    u.contenttype,
                    u.metadata
                FROM uploads_legacy_v2 u{conflictSuffix};

                {insert} uploadparts (id, tenant_id, bucket_id, owner_id, upload_id, partnumber, partlength, md5hash, sha1hash, sha256hash, lastaccessutc, createdutc)
                SELECT
                    {Concat(dialect, "'prt_legacy_'", TextValue(dialect, "p.id"))},
                    'default',
                    COALESCE((SELECT CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", TextValue(dialect, "b.id"))} END FROM buckets_legacy_v2 b WHERE b.guid = p.bucketguid LIMIT 1), 'bkt_default'),
                    CASE WHEN p.ownerguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", TextValue(dialect, "u.id"))} END FROM users_legacy_v2 u WHERE u.guid = p.ownerguid LIMIT 1), 'usr_default_admin') END,
                    COALESCE((SELECT {Concat(dialect, "'upl_legacy_'", TextValue(dialect, "u.id"))} FROM uploads_legacy_v2 u WHERE u.guid = p.uploadguid LIMIT 1), {Concat(dialect, "'upl_legacy_'", TextValue(dialect, "p.uploadguid"))}),
                    p.partnumber,
                    p.partlength,
                    p.md5hash,
                    p.sha1hash,
                    p.sha256hash,
                    p.lastaccessutc,
                    p.createdutc
                FROM uploadparts_legacy_v2 p{conflictSuffix};

                {insert} requesthistory (id, tenant_id, httpmethod, requesturl, sourceip, statuscode, success, durationms, requesttype, user_id, accesskey, requestcontenttype, requestbodylength, responsecontenttype, responsebodylength, requestbody, responsebody, createdutc)
                SELECT
                    {Concat(dialect, "'req_legacy_'", TextValue(dialect, "id"))},
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
                FROM requesthistory_legacy_v2{conflictSuffix};
                ";
        }

        private static string BuildSqlServer()
        {
            const LegacyV2MigrationDialect dialect = LegacyV2MigrationDialect.SqlServer;
            string now = NowExpression(dialect);

            return $@"
                {Merge("tenants", "id, parent_id, name, active, createdutc, lastupdateutc",
                    $"SELECT 'default' AS id, CAST(NULL AS NVARCHAR(32)) AS parent_id, 'Default' AS name, CAST(1 AS BIT) AS active, {now} AS createdutc, {now} AS lastupdateutc",
                    "source.id, source.parent_id, source.name, source.active, source.createdutc, source.lastupdateutc")}

                {Merge("users", "id, tenant_id, name, email, passwordhash, isadmin, istenantadmin, active, createdutc",
                    $@"SELECT
                        CASE WHEN guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "id")} END AS id,
                        'default' AS tenant_id,
                        CASE WHEN name IS NULL OR name = '' THEN 'Migrated user' ELSE name END AS name,
                        CASE WHEN guid = 'default' THEN 'admin@less3' ELSE email END AS email,
                        CASE WHEN guid = 'default' THEN 'password' ELSE '' END AS passwordhash,
                        CAST(CASE WHEN guid = 'default' THEN 1 ELSE 0 END AS BIT) AS isadmin,
                        CAST(CASE WHEN guid = 'default' THEN 1 ELSE 0 END AS BIT) AS istenantadmin,
                        CAST(1 AS BIT) AS active,
                        createdutc
                    FROM users_legacy_v2",
                    "source.id, source.tenant_id, source.name, source.email, source.passwordhash, source.isadmin, source.istenantadmin, source.active, source.createdutc")}

                {Merge("credentials", "id, tenant_id, user_id, description, accesskey, secretkey, isbase64, active, lastusedutc, lastfailedutc, createdutc",
                    $@"SELECT
                        CASE WHEN accesskey = 'default' THEN 'crd_default' ELSE {Concat(dialect, "'crd_legacy_'", "c.id")} END AS id,
                        'default' AS tenant_id,
                        CASE
                            WHEN c.userguid = 'default' THEN 'usr_default_admin'
                            ELSE COALESCE((SELECT TOP 1 CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "u.id")} END FROM users_legacy_v2 u WHERE u.guid = c.userguid), 'usr_default_admin')
                        END AS user_id,
                        c.description,
                        c.accesskey,
                        c.secretkey,
                        c.isbase64,
                        CAST(1 AS BIT) AS active,
                        CAST(NULL AS NVARCHAR(64)) AS lastusedutc,
                        CAST(NULL AS NVARCHAR(64)) AS lastfailedutc,
                        c.createdutc
                    FROM credential_legacy_v2 c",
                    "source.id, source.tenant_id, source.user_id, source.description, source.accesskey, source.secretkey, source.isbase64, source.active, source.lastusedutc, source.lastfailedutc, source.createdutc")}

                {Merge("buckets", "id, tenant_id, owner_id, name, regionstring, storagetype, diskdirectory, enableversioning, enablepublicwrite, enablepublicread, createdutc",
                    $@"SELECT
                        CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", "b.id")} END AS id,
                        'default' AS tenant_id,
                        CASE
                            WHEN b.ownerguid = 'default' THEN 'usr_default_admin'
                            ELSE COALESCE((SELECT TOP 1 CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "u.id")} END FROM users_legacy_v2 u WHERE u.guid = b.ownerguid), 'usr_default_admin')
                        END AS owner_id,
                        b.name,
                        b.regionstring,
                        CASE WHEN b.storagetype = '0' THEN 'Disk' ELSE b.storagetype END AS storagetype,
                        b.diskdirectory,
                        b.enableversioning,
                        b.enablepublicwrite,
                        b.enablepublicread,
                        b.createdutc
                    FROM buckets_legacy_v2 b",
                    "source.id, source.tenant_id, source.owner_id, source.name, source.regionstring, source.storagetype, source.diskdirectory, source.enableversioning, source.enablepublicwrite, source.enablepublicread, source.createdutc")}

                {Merge("bucketacls", "id, tenant_id, usergroup, bucket_id, user_id, issued_by_user_id, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc",
                    $@"SELECT
                        {Concat(dialect, "'bac_legacy_'", "a.id")} AS id,
                        'default' AS tenant_id,
                        a.usergroup,
                        COALESCE((SELECT TOP 1 CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", "b.id")} END FROM buckets_legacy_v2 b WHERE b.guid = a.bucketguid), 'bkt_default') AS bucket_id,
                        CASE WHEN a.userguid = 'default' THEN 'usr_default_admin' ELSE (SELECT TOP 1 CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "u.id")} END FROM users_legacy_v2 u WHERE u.guid = a.userguid) END AS user_id,
                        CASE WHEN a.issuedbyuserguid = 'default' THEN 'usr_default_admin' ELSE (SELECT TOP 1 CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "u.id")} END FROM users_legacy_v2 u WHERE u.guid = a.issuedbyuserguid) END AS issued_by_user_id,
                        a.permitread,
                        a.permitwrite,
                        a.permitreadacp,
                        a.permitwriteacp,
                        a.fullcontrol,
                        a.createdutc
                    FROM bucketacls_legacy_v2 a",
                    "source.id, source.tenant_id, source.usergroup, source.bucket_id, source.user_id, source.issued_by_user_id, source.permitread, source.permitwrite, source.permitreadacp, source.permitwriteacp, source.fullcontrol, source.createdutc")}

                {Merge("buckettags", "id, tenant_id, bucket_id, [key], value, createdutc",
                    $@"SELECT
                        {Concat(dialect, "'btg_legacy_'", "t.id")} AS id,
                        'default' AS tenant_id,
                        COALESCE((SELECT TOP 1 CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", "b.id")} END FROM buckets_legacy_v2 b WHERE b.guid = t.bucketguid), 'bkt_default') AS bucket_id,
                        t.[key] AS [key],
                        t.value,
                        t.createdutc
                    FROM buckettags_legacy_v2 t",
                    "source.id, source.tenant_id, source.bucket_id, source.[key], source.value, source.createdutc")}

                {Merge("objects", "id, tenant_id, bucket_id, owner_id, author_id, [key], contenttype, contentlength, version, etag, retention, blobfilename, isfolder, deletemarker, md5, createdutc, lastupdateutc, lastaccessutc, metadata, expirationutc",
                    $@"SELECT
                        {Concat(dialect, "'obj_legacy_'", "o.id")} AS id,
                        'default' AS tenant_id,
                        COALESCE((SELECT TOP 1 CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", "b.id")} END FROM buckets_legacy_v2 b WHERE b.guid = o.bucketguid), 'bkt_default') AS bucket_id,
                        CASE WHEN o.ownerguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT TOP 1 CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "u.id")} END FROM users_legacy_v2 u WHERE u.guid = o.ownerguid), 'usr_default_admin') END AS owner_id,
                        CASE WHEN o.authorguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT TOP 1 CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "u.id")} END FROM users_legacy_v2 u WHERE u.guid = o.authorguid), 'usr_default_admin') END AS author_id,
                        o.[key] AS [key],
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
                    FROM objects_legacy_v2 o",
                    "source.id, source.tenant_id, source.bucket_id, source.owner_id, source.author_id, source.[key], source.contenttype, source.contentlength, source.version, source.etag, source.retention, source.blobfilename, source.isfolder, source.deletemarker, source.md5, source.createdutc, source.lastupdateutc, source.lastaccessutc, source.metadata, source.expirationutc")}

                {Merge("objectacls", "id, tenant_id, usergroup, user_id, issued_by_user_id, bucket_id, object_id, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc",
                    $@"SELECT
                        {Concat(dialect, "'oac_legacy_'", "a.id")} AS id,
                        'default' AS tenant_id,
                        a.usergroup,
                        CASE WHEN a.userguid = 'default' THEN 'usr_default_admin' ELSE (SELECT TOP 1 CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "u.id")} END FROM users_legacy_v2 u WHERE u.guid = a.userguid) END AS user_id,
                        CASE WHEN a.issuedbyuserguid = 'default' THEN 'usr_default_admin' ELSE (SELECT TOP 1 CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "u.id")} END FROM users_legacy_v2 u WHERE u.guid = a.issuedbyuserguid) END AS issued_by_user_id,
                        COALESCE((SELECT TOP 1 CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", "b.id")} END FROM buckets_legacy_v2 b WHERE b.guid = a.bucketguid), 'bkt_default') AS bucket_id,
                        COALESCE((SELECT TOP 1 {Concat(dialect, "'obj_legacy_'", "o.id")} FROM objects_legacy_v2 o WHERE o.guid = a.objectguid), {Concat(dialect, "'obj_legacy_'", "a.objectguid")}) AS object_id,
                        a.permitread,
                        a.permitwrite,
                        a.permitreadacp,
                        a.permitwriteacp,
                        a.fullcontrol,
                        a.createdutc
                    FROM objectacls_legacy_v2 a",
                    "source.id, source.tenant_id, source.usergroup, source.user_id, source.issued_by_user_id, source.bucket_id, source.object_id, source.permitread, source.permitwrite, source.permitreadacp, source.permitwriteacp, source.fullcontrol, source.createdutc")}

                {Merge("objecttags", "id, tenant_id, bucket_id, object_id, [key], value, createdutc",
                    $@"SELECT
                        {Concat(dialect, "'otg_legacy_'", "t.id")} AS id,
                        'default' AS tenant_id,
                        COALESCE((SELECT TOP 1 CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", "b.id")} END FROM buckets_legacy_v2 b WHERE b.guid = t.bucketguid), 'bkt_default') AS bucket_id,
                        COALESCE((SELECT TOP 1 {Concat(dialect, "'obj_legacy_'", "o.id")} FROM objects_legacy_v2 o WHERE o.guid = t.objectguid), {Concat(dialect, "'obj_legacy_'", "t.objectguid")}) AS object_id,
                        t.[key] AS [key],
                        t.value,
                        t.createdutc
                    FROM objecttags_legacy_v2 t",
                    "source.id, source.tenant_id, source.bucket_id, source.object_id, source.[key], source.value, source.createdutc")}

                {Merge("uploads", "id, tenant_id, bucket_id, owner_id, author_id, [key], createdutc, lastaccessutc, expirationutc, contenttype, metadata",
                    $@"SELECT
                        {Concat(dialect, "'upl_legacy_'", "u.id")} AS id,
                        'default' AS tenant_id,
                        COALESCE((SELECT TOP 1 CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", "b.id")} END FROM buckets_legacy_v2 b WHERE b.guid = u.bucketguid), 'bkt_default') AS bucket_id,
                        CASE WHEN u.ownerguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT TOP 1 CASE WHEN usr.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "usr.id")} END FROM users_legacy_v2 usr WHERE usr.guid = u.ownerguid), 'usr_default_admin') END AS owner_id,
                        CASE WHEN u.authorguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT TOP 1 CASE WHEN usr.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "usr.id")} END FROM users_legacy_v2 usr WHERE usr.guid = u.authorguid), 'usr_default_admin') END AS author_id,
                        u.[key] AS [key],
                        u.createdutc,
                        u.lastaccessutc,
                        u.expirationutc,
                        u.contenttype,
                        u.metadata
                    FROM uploads_legacy_v2 u",
                    "source.id, source.tenant_id, source.bucket_id, source.owner_id, source.author_id, source.[key], source.createdutc, source.lastaccessutc, source.expirationutc, source.contenttype, source.metadata")}

                {Merge("uploadparts", "id, tenant_id, bucket_id, owner_id, upload_id, partnumber, partlength, md5hash, sha1hash, sha256hash, lastaccessutc, createdutc",
                    $@"SELECT
                        {Concat(dialect, "'prt_legacy_'", "p.id")} AS id,
                        'default' AS tenant_id,
                        COALESCE((SELECT TOP 1 CASE WHEN b.name = 'default' THEN 'bkt_default' ELSE {Concat(dialect, "'bkt_legacy_'", "b.id")} END FROM buckets_legacy_v2 b WHERE b.guid = p.bucketguid), 'bkt_default') AS bucket_id,
                        CASE WHEN p.ownerguid = 'default' THEN 'usr_default_admin' ELSE COALESCE((SELECT TOP 1 CASE WHEN u.guid = 'default' THEN 'usr_default_admin' ELSE {Concat(dialect, "'usr_legacy_'", "u.id")} END FROM users_legacy_v2 u WHERE u.guid = p.ownerguid), 'usr_default_admin') END AS owner_id,
                        COALESCE((SELECT TOP 1 {Concat(dialect, "'upl_legacy_'", "u.id")} FROM uploads_legacy_v2 u WHERE u.guid = p.uploadguid), {Concat(dialect, "'upl_legacy_'", "p.uploadguid")}) AS upload_id,
                        p.partnumber,
                        p.partlength,
                        p.md5hash,
                        p.sha1hash,
                        p.sha256hash,
                        p.lastaccessutc,
                        p.createdutc
                    FROM uploadparts_legacy_v2 p",
                    "source.id, source.tenant_id, source.bucket_id, source.owner_id, source.upload_id, source.partnumber, source.partlength, source.md5hash, source.sha1hash, source.sha256hash, source.lastaccessutc, source.createdutc")}

                {Merge("requesthistory", "id, tenant_id, httpmethod, requesturl, sourceip, statuscode, success, durationms, requesttype, user_id, accesskey, requestcontenttype, requestbodylength, responsecontenttype, responsebodylength, requestbody, responsebody, createdutc",
                    $@"SELECT
                        {Concat(dialect, "'req_legacy_'", "id")} AS id,
                        'default' AS tenant_id,
                        httpmethod,
                        requesturl,
                        sourceip,
                        statuscode,
                        success,
                        durationms,
                        requesttype,
                        CASE WHEN userguid = 'default' THEN 'usr_default_admin' ELSE NULL END AS user_id,
                        accesskey,
                        requestcontenttype,
                        requestbodylength,
                        responsecontenttype,
                        responsebodylength,
                        requestbody,
                        responsebody,
                        createdutc
                    FROM requesthistory_legacy_v2",
                    "source.id, source.tenant_id, source.httpmethod, source.requesturl, source.sourceip, source.statuscode, source.success, source.durationms, source.requesttype, source.user_id, source.accesskey, source.requestcontenttype, source.requestbodylength, source.responsecontenttype, source.responsebodylength, source.requestbody, source.responsebody, source.createdutc")}
                ";
        }

        private static string InsertPrefix(LegacyV2MigrationDialect dialect)
        {
            switch (dialect)
            {
                case LegacyV2MigrationDialect.Sqlite:
                    return "INSERT OR IGNORE INTO";
                case LegacyV2MigrationDialect.MySql:
                    return "INSERT IGNORE INTO";
                default:
                    return "INSERT INTO";
            }
        }

        private static string ConflictSuffix(LegacyV2MigrationDialect dialect)
        {
            return dialect == LegacyV2MigrationDialect.PostgreSql ? " ON CONFLICT DO NOTHING" : String.Empty;
        }

        private static string NowExpression(LegacyV2MigrationDialect dialect)
        {
            switch (dialect)
            {
                case LegacyV2MigrationDialect.Sqlite:
                    return "strftime('%Y-%m-%d %H:%M:%f', 'now')";
                case LegacyV2MigrationDialect.MySql:
                    return "UTC_TIMESTAMP(6)";
                case LegacyV2MigrationDialect.SqlServer:
                    return "CONVERT(NVARCHAR(64), SYSUTCDATETIME(), 127)";
                default:
                    return "CURRENT_TIMESTAMP";
            }
        }

        private static string TrueValue(LegacyV2MigrationDialect dialect)
        {
            return dialect == LegacyV2MigrationDialect.PostgreSql ? "TRUE" : "1";
        }

        private static string Identifier(LegacyV2MigrationDialect dialect, string name)
        {
            if (!String.Equals(name, "key", StringComparison.OrdinalIgnoreCase)) return name;

            switch (dialect)
            {
                case LegacyV2MigrationDialect.MySql:
                    return "`key`";
                case LegacyV2MigrationDialect.SqlServer:
                    return "[key]";
                default:
                    return "key";
            }
        }

        private static string Column(LegacyV2MigrationDialect dialect, string alias, string name)
        {
            return alias + "." + Identifier(dialect, name);
        }

        private static string TextValue(LegacyV2MigrationDialect dialect, string expression)
        {
            return dialect == LegacyV2MigrationDialect.PostgreSql ? "CAST(" + expression + " AS TEXT)" : expression;
        }

        private static string Concat(LegacyV2MigrationDialect dialect, params string[] parts)
        {
            if (dialect == LegacyV2MigrationDialect.MySql || dialect == LegacyV2MigrationDialect.SqlServer)
            {
                return "CONCAT(" + String.Join(", ", parts) + ")";
            }

            return String.Join(" || ", parts);
        }

        private static string Merge(string table, string columns, string sourceSelect, string values)
        {
            return $@"MERGE {table} AS target
                USING (
                    {sourceSelect}
                ) AS source
                ON target.id = source.id
                WHEN NOT MATCHED THEN
                    INSERT ({columns})
                    VALUES ({values});";
        }
    }
}
