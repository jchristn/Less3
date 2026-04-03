namespace Less3.Database.MySql.Queries
{
    using System;
    using Less3.Classes;

    internal static class ObjectAclQueries
    {
        internal static string InsertQuery(ObjectAcl acl)
        {
            return "INSERT INTO objectacls (guid, usergroup, userguid, issuedbyuserguid, bucketguid, objectguid, permitread, permitwrite, permitreadacp, permitwriteacp, fullcontrol, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(acl.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(acl.UserGroup) + "', "
                + "'" + Sanitizer.SanitizeString(acl.UserGUID) + "', "
                + "'" + Sanitizer.SanitizeString(acl.IssuedByUserGUID) + "', "
                + "'" + Sanitizer.SanitizeString(acl.BucketGUID) + "', "
                + "'" + Sanitizer.SanitizeString(acl.ObjectGUID) + "', "
                + (acl.PermitRead ? 1 : 0) + ", "
                + (acl.PermitWrite ? 1 : 0) + ", "
                + (acl.PermitReadAcp ? 1 : 0) + ", "
                + (acl.PermitWriteAcp ? 1 : 0) + ", "
                + (acl.FullControl ? 1 : 0) + ", "
                + "'" + acl.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectByObjectGuid(string objectGuid, string bucketGuid)
        {
            return "SELECT * FROM objectacls WHERE objectguid = '" + Sanitizer.SanitizeString(objectGuid) + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }

        internal static string SelectByBucketGuid(string bucketGuid)
        {
            return "SELECT * FROM objectacls WHERE bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }

        internal static string ExistsByGroupName(string groupName, string objectGuid, string bucketGuid)
        {
            return "SELECT COUNT(*) AS cnt FROM objectacls WHERE usergroup = '" + Sanitizer.SanitizeString(groupName) + "' AND objectguid = '" + Sanitizer.SanitizeString(objectGuid) + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }

        internal static string ExistsByUserGuid(string userGuid, string objectGuid, string bucketGuid)
        {
            return "SELECT COUNT(*) AS cnt FROM objectacls WHERE userguid = '" + Sanitizer.SanitizeString(userGuid) + "' AND objectguid = '" + Sanitizer.SanitizeString(objectGuid) + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }

        internal static string DeleteByObjectGuidAndBucketGuid(string objectGuid, string bucketGuid)
        {
            return "DELETE FROM objectacls WHERE objectguid = '" + Sanitizer.SanitizeString(objectGuid) + "' AND bucketguid = '" + Sanitizer.SanitizeString(bucketGuid) + "';";
        }
    }
}
