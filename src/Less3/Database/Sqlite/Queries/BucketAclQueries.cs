namespace Less3.Database.Sqlite.Queries
{
    using System;
    using Less3.Classes;

    internal static class BucketAclQueries
    {
        internal static string InsertQuery(BucketAcl acl)
        {
            return "INSERT INTO bucketacls (guid, usergroup, bucketguid, userguid, issuedbyuserguid, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(acl.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(acl.UserGroup) + "', "
                + "'" + Sanitizer.SanitizeString(acl.BucketGUID) + "', "
                + "'" + Sanitizer.SanitizeString(acl.UserGUID) + "', "
                + "'" + Sanitizer.SanitizeString(acl.IssuedByUserGUID) + "', "
                + (acl.PermitRead ? 1 : 0) + ", "
                + (acl.PermitWrite ? 1 : 0) + ", "
                + (acl.PermitReadAcp ? 1 : 0) + ", "
                + (acl.PermitWriteAcp ? 1 : 0) + ", "
                + (acl.FullControl ? 1 : 0) + ", "
                + "'" + acl.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectByBucketGuid(string bucketGuid)
        {
            return "SELECT * FROM bucketacls WHERE bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }

        internal static string ExistsByGroupName(string groupName, string bucketGuid)
        {
            return "SELECT COUNT(*) AS cnt FROM bucketacls WHERE usergroup = '" + Sanitizer.SanitizeString(groupName) + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }

        internal static string ExistsByUserGuid(string userGuid, string bucketGuid)
        {
            return "SELECT COUNT(*) AS cnt FROM bucketacls WHERE userguid = '" + Sanitizer.SanitizeString(userGuid) + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }

        internal static string DeleteByBucketGuid(string bucketGuid)
        {
            return "DELETE FROM bucketacls WHERE bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }
    }
}
