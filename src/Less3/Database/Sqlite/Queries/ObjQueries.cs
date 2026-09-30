namespace Less3.Database.Sqlite.Queries
{
    using System;
    using Less3.Classes;

    internal static class ObjQueries
    {
        internal static string InsertQuery(Obj obj)
        {
            string expirationUtc = obj.ExpirationUtc != null
                ? "'" + obj.ExpirationUtc.Value.ToString(Sanitizer.TimestampFormat) + "'"
                : "NULL";

            return "INSERT INTO objects (id, tenant_id, bucket_id, owner_id, author_id, key, contenttype, contentlength, version, etag, retention, blobfilename, isfolder, deletemarker, md5, createdutc, lastupdateutc, lastaccessutc, metadata, expirationutc, nullversion) VALUES ("
                + "'" + Sanitizer.SanitizeString(obj.Id) + "', "
                + "'" + Sanitizer.SanitizeString(obj.TenantId) + "', "
                + "'" + Sanitizer.SanitizeString(obj.BucketId) + "', "
                + "'" + Sanitizer.SanitizeString(obj.OwnerId) + "', "
                + "'" + Sanitizer.SanitizeString(obj.AuthorId) + "', "
                + "'" + Sanitizer.SanitizeString(obj.Key) + "', "
                + "'" + Sanitizer.SanitizeString(obj.ContentType) + "', "
                + obj.ContentLength + ", "
                + obj.Version + ", "
                + "'" + Sanitizer.SanitizeString(obj.Etag) + "', "
                + "'" + obj.Retention.ToString() + "', "
                + "'" + Sanitizer.SanitizeString(obj.BlobFilename) + "', "
                + (obj.IsFolder ? 1 : 0) + ", "
                + (obj.DeleteMarker ? 1 : 0) + ", "
                + "'" + Sanitizer.SanitizeString(obj.Md5) + "', "
                + "'" + obj.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "'" + obj.LastUpdateUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "'" + obj.LastAccessUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "'" + Sanitizer.SanitizeString(obj.Metadata) + "', "
                + expirationUtc + ", "
                + (obj.NullVersion ? 1 : 0)
                + ");";
        }

        internal static string SelectLatestByKey(string key, string bucketId)
        {
            return "SELECT * FROM objects WHERE key = '" + Sanitizer.SanitizeString(key) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "' ORDER BY version DESC LIMIT 1;";
        }

        internal static string SelectByKeyAndVersion(string key, long version, string bucketId)
        {
            return "SELECT * FROM objects WHERE key = '" + Sanitizer.SanitizeString(key) + "' AND version = " + version + " AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "' LIMIT 1;";
        }

        internal static string SelectById(string id, string bucketId)
        {
            return "SELECT * FROM objects WHERE id = '" + Sanitizer.SanitizeString(id) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "' LIMIT 1;";
        }

        internal static string SelectLatestVersion(string key, string bucketId)
        {
            return "SELECT version FROM objects WHERE key = '" + Sanitizer.SanitizeString(key) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "' ORDER BY version DESC LIMIT 1;";
        }

        internal static string UpdateQuery(Obj obj)
        {
            string expirationUtc = obj.ExpirationUtc != null
                ? "'" + obj.ExpirationUtc.Value.ToString(Sanitizer.TimestampFormat) + "'"
                : "NULL";

            return "UPDATE objects SET "
                + "id = '" + Sanitizer.SanitizeString(obj.Id) + "', "
                + "tenant_id = '" + Sanitizer.SanitizeString(obj.TenantId) + "', "
                + "bucket_id = '" + Sanitizer.SanitizeString(obj.BucketId) + "', "
                + "owner_id = '" + Sanitizer.SanitizeString(obj.OwnerId) + "', "
                + "author_id = '" + Sanitizer.SanitizeString(obj.AuthorId) + "', "
                + "key = '" + Sanitizer.SanitizeString(obj.Key) + "', "
                + "contenttype = '" + Sanitizer.SanitizeString(obj.ContentType) + "', "
                + "contentlength = " + obj.ContentLength + ", "
                + "version = " + obj.Version + ", "
                + "etag = '" + Sanitizer.SanitizeString(obj.Etag) + "', "
                + "retention = '" + obj.Retention.ToString() + "', "
                + "blobfilename = '" + Sanitizer.SanitizeString(obj.BlobFilename) + "', "
                + "isfolder = " + (obj.IsFolder ? 1 : 0) + ", "
                + "deletemarker = " + (obj.DeleteMarker ? 1 : 0) + ", "
                + "md5 = '" + Sanitizer.SanitizeString(obj.Md5) + "', "
                + "createdutc = '" + obj.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "lastupdateutc = '" + obj.LastUpdateUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "lastaccessutc = '" + obj.LastAccessUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "metadata = '" + Sanitizer.SanitizeString(obj.Metadata) + "', "
                + "expirationutc = " + expirationUtc + ", "
                + "nullversion = " + (obj.NullVersion ? 1 : 0) + " "
                + "WHERE id = '" + Sanitizer.SanitizeString(obj.Id) + "';";
        }

        internal static string DeleteById(string id)
        {
            return "DELETE FROM objects WHERE id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string Enumerate(string bucketId, int startIndex, int maxResults, bool excludeDeleteMarkers, string prefix)
        {
            string query = "SELECT * FROM objects WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "'";

            if (excludeDeleteMarkers)
            {
                query += " AND deletemarker = 0";
            }

            if (!String.IsNullOrEmpty(prefix))
            {
                query += " AND key LIKE '" + Sanitizer.SanitizeString(prefix) + "%'";
            }

            query += " ORDER BY id ASC LIMIT " + maxResults + " OFFSET " + startIndex + ";";
            return query;
        }

        internal static string SelectNullVersion(string key, string bucketId)
        {
            return "SELECT * FROM objects WHERE key = '" + Sanitizer.SanitizeString(key) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "' AND nullversion = 1 ORDER BY version DESC LIMIT 1;";
        }

        internal static string EnumerateLatest(string bucketId, string prefix, string afterKey, int maxResults)
        {
            // SQLite compares TEXT with the BINARY collation by default, which orders UTF-8 keys the
            // same way Amazon S3 does.
            string query = "SELECT o.* FROM objects o WHERE o.bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "' "
                + "AND o.version = (SELECT MAX(i.version) FROM objects i WHERE i.bucket_id = o.bucket_id AND i.key = o.key) "
                + "AND o.deletemarker = 0"
                + KeyFilters(prefix, afterKey, null)
                + " ORDER BY o.key ASC LIMIT " + maxResults + ";";
            return query;
        }

        internal static string EnumerateVersions(string bucketId, string prefix, string afterKey, long? afterVersion, int maxResults)
        {
            string query = "SELECT o.* FROM objects o WHERE o.bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "'"
                + KeyFilters(prefix, afterKey, afterVersion)
                + " ORDER BY o.key ASC, o.version DESC LIMIT " + maxResults + ";";
            return query;
        }

        private static string KeyFilters(string prefix, string afterKey, long? afterVersion)
        {
            string filters = "";

            if (!String.IsNullOrEmpty(prefix))
            {
                // substr counts characters (code points); LIKE would treat % and _ as wildcards and
                // match ASCII case-insensitively.
                filters += " AND o.key >= '" + Sanitizer.SanitizeString(prefix) + "'"
                    + " AND substr(o.key, 1, " + CodePointLength(prefix) + ") = '" + Sanitizer.SanitizeString(prefix) + "'";
            }

            if (afterKey != null)
            {
                if (afterVersion != null)
                {
                    filters += " AND (o.key > '" + Sanitizer.SanitizeString(afterKey) + "'"
                        + " OR (o.key = '" + Sanitizer.SanitizeString(afterKey) + "' AND o.version < " + afterVersion.Value + "))";
                }
                else
                {
                    filters += " AND o.key > '" + Sanitizer.SanitizeString(afterKey) + "'";
                }
            }

            return filters;
        }

        private static int CodePointLength(string value)
        {
            int count = 0;
            foreach (char c in value)
            {
                if (!Char.IsLowSurrogate(c)) count++;
            }
            return count;
        }

        internal static string GetStatistics(string bucketId)
        {
            return "SELECT COUNT(*) AS numobjects, COALESCE(SUM(contentlength), 0) AS totalbytes "
                + "FROM objects o "
                + "WHERE o.bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "' "
                + "AND o.deletemarker = 0 "
                + "AND o.version = ("
                + "SELECT MAX(i.version) FROM objects i "
                + "WHERE i.bucket_id = o.bucket_id AND i.key = o.key AND i.deletemarker = 0"
                + ");";
        }
    }
}
