namespace Less3.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
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
        public bool ExistsByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Database.ExecuteQuery(UserQueries.ExistsByGuid(guid)).Result;
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
        public User GetByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Database.ExecuteQuery(UserQueries.SelectByGuid(guid)).Result;
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
        public User GetByEmail(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            DataTable result = _Database.ExecuteQuery(UserQueries.SelectByEmail(email)).Result;
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
        public void DeleteByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Database.ExecuteQuery(UserQueries.DeleteByGuid(guid), true).Wait();
        }

        private User MapFromRow(DataRow row)
        {
            User user = new User();
            user.Id = Convert.ToInt32(row["id"]);
            user.GUID = row["guid"] != null && row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
            user.Name = row["name"] != null && row["name"] != DBNull.Value ? row["name"].ToString() : null;
            user.Email = row["email"] != null && row["email"] != DBNull.Value ? row["email"].ToString() : null;
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
