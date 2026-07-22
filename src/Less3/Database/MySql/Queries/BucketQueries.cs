namespace Less3.Database.MySql.Queries
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
                + "'" + bucket.StorageType.ToString() + "', "
                + "'" + Sanitizer.SanitizeString(bucket.DiskDirectory) + "', "
                + (bucket.EnableVersioning ? 1 : 0) + ", "
                + (bucket.EnablePublicWrite ? 1 : 0) + ", "
                + (bucket.EnablePublicRead ? 1 : 0) + ", "
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
