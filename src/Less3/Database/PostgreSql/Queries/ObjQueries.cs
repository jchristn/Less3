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

            return "INSERT INTO objects (guid, bucketguid, ownerguid, authorguid, key, contenttype, contentlength, version, etag, retention, blobfilename, isfolder, deletemarker, md5, createdutc, lastupdateutc, lastaccessutc, metadata, expirationutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(obj.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(obj.BucketGUID) + "', "
                + "'" + Sanitizer.SanitizeString(obj.OwnerGUID) + "', "
                + "'" + Sanitizer.SanitizeString(obj.AuthorGUID) + "', "
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

        internal static string SelectLatestByKey(string key, string bucketGuid)
        {
            return "SELECT * FROM objects WHERE key = '" + Sanitizer.SanitizeString(key)
                + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid)
                + "' ORDER BY version DESC LIMIT 1;";
        }

        internal static string SelectByKeyAndVersion(string key, long version, string bucketGuid)
        {
            return "SELECT * FROM objects WHERE key = '" + Sanitizer.SanitizeString(key)
                + "' AND version = " + version
                + " AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid)
                + "' LIMIT 1;";
        }

        internal static string SelectByGuid(string guid, string bucketGuid)
        {
            return "SELECT * FROM objects WHERE guid = '" + Sanitizer.SanitizeString(guid)
                + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid)
                + "' LIMIT 1;";
        }

        internal static string SelectLatestVersion(string key, string bucketGuid)
        {
            return "SELECT COALESCE(MAX(version), 0) AS maxversion FROM objects WHERE key = '" + Sanitizer.SanitizeString(key)
                + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }

        internal static string UpdateQuery(Obj obj)
        {
            string expirationVal = obj.ExpirationUtc.HasValue
                ? "'" + obj.ExpirationUtc.Value.ToString(Sanitizer.TimestampFormat) + "'"
                : "NULL";

            return "UPDATE objects SET "
                + "ownerguid = '" + Sanitizer.SanitizeString(obj.OwnerGUID) + "', "
                + "authorguid = '" + Sanitizer.SanitizeString(obj.AuthorGUID) + "', "
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
                + "WHERE id = " + obj.Id + ";";
        }

        internal static string DeleteQuery(Obj obj)
        {
            return "DELETE FROM objects WHERE id = " + obj.Id + ";";
        }

        internal static string Enumerate(string bucketGuid, int startIndex, int maxResults, bool excludeDeleteMarkers, string prefix)
        {
            string query = "SELECT * FROM objects WHERE bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "'"
                + " AND id >= " + startIndex;

            if (excludeDeleteMarkers)
                query += " AND deletemarker = FALSE";

            if (!String.IsNullOrEmpty(prefix))
                query += " AND key LIKE '" + Sanitizer.SanitizeString(prefix) + "%'";

            query += " ORDER BY id ASC LIMIT " + maxResults + ";";
            return query;
        }

        internal static string GetStatistics(string bucketGuid)
        {
            return "SELECT COUNT(*) AS objectcount, COALESCE(SUM(contentlength), 0) AS totalbytes FROM objects WHERE bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }
    }
}
