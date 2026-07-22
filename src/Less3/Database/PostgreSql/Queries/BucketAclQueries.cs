namespace Less3.Database.PostgreSql.Queries
{
    using System;
    using Less3.Classes;

    internal static class BucketAclQueries
    {
        internal static string InsertQuery(BucketAcl acl)
        {
            return "INSERT INTO bucketacls (id, usergroup, bucket_id, user_id, issued_by_user_id, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(acl.Id) + "', "
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

        internal static string ExistsByGroupName(string groupName, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM bucketacls WHERE usergroup = '" + Sanitizer.SanitizeString(groupName)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string ExistsByUserId(string userId, string bucketId)
        {
            return "SELECT COUNT(*) AS cnt FROM bucketacls WHERE user_id = '" + Sanitizer.SanitizeString(userId)
                + "' AND bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }

        internal static string DeleteByBucketId(string bucketId)
        {
            return "DELETE FROM bucketacls WHERE bucket_id = '" + Sanitizer.SanitizeString(bucketId) + "';";
        }
    }
}
