namespace Less3.Database.Sqlite.Queries
{
    using System;
    using Less3.Classes;

    internal static class UploadQueries
    {
        internal static string InsertQuery(Upload upload)
        {
            return "INSERT INTO uploads (guid, bucketguid, ownerguid, authorguid, key, createdutc, lastaccessutc, expirationutc, contenttype, metadata) VALUES ("
                + "'" + Sanitizer.SanitizeString(upload.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(upload.BucketGUID) + "', "
                + "'" + Sanitizer.SanitizeString(upload.OwnerGUID) + "', "
                + "'" + Sanitizer.SanitizeString(upload.AuthorGUID) + "', "
                + "'" + Sanitizer.SanitizeString(upload.Key) + "', "
                + "'" + upload.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "'" + upload.LastAccessUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "'" + upload.ExpirationUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "'" + Sanitizer.SanitizeString(upload.ContentType) + "', "
                + "'" + Sanitizer.SanitizeString(upload.Metadata) + "'"
                + ");";
        }

        internal static string SelectAll()
        {
            return "SELECT * FROM uploads;";
        }

        internal static string SelectByGuid(string guid)
        {
            return "SELECT * FROM uploads WHERE guid = '" + Sanitizer.SanitizeString(guid) + "' LIMIT 1;";
        }

        internal static string SelectByBucketGuid(string bucketGuid)
        {
            return "SELECT * FROM uploads WHERE bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }

        internal static string DeleteByGuid(string guid)
        {
            return "DELETE FROM uploads WHERE guid = '" + Sanitizer.SanitizeString(guid) + "';";
        }
    }
}
