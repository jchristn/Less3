namespace Less3.Database.Sqlite.Queries
{
    using System;
    using Less3.Classes;

    internal static class ObjectTagQueries
    {
        internal static string InsertQuery(ObjectTag tag)
        {
            return "INSERT INTO objecttags (id, bucket_id, object_id, key, value, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(tag.Id) + "', "
                + "'" + Sanitizer.SanitizeString(tag.BucketId) + "', "
                + "'" + Sanitizer.SanitizeString(tag.ObjectId) + "', "
                + "'" + Sanitizer.SanitizeString(tag.Key) + "', "
                + "'" + Sanitizer.SanitizeString(tag.Value) + "', "
                + "'" + tag.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectByObjectId(string objectId, string bucketId)
        {
            return "SELECT * FROM objecttags WHERE object_id = '" + Sanitizer.SanitizeString(objectId) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string DeleteByObjectId(string objectId, string bucketId)
        {
            return "DELETE FROM objecttags WHERE object_id = '" + Sanitizer.SanitizeString(objectId) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }
    }
}
