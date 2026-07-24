namespace Less3.Database.Sqlite.Queries
{
    using System;
    using Less3.Classes;

    internal static class ObjectTagQueries
    {
        internal static string InsertQuery(ObjectTag tag)
        {
            return "INSERT INTO objecttags (id, tenant_id, bucket_id, object_id, key, value, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(tag.Id) + "', "
                + "'" + Sanitizer.SanitizeString(tag.TenantId) + "', "
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

        internal static string SelectByObjectId(string tenantId, string objectId, string bucketId)
        {
            return "SELECT * FROM objecttags WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND object_id = '" + Sanitizer.SanitizeString(objectId)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string SelectById(string tenantId, string id)
        {
            return "SELECT * FROM objecttags WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string ExistsById(string tenantId, string id)
        {
            return "SELECT COUNT(*) AS cnt FROM objecttags WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string UpdateQuery(ObjectTag tag)
        {
            return "UPDATE objecttags SET "
                + "tenant_id = '" + Sanitizer.SanitizeString(tag.TenantId) + "', "
                + "bucket_id = '" + Sanitizer.SanitizeString(tag.BucketId) + "', "
                + "object_id = '" + Sanitizer.SanitizeString(tag.ObjectId) + "', "
                + "key = '" + Sanitizer.SanitizeString(tag.Key) + "', "
                + "value = '" + Sanitizer.SanitizeString(tag.Value) + "', "
                + "createdutc = '" + tag.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "' "
                + "WHERE tenant_id = '" + Sanitizer.SanitizeString(tag.TenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(tag.Id) + "';";
        }

        internal static string DeleteByObjectId(string objectId, string bucketId)
        {
            return "DELETE FROM objecttags WHERE object_id = '" + Sanitizer.SanitizeString(objectId) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string DeleteById(string tenantId, string id)
        {
            return "DELETE FROM objecttags WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }
    }
}
