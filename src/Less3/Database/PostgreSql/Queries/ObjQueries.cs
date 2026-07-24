namespace Less3.Database.PostgreSql.Queries
{
    using System;
    using Less3.Classes;

    internal static class ObjQueries
    {
        internal static string InsertQuery(Obj obj)
        {
            string expirationVal = obj.ExpirationUtc.HasValue
                ? "'" + obj.ExpirationUtc.Value.ToString(Sanitizer.TimestampFormat) + "'"
                : "NULL";

            return "INSERT INTO objects (id, tenant_id, bucket_id, owner_id, author_id, key, contenttype, contentlength, version, etag, retention, blobfilename, isfolder, deletemarker, md5, createdutc, lastupdateutc, lastaccessutc, metadata, expirationutc) VALUES ("
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
                + "'" + Sanitizer.SanitizeString(obj.Retention.ToString()) + "', "
                + "'" + Sanitizer.SanitizeString(obj.BlobFilename) + "', "
                + (obj.IsFolder ? "TRUE" : "FALSE") + ", "
                + (obj.DeleteMarker ? "TRUE" : "FALSE") + ", "
                + "'" + Sanitizer.SanitizeString(obj.Md5) + "', "
                + "'" + obj.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "'" + obj.LastUpdateUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "'" + obj.LastAccessUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "'" + Sanitizer.SanitizeString(obj.Metadata) + "', "
                + expirationVal
                + ");";
        }

        internal static string SelectLatestByKey(string key, string bucketId)
        {
            return "SELECT * FROM objects WHERE key = '" + Sanitizer.SanitizeString(key)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId)
                + "' ORDER BY version DESC LIMIT 1;";
        }

        internal static string SelectByKeyAndVersion(string key, long version, string bucketId)
        {
            return "SELECT * FROM objects WHERE key = '" + Sanitizer.SanitizeString(key)
                + "' AND version = " + version
                + " AND bucket_id = '" + Sanitizer.SanitizeString(bucketId)
                + "' LIMIT 1;";
        }

        internal static string SelectById(string id, string bucketId)
        {
            return "SELECT * FROM objects WHERE id = '" + Sanitizer.SanitizeString(id)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId)
                + "' LIMIT 1;";
        }

        internal static string SelectLatestVersion(string key, string bucketId)
        {
            return "SELECT COALESCE(MAX(version), 0) AS maxversion FROM objects WHERE key = '" + Sanitizer.SanitizeString(key)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string UpdateQuery(Obj obj)
        {
            string expirationVal = obj.ExpirationUtc.HasValue
                ? "'" + obj.ExpirationUtc.Value.ToString(Sanitizer.TimestampFormat) + "'"
                : "NULL";

            return "UPDATE objects SET "
                + "tenant_id = '" + Sanitizer.SanitizeString(obj.TenantId) + "', "
                + "bucket_id = '" + Sanitizer.SanitizeString(obj.BucketId) + "', "
                + "owner_id = '" + Sanitizer.SanitizeString(obj.OwnerId) + "', "
                + "author_id = '" + Sanitizer.SanitizeString(obj.AuthorId) + "', "
                + "key = '" + Sanitizer.SanitizeString(obj.Key) + "', "
                + "contenttype = '" + Sanitizer.SanitizeString(obj.ContentType) + "', "
                + "contentlength = " + obj.ContentLength + ", "
                + "version = " + obj.Version + ", "
                + "etag = '" + Sanitizer.SanitizeString(obj.Etag) + "', "
                + "retention = '" + Sanitizer.SanitizeString(obj.Retention.ToString()) + "', "
                + "blobfilename = '" + Sanitizer.SanitizeString(obj.BlobFilename) + "', "
                + "isfolder = " + (obj.IsFolder ? "TRUE" : "FALSE") + ", "
                + "deletemarker = " + (obj.DeleteMarker ? "TRUE" : "FALSE") + ", "
                + "md5 = '" + Sanitizer.SanitizeString(obj.Md5) + "', "
                + "lastupdateutc = '" + obj.LastUpdateUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "lastaccessutc = '" + obj.LastAccessUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "metadata = '" + Sanitizer.SanitizeString(obj.Metadata) + "', "
                + "expirationutc = " + expirationVal + " "
                + "WHERE id = '" + Sanitizer.SanitizeString(obj.Id) + "';";
        }

        internal static string DeleteQuery(Obj obj)
        {
            return "DELETE FROM objects WHERE id = '" + Sanitizer.SanitizeString(obj.Id) + "';";
        }

        internal static string Enumerate(string bucketId, int startIndex, int maxResults, bool excludeDeleteMarkers, string prefix)
        {
            string query = "SELECT * FROM objects WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "'"
                + "";

            if (excludeDeleteMarkers)
                query += " AND deletemarker = FALSE";

            if (!String.IsNullOrEmpty(prefix))
                query += " AND key LIKE '" + Sanitizer.SanitizeString(prefix) + "%'";

            query += " ORDER BY id ASC LIMIT " + maxResults + " OFFSET " + startIndex + ";";
            return query;
        }

        internal static string GetStatistics(string bucketId)
        {
            return "SELECT COUNT(*) AS numobjects, COALESCE(SUM(contentlength), 0) AS totalbytes "
                + "FROM objects o "
                + "WHERE o.bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "' "
                + "AND o.deletemarker = FALSE "
                + "AND o.version = ("
                + "SELECT MAX(i.version) FROM objects i "
                + "WHERE i.bucket_id = o.bucket_id AND i.key = o.key AND i.deletemarker = FALSE"
                + ");";
        }
    }
}
