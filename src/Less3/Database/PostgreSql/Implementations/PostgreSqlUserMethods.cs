namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
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

        public List<User> GetAll(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            DataTable result = _Driver.ExecuteQuery(UserQueries.SelectAll(tenantId)).Result;
            return MapUsers(result);
        }

        public bool ExistsById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(UserQueries.ExistsById(id)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public bool ExistsById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(UserQueries.ExistsById(tenantId, id)).Result;
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

        public bool ExistsByEmail(string tenantId, string email)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            DataTable result = _Driver.ExecuteQuery(UserQueries.ExistsByEmail(tenantId, email)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public User GetById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(UserQueries.SelectById(id)).Result;
            List<User> users = MapUsers(result);
            if (users.Count > 0) return users[0];
            return null;
        }

        public User GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(UserQueries.SelectById(tenantId, id)).Result;
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

        public User GetByName(string tenantId, string name)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Driver.ExecuteQuery(UserQueries.SelectByName(tenantId, name)).Result;
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

        public User GetByEmail(string tenantId, string email)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            DataTable result = _Driver.ExecuteQuery(UserQueries.SelectByEmail(tenantId, email)).Result;
            List<User> users = MapUsers(result);
            if (users.Count > 0) return users[0];
            return null;
        }

        public void Insert(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            _Driver.ExecuteQuery(UserQueries.InsertQuery(user), true).Wait();
        }

        public void Update(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            _Driver.ExecuteQuery(UserQueries.UpdateQuery(user), true).Wait();
        }

        public void DeleteById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(UserQueries.DeleteById(id), true).Wait();
        }

        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(UserQueries.DeleteById(tenantId, id), true).Wait();
        }

        private List<User> MapUsers(DataTable dt)
        {
            List<User> users = new List<User>();
            if (dt == null || dt.Rows.Count == 0) return users;

            foreach (DataRow row in dt.Rows)
            {
                User user = new User();
                user.Id = row["id"] != DBNull.Value ? row["id"].ToString() : null;
                user.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
                user.Name = row["name"] != DBNull.Value ? row["name"].ToString() : null;
                user.Email = row["email"] != DBNull.Value ? row["email"].ToString() : null;
                user.PasswordHash = ControlPlaneDataMapper.StringValue(row, "passwordhash");
                user.IsAdmin = ControlPlaneDataMapper.BoolValue(row, "isadmin");
                user.IsTenantAdmin = ControlPlaneDataMapper.BoolValue(row, "istenantadmin");
                user.Active = ControlPlaneDataMapper.BoolValue(row, "active");
                user.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                users.Add(user);
            }

            return users;
        }
    }
}
