namespace Less3.Database.MySql.Queries
{
    using System;
    using Less3.Classes;

    internal static class UserQueries
    {
        internal static string InsertQuery(User user)
        {
            return "INSERT INTO users (guid, name, email, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(user.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(user.Name) + "', "
                + "'" + Sanitizer.SanitizeString(user.Email) + "', "
                + "'" + user.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectAll()
        {
            return "SELECT * FROM users;";
        }

        internal static string SelectByGuid(string guid)
        {
            return "SELECT * FROM users WHERE guid = '" + Sanitizer.SanitizeString(guid) + "' LIMIT 1;";
        }

        internal static string SelectByName(string name)
        {
            return "SELECT * FROM users WHERE name = '" + Sanitizer.SanitizeString(name) + "' LIMIT 1;";
        }

        internal static string SelectByEmail(string email)
        {
            return "SELECT * FROM users WHERE email = '" + Sanitizer.SanitizeString(email) + "' LIMIT 1;";
        }

        internal static string ExistsByGuid(string guid)
        {
            return "SELECT COUNT(*) AS cnt FROM users WHERE guid = '" + Sanitizer.SanitizeString(guid) + "';";
        }

        internal static string ExistsByEmail(string email)
        {
            return "SELECT COUNT(*) AS cnt FROM users WHERE email = '" + Sanitizer.SanitizeString(email) + "';";
        }

        internal static string DeleteByGuid(string guid)
        {
            return "DELETE FROM users WHERE guid = '" + Sanitizer.SanitizeString(guid) + "';";
        }
    }
}
