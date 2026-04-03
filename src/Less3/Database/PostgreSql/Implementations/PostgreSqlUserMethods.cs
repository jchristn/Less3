namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlUserMethods : IUserMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlUserMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public List<User> GetAll()
        {
            DataTable result = _Driver.ExecuteQuery(UserQueries.SelectAll()).Result;
            return MapUsers(result);
        }

        public bool ExistsByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Driver.ExecuteQuery(UserQueries.ExistsByGuid(guid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public bool ExistsByEmail(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            DataTable result = _Driver.ExecuteQuery(UserQueries.ExistsByEmail(email)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public User GetByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Driver.ExecuteQuery(UserQueries.SelectByGuid(guid)).Result;
            List<User> users = MapUsers(result);
            if (users.Count > 0) return users[0];
            return null;
        }

        public User GetByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Driver.ExecuteQuery(UserQueries.SelectByName(name)).Result;
            List<User> users = MapUsers(result);
            if (users.Count > 0) return users[0];
            return null;
        }

        public User GetByEmail(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            DataTable result = _Driver.ExecuteQuery(UserQueries.SelectByEmail(email)).Result;
            List<User> users = MapUsers(result);
            if (users.Count > 0) return users[0];
            return null;
        }

        public void Insert(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            _Driver.ExecuteQuery(UserQueries.InsertQuery(user), true).Wait();
        }

        public void DeleteByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Driver.ExecuteQuery(UserQueries.DeleteByGuid(guid), true).Wait();
        }

        private List<User> MapUsers(DataTable dt)
        {
            List<User> users = new List<User>();
            if (dt == null || dt.Rows.Count == 0) return users;

            foreach (DataRow row in dt.Rows)
            {
                User user = new User();
                user.Id = Convert.ToInt32(row["id"]);
                user.GUID = row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
                user.Name = row["name"] != DBNull.Value ? row["name"].ToString() : null;
                user.Email = row["email"] != DBNull.Value ? row["email"].ToString() : null;
                user.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                users.Add(user);
            }

            return users;
        }
    }
}
