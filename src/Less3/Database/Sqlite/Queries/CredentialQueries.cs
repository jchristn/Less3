namespace Less3.Database.Sqlite.Queries
{
    using System;
    using Less3.Classes;

    internal static class CredentialQueries
    {
        internal static string InsertQuery(Credential cred)
        {
            return "INSERT INTO credentials (tenant_id, id, user_id, description, accesskey, secretkey, isbase64, active, lastusedutc, lastfailedutc, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(cred.TenantId) + "', "
                + "'" + Sanitizer.SanitizeString(cred.Id) + "', "
                + "'" + Sanitizer.SanitizeString(cred.UserId) + "', "
                + "'" + Sanitizer.SanitizeString(cred.Description) + "', "
                + "'" + Sanitizer.SanitizeString(cred.AccessKey) + "', "
                + "'" + Sanitizer.SanitizeString(cred.SecretKey) + "', "
                + (cred.IsBase64 ? 1 : 0) + ", "
                + (cred.Active ? 1 : 0) + ", "
                + (cred.LastUsedUtc.HasValue ? "'" + cred.LastUsedUtc.Value.ToString(Sanitizer.TimestampFormat) + "'" : "NULL") + ", "
                + (cred.LastFailedUtc.HasValue ? "'" + cred.LastFailedUtc.Value.ToString(Sanitizer.TimestampFormat) + "'" : "NULL") + ", "
                + "'" + cred.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectAll()
        {
            return "SELECT * FROM credentials;";
        }

        internal static string SelectAll(string tenantId)
        {
            return "SELECT * FROM credentials WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "';";
        }

        internal static string SelectById(string id)
        {
            return "SELECT * FROM credentials WHERE id = '" + Sanitizer.SanitizeString(id) + "' LIMIT 1;";
        }

        internal static string SelectById(string tenantId, string id)
        {
            return "SELECT * FROM credentials WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND id = '" + Sanitizer.SanitizeString(id) + "' LIMIT 1;";
        }

        internal static string SelectByUserId(string userId)
        {
            return "SELECT * FROM credentials WHERE user_id = '" + Sanitizer.SanitizeString(userId) + "';";
        }

        internal static string SelectByUserId(string tenantId, string userId)
        {
            return "SELECT * FROM credentials WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND user_id = '" + Sanitizer.SanitizeString(userId) + "';";
        }

        internal static string SelectByAccessKey(string accessKey)
        {
            return "SELECT * FROM credentials WHERE accesskey = '" + Sanitizer.SanitizeString(accessKey) + "' LIMIT 1;";
        }

        internal static string ExistsById(string id)
        {
            return "SELECT COUNT(*) AS cnt FROM credentials WHERE id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string ExistsById(string tenantId, string id)
        {
            return "SELECT COUNT(*) AS cnt FROM credentials WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string UpdateQuery(Credential cred)
        {
            return "UPDATE credentials SET "
                + "tenant_id = '" + Sanitizer.SanitizeString(cred.TenantId) + "', "
                + "user_id = '" + Sanitizer.SanitizeString(cred.UserId) + "', "
                + "description = '" + Sanitizer.SanitizeString(cred.Description) + "', "
                + "accesskey = '" + Sanitizer.SanitizeString(cred.AccessKey) + "', "
                + "secretkey = '" + Sanitizer.SanitizeString(cred.SecretKey) + "', "
                + "isbase64 = " + (cred.IsBase64 ? 1 : 0) + ", "
                + "active = " + (cred.Active ? 1 : 0) + ", "
                + "lastusedutc = " + (cred.LastUsedUtc.HasValue ? "'" + cred.LastUsedUtc.Value.ToString(Sanitizer.TimestampFormat) + "'" : "NULL") + ", "
                + "lastfailedutc = " + (cred.LastFailedUtc.HasValue ? "'" + cred.LastFailedUtc.Value.ToString(Sanitizer.TimestampFormat) + "'" : "NULL") + " "
                + "WHERE id = '" + Sanitizer.SanitizeString(cred.Id) + "';";
        }

        internal static string DeleteById(string id)
        {
            return "DELETE FROM credentials WHERE id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string DeleteById(string tenantId, string id)
        {
            return "DELETE FROM credentials WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }
    }
}
