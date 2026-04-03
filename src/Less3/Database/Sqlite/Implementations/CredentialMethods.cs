namespace Less3.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.Sqlite.Queries;

    internal class CredentialMethods : ICredentialMethods
    {
        private DatabaseDriverBase _Database;

        internal CredentialMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public List<Credential> GetAll()
        {
            DataTable result = _Database.ExecuteQuery(CredentialQueries.SelectAll()).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Database.ExecuteQuery(CredentialQueries.ExistsByGuid(guid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public Credential GetByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Database.ExecuteQuery(CredentialQueries.SelectByGuid(guid)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public List<Credential> GetByUserGuid(string userGuid)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            DataTable result = _Database.ExecuteQuery(CredentialQueries.SelectByUserGuid(userGuid)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public Credential GetByAccessKey(string accessKey)
        {
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            DataTable result = _Database.ExecuteQuery(CredentialQueries.SelectByAccessKey(accessKey)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public void Insert(Credential credential)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));
            _Database.ExecuteQuery(CredentialQueries.InsertQuery(credential), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Database.ExecuteQuery(CredentialQueries.DeleteByGuid(guid), true).Wait();
        }

        private Credential MapFromRow(DataRow row)
        {
            Credential cred = new Credential();
            cred.Id = Convert.ToInt32(row["id"]);
            cred.GUID = row["guid"] != null && row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
            cred.UserGUID = row["userguid"] != null && row["userguid"] != DBNull.Value ? row["userguid"].ToString() : null;
            cred.Description = row["description"] != null && row["description"] != DBNull.Value ? row["description"].ToString() : null;
            cred.AccessKey = row["accesskey"] != null && row["accesskey"] != DBNull.Value ? row["accesskey"].ToString() : null;
            cred.SecretKey = row["secretkey"] != null && row["secretkey"] != DBNull.Value ? row["secretkey"].ToString() : null;
            cred.IsBase64 = Convert.ToInt32(row["isbase64"]) != 0;
            cred.CreatedUtc = DateTime.Parse(row["createdutc"].ToString());
            return cred;
        }

        private List<Credential> MapList(DataTable table)
        {
            List<Credential> list = new List<Credential>();
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
