namespace Less3.Database.PostgreSql.Queries
{
    using System;
    using Less3.Classes;

    internal static class BucketQueries
    {
        internal static string InsertQuery(Bucket bucket)
        {
            return "INSERT INTO buckets (id, tenant_id, owner_id, name, regionstring, storagetype, diskdirectory, enableversioning, enablepublicwrite, enablepublicread, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(bucket.Id) + "', "
                + "'" + Sanitizer.SanitizeString(bucket.TenantId) + "', "
                + "'" + Sanitizer.SanitizeString(bucket.OwnerId) + "', "
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

        internal static string SelectAll(string tenantId)
        {
            return "SELECT * FROM buckets WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "';";
        }

        internal static string SelectById(string id)
        {
            return "SELECT * FROM buckets WHERE id = '" + Sanitizer.SanitizeString(id) + "' LIMIT 1;";
        }

        internal static string SelectById(string tenantId, string id)
        {
            return "SELECT * FROM buckets WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND id = '" + Sanitizer.SanitizeString(id) + "' LIMIT 1;";
        }

        internal static string SelectByName(string name)
        {
            return "SELECT * FROM buckets WHERE name = '" + Sanitizer.SanitizeString(name) + "' LIMIT 1;";
        }

        internal static string SelectByName(string tenantId, string name)
        {
            return "SELECT * FROM buckets WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND name = '" + Sanitizer.SanitizeString(name) + "' LIMIT 1;";
        }

        internal static string SelectByOwnerId(string ownerId)
        {
            return "SELECT * FROM buckets WHERE owner_id = '" + Sanitizer.SanitizeString(ownerId) + "';";
        }

        internal static string SelectByOwnerId(string tenantId, string ownerId)
        {
            return "SELECT * FROM buckets WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND owner_id = '" + Sanitizer.SanitizeString(ownerId) + "';";
        }

        internal static string ExistsByName(string name)
        {
            return "SELECT COUNT(*) AS cnt FROM buckets WHERE name = '" + Sanitizer.SanitizeString(name) + "';";
        }

        internal static string ExistsByName(string tenantId, string name)
        {
            return "SELECT COUNT(*) AS cnt FROM buckets WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND name = '" + Sanitizer.SanitizeString(name) + "';";
        }

        internal static string UpdateQuery(Bucket bucket)
        {
            return "UPDATE buckets SET "
                + "tenant_id = '" + Sanitizer.SanitizeString(bucket.TenantId) + "', "
                + "owner_id = '" + Sanitizer.SanitizeString(bucket.OwnerId) + "', "
                + "name = '" + Sanitizer.SanitizeString(bucket.Name) + "', "
                + "regionstring = '" + Sanitizer.SanitizeString(bucket.RegionString) + "', "
                + "storagetype = '" + Sanitizer.SanitizeString(bucket.StorageType.ToString()) + "', "
                + "diskdirectory = '" + Sanitizer.SanitizeString(bucket.DiskDirectory) + "', "
                + "enableversioning = " + (bucket.EnableVersioning ? "TRUE" : "FALSE") + ", "
                + "enablepublicwrite = " + (bucket.EnablePublicWrite ? "TRUE" : "FALSE") + ", "
                + "enablepublicread = " + (bucket.EnablePublicRead ? "TRUE" : "FALSE") + ", "
                + "createdutc = '" + bucket.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "' "
                + "WHERE tenant_id = '" + Sanitizer.SanitizeString(bucket.TenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(bucket.Id) + "';";
        }

        internal static string DeleteById(string id)
        {
            return "DELETE FROM buckets WHERE id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string DeleteById(string tenantId, string id)
        {
            return "DELETE FROM buckets WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }
    }
}
