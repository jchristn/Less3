namespace Less3.Database.PostgreSql.Queries
{
    using System;

    internal static class SetupQueries
    {
        internal static string CreateTablesAndIndices()
        {
            return
                @"CREATE TABLE IF NOT EXISTS tenants (
                    id VARCHAR(32) PRIMARY KEY,
                    parent_id VARCHAR(32),
                    name VARCHAR(256) NOT NULL,
                    active BOOLEAN NOT NULL DEFAULT TRUE,
                    createdutc TIMESTAMPTZ NOT NULL,
                    lastupdateutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_tenants_name ON tenants (name);
                CREATE INDEX IF NOT EXISTS idx_tenants_active ON tenants (active);

                CREATE TABLE IF NOT EXISTS roles (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64),
                    name VARCHAR(128) NOT NULL,
                    description VARCHAR(1024),
                    isbuiltin BOOLEAN NOT NULL DEFAULT FALSE,
                    inheritstochildren BOOLEAN NOT NULL DEFAULT TRUE,
                    active BOOLEAN NOT NULL DEFAULT TRUE,
                    createdutc TIMESTAMPTZ NOT NULL,
                    lastupdateutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_roles_tenant_id ON roles (tenant_id);
                CREATE UNIQUE INDEX IF NOT EXISTS idx_roles_tenant_name ON roles (tenant_id, name);

                CREATE TABLE IF NOT EXISTS permissions (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64),
                    role_id VARCHAR(32) NOT NULL,
                    resourcetype VARCHAR(128) NOT NULL,
                    operation VARCHAR(128) NOT NULL,
                    permit BOOLEAN NOT NULL DEFAULT TRUE,
                    active BOOLEAN NOT NULL DEFAULT TRUE,
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_permissions_tenant_role ON permissions (tenant_id, role_id);
                CREATE INDEX IF NOT EXISTS idx_permissions_lookup ON permissions (tenant_id, resourcetype, operation, active);

                CREATE TABLE IF NOT EXISTS roleassignments (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL,
                    role_id VARCHAR(32) NOT NULL,
                    principaltype VARCHAR(64) NOT NULL,
                    principal_id VARCHAR(32) NOT NULL,
                    resourcetype VARCHAR(128),
                    resource_id VARCHAR(32),
                    active BOOLEAN NOT NULL DEFAULT TRUE,
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_roleassignments_principal ON roleassignments (tenant_id, principaltype, principal_id, active);
                CREATE INDEX IF NOT EXISTS idx_roleassignments_role ON roleassignments (tenant_id, role_id);

                CREATE TABLE IF NOT EXISTS authsessions (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL,
                    principaltype VARCHAR(64) NOT NULL,
                    principal_id VARCHAR(32) NOT NULL,
                    tokenhash VARCHAR(256) NOT NULL,
                    active BOOLEAN NOT NULL DEFAULT TRUE,
                    createdutc TIMESTAMPTZ NOT NULL,
                    expirationutc TIMESTAMPTZ NOT NULL,
                    revokedutc TIMESTAMPTZ,
                    sourceip VARCHAR(64)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS idx_authsessions_tokenhash ON authsessions (tokenhash);
                CREATE INDEX IF NOT EXISTS idx_authsessions_principal ON authsessions (tenant_id, principaltype, principal_id, active);

                CREATE TABLE IF NOT EXISTS authorizationaudit (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64),
                    user_id VARCHAR(32),
                    credential_id VARCHAR(32),
                    resourcetype VARCHAR(128),
                    resource_id VARCHAR(32),
                    operation VARCHAR(128),
                    permitted BOOLEAN NOT NULL DEFAULT FALSE,
                    reason TEXT,
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_authorizationaudit_tenant_createdutc ON authorizationaudit (tenant_id, createdutc);
                CREATE INDEX IF NOT EXISTS idx_authorizationaudit_principal ON authorizationaudit (tenant_id, user_id, credential_id);

                CREATE TABLE IF NOT EXISTS users (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    name VARCHAR(256),
                    email VARCHAR(256),
                    passwordhash VARCHAR(512),
                    isadmin INT NOT NULL DEFAULT 0,
                    istenantadmin INT NOT NULL DEFAULT 0,
                    active INT NOT NULL DEFAULT 1,
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_users_id ON users (id);
                CREATE INDEX IF NOT EXISTS idx_users_name ON users (name);
                CREATE INDEX IF NOT EXISTS idx_users_email ON users (email);

                CREATE TABLE IF NOT EXISTS credentials (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    user_id VARCHAR(64) NOT NULL,
                    description VARCHAR(256),
                    accesskey VARCHAR(256),
                    secretkey VARCHAR(256),
                    isbase64 BOOLEAN NOT NULL DEFAULT FALSE,
                    active INT NOT NULL DEFAULT 1,
                    lastusedutc TIMESTAMPTZ,
                    lastfailedutc TIMESTAMPTZ,
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_credentials_id ON credentials (id);
                CREATE INDEX IF NOT EXISTS idx_credentials_user_id ON credentials (user_id);
                CREATE INDEX IF NOT EXISTS idx_credentials_accesskey ON credentials (accesskey);

                CREATE TABLE IF NOT EXISTS buckets (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    owner_id VARCHAR(64) NOT NULL,
                    name VARCHAR(256) NOT NULL,
                    regionstring VARCHAR(64),
                    storagetype VARCHAR(32),
                    diskdirectory VARCHAR(1024),
                    enableversioning BOOLEAN NOT NULL DEFAULT FALSE,
                    enablepublicwrite BOOLEAN NOT NULL DEFAULT FALSE,
                    enablepublicread BOOLEAN NOT NULL DEFAULT FALSE,
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_buckets_id ON buckets (id);
                CREATE INDEX IF NOT EXISTS idx_buckets_name ON buckets (name);
                CREATE INDEX IF NOT EXISTS idx_buckets_owner_id ON buckets (owner_id);

                CREATE TABLE IF NOT EXISTS objects (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id VARCHAR(64) NOT NULL,
                    owner_id VARCHAR(64),
                    author_id VARCHAR(64),
                    key VARCHAR(1024),
                    contenttype VARCHAR(256),
                    contentlength BIGINT NOT NULL DEFAULT 0,
                    version BIGINT NOT NULL DEFAULT 1,
                    etag VARCHAR(256),
                    retention VARCHAR(32),
                    blobfilename VARCHAR(1024),
                    isfolder BOOLEAN NOT NULL DEFAULT FALSE,
                    deletemarker BOOLEAN NOT NULL DEFAULT FALSE,
                    md5 VARCHAR(64),
                    createdutc TIMESTAMPTZ NOT NULL,
                    lastupdateutc TIMESTAMPTZ NOT NULL,
                    lastaccessutc TIMESTAMPTZ NOT NULL,
                    metadata TEXT,
                    expirationutc TIMESTAMPTZ
                );

                CREATE INDEX IF NOT EXISTS idx_objects_id ON objects (id);
                CREATE INDEX IF NOT EXISTS idx_objects_bucket_id ON objects (bucket_id);
                CREATE INDEX IF NOT EXISTS idx_objects_owner_id ON objects (owner_id);
                CREATE INDEX IF NOT EXISTS idx_objects_key ON objects (key);
                CREATE INDEX IF NOT EXISTS idx_objects_deletemarker ON objects (deletemarker);

                CREATE TABLE IF NOT EXISTS bucketacls (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    usergroup VARCHAR(256),
                    bucket_id VARCHAR(64) NOT NULL,
                    user_id VARCHAR(64),
                    issued_by_user_id VARCHAR(64),
                    permitread BOOLEAN NOT NULL DEFAULT FALSE,
                    permitwrite BOOLEAN NOT NULL DEFAULT FALSE,
                    permitreadacp BOOLEAN NOT NULL DEFAULT FALSE,
                    permitwriteacp BOOLEAN NOT NULL DEFAULT FALSE,
                    fullcontrol BOOLEAN NOT NULL DEFAULT FALSE,
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_bucketacls_bucket_id ON bucketacls (bucket_id);
                CREATE INDEX IF NOT EXISTS idx_bucketacls_user_id ON bucketacls (user_id);

                CREATE TABLE IF NOT EXISTS objectacls (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    usergroup VARCHAR(256),
                    user_id VARCHAR(64),
                    issued_by_user_id VARCHAR(64),
                    bucket_id VARCHAR(64) NOT NULL,
                    object_id VARCHAR(64) NOT NULL,
                    permitread BOOLEAN NOT NULL DEFAULT FALSE,
                    permitwrite BOOLEAN NOT NULL DEFAULT FALSE,
                    permitreadacp BOOLEAN NOT NULL DEFAULT FALSE,
                    permitwriteacp BOOLEAN NOT NULL DEFAULT FALSE,
                    fullcontrol BOOLEAN NOT NULL DEFAULT FALSE,
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_objectacls_object_id ON objectacls (object_id);
                CREATE INDEX IF NOT EXISTS idx_objectacls_bucket_id ON objectacls (bucket_id);
                CREATE INDEX IF NOT EXISTS idx_objectacls_user_id ON objectacls (user_id);

                CREATE TABLE IF NOT EXISTS buckettags (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id VARCHAR(64) NOT NULL,
                    key VARCHAR(256),
                    value VARCHAR(1024),
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_buckettags_bucket_id ON buckettags (bucket_id);

                CREATE TABLE IF NOT EXISTS objecttags (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id VARCHAR(64) NOT NULL,
                    object_id VARCHAR(64) NOT NULL,
                    key VARCHAR(256),
                    value VARCHAR(1024),
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_objecttags_object_id ON objecttags (object_id);
                CREATE INDEX IF NOT EXISTS idx_objecttags_bucket_id ON objecttags (bucket_id);

                CREATE TABLE IF NOT EXISTS uploads (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id VARCHAR(64),
                    owner_id VARCHAR(64),
                    author_id VARCHAR(64),
                    key VARCHAR(1024),
                    createdutc TIMESTAMPTZ NOT NULL,
                    lastaccessutc TIMESTAMPTZ NOT NULL,
                    expirationutc TIMESTAMPTZ NOT NULL,
                    contenttype VARCHAR(256),
                    metadata TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_uploads_id ON uploads (id);
                CREATE INDEX IF NOT EXISTS idx_uploads_bucket_id ON uploads (bucket_id);

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
                    lastaccessutc TIMESTAMPTZ NOT NULL,
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_uploadparts_upload_id ON uploadparts (upload_id);

                CREATE TABLE IF NOT EXISTS requesthistory (
                    id VARCHAR(32) PRIMARY KEY,
                    tenant_id VARCHAR(64) NOT NULL DEFAULT 'default',
                    httpmethod VARCHAR(16),
                    requesturl VARCHAR(2048),
                    sourceip VARCHAR(64),
                    statuscode INT NOT NULL DEFAULT 0,
                    success BOOLEAN NOT NULL DEFAULT TRUE,
                    durationms BIGINT NOT NULL DEFAULT 0,
                    requesttype VARCHAR(128),
                    user_id VARCHAR(64),
                    accesskey VARCHAR(256),
                    requestcontenttype VARCHAR(256),
                    requestbodylength BIGINT NOT NULL DEFAULT 0,
                    responsecontenttype VARCHAR(256),
                    responsebodylength BIGINT NOT NULL DEFAULT 0,
                    requestbody TEXT,
                    responsebody TEXT,
                    createdutc TIMESTAMPTZ NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_requesthistory_id ON requesthistory (id);
                CREATE INDEX IF NOT EXISTS idx_requesthistory_createdutc ON requesthistory (createdutc);
                CREATE INDEX IF NOT EXISTS idx_users_tenant_id ON users (tenant_id);
                CREATE UNIQUE INDEX IF NOT EXISTS idx_users_tenant_email ON users (tenant_id, email);
                CREATE INDEX IF NOT EXISTS idx_credentials_tenant_id ON credentials (tenant_id);
                CREATE INDEX IF NOT EXISTS idx_credentials_tenant_user_id ON credentials (tenant_id, user_id);
                CREATE UNIQUE INDEX IF NOT EXISTS idx_credentials_accesskey_unique ON credentials (accesskey);
                CREATE INDEX IF NOT EXISTS idx_buckets_tenant_id ON buckets (tenant_id);
                CREATE UNIQUE INDEX IF NOT EXISTS idx_buckets_tenant_name ON buckets (tenant_id, name);
                CREATE INDEX IF NOT EXISTS idx_objects_tenant_id ON objects (tenant_id);
                CREATE INDEX IF NOT EXISTS idx_objects_tenant_bucket_key ON objects (tenant_id, bucket_id, key);
                CREATE INDEX IF NOT EXISTS idx_objects_tenant_bucket_key_version ON objects (tenant_id, bucket_id, key, version);
                CREATE INDEX IF NOT EXISTS idx_objects_tenant_bucket_createdutc ON objects (tenant_id, bucket_id, createdutc);
                CREATE INDEX IF NOT EXISTS idx_bucketacls_tenant_id ON bucketacls (tenant_id);
                CREATE INDEX IF NOT EXISTS idx_bucketacls_tenant_bucket_id ON bucketacls (tenant_id, bucket_id);
                CREATE INDEX IF NOT EXISTS idx_objectacls_tenant_id ON objectacls (tenant_id);
                CREATE INDEX IF NOT EXISTS idx_objectacls_tenant_object_id ON objectacls (tenant_id, object_id);
                CREATE INDEX IF NOT EXISTS idx_buckettags_tenant_id ON buckettags (tenant_id);
                CREATE INDEX IF NOT EXISTS idx_buckettags_tenant_bucket_id ON buckettags (tenant_id, bucket_id);
                CREATE INDEX IF NOT EXISTS idx_objecttags_tenant_id ON objecttags (tenant_id);
                CREATE INDEX IF NOT EXISTS idx_objecttags_tenant_object_id ON objecttags (tenant_id, object_id);
                CREATE INDEX IF NOT EXISTS idx_uploads_tenant_id ON uploads (tenant_id);
                CREATE INDEX IF NOT EXISTS idx_uploads_tenant_bucket_id ON uploads (tenant_id, bucket_id);
                CREATE INDEX IF NOT EXISTS idx_uploadparts_tenant_id ON uploadparts (tenant_id);
                CREATE INDEX IF NOT EXISTS idx_uploadparts_tenant_upload_id ON uploadparts (tenant_id, upload_id);
                CREATE INDEX IF NOT EXISTS idx_requesthistory_tenant_createdutc ON requesthistory (tenant_id, createdutc);
                CREATE INDEX IF NOT EXISTS idx_requesthistory_tenant_status_createdutc ON requesthistory (tenant_id, statuscode, createdutc);
                CREATE INDEX IF NOT EXISTS idx_requesthistory_tenant_method_createdutc ON requesthistory (tenant_id, httpmethod, createdutc);
                CREATE INDEX IF NOT EXISTS idx_requesthistory_tenant_sourceip_createdutc ON requesthistory (tenant_id, sourceip, createdutc);
                CREATE INDEX IF NOT EXISTS idx_requesthistory_tenant_requesttype_createdutc ON requesthistory (tenant_id, requesttype, createdutc);
                CREATE INDEX IF NOT EXISTS idx_requesthistory_tenant_user_createdutc ON requesthistory (tenant_id, user_id, createdutc);
                CREATE INDEX IF NOT EXISTS idx_requesthistory_tenant_accesskey_createdutc ON requesthistory (tenant_id, accesskey, createdutc);
                ";
        }
    }
}
