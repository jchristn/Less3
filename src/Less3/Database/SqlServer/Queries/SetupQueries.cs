namespace Less3.Database.SqlServer.Queries
{
    using System;

    internal static class SetupQueries
    {
        internal static string CreateTablesAndIndices()
        {
            return
                @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='tenants' AND xtype='U')
                CREATE TABLE tenants (
                    id NVARCHAR(32) PRIMARY KEY,
                    parent_id NVARCHAR(32),
                    name NVARCHAR(256) NOT NULL,
                    active BIT NOT NULL DEFAULT 1,
                    createdutc NVARCHAR(64) NOT NULL,
                    lastupdateutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_tenants_name')
                CREATE INDEX idx_tenants_name ON tenants (name);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_tenants_active')
                CREATE INDEX idx_tenants_active ON tenants (active);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='roles' AND xtype='U')
                CREATE TABLE roles (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64),
                    name NVARCHAR(128) NOT NULL,
                    description NVARCHAR(1024),
                    isbuiltin BIT NOT NULL DEFAULT 0,
                    inheritstochildren BIT NOT NULL DEFAULT 1,
                    active BIT NOT NULL DEFAULT 1,
                    createdutc NVARCHAR(64) NOT NULL,
                    lastupdateutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_roles_tenant_id')
                CREATE INDEX idx_roles_tenant_id ON roles (tenant_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_roles_tenant_name')
                CREATE UNIQUE INDEX idx_roles_tenant_name ON roles (tenant_id, name);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='permissions' AND xtype='U')
                CREATE TABLE permissions (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64),
                    role_id NVARCHAR(32) NOT NULL,
                    resourcetype NVARCHAR(128) NOT NULL,
                    operation NVARCHAR(128) NOT NULL,
                    permit BIT NOT NULL DEFAULT 1,
                    active BIT NOT NULL DEFAULT 1,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_permissions_tenant_role')
                CREATE INDEX idx_permissions_tenant_role ON permissions (tenant_id, role_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_permissions_lookup')
                CREATE INDEX idx_permissions_lookup ON permissions (tenant_id, resourcetype, operation, active);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='roleassignments' AND xtype='U')
                CREATE TABLE roleassignments (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL,
                    role_id NVARCHAR(32) NOT NULL,
                    principaltype NVARCHAR(64) NOT NULL,
                    principal_id NVARCHAR(32) NOT NULL,
                    resourcetype NVARCHAR(128),
                    resource_id NVARCHAR(32),
                    active BIT NOT NULL DEFAULT 1,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_roleassignments_principal')
                CREATE INDEX idx_roleassignments_principal ON roleassignments (tenant_id, principaltype, principal_id, active);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_roleassignments_role')
                CREATE INDEX idx_roleassignments_role ON roleassignments (tenant_id, role_id);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='authsessions' AND xtype='U')
                CREATE TABLE authsessions (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL,
                    principaltype NVARCHAR(64) NOT NULL,
                    principal_id NVARCHAR(32) NOT NULL,
                    tokenhash NVARCHAR(256) NOT NULL,
                    active BIT NOT NULL DEFAULT 1,
                    createdutc NVARCHAR(64) NOT NULL,
                    expirationutc NVARCHAR(64) NOT NULL,
                    revokedutc NVARCHAR(64),
                    sourceip NVARCHAR(64)
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_authsessions_tokenhash')
                CREATE UNIQUE INDEX idx_authsessions_tokenhash ON authsessions (tokenhash);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_authsessions_principal')
                CREATE INDEX idx_authsessions_principal ON authsessions (tenant_id, principaltype, principal_id, active);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='authorizationaudit' AND xtype='U')
                CREATE TABLE authorizationaudit (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64),
                    user_id NVARCHAR(32),
                    credential_id NVARCHAR(32),
                    resourcetype NVARCHAR(128),
                    resource_id NVARCHAR(32),
                    operation NVARCHAR(128),
                    permitted BIT NOT NULL DEFAULT 0,
                    reason NVARCHAR(MAX),
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_authorizationaudit_tenant_createdutc')
                CREATE INDEX idx_authorizationaudit_tenant_createdutc ON authorizationaudit (tenant_id, createdutc);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_authorizationaudit_principal')
                CREATE INDEX idx_authorizationaudit_principal ON authorizationaudit (tenant_id, user_id, credential_id);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='users' AND xtype='U')
                CREATE TABLE users (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
                    name NVARCHAR(256),
                    email NVARCHAR(256),
                    passwordhash NVARCHAR(512),
                    isadmin BIT NOT NULL DEFAULT 0,
                    istenantadmin BIT NOT NULL DEFAULT 0,
                    active BIT NOT NULL DEFAULT 1,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_users_id')
                CREATE INDEX idx_users_id ON users (id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_users_name')
                CREATE INDEX idx_users_name ON users (name);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_users_email')
                CREATE INDEX idx_users_email ON users (email);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='credentials' AND xtype='U')
                CREATE TABLE credentials (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
                    user_id NVARCHAR(64) NOT NULL,
                    description NVARCHAR(256),
                    accesskey NVARCHAR(256),
                    secretkey NVARCHAR(256),
                    isbase64 BIT NOT NULL DEFAULT 0,
                    active BIT NOT NULL DEFAULT 1,
                    lastusedutc NVARCHAR(64),
                    lastfailedutc NVARCHAR(64),
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_credentials_id')
                CREATE INDEX idx_credentials_id ON credentials (id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_credentials_user_id')
                CREATE INDEX idx_credentials_user_id ON credentials (user_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_credentials_accesskey')
                CREATE INDEX idx_credentials_accesskey ON credentials (accesskey);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='buckets' AND xtype='U')
                CREATE TABLE buckets (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
                    owner_id NVARCHAR(64) NOT NULL,
                    name NVARCHAR(256) NOT NULL,
                    regionstring NVARCHAR(64),
                    storagetype NVARCHAR(32),
                    diskdirectory NVARCHAR(1024),
                    enableversioning BIT NOT NULL DEFAULT 0,
                    enablepublicwrite BIT NOT NULL DEFAULT 0,
                    enablepublicread BIT NOT NULL DEFAULT 0,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckets_id')
                CREATE INDEX idx_buckets_id ON buckets (id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckets_name')
                CREATE INDEX idx_buckets_name ON buckets (name);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckets_owner_id')
                CREATE INDEX idx_buckets_owner_id ON buckets (owner_id);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='objects' AND xtype='U')
                CREATE TABLE objects (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id NVARCHAR(64) NOT NULL,
                    owner_id NVARCHAR(64),
                    author_id NVARCHAR(64),
                    [key] NVARCHAR(1024),
                    contenttype NVARCHAR(256),
                    contentlength BIGINT NOT NULL DEFAULT 0,
                    version BIGINT NOT NULL DEFAULT 1,
                    etag NVARCHAR(256),
                    retention NVARCHAR(32),
                    blobfilename NVARCHAR(1024),
                    isfolder BIT NOT NULL DEFAULT 0,
                    deletemarker BIT NOT NULL DEFAULT 0,
                    md5 NVARCHAR(64),
                    createdutc NVARCHAR(64) NOT NULL,
                    lastupdateutc NVARCHAR(64) NOT NULL,
                    lastaccessutc NVARCHAR(64) NOT NULL,
                    metadata NVARCHAR(MAX),
                    expirationutc NVARCHAR(64)
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_id')
                CREATE INDEX idx_objects_id ON objects (id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_bucket_id')
                CREATE INDEX idx_objects_bucket_id ON objects (bucket_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_owner_id')
                CREATE INDEX idx_objects_owner_id ON objects (owner_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_key')
                CREATE INDEX idx_objects_key ON objects ([key]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_deletemarker')
                CREATE INDEX idx_objects_deletemarker ON objects (deletemarker);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='bucketacls' AND xtype='U')
                CREATE TABLE bucketacls (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
                    usergroup NVARCHAR(256),
                    bucket_id NVARCHAR(64) NOT NULL,
                    user_id NVARCHAR(64),
                    issued_by_user_id NVARCHAR(64),
                    permitread BIT NOT NULL DEFAULT 0,
                    permitwrite BIT NOT NULL DEFAULT 0,
                    permitreadacp BIT NOT NULL DEFAULT 0,
                    permitwriteacp BIT NOT NULL DEFAULT 0,
                    fullcontrol BIT NOT NULL DEFAULT 0,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_bucketacls_bucket_id')
                CREATE INDEX idx_bucketacls_bucket_id ON bucketacls (bucket_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_bucketacls_user_id')
                CREATE INDEX idx_bucketacls_user_id ON bucketacls (user_id);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='objectacls' AND xtype='U')
                CREATE TABLE objectacls (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
                    usergroup NVARCHAR(256),
                    user_id NVARCHAR(64),
                    issued_by_user_id NVARCHAR(64),
                    bucket_id NVARCHAR(64) NOT NULL,
                    object_id NVARCHAR(64) NOT NULL,
                    permitread BIT NOT NULL DEFAULT 0,
                    permitwrite BIT NOT NULL DEFAULT 0,
                    permitreadacp BIT NOT NULL DEFAULT 0,
                    permitwriteacp BIT NOT NULL DEFAULT 0,
                    fullcontrol BIT NOT NULL DEFAULT 0,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objectacls_object_id')
                CREATE INDEX idx_objectacls_object_id ON objectacls (object_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objectacls_bucket_id')
                CREATE INDEX idx_objectacls_bucket_id ON objectacls (bucket_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objectacls_user_id')
                CREATE INDEX idx_objectacls_user_id ON objectacls (user_id);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='buckettags' AND xtype='U')
                CREATE TABLE buckettags (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id NVARCHAR(64) NOT NULL,
                    [key] NVARCHAR(256),
                    value NVARCHAR(1024),
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckettags_bucket_id')
                CREATE INDEX idx_buckettags_bucket_id ON buckettags (bucket_id);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='objecttags' AND xtype='U')
                CREATE TABLE objecttags (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id NVARCHAR(64) NOT NULL,
                    object_id NVARCHAR(64) NOT NULL,
                    [key] NVARCHAR(256),
                    value NVARCHAR(1024),
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objecttags_object_id')
                CREATE INDEX idx_objecttags_object_id ON objecttags (object_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objecttags_bucket_id')
                CREATE INDEX idx_objecttags_bucket_id ON objecttags (bucket_id);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='uploads' AND xtype='U')
                CREATE TABLE uploads (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id NVARCHAR(64),
                    owner_id NVARCHAR(64),
                    author_id NVARCHAR(64),
                    [key] NVARCHAR(1024),
                    createdutc NVARCHAR(64) NOT NULL,
                    lastaccessutc NVARCHAR(64) NOT NULL,
                    expirationutc NVARCHAR(64) NOT NULL,
                    contenttype NVARCHAR(256),
                    metadata NVARCHAR(MAX)
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_uploads_id')
                CREATE INDEX idx_uploads_id ON uploads (id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_uploads_bucket_id')
                CREATE INDEX idx_uploads_bucket_id ON uploads (bucket_id);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='uploadparts' AND xtype='U')
                CREATE TABLE uploadparts (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
                    bucket_id NVARCHAR(64) NOT NULL,
                    owner_id NVARCHAR(64) NOT NULL,
                    upload_id NVARCHAR(64) NOT NULL,
                    partnumber INT NOT NULL DEFAULT 1,
                    partlength INT NOT NULL DEFAULT 0,
                    md5hash NVARCHAR(64),
                    sha1hash NVARCHAR(64),
                    sha256hash NVARCHAR(64),
                    lastaccessutc NVARCHAR(64) NOT NULL,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_uploadparts_upload_id')
                CREATE INDEX idx_uploadparts_upload_id ON uploadparts (upload_id);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='requesthistory' AND xtype='U')
                CREATE TABLE requesthistory (
                    id NVARCHAR(32) PRIMARY KEY,
                    tenant_id NVARCHAR(64) NOT NULL DEFAULT 'default',
                    httpmethod NVARCHAR(16),
                    requesturl NVARCHAR(2048),
                    sourceip NVARCHAR(64),
                    statuscode INT NOT NULL DEFAULT 0,
                    success BIT NOT NULL DEFAULT 1,
                    durationms BIGINT NOT NULL DEFAULT 0,
                    requesttype NVARCHAR(128),
                    user_id NVARCHAR(64),
                    accesskey NVARCHAR(256),
                    requestcontenttype NVARCHAR(256),
                    requestbodylength BIGINT NOT NULL DEFAULT 0,
                    responsecontenttype NVARCHAR(256),
                    responsebodylength BIGINT NOT NULL DEFAULT 0,
                    requestbody NVARCHAR(MAX),
                    responsebody NVARCHAR(MAX),
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_requesthistory_id')
                CREATE INDEX idx_requesthistory_id ON requesthistory (id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_requesthistory_createdutc')
                CREATE INDEX idx_requesthistory_createdutc ON requesthistory (createdutc);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_users_tenant_id')
                CREATE INDEX idx_users_tenant_id ON users (tenant_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_users_tenant_email')
                CREATE UNIQUE INDEX idx_users_tenant_email ON users (tenant_id, email);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_credentials_tenant_id')
                CREATE INDEX idx_credentials_tenant_id ON credentials (tenant_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_credentials_tenant_user_id')
                CREATE INDEX idx_credentials_tenant_user_id ON credentials (tenant_id, user_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_credentials_accesskey_unique')
                CREATE UNIQUE INDEX idx_credentials_accesskey_unique ON credentials (accesskey);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckets_tenant_id')
                CREATE INDEX idx_buckets_tenant_id ON buckets (tenant_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckets_tenant_name')
                CREATE UNIQUE INDEX idx_buckets_tenant_name ON buckets (tenant_id, name);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_tenant_id')
                CREATE INDEX idx_objects_tenant_id ON objects (tenant_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_tenant_bucket_key')
                CREATE INDEX idx_objects_tenant_bucket_key ON objects (tenant_id, bucket_id, [key]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_tenant_bucket_createdutc')
                CREATE INDEX idx_objects_tenant_bucket_createdutc ON objects (tenant_id, bucket_id, createdutc);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_bucketacls_tenant_id')
                CREATE INDEX idx_bucketacls_tenant_id ON bucketacls (tenant_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_bucketacls_tenant_bucket_id')
                CREATE INDEX idx_bucketacls_tenant_bucket_id ON bucketacls (tenant_id, bucket_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objectacls_tenant_id')
                CREATE INDEX idx_objectacls_tenant_id ON objectacls (tenant_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objectacls_tenant_object_id')
                CREATE INDEX idx_objectacls_tenant_object_id ON objectacls (tenant_id, object_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckettags_tenant_id')
                CREATE INDEX idx_buckettags_tenant_id ON buckettags (tenant_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckettags_tenant_bucket_id')
                CREATE INDEX idx_buckettags_tenant_bucket_id ON buckettags (tenant_id, bucket_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objecttags_tenant_id')
                CREATE INDEX idx_objecttags_tenant_id ON objecttags (tenant_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objecttags_tenant_object_id')
                CREATE INDEX idx_objecttags_tenant_object_id ON objecttags (tenant_id, object_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_uploads_tenant_id')
                CREATE INDEX idx_uploads_tenant_id ON uploads (tenant_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_uploads_tenant_bucket_id')
                CREATE INDEX idx_uploads_tenant_bucket_id ON uploads (tenant_id, bucket_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_uploadparts_tenant_id')
                CREATE INDEX idx_uploadparts_tenant_id ON uploadparts (tenant_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_uploadparts_tenant_upload_id')
                CREATE INDEX idx_uploadparts_tenant_upload_id ON uploadparts (tenant_id, upload_id);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_requesthistory_tenant_createdutc')
                CREATE INDEX idx_requesthistory_tenant_createdutc ON requesthistory (tenant_id, createdutc);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_requesthistory_tenant_status_createdutc')
                CREATE INDEX idx_requesthistory_tenant_status_createdutc ON requesthistory (tenant_id, statuscode, createdutc);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_requesthistory_tenant_method_createdutc')
                CREATE INDEX idx_requesthistory_tenant_method_createdutc ON requesthistory (tenant_id, httpmethod, createdutc);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_requesthistory_tenant_sourceip_createdutc')
                CREATE INDEX idx_requesthistory_tenant_sourceip_createdutc ON requesthistory (tenant_id, sourceip, createdutc);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_requesthistory_tenant_requesttype_createdutc')
                CREATE INDEX idx_requesthistory_tenant_requesttype_createdutc ON requesthistory (tenant_id, requesttype, createdutc);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_requesthistory_tenant_user_createdutc')
                CREATE INDEX idx_requesthistory_tenant_user_createdutc ON requesthistory (tenant_id, user_id, createdutc);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_requesthistory_tenant_accesskey_createdutc')
                CREATE INDEX idx_requesthistory_tenant_accesskey_createdutc ON requesthistory (tenant_id, accesskey, createdutc);
                ";
        }
    }
}
