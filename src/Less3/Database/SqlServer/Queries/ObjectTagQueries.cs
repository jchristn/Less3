namespace Less3.Database.SqlServer.Queries
{
    using System;
    using Less3.Classes;

    internal static class ObjectTagQueries
    {
        internal static string InsertQuery(ObjectTag tag)
        {
            return "INSERT INTO objecttags (guid, bucketguid, objectguid, [key], value, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(tag.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(tag.BucketGUID) + "', "
                + "'" + Sanitizer.SanitizeString(tag.ObjectGUID) + "', "
                + "'" + Sanitizer.SanitizeString(tag.Key) + "', "
                + "'" + Sanitizer.SanitizeString(tag.Value) + "', "
                + "'" + tag.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectByObjectGuid(string objectGuid, string bucketGuid)
        {
            return "SELECT * FROM objecttags WHERE objectguid = '" + Sanitizer.SanitizeString(objectGuid) + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }

        internal static string DeleteByObjectGuid(string objectGuid, string bucketGuid)
        {
            return "DELETE FROM objecttags WHERE objectguid = '" + Sanitizer.SanitizeString(objectGuid) + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }
    }
}
