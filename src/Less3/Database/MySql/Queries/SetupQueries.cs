namespace Less3.Database.MySql.Queries
{
    using System;
    using System.Collections.Generic;

    internal static class SetupQueries
    {
        internal static string CreateTables()
        {
            return
                @"CREATE TABLE IF NOT EXISTS tenants (
                    id VARCHAR(32) PRIMARY KEY,
                    parent_id VARCHAR(32),
                    name VARCHAR(256) NOT NULL,
                    active TINYINT(1) NOT NULL DEFAULT 1,
                    createdutc DATETIME(6) NOT NULL,
                    lastupdateutc DATETIME(6) NOT NULL,
                    INDEX idx_tenants_name (name),
                    INDEX idx_tenants_active (active)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS roles (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64),
                    name VARCHAR(128) NOT NULL,
                    description VARCHAR(1024),
                    isbuiltin TINYINT(1) NOT NULL DEFAULT 0,
                    inheritstochildren TINYINT(1) NOT NULL DEFAULT 1,
                    active TINYINT(1) NOT NULL DEFAULT 1,
                    createdutc DATETIME(6) NOT NULL,
                    lastupdateutc DATETIME(6) NOT NULL,
                    INDEX idx_roles_tenant_id (tenant_id),
                    UNIQUE INDEX idx_roles_tenant_name (tenant_id, name)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS permissions (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64),
                    role_id VARCHAR(32) NOT NULL,
                    resourcetype VARCHAR(128) NOT NULL,
                    operation VARCHAR(128) NOT NULL,
                    permit TINYINT(1) NOT NULL DEFAULT 1,
                    active TINYINT(1) NOT NULL DEFAULT 1,
                    createdutc DATETIME(6) NOT NULL,
                    INDEX idx_permissions_tenant_role (tenant_id, role_id),
                    INDEX idx_permissions_lookup (tenant_id, resourcetype, operation, active)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS roleassignments (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL,
                    role_id VARCHAR(32) NOT NULL,
                    principaltype VARCHAR(64) NOT NULL,
                    principal_id VARCHAR(32) NOT NULL,
                    resourcetype VARCHAR(128),
                    resource_id VARCHAR(32),
                    active TINYINT(1) NOT NULL DEFAULT 1,
                    createdutc DATETIME(6) NOT NULL,
                    INDEX idx_roleassignments_principal (tenant_id, principaltype, principal_id, active),
                    INDEX idx_roleassignments_role (tenant_id, role_id)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS authsessions (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL,
                    principaltype VARCHAR(64) NOT NULL,
                    principal_id VARCHAR(32) NOT NULL,
                    tokenhash VARCHAR(256) NOT NULL,
                    active TINYINT(1) NOT NULL DEFAULT 1,
                    createdutc DATETIME(6) NOT NULL,
                    expirationutc DATETIME(6) NOT NULL,
                    revokedutc DATETIME(6),
                    sourceip VARCHAR(64),
                    UNIQUE INDEX idx_authsessions_tokenhash (tokenhash),
                    INDEX idx_authsessions_principal (tenant_id, principaltype, principal_id, active)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS authorizationaudit (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64),
                    user_id VARCHAR(32),
                    credential_id VARCHAR(32),
                    resourcetype VARCHAR(128),
                    resource_id VARCHAR(32),
                    operation VARCHAR(128),
                    permitted TINYINT(1) NOT NULL DEFAULT 0,
                    reason TEXT,
                    createdutc DATETIME(6) NOT NULL,
                    INDEX idx_authorizationaudit_tenant_createdutc (tenant_id, createdutc),
                    INDEX idx_authorizationaudit_principal (tenant_id, user_id, credential_id)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS users (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    name VARCHAR(256),
                    email VARCHAR(256),
                    passwordhash VARCHAR(512),
                    isadmin TINYINT(1) NOT NULL DEFAULT 0,
                    istenantadmin TINYINT(1) NOT NULL DEFAULT 0,
                    active TINYINT(1) NOT NULL DEFAULT 1,
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS credentials (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    user_id VARCHAR(64) NOT NULL,
                    description VARCHAR(256),
                    accesskey VARCHAR(256),
                    secretkey VARCHAR(256),
                    isbase64 TINYINT(1) NOT NULL DEFAULT 0,
                    active TINYINT(1) NOT NULL DEFAULT 1,
                    lastusedutc DATETIME(6),
                    lastfailedutc DATETIME(6),
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS buckets (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    owner_id VARCHAR(64) NOT NULL,
                    name VARCHAR(256) NOT NULL,
                    regionstring VARCHAR(64),
                    storagetype VARCHAR(32),
                    diskdirectory VARCHAR(1024),
                    enableversioning TINYINT(1) NOT NULL DEFAULT 0,
                    enablepublicwrite TINYINT(1) NOT NULL DEFAULT 0,
                    enablepublicread TINYINT(1) NOT NULL DEFAULT 0,
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS objects (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id VARCHAR(64) NOT NULL,
                    owner_id VARCHAR(64),
                    author_id VARCHAR(64),
                    `key` VARCHAR(1024),
                    contenttype VARCHAR(256),
                    contentlength BIGINT NOT NULL DEFAULT 0,
                    version BIGINT NOT NULL DEFAULT 1,
                    etag VARCHAR(256),
                    retention VARCHAR(32),
                    blobfilename VARCHAR(1024),
                    isfolder TINYINT(1) NOT NULL DEFAULT 0,
                    deletemarker TINYINT(1) NOT NULL DEFAULT 0,
                    md5 VARCHAR(64),
                    createdutc DATETIME(6) NOT NULL,
                    lastupdateutc DATETIME(6) NOT NULL,
                    lastaccessutc DATETIME(6) NOT NULL,
                    metadata TEXT,
                    expirationutc DATETIME(6)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS bucketacls (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    usergroup VARCHAR(256),
                    bucket_id VARCHAR(64) NOT NULL,
                    user_id VARCHAR(64),
                    issued_by_user_id VARCHAR(64),
                    permitread TINYINT(1) NOT NULL DEFAULT 0,
                    permitwrite TINYINT(1) NOT NULL DEFAULT 0,
                    permitreadacp TINYINT(1) NOT NULL DEFAULT 0,
                    permitwriteacp TINYINT(1) NOT NULL DEFAULT 0,
                    fullcontrol TINYINT(1) NOT NULL DEFAULT 0,
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS objectacls (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    usergroup VARCHAR(256),
                    user_id VARCHAR(64),
                    issued_by_user_id VARCHAR(64),
                    bucket_id VARCHAR(64) NOT NULL,
                    object_id VARCHAR(64) NOT NULL,
                    permitread TINYINT(1) NOT NULL DEFAULT 0,
                    permitwrite TINYINT(1) NOT NULL DEFAULT 0,
                    permitreadacp TINYINT(1) NOT NULL DEFAULT 0,
                    permitwriteacp TINYINT(1) NOT NULL DEFAULT 0,
                    fullcontrol TINYINT(1) NOT NULL DEFAULT 0,
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS buckettags (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id VARCHAR(64) NOT NULL,
                    `key` VARCHAR(256),
                    value VARCHAR(1024),
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS objecttags (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id VARCHAR(64) NOT NULL,
                    object_id VARCHAR(64) NOT NULL,
                    `key` VARCHAR(256),
                    value VARCHAR(1024),
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS uploads (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id VARCHAR(64),
                    owner_id VARCHAR(64),
                    author_id VARCHAR(64),
                    `key` VARCHAR(1024),
                    createdutc DATETIME(6) NOT NULL,
                    lastaccessutc DATETIME(6) NOT NULL,
                    expirationutc DATETIME(6) NOT NULL,
                    contenttype VARCHAR(256),
                    metadata TEXT
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS uploadparts (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id VARCHAR(64) NOT NULL,
                    owner_id VARCHAR(64) NOT NULL,
                    upload_id VARCHAR(64) NOT NULL,
                    partnumber INT NOT NULL DEFAULT 1,
                    partlength INT NOT NULL DEFAULT 0,
                    md5hash VARCHAR(64),
                    sha1hash VARCHAR(64),
                    sha256hash VARCHAR(64),
                    lastaccessutc DATETIME(6) NOT NULL,
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS requesthistory (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    httpmethod VARCHAR(16),
                    requesturl VARCHAR(2048),
                    sourceip VARCHAR(64),
                    statuscode INT NOT NULL DEFAULT 0,
                    success TINYINT(1) NOT NULL DEFAULT 1,
                    durationms BIGINT NOT NULL DEFAULT 0,
                    requesttype VARCHAR(128),
                    user_id VARCHAR(64),
                    accesskey VARCHAR(256),
                    requestcontenttype VARCHAR(256),
                    requestbodylength BIGINT NOT NULL DEFAULT 0,
                    responsecontenttype VARCHAR(256),
                    responsebodylength BIGINT NOT NULL DEFAULT 0,
                    requestbody MEDIUMTEXT,
                    responsebody MEDIUMTEXT,
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                ";
        }

        internal static List<string> CreateIndices()
        {
            List<string> indices = new List<string>();

            indices.Add("CREATE INDEX idx_users_id ON users (id);");
            indices.Add("CREATE INDEX idx_users_name ON users (name);");
            indices.Add("CREATE INDEX idx_users_email ON users (email);");
            indices.Add("CREATE INDEX idx_users_tenant_id ON users (tenant_id);");
            indices.Add("CREATE UNIQUE INDEX idx_users_tenant_email ON users (tenant_id, email);");

            indices.Add("CREATE INDEX idx_credentials_id ON credentials (id);");
            indices.Add("CREATE INDEX idx_credentials_user_id ON credentials (user_id);");
            indices.Add("CREATE INDEX idx_credentials_accesskey ON credentials (accesskey);");
            indices.Add("CREATE INDEX idx_credentials_tenant_id ON credentials (tenant_id);");
            indices.Add("CREATE INDEX idx_credentials_tenant_user_id ON credentials (tenant_id, user_id);");
            indices.Add("CREATE UNIQUE INDEX idx_credentials_accesskey_unique ON credentials (accesskey);");

            indices.Add("CREATE INDEX idx_buckets_id ON buckets (id);");
            indices.Add("CREATE INDEX idx_buckets_name ON buckets (name);");
            indices.Add("CREATE INDEX idx_buckets_owner_id ON buckets (owner_id);");
            indices.Add("CREATE INDEX idx_buckets_tenant_id ON buckets (tenant_id);");
            indices.Add("CREATE UNIQUE INDEX idx_buckets_tenant_name ON buckets (tenant_id, name);");

            indices.Add("CREATE INDEX idx_objects_id ON objects (id);");
            indices.Add("CREATE INDEX idx_objects_bucket_id ON objects (bucket_id);");
            indices.Add("CREATE INDEX idx_objects_owner_id ON objects (owner_id);");
            indices.Add("CREATE INDEX idx_objects_key ON objects (`key`);");
            indices.Add("CREATE INDEX idx_objects_deletemarker ON objects (deletemarker);");
            indices.Add("CREATE INDEX idx_objects_tenant_id ON objects (tenant_id);");
            indices.Add("CREATE INDEX idx_objects_tenant_bucket_key ON objects (tenant_id, bucket_id, `key`);");
            indices.Add("CREATE INDEX idx_objects_tenant_bucket_key_version ON objects (tenant_id, bucket_id, `key`, version);");
            indices.Add("CREATE INDEX idx_objects_tenant_bucket_createdutc ON objects (tenant_id, bucket_id, createdutc);");

            indices.Add("CREATE INDEX idx_bucketacls_bucket_id ON bucketacls (bucket_id);");
            indices.Add("CREATE INDEX idx_bucketacls_user_id ON bucketacls (user_id);");
            indices.Add("CREATE INDEX idx_bucketacls_tenant_id ON bucketacls (tenant_id);");
            indices.Add("CREATE INDEX idx_bucketacls_tenant_bucket_id ON bucketacls (tenant_id, bucket_id);");

            indices.Add("CREATE INDEX idx_objectacls_object_id ON objectacls (object_id);");
            indices.Add("CREATE INDEX idx_objectacls_bucket_id ON objectacls (bucket_id);");
            indices.Add("CREATE INDEX idx_objectacls_user_id ON objectacls (user_id);");
            indices.Add("CREATE INDEX idx_objectacls_tenant_id ON objectacls (tenant_id);");
            indices.Add("CREATE INDEX idx_objectacls_tenant_object_id ON objectacls (tenant_id, object_id);");

            indices.Add("CREATE INDEX idx_buckettags_bucket_id ON buckettags (bucket_id);");
            indices.Add("CREATE INDEX idx_buckettags_tenant_id ON buckettags (tenant_id);");
            indices.Add("CREATE INDEX idx_buckettags_tenant_bucket_id ON buckettags (tenant_id, bucket_id);");

            indices.Add("CREATE INDEX idx_objecttags_object_id ON objecttags (object_id);");
            indices.Add("CREATE INDEX idx_objecttags_bucket_id ON objecttags (bucket_id);");
            indices.Add("CREATE INDEX idx_objecttags_tenant_id ON objecttags (tenant_id);");
            indices.Add("CREATE INDEX idx_objecttags_tenant_object_id ON objecttags (tenant_id, object_id);");

            indices.Add("CREATE INDEX idx_uploads_id ON uploads (id);");
            indices.Add("CREATE INDEX idx_uploads_bucket_id ON uploads (bucket_id);");
            indices.Add("CREATE INDEX idx_uploads_tenant_id ON uploads (tenant_id);");
            indices.Add("CREATE INDEX idx_uploads_tenant_bucket_id ON uploads (tenant_id, bucket_id);");

            indices.Add("CREATE INDEX idx_uploadparts_upload_id ON uploadparts (upload_id);");
            indices.Add("CREATE INDEX idx_uploadparts_tenant_id ON uploadparts (tenant_id);");
            indices.Add("CREATE INDEX idx_uploadparts_tenant_upload_id ON uploadparts (tenant_id, upload_id);");

            indices.Add("CREATE INDEX idx_requesthistory_id ON requesthistory (id);");
            indices.Add("CREATE INDEX idx_requesthistory_createdutc ON requesthistory (createdutc);");
            indices.Add("CREATE INDEX idx_requesthistory_tenant_createdutc ON requesthistory (tenant_id, createdutc);");
            indices.Add("CREATE INDEX idx_requesthistory_tenant_status_createdutc ON requesthistory (tenant_id, statuscode, createdutc);");
            indices.Add("CREATE INDEX idx_requesthistory_tenant_method_createdutc ON requesthistory (tenant_id, httpmethod, createdutc);");
            indices.Add("CREATE INDEX idx_requesthistory_tenant_sourceip_createdutc ON requesthistory (tenant_id, sourceip, createdutc);");
            indices.Add("CREATE INDEX idx_requesthistory_tenant_requesttype_createdutc ON requesthistory (tenant_id, requesttype, createdutc);");
            indices.Add("CREATE INDEX idx_requesthistory_tenant_user_createdutc ON requesthistory (tenant_id, user_id, createdutc);");
            indices.Add("CREATE INDEX idx_requesthistory_tenant_accesskey_createdutc ON requesthistory (tenant_id, accesskey, createdutc);");

            return indices;
        }

        internal static string AnalyzeTables()
        {
            return "ANALYZE TABLE users, credentials, buckets, objects, bucketacls, objectacls, buckettags, objecttags, uploads, uploadparts, requesthistory;";
        }
    }
}
