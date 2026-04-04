namespace Less3.Database.SqlServer.Queries
{
    using System;

    internal static class SetupQueries
    {
        internal static string CreateTablesAndIndices()
        {
            return
                @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='users' AND xtype='U')
                CREATE TABLE users (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    guid NVARCHAR(64) NOT NULL,
                    name NVARCHAR(256),
                    email NVARCHAR(256),
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_users_guid')
                CREATE INDEX idx_users_guid ON users (guid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_users_name')
                CREATE INDEX idx_users_name ON users (name);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_users_email')
                CREATE INDEX idx_users_email ON users (email);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='credential' AND xtype='U')
                CREATE TABLE credential (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    guid NVARCHAR(64) NOT NULL,
                    userguid NVARCHAR(64) NOT NULL,
                    description NVARCHAR(256),
                    accesskey NVARCHAR(256),
                    secretkey NVARCHAR(256),
                    isbase64 BIT NOT NULL DEFAULT 0,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_credential_guid')
                CREATE INDEX idx_credential_guid ON credential (guid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_credential_userguid')
                CREATE INDEX idx_credential_userguid ON credential (userguid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_credential_accesskey')
                CREATE INDEX idx_credential_accesskey ON credential (accesskey);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='buckets' AND xtype='U')
                CREATE TABLE buckets (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    guid NVARCHAR(64) NOT NULL,
                    ownerguid NVARCHAR(64) NOT NULL,
                    name NVARCHAR(256) NOT NULL,
                    regionstring NVARCHAR(64),
                    storagetype NVARCHAR(32),
                    diskdirectory NVARCHAR(1024),
                    enableversioning BIT NOT NULL DEFAULT 0,
                    enablepublicwrite BIT NOT NULL DEFAULT 0,
                    enablepublicread BIT NOT NULL DEFAULT 0,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckets_guid')
                CREATE INDEX idx_buckets_guid ON buckets (guid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckets_name')
                CREATE INDEX idx_buckets_name ON buckets (name);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckets_ownerguid')
                CREATE INDEX idx_buckets_ownerguid ON buckets (ownerguid);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='objects' AND xtype='U')
                CREATE TABLE objects (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    guid NVARCHAR(64) NOT NULL,
                    bucketguid NVARCHAR(64) NOT NULL,
                    ownerguid NVARCHAR(64),
                    authorguid NVARCHAR(64),
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

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_guid')
                CREATE INDEX idx_objects_guid ON objects (guid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_bucketguid')
                CREATE INDEX idx_objects_bucketguid ON objects (bucketguid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_ownerguid')
                CREATE INDEX idx_objects_ownerguid ON objects (ownerguid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_key')
                CREATE INDEX idx_objects_key ON objects ([key]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objects_deletemarker')
                CREATE INDEX idx_objects_deletemarker ON objects (deletemarker);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='bucketacls' AND xtype='U')
                CREATE TABLE bucketacls (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    guid NVARCHAR(64) NOT NULL,
                    usergroup NVARCHAR(256),
                    bucketguid NVARCHAR(64) NOT NULL,
                    userguid NVARCHAR(64),
                    issuedbyuserguid NVARCHAR(64),
                    permitread BIT NOT NULL DEFAULT 0,
                    permitwrite BIT NOT NULL DEFAULT 0,
                    permitreadacp BIT NOT NULL DEFAULT 0,
                    permitwriteacp BIT NOT NULL DEFAULT 0,
                    fullcontrol BIT NOT NULL DEFAULT 0,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_bucketacls_bucketguid')
                CREATE INDEX idx_bucketacls_bucketguid ON bucketacls (bucketguid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_bucketacls_userguid')
                CREATE INDEX idx_bucketacls_userguid ON bucketacls (userguid);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='objectacls' AND xtype='U')
                CREATE TABLE objectacls (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    guid NVARCHAR(64) NOT NULL,
                    usergroup NVARCHAR(256),
                    userguid NVARCHAR(64),
                    issuedbyuserguid NVARCHAR(64),
                    bucketguid NVARCHAR(64) NOT NULL,
                    objectguid NVARCHAR(64) NOT NULL,
                    permitread BIT NOT NULL DEFAULT 0,
                    permitwrite BIT NOT NULL DEFAULT 0,
                    permitreadacp BIT NOT NULL DEFAULT 0,
                    permitwriteacp BIT NOT NULL DEFAULT 0,
                    fullcontrol BIT NOT NULL DEFAULT 0,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objectacls_objectguid')
                CREATE INDEX idx_objectacls_objectguid ON objectacls (objectguid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objectacls_bucketguid')
                CREATE INDEX idx_objectacls_bucketguid ON objectacls (bucketguid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objectacls_userguid')
                CREATE INDEX idx_objectacls_userguid ON objectacls (userguid);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='buckettags' AND xtype='U')
                CREATE TABLE buckettags (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    guid NVARCHAR(64) NOT NULL,
                    bucketguid NVARCHAR(64) NOT NULL,
                    [key] NVARCHAR(256),
                    value NVARCHAR(1024),
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_buckettags_bucketguid')
                CREATE INDEX idx_buckettags_bucketguid ON buckettags (bucketguid);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='objecttags' AND xtype='U')
                CREATE TABLE objecttags (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    guid NVARCHAR(64) NOT NULL,
                    bucketguid NVARCHAR(64) NOT NULL,
                    objectguid NVARCHAR(64) NOT NULL,
                    [key] NVARCHAR(256),
                    value NVARCHAR(1024),
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objecttags_objectguid')
                CREATE INDEX idx_objecttags_objectguid ON objecttags (objectguid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_objecttags_bucketguid')
                CREATE INDEX idx_objecttags_bucketguid ON objecttags (bucketguid);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='uploads' AND xtype='U')
                CREATE TABLE uploads (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    guid NVARCHAR(64) NOT NULL,
                    bucketguid NVARCHAR(64),
                    ownerguid NVARCHAR(64),
                    authorguid NVARCHAR(64),
                    [key] NVARCHAR(1024),
                    createdutc NVARCHAR(64) NOT NULL,
                    lastaccessutc NVARCHAR(64) NOT NULL,
                    expirationutc NVARCHAR(64) NOT NULL,
                    contenttype NVARCHAR(256),
                    metadata NVARCHAR(MAX)
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_uploads_guid')
                CREATE INDEX idx_uploads_guid ON uploads (guid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_uploads_bucketguid')
                CREATE INDEX idx_uploads_bucketguid ON uploads (bucketguid);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='uploadparts' AND xtype='U')
                CREATE TABLE uploadparts (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    guid NVARCHAR(64) NOT NULL,
                    bucketguid NVARCHAR(64) NOT NULL,
                    ownerguid NVARCHAR(64) NOT NULL,
                    uploadguid NVARCHAR(64) NOT NULL,
                    partnumber INT NOT NULL DEFAULT 1,
                    partlength INT NOT NULL DEFAULT 0,
                    md5hash NVARCHAR(64),
                    sha1hash NVARCHAR(64),
                    sha256hash NVARCHAR(64),
                    lastaccessutc NVARCHAR(64) NOT NULL,
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_uploadparts_uploadguid')
                CREATE INDEX idx_uploadparts_uploadguid ON uploadparts (uploadguid);

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='requesthistory' AND xtype='U')
                CREATE TABLE requesthistory (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    guid NVARCHAR(64) NOT NULL,
                    httpmethod NVARCHAR(16),
                    requesturl NVARCHAR(2048),
                    sourceip NVARCHAR(64),
                    statuscode INT NOT NULL DEFAULT 0,
                    success BIT NOT NULL DEFAULT 1,
                    durationms BIGINT NOT NULL DEFAULT 0,
                    requesttype NVARCHAR(128),
                    userguid NVARCHAR(64),
                    accesskey NVARCHAR(256),
                    requestcontenttype NVARCHAR(256),
                    requestbodylength BIGINT NOT NULL DEFAULT 0,
                    responsecontenttype NVARCHAR(256),
                    responsebodylength BIGINT NOT NULL DEFAULT 0,
                    requestbody NVARCHAR(MAX),
                    responsebody NVARCHAR(MAX),
                    createdutc NVARCHAR(64) NOT NULL
                );

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_requesthistory_guid')
                CREATE INDEX idx_requesthistory_guid ON requesthistory (guid);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_requesthistory_createdutc')
                CREATE INDEX idx_requesthistory_createdutc ON requesthistory (createdutc);
                ";
        }
    }
}
