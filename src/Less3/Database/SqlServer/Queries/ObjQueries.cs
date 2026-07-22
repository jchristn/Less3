namespace Less3.Database.SqlServer.Queries
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

            return "INSERT INTO objects (id, tenant_id, bucket_id, owner_id, author_id, [key], contenttype, contentlength, version, etag, retention, blobfilename, isfolder, deletemarker, md5, createdutc, lastupdateutc, lastaccessutc, metadata, expirationutc) VALUES ("
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
                + expirationUtc
                + ");";
        }

        internal static string SelectLatestByKey(string key, string bucketId)
        {
            return "SELECT TOP 1 * FROM objects WHERE [key] = '" + Sanitizer.SanitizeString(key) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "' ORDER BY version DESC;";
        }

        internal static string SelectByKeyAndVersion(string key, long version, string bucketId)
        {
            return "SELECT TOP 1 * FROM objects WHERE [key] = '" + Sanitizer.SanitizeString(key) + "' AND version = " + version + " AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string SelectById(string id, string bucketId)
        {
            return "SELECT TOP 1 * FROM objects WHERE id = '" + Sanitizer.SanitizeString(id) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string SelectLatestVersion(string key, string bucketId)
        {
            return "SELECT TOP 1 version FROM objects WHERE [key] = '" + Sanitizer.SanitizeString(key) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "' ORDER BY version DESC;";
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
                + "[key] = '" + Sanitizer.SanitizeString(obj.Key) + "', "
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
                + "expirationutc = " + expirationUtc + " "
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
                query += " AND [key] LIKE '" + Sanitizer.SanitizeString(prefix) + "%'";
            }

            query += " ORDER BY id ASC OFFSET " + startIndex + " ROWS FETCH NEXT " + maxResults + " ROWS ONLY;";
            return query;
        }

        internal static string GetStatistics(string bucketId)
        {
            return "SELECT COUNT(*) AS numobjects, SUM(contentlength) AS totalbytes FROM objects WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }
    }
}
