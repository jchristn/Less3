namespace Less3.Database.PostgreSql.Queries
{
    using System;
    using Less3.Classes;

    internal static class UploadPartQueries
    {
        internal static string InsertQuery(UploadPart part)
        {
            return "INSERT INTO uploadparts (id, tenant_id, bucket_id, owner_id, upload_id, partnumber, partlength, md5hash, sha1hash, sha256hash, lastaccessutc, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(part.Id) + "', "
                + "'" + Sanitizer.SanitizeString(part.TenantId) + "', "
                + "'" + Sanitizer.SanitizeString(part.BucketId) + "', "
                + "'" + Sanitizer.SanitizeString(part.OwnerId) + "', "
                + "'" + Sanitizer.SanitizeString(part.UploadId) + "', "
                + part.PartNumber + ", "
                + part.PartLength + ", "
                + "'" + Sanitizer.SanitizeString(part.MD5Hash) + "', "
                + "'" + Sanitizer.SanitizeString(part.Sha1Hash) + "', "
                + "'" + Sanitizer.SanitizeString(part.Sha256Hash) + "', "
                + "'" + part.LastAccessUtc.ToString(Sanitizer.TimestampFormat) + "', "
                + "'" + part.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectByUploadId(string uploadId)
        {
            return "SELECT * FROM uploadparts WHERE upload_id = '" + Sanitizer.SanitizeString(uploadId) + "';";
        }

        internal static string SelectByUploadId(string tenantId, string uploadId)
        {
            return "SELECT * FROM uploadparts WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND upload_id = '" + Sanitizer.SanitizeString(uploadId) + "';";
        }

        internal static string DeleteByUploadId(string uploadId)
        {
            return "DELETE FROM uploadparts WHERE upload_id = '" + Sanitizer.SanitizeString(uploadId) + "';";
        }

        internal static string DeleteByUploadId(string tenantId, string uploadId)
        {
            return "DELETE FROM uploadparts WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND upload_id = '" + Sanitizer.SanitizeString(uploadId) + "';";
        }

        internal static string DeleteByUploadIdAndPartNumber(string uploadId, int partNumber)
        {
            return "DELETE FROM uploadparts WHERE upload_id = '" + Sanitizer.SanitizeString(uploadId) + "' AND partnumber = " + partNumber + ";";
        }

        internal static string DeleteByUploadIdAndPartNumber(string tenantId, string uploadId, int partNumber)
        {
            return "DELETE FROM uploadparts WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND upload_id = '" + Sanitizer.SanitizeString(uploadId) + "' AND partnumber = " + partNumber + ";";
        }
    }
}
