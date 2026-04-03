namespace Less3.Database.Sqlite.Queries
{
    using System;

    internal static class SetupQueries
    {
        internal static string CreateTablesAndIndices()
        {
            return
                @"CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    guid VARCHAR(64) NOT NULL,
                    name VARCHAR(256),
                    email VARCHAR(256),
                    createdutc VARCHAR(64) NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_users_guid ON users (guid);
                CREATE INDEX IF NOT EXISTS idx_users_name ON users (name);
                CREATE INDEX IF NOT EXISTS idx_users_email ON users (email);

                CREATE TABLE IF NOT EXISTS credential (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    guid VARCHAR(64) NOT NULL,
                    userguid VARCHAR(64) NOT NULL,
                    description VARCHAR(256),
                    accesskey VARCHAR(256),
                    secretkey VARCHAR(256),
                    isbase64 INT NOT NULL DEFAULT 0,
                    createdutc VARCHAR(64) NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_credential_guid ON credential (guid);
                CREATE INDEX IF NOT EXISTS idx_credential_userguid ON credential (userguid);
                CREATE INDEX IF NOT EXISTS idx_credential_accesskey ON credential (accesskey);

                CREATE TABLE IF NOT EXISTS buckets (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    guid VARCHAR(64) NOT NULL,
                    ownerguid VARCHAR(64) NOT NULL,
                    name VARCHAR(256) NOT NULL,
                    regionstring VARCHAR(64),
                    storagetype VARCHAR(32),
                    diskdirectory VARCHAR(1024),
                    enableversioning INT NOT NULL DEFAULT 0,
                    enablepublicwrite INT NOT NULL DEFAULT 0,
                    enablepublicread INT NOT NULL DEFAULT 0,
                    createdutc VARCHAR(64) NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_buckets_guid ON buckets (guid);
                CREATE INDEX IF NOT EXISTS idx_buckets_name ON buckets (name);
                CREATE INDEX IF NOT EXISTS idx_buckets_ownerguid ON buckets (ownerguid);

                CREATE TABLE IF NOT EXISTS objects (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    guid VARCHAR(64) NOT NULL,
                    bucketguid VARCHAR(64) NOT NULL,
                    ownerguid VARCHAR(64),
                    authorguid VARCHAR(64),
                    key VARCHAR(1024),
                    contenttype VARCHAR(256),
                    contentlength INTEGER NOT NULL DEFAULT 0,
                    version INTEGER NOT NULL DEFAULT 1,
                    etag VARCHAR(256),
                    retention VARCHAR(32),
                    blobfilename VARCHAR(1024),
                    isfolder INT NOT NULL DEFAULT 0,
                    deletemarker INT NOT NULL DEFAULT 0,
                    md5 VARCHAR(64),
                    createdutc VARCHAR(64) NOT NULL,
                    lastupdateutc VARCHAR(64) NOT NULL,
                    lastaccessutc VARCHAR(64) NOT NULL,
                    metadata TEXT,
                    expirationutc VARCHAR(64)
                );

                CREATE INDEX IF NOT EXISTS idx_objects_guid ON objects (guid);
                CREATE INDEX IF NOT EXISTS idx_objects_bucketguid ON objects (bucketguid);
                CREATE INDEX IF NOT EXISTS idx_objects_ownerguid ON objects (ownerguid);
                CREATE INDEX IF NOT EXISTS idx_objects_key ON objects (key);
                CREATE INDEX IF NOT EXISTS idx_objects_deletemarker ON objects (deletemarker);

                CREATE TABLE IF NOT EXISTS bucketacls (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    guid VARCHAR(64) NOT NULL,
                    usergroup VARCHAR(256),
                    bucketguid VARCHAR(64) NOT NULL,
                    userguid VARCHAR(64),
                    issuedbyuserguid VARCHAR(64),
                    permitread INT NOT NULL DEFAULT 0,
                    permitwrite INT NOT NULL DEFAULT 0,
                    permitreadacp INT NOT NULL DEFAULT 0,
                    permitwriteacp INT NOT NULL DEFAULT 0,
                    fullcontrol INT NOT NULL DEFAULT 0,
                    createdutc VARCHAR(64) NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_bucketacls_bucketguid ON bucketacls (bucketguid);
                CREATE INDEX IF NOT EXISTS idx_bucketacls_userguid ON bucketacls (userguid);

                CREATE TABLE IF NOT EXISTS objectacls (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    guid VARCHAR(64) NOT NULL,
                    usergroup VARCHAR(256),
                    userguid VARCHAR(64),
                    issuedbyuserguid VARCHAR(64),
                    bucketguid VARCHAR(64) NOT NULL,
                    objectguid VARCHAR(64) NOT NULL,
                    permitread INT NOT NULL DEFAULT 0,
                    permitwrite INT NOT NULL DEFAULT 0,
                    permitreadacp INT NOT NULL DEFAULT 0,
                    permitwriteacp INT NOT NULL DEFAULT 0,
                    fullcontrol INT NOT NULL DEFAULT 0,
                    createdutc VARCHAR(64) NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_objectacls_objectguid ON objectacls (objectguid);
                CREATE INDEX IF NOT EXISTS idx_objectacls_bucketguid ON objectacls (bucketguid);
                CREATE INDEX IF NOT EXISTS idx_objectacls_userguid ON objectacls (userguid);

                CREATE TABLE IF NOT EXISTS buckettags (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    guid VARCHAR(64) NOT NULL,
                    bucketguid VARCHAR(64) NOT NULL,
                    key VARCHAR(256),
                    value VARCHAR(1024),
                    createdutc VARCHAR(64) NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_buckettags_bucketguid ON buckettags (bucketguid);

                CREATE TABLE IF NOT EXISTS objecttags (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    guid VARCHAR(64) NOT NULL,
                    bucketguid VARCHAR(64) NOT NULL,
                    objectguid VARCHAR(64) NOT NULL,
                    key VARCHAR(256),
                    value VARCHAR(1024),
                    createdutc VARCHAR(64) NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_objecttags_objectguid ON objecttags (objectguid);
                CREATE INDEX IF NOT EXISTS idx_objecttags_bucketguid ON objecttags (bucketguid);

                CREATE TABLE IF NOT EXISTS uploads (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    guid VARCHAR(64) NOT NULL,
                    bucketguid VARCHAR(64),
                    ownerguid VARCHAR(64),
                    authorguid VARCHAR(64),
                    key VARCHAR(1024),
                    createdutc VARCHAR(64) NOT NULL,
                    lastaccessutc VARCHAR(64) NOT NULL,
                    expirationutc VARCHAR(64) NOT NULL,
                    contenttype VARCHAR(256),
                    metadata TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_uploads_guid ON uploads (guid);
                CREATE INDEX IF NOT EXISTS idx_uploads_bucketguid ON uploads (bucketguid);

                CREATE TABLE IF NOT EXISTS uploadparts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    guid VARCHAR(64) NOT NULL,
                    bucketguid VARCHAR(64) NOT NULL,
                    ownerguid VARCHAR(64) NOT NULL,
                    uploadguid VARCHAR(64) NOT NULL,
                    partnumber INT NOT NULL DEFAULT 1,
                    partlength INT NOT NULL DEFAULT 0,
                    md5hash VARCHAR(64),
                    sha1hash VARCHAR(64),
                    sha256hash VARCHAR(64),
                    lastaccessutc VARCHAR(64) NOT NULL,
                    createdutc VARCHAR(64) NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_uploadparts_uploadguid ON uploadparts (uploadguid);

                CREATE TABLE IF NOT EXISTS requesthistory (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    guid VARCHAR(64) NOT NULL,
                    httpmethod VARCHAR(16),
                    requesturl VARCHAR(2048),
                    sourceip VARCHAR(64),
                    statuscode INT NOT NULL DEFAULT 0,
                    success INT NOT NULL DEFAULT 1,
                    durationms INTEGER NOT NULL DEFAULT 0,
                    requesttype VARCHAR(128),
                    userguid VARCHAR(64),
                    accesskey VARCHAR(256),
                    requestcontenttype VARCHAR(256),
                    requestbodylength INTEGER NOT NULL DEFAULT 0,
                    responsecontenttype VARCHAR(256),
                    responsebodylength INTEGER NOT NULL DEFAULT 0,
                    createdutc VARCHAR(64) NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_requesthistory_guid ON requesthistory (guid);
                CREATE INDEX IF NOT EXISTS idx_requesthistory_createdutc ON requesthistory (createdutc);
                ";
        }
    }
}
