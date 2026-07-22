namespace Less3.Database.PostgreSql.Queries
{
    using System;
    using Less3.Classes;

    internal static class UserQueries
    {
        internal static string InsertQuery(User user)
        {
            return "INSERT INTO users (tenant_id, id, name, email, passwordhash, isadmin, istenantadmin, active, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(user.TenantId) + "', "
                + "'" + Sanitizer.SanitizeString(user.Id) + "', "
                + "'" + Sanitizer.SanitizeString(user.Name) + "', "
                + "'" + Sanitizer.SanitizeString(user.Email) + "', "
                + "'" + Sanitizer.SanitizeString(user.PasswordHash) + "', "
                + (user.IsAdmin ? 1 : 0) + ", "
                + (user.IsTenantAdmin ? 1 : 0) + ", "
                + (user.Active ? 1 : 0) + ", "
                + "'" + user.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectAll()
        {
            return "SELECT * FROM users;";
        }

        internal static string SelectAll(string tenantId)
        {
            return "SELECT * FROM users WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "';";
        }

        internal static string SelectById(string id)
        {
            return "SELECT * FROM users WHERE id = '" + Sanitizer.SanitizeString(id) + "' LIMIT 1;";
        }

        internal static string SelectById(string tenantId, string id)
        {
            return "SELECT * FROM users WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND id = '" + Sanitizer.SanitizeString(id) + "' LIMIT 1;";
        }

        internal static string SelectByName(string name)
        {
            return "SELECT * FROM users WHERE name = '" + Sanitizer.SanitizeString(name) + "' LIMIT 1;";
        }

        internal static string SelectByName(string tenantId, string name)
        {
            return "SELECT * FROM users WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND name = '" + Sanitizer.SanitizeString(name) + "' LIMIT 1;";
        }

        internal static string SelectByEmail(string email)
        {
            return "SELECT * FROM users WHERE email = '" + Sanitizer.SanitizeString(email) + "' LIMIT 1;";
        }

        internal static string SelectByEmail(string tenantId, string email)
        {
            return "SELECT * FROM users WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND email = '" + Sanitizer.SanitizeString(email) + "' LIMIT 1;";
        }

        internal static string ExistsById(string id)
        {
            return "SELECT COUNT(*) AS cnt FROM users WHERE id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string ExistsById(string tenantId, string id)
        {
            return "SELECT COUNT(*) AS cnt FROM users WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string ExistsByEmail(string email)
        {
            return "SELECT COUNT(*) AS cnt FROM users WHERE email = '" + Sanitizer.SanitizeString(email) + "';";
        }

        internal static string ExistsByEmail(string tenantId, string email)
        {
            return "SELECT COUNT(*) AS cnt FROM users WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND email = '" + Sanitizer.SanitizeString(email) + "';";
        }

        internal static string UpdateQuery(User user)
        {
            return "UPDATE users SET "
                + "tenant_id = '" + Sanitizer.SanitizeString(user.TenantId) + "', "
                + "name = '" + Sanitizer.SanitizeString(user.Name) + "', "
                + "email = '" + Sanitizer.SanitizeString(user.Email) + "', "
                + "passwordhash = '" + Sanitizer.SanitizeString(user.PasswordHash) + "', "
                + "isadmin = " + (user.IsAdmin ? 1 : 0) + ", "
                + "istenantadmin = " + (user.IsTenantAdmin ? 1 : 0) + ", "
                + "active = " + (user.Active ? 1 : 0) + " "
                + "WHERE id = '" + Sanitizer.SanitizeString(user.Id) + "';";
        }

        internal static string DeleteById(string id)
        {
            return "DELETE FROM users WHERE id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string DeleteById(string tenantId, string id)
        {
            return "DELETE FROM users WHERE tenant_id = '" + Sanitizer.SanitizeString(tenantId) + "' AND id = '" + Sanitizer.SanitizeString(id) + "';";
        }
    }
}
