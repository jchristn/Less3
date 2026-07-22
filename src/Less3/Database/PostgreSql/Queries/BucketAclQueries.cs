namespace Less3.Database.PostgreSql.Queries
{
    using System;
    using Less3.Classes;

    internal static class BucketAclQueries
    {
        internal static string InsertQuery(BucketAcl acl)
        {
            return "INSERT INTO bucketacls (id, tenant_id, usergroup, bucket_id, user_id, issued_by_user_id, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(acl.Id) + "', "
                + "'" + Sanitizer.SanitizeString(acl.TenantId) + "', "
                + "'" + Sanitizer.SanitizeString(acl.UserGroup) + "', "
                + "'" + Sanitizer.SanitizeString(acl.BucketId) + "', "
                + "'" + Sanitizer.SanitizeString(acl.UserId) + "', "
                + "'" + Sanitizer.SanitizeString(acl.IssuedByUserId) + "', "
                + (acl.PermitRead ? "TRUE" : "FALSE") + ", "
                + (acl.PermitWrite ? "TRUE" : "FALSE") + ", "
                + (acl.PermitReadAcp ? "TRUE" : "FALSE") + ", "
                + (acl.PermitWriteAcp ? "TRUE" : "FALSE") + ", "
                + (acl.FullControl ? "TRUE" : "FALSE") + ", "
                + "'" + acl.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectByBucketId(string bucketId)
        {
            return "SELECT * FROM bucketacls WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string SelectByBucketId(string tenantId, string bucketId)
        {
            return "SELECT * FROM bucketacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string SelectById(string tenantId, string id)
        {
            return "SELECT * FROM bucketacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string ExistsById(string tenantId, string id)
        {
            return "SELECT COUNT(*) AS cnt FROM bucketacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string ExistsByGroupName(string groupName, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM bucketacls WHERE usergroup = '" + Sanitizer.SanitizeString(groupName)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string ExistsByGroupName(string tenantId, string groupName, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM bucketacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND usergroup = '" + Sanitizer.SanitizeString(groupName)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string ExistsByUserId(string userId, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM bucketacls WHERE user_id = '" + Sanitizer.SanitizeString(userId)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string ExistsByUserId(string tenantId, string userId, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM bucketacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND user_id = '" + Sanitizer.SanitizeString(userId)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string UpdateQuery(BucketAcl acl)
        {
            return "UPDATE bucketacls SET "
                + "tenant_id = '" + Sanitizer.SanitizeString(acl.TenantId) + "', "
                + "usergroup = '" + Sanitizer.SanitizeString(acl.UserGroup) + "', "
                + "bucket_id = '" + Sanitizer.SanitizeString(acl.BucketId) + "', "
                + "user_id = '" + Sanitizer.SanitizeString(acl.UserId) + "', "
                + "issued_by_user_id = '" + Sanitizer.SanitizeString(acl.IssuedByUserId) + "', "
                + "permitread = " + (acl.PermitRead ? "TRUE" : "FALSE") + ", "
                + "permitwrite = " + (acl.PermitWrite ? "TRUE" : "FALSE") + ", "
                + "permitreadacp = " + (acl.PermitReadAcp ? "TRUE" : "FALSE") + ", "
                + "permitwriteacp = " + (acl.PermitWriteAcp ? "TRUE" : "FALSE") + ", "
                + "fullcontrol = " + (acl.FullControl ? "TRUE" : "FALSE") + ", "
                + "createdutc = '" + acl.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "' "
                + "WHERE tenant_id = '" + Sanitizer.SanitizeString(acl.TenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(acl.Id) + "';";
        }

        internal static string DeleteByBucketId(string bucketId)
        {
            return "DELETE FROM bucketacls WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string DeleteById(string tenantId, string id)
        {
            return "DELETE FROM bucketacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }
    }
}
