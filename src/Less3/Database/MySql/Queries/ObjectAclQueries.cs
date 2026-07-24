namespace Less3.Database.MySql.Queries
{
    using System;
    using Less3.Classes;

    internal static class ObjectAclQueries
    {
        internal static string InsertQuery(ObjectAcl acl)
        {
            return "INSERT INTO objectacls (id, tenant_id, usergroup, user_id, issued_by_user_id, bucket_id, object_id, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(acl.Id) + "', "
                + "'" + Sanitizer.SanitizeString(acl.TenantId) + "', "
                + "'" + Sanitizer.SanitizeString(acl.UserGroup) + "', "
                + "'" + Sanitizer.SanitizeString(acl.UserId) + "', "
                + "'" + Sanitizer.SanitizeString(acl.IssuedByUserId) + "', "
                + "'" + Sanitizer.SanitizeString(acl.BucketId) + "', "
                + "'" + Sanitizer.SanitizeString(acl.ObjectId) + "', "
                + (acl.PermitRead ? 1 : 0) + ", "
                + (acl.PermitWrite ? 1 : 0) + ", "
                + (acl.PermitReadAcp ? 1 : 0) + ", "
                + (acl.PermitWriteAcp ? 1 : 0) + ", "
                + (acl.FullControl ? 1 : 0) + ", "
                + "'" + acl.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectByObjectId(string objectId, string bucketId)
        {
            return "SELECT * FROM objectacls WHERE object_id = '" + Sanitizer.SanitizeString(objectId) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string SelectByObjectId(string tenantId, string objectId, string bucketId)
        {
            return "SELECT * FROM objectacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND object_id = '" + Sanitizer.SanitizeString(objectId)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string SelectByBucketId(string bucketId)
        {
            return "SELECT * FROM objectacls WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string SelectByBucketId(string tenantId, string bucketId)
        {
            return "SELECT * FROM objectacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string SelectById(string tenantId, string id)
        {
            return "SELECT * FROM objectacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string ExistsById(string tenantId, string id)
        {
            return "SELECT COUNT(*) AS cnt FROM objectacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string ExistsByGroupName(string groupName, string objectId, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM objectacls WHERE usergroup = '" + Sanitizer.SanitizeString(groupName) + "' AND object_id = '" + Sanitizer.SanitizeString(objectId) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string ExistsByGroupName(string tenantId, string groupName, string objectId, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM objectacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND usergroup = '" + Sanitizer.SanitizeString(groupName)
                + "' AND object_id = '" + Sanitizer.SanitizeString(objectId)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string ExistsByUserId(string userId, string objectId, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM objectacls WHERE user_id = '" + Sanitizer.SanitizeString(userId) + "' AND object_id = '" + Sanitizer.SanitizeString(objectId) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string ExistsByUserId(string tenantId, string userId, string objectId, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM objectacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND user_id = '" + Sanitizer.SanitizeString(userId)
                + "' AND object_id = '" + Sanitizer.SanitizeString(objectId)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string UpdateQuery(ObjectAcl acl)
        {
            return "UPDATE objectacls SET "
                + "tenant_id = '" + Sanitizer.SanitizeString(acl.TenantId) + "', "
                + "usergroup = '" + Sanitizer.SanitizeString(acl.UserGroup) + "', "
                + "user_id = '" + Sanitizer.SanitizeString(acl.UserId) + "', "
                + "issued_by_user_id = '" + Sanitizer.SanitizeString(acl.IssuedByUserId) + "', "
                + "bucket_id = '" + Sanitizer.SanitizeString(acl.BucketId) + "', "
                + "object_id = '" + Sanitizer.SanitizeString(acl.ObjectId) + "', "
                + "permitread = " + (acl.PermitRead ? 1 : 0) + ", "
                + "permitwrite = " + (acl.PermitWrite ? 1 : 0) + ", "
                + "permitreadacp = " + (acl.PermitReadAcp ? 1 : 0) + ", "
                + "permitwriteacp = " + (acl.PermitWriteAcp ? 1 : 0) + ", "
                + "fullcontrol = " + (acl.FullControl ? 1 : 0) + ", "
                + "createdutc = '" + acl.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "' "
                + "WHERE tenant_id = '" + Sanitizer.SanitizeString(acl.TenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(acl.Id) + "';";
        }

        internal static string DeleteByObjectIdAndBucketId(string objectId, string bucketId)
        {
            return "DELETE FROM objectacls WHERE object_id = '" + Sanitizer.SanitizeString(objectId) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string DeleteById(string tenantId, string id)
        {
            return "DELETE FROM objectacls WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId)
                + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }
    }
}
