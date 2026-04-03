namespace Less3.Database.SqlServer.Queries
{
    using System;
    using Less3.Classes;

    internal static class BucketTagQueries
    {
        internal static string InsertQuery(BucketTag tag)
        {
            return "INSERT INTO buckettags (guid, bucketguid, [key], value, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(tag.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(tag.BucketGUID) + "', "
                + "'" + Sanitizer.SanitizeString(tag.Key) + "', "
                + "'" + Sanitizer.SanitizeString(tag.Value) + "', "
                + "'" + tag.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectByBucketGuid(string bucketGuid)
        {
            return "SELECT * FROM buckettags WHERE bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }

        internal static string DeleteByBucketGuid(string bucketGuid)
        {
            return "DELETE FROM buckettags WHERE bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }
    }
}
