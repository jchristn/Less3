namespace Less3.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.Sqlite.Queries;

    internal class UserMethods : IUserMethods
    {
        private DatabaseDriverBase _Database;

        internal UserMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public List<User> GetAll()
        {
            DataTable result = _Database.ExecuteQuery(UserQueries.SelectAll()).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<User> GetAll(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            DataTable result = _Database.ExecuteQuery(UserQueries.SelectAll(tenantId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public bool ExistsById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(UserQueries.ExistsById(id)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(UserQueries.ExistsById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsByEmail(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            DataTable result = _Database.ExecuteQuery(UserQueries.ExistsByEmail(email)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsByEmail(string tenantId, string email)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            DataTable result = _Database.ExecuteQuery(UserQueries.ExistsByEmail(tenantId, email)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public User GetById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(UserQueries.SelectById(id)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public User GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(UserQueries.SelectById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public User GetByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Database.ExecuteQuery(UserQueries.SelectByName(name)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public User GetByName(string tenantId, string name)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Database.ExecuteQuery(UserQueries.SelectByName(tenantId, name)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public User GetByEmail(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            DataTable result = _Database.ExecuteQuery(UserQueries.SelectByEmail(email)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public User GetByEmail(string tenantId, string email)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            DataTable result = _Database.ExecuteQuery(UserQueries.SelectByEmail(tenantId, email)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public void Insert(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            _Database.ExecuteQuery(UserQueries.InsertQuery(user), true).Wait();
        }

        /// <inheritdoc />
        public void Update(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            _Database.ExecuteQuery(UserQueries.UpdateQuery(user), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(UserQueries.DeleteById(id), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(UserQueries.DeleteById(tenantId, id), true).Wait();
        }

        private User MapFromRow(DataRow row)
        {
            User user = new User();
            user.Id = row["id"] != null && row["id"] != DBNull.Value ? row["id"].ToString() : null;
            user.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
            user.Name = row["name"] != null && row["name"] != DBNull.Value ? row["name"].ToString() : null;
            user.Email = row["email"] != null && row["email"] != DBNull.Value ? row["email"].ToString() : null;
            user.PasswordHash = ControlPlaneDataMapper.StringValue(row, "passwordhash");
            user.IsAdmin = ControlPlaneDataMapper.BoolValue(row, "isadmin");
            user.IsTenantAdmin = ControlPlaneDataMapper.BoolValue(row, "istenantadmin");
            user.Active = ControlPlaneDataMapper.BoolValue(row, "active");
            user.CreatedUtc = DateTime.Parse(row["createdutc"].ToString());
            return user;
        }

        private List<User> MapList(DataTable table)
        {
            List<User> list = new List<User>();
            if (table != null && table.Rows.Count > 0)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapFromRow(row));
                }
            }
            return list;
        }
    }
}
