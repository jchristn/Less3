namespace Less3.Database.Sqlite.Queries
{
    using System;
    using Less3.Classes;

    internal static class ObjectAclQueries
    {
        internal static string InsertQuery(ObjectAcl acl)
        {
            return "INSERT INTO objectacls (id, usergroup, user_id, issued_by_user_id, bucket_id, object_id, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(acl.Id) + "', "
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

        internal static string SelectByBucketId(string bucketId)
        {
            return "SELECT * FROM objectacls WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string ExistsByGroupName(string groupName, string objectId, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM objectacls WHERE usergroup = '" + Sanitizer.SanitizeString(groupName) + "' AND object_id = '" + Sanitizer.SanitizeString(objectId) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string ExistsByUserId(string userId, string objectId, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM objectacls WHERE user_id = '" + Sanitizer.SanitizeString(userId) + "' AND object_id = '" + Sanitizer.SanitizeString(objectId) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string DeleteByObjectIdAndBucketId(string objectId, string bucketId)
        {
            return "DELETE FROM objectacls WHERE object_id = '" + Sanitizer.SanitizeString(objectId) + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }
    }
}
