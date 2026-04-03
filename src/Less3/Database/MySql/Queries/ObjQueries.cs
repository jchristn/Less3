namespace Less3.Database.MySql.Queries
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

            return "INSERT INTO objects (guid, bucketguid, ownerguid, authorguid, `key`, contenttype, contentlength, version, etag, retention, blobfilename, isfolder, deletemarker, md5, createdutc, lastupdateutc, lastaccessutc, metadata, expirationutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(obj.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(obj.BucketGUID) + "', "
                + "'" + Sanitizer.SanitizeString(obj.OwnerGUID) + "', "
                + "'" + Sanitizer.SanitizeString(obj.AuthorGUID) + "', "
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

        internal static string SelectLatestByKey(string key, string bucketGuid)
        {
            return "SELECT * FROM objects WHERE `key` = '" + Sanitizer.SanitizeString(key) + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "' ORDER BY version DESC LIMIT 1;";
        }

        internal static string SelectByKeyAndVersion(string key, long version, string bucketGuid)
        {
            return "SELECT * FROM objects WHERE `key` = '" + Sanitizer.SanitizeString(key) + "' AND version = " + version + " AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "' LIMIT 1;";
        }

        internal static string SelectByGuid(string guid, string bucketGuid)
        {
            return "SELECT * FROM objects WHERE guid = '" + Sanitizer.SanitizeString(guid) + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "' LIMIT 1;";
        }

        internal static string SelectLatestVersion(string key, string bucketGuid)
        {
            return "SELECT version FROM objects WHERE `key` = '" + Sanitizer.SanitizeString(key) + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "' ORDER BY version DESC LIMIT 1;";
        }

        internal static string UpdateQuery(Obj obj)
        {
            string expirationUtc = obj.ExpirationUtc != null
                ? "'" + obj.ExpirationUtc.Value.ToString(Sanitizer.TimestampFormat) + "'"
                : "NULL";

            return "UPDATE objects SET "
                + "guid = '" + Sanitizer.SanitizeString(obj.GUID) + "', "
                + "bucketguid = '" + Sanitizer.SanitizeString(obj.BucketGUID) + "', "
                + "ownerguid = '" + Sanitizer.SanitizeString(obj.OwnerGUID) + "', "
                + "authorguid = '" + Sanitizer.SanitizeString(obj.AuthorGUID) + "', "
                + "`key` = '" + Sanitizer.SanitizeString(obj.Key) + "', "
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
                + "WHERE id = " + obj.Id + ";";
        }

        internal static string DeleteById(int id)
        {
            return "DELETE FROM objects WHERE id = " + id + ";";
        }

        internal static string Enumerate(string bucketGuid, int startIndex, int maxResults, bool excludeDeleteMarkers, string prefix)
        {
            string query = "SELECT * FROM objects WHERE bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "' AND id >= " + startIndex;

            if (excludeDeleteMarkers)
            {
                query += " AND deletemarker = 0";
            }

            if (!String.IsNullOrEmpty(prefix))
            {
                query += " AND `key` LIKE '" + Sanitizer.SanitizeString(prefix) + "%'";
            }

            query += " ORDER BY id ASC LIMIT " + maxResults + ";";
            return query;
        }

        internal static string GetStatistics(string bucketGuid)
        {
            return "SELECT COUNT(*) AS numobjects, SUM(contentlength) AS totalbytes FROM objects WHERE bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }
    }
}
