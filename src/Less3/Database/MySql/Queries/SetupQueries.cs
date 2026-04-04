namespace Less3.Database.MySql.Queries
{
    using System;
    using System.Collections.Generic;

    internal static class SetupQueries
    {
        internal static string CreateTables()
        {
            return
                @"CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    guid VARCHAR(64) NOT NULL,
                    name VARCHAR(256),
                    email VARCHAR(256),
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS credential (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    guid VARCHAR(64) NOT NULL,
                    userguid VARCHAR(64) NOT NULL,
                    description VARCHAR(256),
                    accesskey VARCHAR(256),
                    secretkey VARCHAR(256),
                    isbase64 TINYINT(1) NOT NULL DEFAULT 0,
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS buckets (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    guid VARCHAR(64) NOT NULL,
                    ownerguid VARCHAR(64) NOT NULL,
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
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    guid VARCHAR(64) NOT NULL,
                    bucketguid VARCHAR(64) NOT NULL,
                    ownerguid VARCHAR(64),
                    authorguid VARCHAR(64),
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
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    guid VARCHAR(64) NOT NULL,
                    usergroup VARCHAR(256),
                    bucketguid VARCHAR(64) NOT NULL,
                    userguid VARCHAR(64),
                    issuedbyuserguid VARCHAR(64),
                    permitread TINYINT(1) NOT NULL DEFAULT 0,
                    permitwrite TINYINT(1) NOT NULL DEFAULT 0,
                    permitreadacp TINYINT(1) NOT NULL DEFAULT 0,
                    permitwriteacp TINYINT(1) NOT NULL DEFAULT 0,
                    fullcontrol TINYINT(1) NOT NULL DEFAULT 0,
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS objectacls (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    guid VARCHAR(64) NOT NULL,
                    usergroup VARCHAR(256),
                    userguid VARCHAR(64),
                    issuedbyuserguid VARCHAR(64),
                    bucketguid VARCHAR(64) NOT NULL,
                    objectguid VARCHAR(64) NOT NULL,
                    permitread TINYINT(1) NOT NULL DEFAULT 0,
                    permitwrite TINYINT(1) NOT NULL DEFAULT 0,
                    permitreadacp TINYINT(1) NOT NULL DEFAULT 0,
                    permitwriteacp TINYINT(1) NOT NULL DEFAULT 0,
                    fullcontrol TINYINT(1) NOT NULL DEFAULT 0,
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS buckettags (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    guid VARCHAR(64) NOT NULL,
                    bucketguid VARCHAR(64) NOT NULL,
                    `key` VARCHAR(256),
                    value VARCHAR(1024),
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS objecttags (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    guid VARCHAR(64) NOT NULL,
                    bucketguid VARCHAR(64) NOT NULL,
                    objectguid VARCHAR(64) NOT NULL,
                    `key` VARCHAR(256),
                    value VARCHAR(1024),
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS uploads (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    guid VARCHAR(64) NOT NULL,
                    bucketguid VARCHAR(64),
                    ownerguid VARCHAR(64),
                    authorguid VARCHAR(64),
                    `key` VARCHAR(1024),
                    createdutc DATETIME(6) NOT NULL,
                    lastaccessutc DATETIME(6) NOT NULL,
                    expirationutc DATETIME(6) NOT NULL,
                    contenttype VARCHAR(256),
                    metadata TEXT
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS uploadparts (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    guid VARCHAR(64) NOT NULL,
                    bucketguid VARCHAR(64) NOT NULL,
                    ownerguid VARCHAR(64) NOT NULL,
                    uploadguid VARCHAR(64) NOT NULL,
                    partnumber INT NOT NULL DEFAULT 1,
                    partlength INT NOT NULL DEFAULT 0,
                    md5hash VARCHAR(64),
                    sha1hash VARCHAR(64),
                    sha256hash VARCHAR(64),
                    lastaccessutc DATETIME(6) NOT NULL,
                    createdutc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS requesthistory (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    guid VARCHAR(64) NOT NULL,
                    httpmethod VARCHAR(16),
                    requesturl VARCHAR(2048),
                    sourceip VARCHAR(64),
                    statuscode INT NOT NULL DEFAULT 0,
                    success TINYINT(1) NOT NULL DEFAULT 1,
                    durationms BIGINT NOT NULL DEFAULT 0,
                    requesttype VARCHAR(128),
                    userguid VARCHAR(64),
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

            indices.Add("CREATE INDEX idx_users_guid ON users (guid);");
            indices.Add("CREATE INDEX idx_users_name ON users (name);");
            indices.Add("CREATE INDEX idx_users_email ON users (email);");

            indices.Add("CREATE INDEX idx_credential_guid ON credential (guid);");
            indices.Add("CREATE INDEX idx_credential_userguid ON credential (userguid);");
            indices.Add("CREATE INDEX idx_credential_accesskey ON credential (accesskey);");

            indices.Add("CREATE INDEX idx_buckets_guid ON buckets (guid);");
            indices.Add("CREATE INDEX idx_buckets_name ON buckets (name);");
            indices.Add("CREATE INDEX idx_buckets_ownerguid ON buckets (ownerguid);");

            indices.Add("CREATE INDEX idx_objects_guid ON objects (guid);");
            indices.Add("CREATE INDEX idx_objects_bucketguid ON objects (bucketguid);");
            indices.Add("CREATE INDEX idx_objects_ownerguid ON objects (ownerguid);");
            indices.Add("CREATE INDEX idx_objects_key ON objects (`key`);");
            indices.Add("CREATE INDEX idx_objects_deletemarker ON objects (deletemarker);");

            indices.Add("CREATE INDEX idx_bucketacls_bucketguid ON bucketacls (bucketguid);");
            indices.Add("CREATE INDEX idx_bucketacls_userguid ON bucketacls (userguid);");

            indices.Add("CREATE INDEX idx_objectacls_objectguid ON objectacls (objectguid);");
            indices.Add("CREATE INDEX idx_objectacls_bucketguid ON objectacls (bucketguid);");
            indices.Add("CREATE INDEX idx_objectacls_userguid ON objectacls (userguid);");

            indices.Add("CREATE INDEX idx_buckettags_bucketguid ON buckettags (bucketguid);");

            indices.Add("CREATE INDEX idx_objecttags_objectguid ON objecttags (objectguid);");
            indices.Add("CREATE INDEX idx_objecttags_bucketguid ON objecttags (bucketguid);");

            indices.Add("CREATE INDEX idx_uploads_guid ON uploads (guid);");
            indices.Add("CREATE INDEX idx_uploads_bucketguid ON uploads (bucketguid);");

            indices.Add("CREATE INDEX idx_uploadparts_uploadguid ON uploadparts (uploadguid);");

            indices.Add("CREATE INDEX idx_requesthistory_guid ON requesthistory (guid);");
            indices.Add("CREATE INDEX idx_requesthistory_createdutc ON requesthistory (createdutc);");

            return indices;
        }

        internal static string AnalyzeTables()
        {
            return "ANALYZE TABLE users, credential, buckets, objects, bucketacls, objectacls, buckettags, objecttags, uploads, uploadparts, requesthistory;";
        }
    }
}
