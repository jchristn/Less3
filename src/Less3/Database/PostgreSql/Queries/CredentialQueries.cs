namespace Less3.Database.PostgreSql.Queries
{
    using System;
    using Less3.Classes;

    internal static class CredentialQueries
    {
        internal static string InsertQuery(Credential cred)
        {
            return "INSERT INTO credential (guid, userguid, description, accesskey, secretkey, isbase64, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(cred.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(cred.UserGUID) + "', "
                + "'" + Sanitizer.SanitizeString(cred.Description) + "', "
                + "'" + Sanitizer.SanitizeString(cred.AccessKey) + "', "
                + "'" + Sanitizer.SanitizeString(cred.SecretKey) + "', "
                + (cred.IsBase64 ? "TRUE" : "FALSE") + ", "
                + "'" + cred.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectAll()
        {
            return "SELECT * FROM credential;";
        }

        internal static string SelectByGuid(string guid)
        {
            return "SELECT * FROM credential WHERE guid = '" + Sanitizer.SanitizeString(guid) + "' LIMIT 1;";
        }

        internal static string SelectByUserGuid(string userGuid)
        {
            return "SELECT * FROM credential WHERE userguid = '" + Sanitizer.SanitizeString(userGuid) + "';";
        }

        internal static string SelectByAccessKey(string accessKey)
        {
            return "SELECT * FROM credential WHERE accesskey = '" + Sanitizer.SanitizeString(accessKey) + "' LIMIT 1;";
        }

        internal static string ExistsByGuid(string guid)
        {
            return "SELECT COUNT(*) AS cnt FROM credential WHERE guid = '" + Sanitizer.SanitizeString(guid) + "';";
        }

        internal static string UpdateQuery(Credential cred)
        {
            return "UPDATE credential SET "
                + "userguid = '" + Sanitizer.SanitizeString(cred.UserGUID) + "', "
                + "description = '" + Sanitizer.SanitizeString(cred.Description) + "', "
                + "accesskey = '" + Sanitizer.SanitizeString(cred.AccessKey) + "', "
                + "secretkey = '" + Sanitizer.SanitizeString(cred.SecretKey) + "', "
                + "isbase64 = " + (cred.IsBase64 ? "TRUE" : "FALSE") + " "
                + "WHERE guid = '" + Sanitizer.SanitizeString(cred.GUID) + "';";
        }

        internal static string DeleteByGuid(string guid)
        {
            return "DELETE FROM credential WHERE guid = '" + Sanitizer.SanitizeString(guid) + "';";
        }
    }
}
