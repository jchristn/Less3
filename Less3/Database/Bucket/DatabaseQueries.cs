using System;
using System.Collections.Generic;
using System.Text;

using SqliteWrapper;

using Less3.Classes;

namespace Less3.Database.Bucket
{
    internal class DatabaseQueries
    {
        private DatabaseClient _Database = null;

        internal DatabaseQueries(DatabaseClient database)
        {
            _Database = database;
        }

        #region Table-Creation

        internal string CreateObjectTable()
        {
            string query =
                "CREATE TABLE IF NOT EXISTS Objects " +
                "(" +
                "  Id                INTEGER PRIMARY KEY, " +
                "  Owner             VARCHAR(64), " +
                "  Author            VARCHAR(64), " +
                "  Key               VARCHAR(256), " +
                "  ContentType       VARCHAR(128), " +
                "  ContentLength     INTEGER, " +
                "  Version           INTEGER, " +
                "  BlobFilename      VARCHAR(64), " +
                "  Etag              VARCHAR(64), " +
                "  RetentionType     VARCHAR(16), " +
                "  DeleteMarker      INTEGER, " +
                "  Md5               VARCHAR(32), " + 
                "  CreatedUtc        VARCHAR(32), " +
                "  LastUpdateUtc     VARCHAR(32), " +
                "  LastAccessUtc     VARCHAR(32), " +
                "  ExpirationUtc     VARCHAR(32) " +
                ")";
            return query;
        }

        internal string CreateBucketTagsTable()
        {
            string query =
                "CREATE TABLE IF NOT EXISTS BucketTags " +
                "(" +
                "  Id                INTEGER PRIMARY KEY, " + 
                "  Key               VARCHAR(256), " +
                "  Value             VARCHAR(512) " +
                ")";
            return query;
        }

        internal string CreateObjectTagsTable()
        {
            string query =
                "CREATE TABLE IF NOT EXISTS ObjectTags " +
                "(" +
                "  Id                INTEGER PRIMARY KEY, " +
                "  ObjectKey         VARCHAR(64), " +
                "  ObjectVersion     INTEGER, " +
                "  Key               VARCHAR(256), " +
                "  Value             VARCHAR(512) " +
                ")";
            return query;
        }

        internal string CreateBucketAclTable()
        {
            string query =
                "CREATE TABLE IF NOT Exists BucketAcl " +
                "(" +
                "  Id                 INTEGER PRIMARY KEY, " +
                "  UserGroup          VARCHAR(128), " +
                "  UserGUID           VARCHAR(64), " +
                "  IssuedByUserGUID   VARCHAR(64), " +
                "  PermitRead         VARCHAR(8), " +
                "  PermitWrite        VARCHAR(8), " +
                "  PermitReadAcp      VARCHAR(8), " +
                "  PermitWriteAcp     VARCHAR(8), " +
                "  FullControl        VARCHAR(8) " +
                ")";
            return query;
        }

        internal string CreateObjectAclTable()
        {
            string query =
                "CREATE TABLE IF NOT Exists ObjectAcl " +
                "(" +
                "  Id                 INTEGER PRIMARY KEY, " +
                "  UserGroup          VARCHAR(128), " +
                "  UserGUID           VARCHAR(64), " +
                "  IssuedByUserGUID   VARCHAR(64), " +
                "  ObjectKey          VARCHAR(64), " +
                "  ObjectVersion      INTEGER, " +
                "  PermitRead         VARCHAR(8), " +
                "  PermitWrite        VARCHAR(8), " +
                "  PermitReadAcp      VARCHAR(8), " +
                "  PermitWriteAcp     VARCHAR(8), " +
                "  FullControl        VARCHAR(8) " +
                ")";
            return query;
        }

        #endregion

        #region Object-Queries

        internal string ObjectExists(string key)
        {
            string query =
                "SELECT * FROM Objects " +
                "WHERE Key = '" + Sanitize(key) + "' " +
                "ORDER BY LastUpdateUtc DESC " +
                "LIMIT 1";
            return query;
        }

        internal string VersionExists(string key, long version)
        {
            string query =
                "SELECT * FROM Objects " +
                "WHERE Key = '" + Sanitize(key) + "' " +
                "AND Version = '" + version + "' " +
                "ORDER BY LastUpdateUtc DESC " +
                "LIMIT 1"; 
            return query;
        }

        internal string InsertObject(Obj obj)
        { 
            string query =
                "INSERT INTO Objects " +
                "(" +
                "  Owner, " +
                "  Author, " +
                "  Key, " +
                "  ContentType, " +
                "  ContentLength, " +
                "  Version, " +
                "  BlobFilename, " +
                "  Etag, " +
                "  RetentionType, " +
                "  DeleteMarker, " +
                "  Md5, " + 
                "  CreatedUtc, " +
                "  LastUpdateUtc, " +
                "  LastAccessUtc, " +
                "  ExpirationUtc " +
                ") VALUES (" +
                "  '" + Sanitize(obj.Owner) + "', " +
                "  '" + Sanitize(obj.Author) + "', " +
                "  '" + Sanitize(obj.Key) + "', " +
                "  '" + Sanitize(obj.ContentType) + "', " +
                "  '" + obj.ContentLength + "', " +
                "  '" + obj.Version + "', " +
                "  '" + Sanitize(obj.BlobFilename) + "', " +
                "  '" + Sanitize(obj.Etag) + "', " +
                "  '" + Sanitize(obj.RetentionType) + "', " +
                "  '" + obj.DeleteMarker + "', " +
                "  '" + Sanitize(obj.Md5) + "', " + 
                "  '" + TimestampUtc(obj.CreatedUtc) + "', " +
                "  '" + TimestampUtc(obj.LastUpdateUtc) + "', " +
                "  '" + TimestampUtc(obj.LastAccessUtc) + "', " +
                "  '" + TimestampUtc(obj.ExpirationUtc) + "' " +
                ")";
            return query;
        }

        internal string DeleteObject(string key, long version)
        { 
            string query =
                "DELETE FROM Objects WHERE Key = '" + Sanitize(key) + "' " +
                "AND Version = '" + version + "'";
            return query;
        }

        internal string MarkObjectDeleted(Obj obj)
        { 
            DateTime ts = DateTime.Now.ToUniversalTime();
             
            string query =
                "INSERT INTO Objects " +
                "(" +
                "  Owner, " +
                "  Author, " +
                "  Key, " +
                "  ContentType, " +
                "  ContentLength, " +
                "  Version, " +
                "  BlobFilename, " +
                "  Etag, " +
                "  RetentionType, " +
                "  DeleteMarker, " +
                "  Md5, " + 
                "  CreatedUtc, " +
                "  LastUpdateUtc, " +
                "  LastAccessUtc, " +
                "  ExpirationUtc " +
                ") VALUES (" +
                "  '" + Sanitize(obj.Owner) + "', " +
                "  '" + Sanitize(obj.Author) + "', " +
                "  '" + Sanitize(obj.Key) + "', " +
                "  '" + Sanitize(obj.ContentType) + "', " +
                "  0, " +
                "  '" + (obj.Version + 1) + "', " +
                "  null, " +
                "  '" + Sanitize(obj.Etag) + "', " +
                "  '" + Sanitize(obj.RetentionType) + "', " +
                "  '1', " +
                "  null, " + 
                "  '" + TimestampUtc(obj.CreatedUtc) + "', " +
                "  '" + TimestampUtc(ts) + "', " +
                "  '" + TimestampUtc(obj.LastAccessUtc) + "', " +
                "  '" + TimestampUtc(obj.ExpirationUtc) + "' " +
                ")";
            return query;
        }

        internal string UpdateRecord(string key, long version, Dictionary<string, object> vals)
        { 
            int added = 0;
            string query =
                "UPDATE Objects SET ";

            foreach (KeyValuePair<string, object> curr in vals)
            {
                if (String.IsNullOrEmpty(curr.Key)) continue;

                if (added == 0)
                {
                    query += Sanitize(curr.Key) + " = "; 
                    if (curr.Value == null) query += " null ";
                    else if (curr.Value is DateTime) query += "'" + TimestampUtc(Convert.ToDateTime(curr.Value)) + "' ";
                    else if (curr.Value is string) query += "'" + Sanitize(curr.Value.ToString()) + "' ";
                    else query += "'" + curr.Value.ToString() + "' ";
                }
                else
                {
                    query += "," + Sanitize(curr.Key) + " = ";
                    if (curr.Value == null) query += " null ";
                    else if (curr.Value is DateTime) query += "'" + TimestampUtc(Convert.ToDateTime(curr.Value)) + "' ";
                    else if (curr.Value is string) query += "'" + Sanitize(curr.Value.ToString()) + "' ";
                    else query += "'" + curr.Value.ToString() + "' ";
                }
                added++;
            }

            query += 
                "WHERE Key = '" + Sanitize(key) + "' " +
                "AND Version = '" + version + "'";
            return query;
        }

        internal string GetObjectCount()
        {
            return "SELECT COUNT(*) AS NumObjects, SUM(ContentLength) AS TotalBytes FROM Objects";
        }

        internal string Enumerate(string prefix, long indexStart, int maxResults)
        {
            string query =
                "SELECT * FROM " +
                "( " +
                "  SELECT * FROM Objects " +
                "  WHERE Id > 0 " +
                "  AND DeleteMarker = 0 ";

            if (!String.IsNullOrEmpty(prefix))
                query += "AND Key LIKE '" + Sanitize(prefix) + "%' ";

            query += 
                "  ORDER BY LastUpdateUtc DESC " +
                ") " +
                "GROUP BY Key LIMIT " + maxResults + " OFFSET " + indexStart;
                 
            return query;
        }

        internal string EnumerationVersions(string prefix, long indexStart, int maxResults)
        {
            string query =
                "SELECT * FROM " +
                "(" +
                "  SELECT * FROM Objects WHERE Id > 0 ";

            if (!String.IsNullOrEmpty(prefix))
                query += "AND Key LIKE '" + Sanitize(prefix) + "%' ";

            query +=
                "  ORDER BY LastUpdateUtc DESC " +
                ")" +
                "GROUP BY Key " +
                "LIMIT " + maxResults + " " +
                "OFFSET " + maxResults;

            return query;
        }

        #endregion

        #region Bucket-Tags-Queries

        internal string GetBucketTags()
        {
            string query = "SELECT * FROM BucketTags";
            return query;
        }
         
        internal string DeleteBucketTags()
        {
            string query = "DELETE FROM BucketTags";
            return query;
        }
         
        internal string InsertBucketTags(Dictionary<string, string> tags)
        {
            string query =
                "INSERT INTO BucketTags " +
                "( " +
                "  Key, " +
                "  Value " +
                ") " +
                "VALUES ";

            int added = 0;

            foreach (KeyValuePair<string, string> curr in tags)
            {
                if (String.IsNullOrEmpty(curr.Key)) continue;

                if (added > 0) query += ",";

                query +=
                    "( " +
                    "  '" + Sanitize(curr.Key) + "', " +
                    "  '" + Sanitize(curr.Value) + "'" +
                    ") ";

                added++;
            }

            return query;
        }
         
        #endregion

        #region Object-Tags-Queries
         
        internal string GetObjectTags(string key, long version)
        {
            string query =
                "SELECT * FROM ObjectTags WHERE " +
                "  ObjectKey = '" + Sanitize(key) + "' " +
                "  AND ObjectVersion = '" + version + "'";
            return query;
        }
         
        internal string DeleteObjectTags(string key, long version)
        {
            string query =
                "DELETE FROM ObjectTags WHERE " +
                "  ObjectKey = '" + Sanitize(key) + "' " +
                "  AND ObjectVersion = '" + version + "'";
            return query;
        }
         
        internal string InsertObjectTags(string key, long version, Dictionary<string, string> tags)
        {
            string query =
                "INSERT INTO ObjectTags " +
                "( " +
                "  ObjectKey, " +
                "  ObjectVersion, " +
                "  Key, " +
                "  Value " +
                ") " +
                "VALUES ";

            int added = 0;

            foreach (KeyValuePair<string, string> curr in tags)
            {
                if (String.IsNullOrEmpty(curr.Key)) continue;

                if (added > 0) query += ",";

                query +=
                    "( " +
                    "  '" + Sanitize(key) + "', " +
                    "  '" + version + "', " +
                    "  '" + Sanitize(curr.Key) + "', " +
                    "  '" + Sanitize(curr.Value) + "'" +
                    ") ";

                added++;
            }

            return query;
        }

        #endregion

        #region Bucket-Acl-Queries

        internal string GetBucketAcl()
        {
            string query = "SELECT * FROM BucketAcl";
            return query;
        }

        internal string GetBucketAclForUserByGuid(string userGuid)
        {
            string query =
                "SELECT * FROM BucketAcl " +
                "AND UserGUID = '" + Sanitize(userGuid) + "'";
            return query;
        }

        internal string GetBucketAclIssuedByUserGuid(string userGuid)
        {
            string query =
                "SELECT * FROM BucketAcl " +
                "AND IssuedByUserGUID = '" + Sanitize(userGuid) + "'";
            return query;
        }
        
        internal string DeleteBucketAcl()
        {
            string query =
                "DELETE FROM BucketAcl WHERE Id > 0";
            return query;
        }
         
        internal string InsertBucketAcl(BucketAcl acl)
        {
            string query =
               "INSERT INTO BucketAcl " +
               "(" +
               "  UserGroup, " +
               "  UserGUID, " +
               "  IssuedByUserGUID, " + 
               "  PermitRead, " +
               "  PermitWrite, " +
               "  PermitReadAcp, " +
               "  PermitWriteAcp, " +
               "  FullControl " +
               ") " +
               "VALUES " +
               "(";

            if (String.IsNullOrEmpty(acl.UserGroup)) query += "  null, ";
            else query += "  '" + Sanitize(acl.UserGroup) + "', ";

            query +=
               "  '" + Sanitize(acl.UserGUID) + "', " +
               "  '" + Sanitize(acl.IssuedByUserGUID) + "', " +
               "  '" + acl.PermitRead.ToString() + "', " +
               "  '" + acl.PermitWrite.ToString() + "', " +
               "  '" + acl.PermitReadAcp.ToString() + "', " +
               "  '" + acl.PermitWriteAcp.ToString() + "', " +
               "  '" + acl.FullControl.ToString() + "' " +
               ")";
            return query;
        }
        
        internal string BucketGroupAclExists(string groupName)
        {
            string query =
               "SELECT * FROM BucketAcl WHERE Id > 0 " +
               "AND UserGroup = '" + Sanitize(groupName) + "' ";
            return query;

        }

        internal string BucketUserAclExists(string userGuid)
        {
            string query =
                "SELECT * FROM BucketAcl WHERE Id > 0 " +
                "AND UserGUID = '" + Sanitize(userGuid) + "' ";
            return query;
        }
        
        internal string UpdateBucketGroupAcl(string groupName, string perm)
        {
            string query =
                "UPDATE BucketAcl SET " + Sanitize(perm) + " = 'True' " +
                "WHERE UserGroup = '" + Sanitize(groupName) + "' ";
            return query;
        }

        internal string UpdateBucketUserAcl(string userGuid, string perm)
        {
            string query =
                "UPDATE BucketAcl SET " + Sanitize(perm) + " = 'True' " +
                "WHERE UserGUID = '" + Sanitize(userGuid) + "' ";
            return query;
        }
        
        #endregion

        #region Object-Acl-Queries
        
        internal string GetObjectAcl(string key, long version)
        {
            string query =
                "SELECT * FROM ObjectAcl " +
                "WHERE ObjectKey = '" + Sanitize(key) + "' " +
                "AND ObjectVersion = " + version;
            return query;
        }

        internal string GetObjectAclForUserByGuid(string key, long version, string userGuid)
        {
            string query =
                "SELECT * FROM ObjectAcl " +
                "WHERE ObjectKey = '" + Sanitize(key) + "' " +
                "AND ObjectVersion = " + version + " " +
                "AND UserGUID = '" + Sanitize(userGuid) + "'";
            return query;
        }
        
        internal string DeleteObjectAcl(string key, long version)
        {
            string query =
                "DELETE FROM ObjectAcl WHERE ObjectKey = '" + Sanitize(key) + "' " +
                "AND ObjectVersion = " + version;
            return query;
        }

        internal string DeleteObjectAcl(string key)
        {
            string query =
                "DELETE FROM ObjectAcl WHERE ObjectKey = '" + Sanitize(key) + "'";
            return query;
        }
        
        internal string InsertObjectAcl(ObjectAcl acl)
        {
            string query =
                "INSERT INTO ObjectAcl " +
                "(" +
                "  UserGroup, " +
                "  UserGUID, " +
                "  IssuedByUserGUID, " +
                "  ObjectKey, " +
                "  ObjectVersion, " +
                "  PermitRead, " +
                "  PermitWrite, " +
                "  PermitReadAcp, " +
                "  PermitWriteAcp, " +
                "  FullControl " +
                ") " +
                "VALUES " +
                "(";

            if (String.IsNullOrEmpty(acl.UserGroup)) query += "  null, ";
            else query += "  '" + Sanitize(acl.UserGroup) + "', ";

            query +=
                "  '" + Sanitize(acl.UserGUID) + "', " +
                "  '" + Sanitize(acl.IssuedByUserGUID) + "', " +
                "  '" + Sanitize(acl.ObjectKey) + "', " +
                "  '" + acl.ObjectVersion + "', " +
                "  '" + acl.PermitRead.ToString() + "', " +
                "  '" + acl.PermitWrite.ToString() + "', " +
                "  '" + acl.PermitReadAcp.ToString() + "', " +
                "  '" + acl.PermitWriteAcp.ToString() + "', " +
                "  '" + acl.FullControl.ToString() + "' " +
                ")";

            return query;
        }
        
        internal string ObjectGroupAclExists(string groupName, string objectKey, long objectVersion)
        {
            string query =
                "SELECT * FROM ObjectAcl WHERE Id > 0 " +
                "AND UserGroup = '" + Sanitize(groupName) + "' " +
                "AND ObjectKey = '" + Sanitize(objectKey) + "' " +
                "AND ObjectVersion = " + objectVersion + " ";
            return query;
        }

        internal string ObjectUserAclExists(string userGuid, string objectKey, long objectVersion)
        {
            string query =
                "SELECT * FROM ObjectAcl WHERE Id > 0 " +
                "AND UserGUID = '" + Sanitize(userGuid) + "' " +
                "AND ObjectKey = '" + Sanitize(objectKey) + "' " +
                "AND ObjectVersion = " + objectVersion + " ";
            return query;
        }
        
        internal string UpdateObjectGroupAcl(string groupName, string objectKey, long versionId, string perm)
        {
            string query =
                "UPDATE ObjectAcl SET " + Sanitize(perm) + " = 'True' " +
                "WHERE UserGroup = '" + Sanitize(groupName) + "' " +
                "AND ObjectKey = '" + Sanitize(objectKey) + "' " +
                "AND ObjectVersion = " + versionId;
            return query;
        }

        internal string UpdateObjectUserAcl(string userGuid, string objectKey, long versionId, string perm)
        {
            string query =
                "UPDATE ObjectAcl SET " + Sanitize(perm) + " = 'True' " +
                "WHERE UserGUID = '" + Sanitize(userGuid) + "' " +
                "AND ObjectKey = '" + Sanitize(objectKey) + "' " +
                "AND ObjectVersion = " + versionId;
            return query;
        }

        #endregion

        #region General-Queries

        internal string UpdateRecord(string table, string key, string val, Dictionary<string, object> vals)
        {
            int added = 0;
            string query =
                "UPDATE " + Sanitize(table) + " SET ";

            foreach (KeyValuePair<string, object> curr in vals)
            {
                if (String.IsNullOrEmpty(curr.Key)) continue;

                if (added == 0)
                {
                    query += Sanitize(curr.Key) + " = ";
                    if (curr.Value == null) query += " null ";
                    else if (curr.Value is DateTime) query += "'" + TimestampUtc(Convert.ToDateTime(curr.Value)) + "' ";
                    else if (curr.Value is string) query += "'" + Sanitize(curr.Value.ToString()) + "' ";
                    else query += "'" + curr.Value.ToString() + "' ";
                }
                else
                {
                    query += "," + Sanitize(curr.Key) + " = ";
                    if (curr.Value == null) query += " null ";
                    else if (curr.Value is DateTime) query += "'" + TimestampUtc(Convert.ToDateTime(curr.Value)) + "' ";
                    else if (curr.Value is string) query += "'" + Sanitize(curr.Value.ToString()) + "' ";
                    else query += "'" + curr.Value.ToString() + "' ";
                }
                added++;
            }

            query +=
                "WHERE " + Sanitize(key) + " = '" + Sanitize(val) + "'";

            return query;
        }

        #endregion

        #region Internal

        internal string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

        internal string Sanitize(string str)
        { 
            return _Database.SanitizeString(str);
        }

        internal string TimestampUtc()
        {
            return DateTime.Now.ToUniversalTime().ToString(TimestampFormat);
        }

        internal string TimestampUtc(DateTime? ts)
        {
            if (ts == null) return null;
            return Convert.ToDateTime(ts).ToUniversalTime().ToString(TimestampFormat);
        }

        internal string TimestampUtc(DateTime ts)
        {
            return ts.ToUniversalTime().ToString(TimestampFormat);
        }

        #endregion
    }
}
