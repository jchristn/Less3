namespace Less3.Database.PostgreSql.Queries
{
    using System;
    using Less3.Classes;

    internal static class BucketQueries
    {
        internal static string InsertQuery(Bucket bucket)
        {
            return "INSERT INTO buckets (guid, ownerguid, name, regionstring, storagetype, diskdirectory, enableversioning, enablepublicwrite, enablepublicread, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(bucket.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(bucket.OwnerGUID) + "', "
                + "'" + Sanitizer.SanitizeString(bucket.Name) + "', "
                + "'" + Sanitizer.SanitizeString(bucket.RegionString) + "', "
                + "'" + Sanitizer.SanitizeString(bucket.StorageType.ToString()) + "', "
                + "'" + Sanitizer.SanitizeString(bucket.DiskDirectory) + "', "
                + (bucket.EnableVersioning ? "TRUE" : "FALSE") + ", "
                + (bucket.EnablePublicWrite ? "TRUE" : "FALSE") + ", "
                + (bucket.EnablePublicRead ? "TRUE" : "FALSE") + ", "
                + "'" + bucket.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectAll()
        {
            return "SELECT * FROM buckets;";
        }

        internal static string SelectByGuid(string guid)
        {
            return "SELECT * FROM buckets WHERE guid = '" + Sanitizer.SanitizeString(guid) + "' LIMIT 1;";
        }

        internal static string SelectByName(string name)
        {
            return "SELECT * FROM buckets WHERE name = '" + Sanitizer.SanitizeString(name) + "' LIMIT 1;";
        }

        internal static string SelectByOwnerGuid(string ownerGuid)
        {
            return "SELECT * FROM buckets WHERE ownerguid = '" + Sanitizer.SanitizeString(ownerGuid) + "';";
        }

        internal static string ExistsByName(string name)
        {
            return "SELECT COUNT(*) AS cnt FROM buckets WHERE name = '" + Sanitizer.SanitizeString(name) + "';";
        }

        internal static string DeleteByGuid(string guid)
        {
            return "DELETE FROM buckets WHERE guid = '" + Sanitizer.SanitizeString(guid) + "';";
        }
    }
}
