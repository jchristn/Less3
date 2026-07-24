namespace Less3.Database.SqlServer.Queries
{
    using System;
    using Less3.Classes;

    internal static class BucketTagQueries
    {
        internal static string InsertQuery(BucketTag tag)
        {
            return "INSERT INTO buckettags (id, tenant_id, bucket_id, [key], value, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(tag.Id) + "', "
                + "'" + Sanitizer.SanitizeString(tag.TenantId) + "', "
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

        internal static string SelectByBucketId(string tenantId, string bucketId)
        {
            return "SELECT * FROM buckettags WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string SelectById(string tenantId, string id)
        {
            return "SELECT * FROM buckettags WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string ExistsById(string tenantId, string id)
        {
            return "SELECT COUNT(*) AS cnt FROM buckettags WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string UpdateQuery(BucketTag tag)
        {
            return "UPDATE buckettags SET "
                + "tenant_id = '" + Sanitizer.SanitizeString(tag.TenantId) + "', "
                + "bucket_id = '" + Sanitizer.SanitizeString(tag.BucketId) + "', "
                + "[key] = '" + Sanitizer.SanitizeString(tag.Key) + "', "
                + "value = '" + Sanitizer.SanitizeString(tag.Value) + "', "
                + "createdutc = '" + tag.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "' "
                + "WHERE tenant_id = '" + Sanitizer.SanitizeString(tag.TenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(tag.Id) + "';";
        }

        internal static string DeleteByBucketId(string bucketId)
        {
            return "DELETE FROM buckettags WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string DeleteById(string tenantId, string id)
        {
            return "DELETE FROM buckettags WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }
    }
}
