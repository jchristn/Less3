namespace Less3.Database.Sqlite.Queries
{
    using System;
    using Less3.Classes;

    internal static class BucketTagQueries
    {
        internal static string InsertQuery(BucketTag tag)
        {
            return "INSERT INTO buckettags (id, bucket_id, key, value, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(tag.Id) + "', "
                + "'" + Sanitizer.SanitizeString(tag.BucketId) + "', "
                + "'" + Sanitizer.SanitizeString(tag.Key) + "', "
                + "'" + Sanitizer.SanitizeString(tag.Value) + "', "
                + "'" + tag.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectByBucketId(string bucketId)
        {
            return "SELECT * FROM buckettags WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string DeleteByBucketId(string bucketId)
        {
            return "DELETE FROM buckettags WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }
    }
}
