namespace Less3.Database.MySql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.MySql.Queries;

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
        public List<Credential> GetAll(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            DataTable result = _Database.ExecuteQuery(CredentialQueries.SelectAll(tenantId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public bool ExistsById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(CredentialQueries.ExistsById(id)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(CredentialQueries.ExistsById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public Credential GetById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(CredentialQueries.SelectById(id)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public Credential GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(CredentialQueries.SelectById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public List<Credential> GetByUserId(string userId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            DataTable result = _Database.ExecuteQuery(CredentialQueries.SelectByUserId(userId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<Credential> GetByUserId(string tenantId, string userId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            DataTable result = _Database.ExecuteQuery(CredentialQueries.SelectByUserId(tenantId, userId)).Result;
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
        public void Insert(Credential credentials)
        {
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            _Database.ExecuteQuery(CredentialQueries.InsertQuery(credentials), true).Wait();
        }

        /// <inheritdoc />
        public void Update(Credential credentials)
        {
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            _Database.ExecuteQuery(CredentialQueries.UpdateQuery(credentials), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(CredentialQueries.DeleteById(id), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(CredentialQueries.DeleteById(tenantId, id), true).Wait();
        }

        private Credential MapFromRow(DataRow row)
        {
            Credential cred = new Credential();
            cred.Id = row["id"] != null && row["id"] != DBNull.Value ? row["id"].ToString() : null;
            cred.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
            cred.UserId = row["user_id"] != null && row["user_id"] != DBNull.Value ? row["user_id"].ToString() : null;
            cred.Description = row["description"] != null && row["description"] != DBNull.Value ? row["description"].ToString() : null;
            cred.AccessKey = row["accesskey"] != null && row["accesskey"] != DBNull.Value ? row["accesskey"].ToString() : null;
            cred.SecretKey = row["secretkey"] != null && row["secretkey"] != DBNull.Value ? row["secretkey"].ToString() : null;
            cred.IsBase64 = ControlPlaneDataMapper.BoolValue(row, "isbase64");
            cred.Active = ControlPlaneDataMapper.BoolValue(row, "active");
            cred.LastUsedUtc = ControlPlaneDataMapper.NullableDateValue(row, "lastusedutc");
            cred.LastFailedUtc = ControlPlaneDataMapper.NullableDateValue(row, "lastfailedutc");
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
