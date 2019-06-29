using System;
using System.Collections.Generic;
using System.Text;

using SqliteWrapper;

namespace Less3.Classes
{
    public static class DatabaseQueries
    {
        public static string CreateObjectTable()
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

        public static string ObjectExists(string key)
        {
            string query =
                "SELECT * FROM Objects " +
                "WHERE Key = '" + Sanitize(key) + "' " +
                "ORDER BY LastUpdateUtc DESC " +
                "LIMIT 1";
            return query;
        }

        public static string VersionExists(string key, long version)
        {
            string query =
                "SELECT * FROM Objects " +
                "WHERE Key = '" + Sanitize(key) + "' " +
                "AND Version = '" + version + "' " +
                "ORDER BY LastUpdateUtc DESC " +
                "LIMIT 1";
            return query;
        }

        public static string InsertObject(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

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
         
        public static string DeleteObject(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string query =
                "DELETE FROM Objects WHERE Key = '" + Sanitize(key) + "' " +
                "AND Version = '" + version + "'";
            return query;
        }

        public static string MarkObjectDeleted(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

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
         
        public static string UpdateRecord(string key, long version, Dictionary<string, object> vals)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key)); 
            if (vals == null || vals.Count < 1) throw new ArgumentNullException(nameof(vals));

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

        public static string GetObjectCount()
        {
            return "SELECT COUNT(*) AS NumObjects, SUM(ContentLength) AS TotalBytes FROM Objects";
        }

        public static string Enumerate(string prefix, long indexStart, int maxResults)
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

        public static string EnumerationVersions(string prefix, long indexStart, int maxResults)
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

        private static string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

        private static string Sanitize(string str)
        {
            return DatabaseClient.SanitizeString(str);
        }

        private static string TimestampUtc()
        {
            return DateTime.Now.ToUniversalTime().ToString(TimestampFormat);
        }

        private static string TimestampUtc(DateTime? ts)
        {
            if (ts == null) return null;
            return Convert.ToDateTime(ts).ToUniversalTime().ToString(TimestampFormat);
        }

        private static string TimestampUtc(DateTime ts)
        {
            return ts.ToUniversalTime().ToString(TimestampFormat);
        }
    }
}
