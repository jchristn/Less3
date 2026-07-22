namespace Less3.Database.MySql.Queries
{
    using System;
    using Less3.Classes;

    internal static class UploadQueries
    {
        internal static string InsertQuery(Upload upload)
        {
            return "INSERT INTO uploads (id, tenant_id, bucket_id, owner_id, author_id, `key`, createdutc, lastaccessutc, expirationutc, contenttype, metadata) VALUES ("
                + "'" + Sanitizer.SanitizeString(upload.Id) + "', "
                + "'" + Sanitizer.SanitizeString(upload.TenantId) + "', "
                + "'" + Sanitizer.SanitizeString(upload.BucketId) + "', "
                + "'" + Sanitizer.SanitizeString(upload.OwnerId) + "', "
                + "'" + Sanitizer.SanitizeString(upload.AuthorId) + "', "
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

        internal static string SelectAll(string tenantId)
        {
            return "SELECT * FROM uploads WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "';";
        }

        internal static string SelectById(string id)
        {
            return "SELECT * FROM uploads WHERE id = '" + Sanitizer.SanitizeString(id) + "' LIMIT 1;";
        }

        internal static string SelectById(string tenantId, string id)
        {
            return "SELECT * FROM uploads WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND id = '" + Sanitizer.SanitizeString(id) + "' LIMIT 1;";
        }

        internal static string SelectByBucketId(string bucketId)
        {
            return "SELECT * FROM uploads WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string SelectByBucketId(string tenantId, string bucketId)
        {
            return "SELECT * FROM uploads WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string DeleteById(string id)
        {
            return "DELETE FROM uploads WHERE id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string DeleteById(string tenantId, string id)
        {
            return "DELETE FROM uploads WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }
    }
}
