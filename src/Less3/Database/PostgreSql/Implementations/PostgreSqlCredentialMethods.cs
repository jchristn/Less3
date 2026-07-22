namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlCredentialMethods : ICredentialMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlCredentialMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public List<Credential> GetAll()
        {
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.SelectAll()).Result;
            return MapCredentials(result);
        }

        public List<Credential> GetAll(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.SelectAll(tenantId)).Result;
            return MapCredentials(result);
        }

        public bool ExistsById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.ExistsById(id)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public bool ExistsById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.ExistsById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public Credential GetById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.SelectById(id)).Result;
            List<Credential> creds = MapCredentials(result);
            if (creds.Count > 0) return creds[0];
            return null;
        }

        public Credential GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.SelectById(tenantId, id)).Result;
            List<Credential> creds = MapCredentials(result);
            if (creds.Count > 0) return creds[0];
            return null;
        }

        public List<Credential> GetByUserId(string userId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.SelectByUserId(userId)).Result;
            return MapCredentials(result);
        }

        public List<Credential> GetByUserId(string tenantId, string userId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.SelectByUserId(tenantId, userId)).Result;
            return MapCredentials(result);
        }

        public Credential GetByAccessKey(string accessKey)
        {
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.SelectByAccessKey(accessKey)).Result;
            List<Credential> creds = MapCredentials(result);
            if (creds.Count > 0) return creds[0];
            return null;
        }

        public void Insert(Credential credentials)
        {
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            _Driver.ExecuteQuery(CredentialQueries.InsertQuery(credentials), true).Wait();
        }

        public void Update(Credential credentials)
        {
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            _Driver.ExecuteQuery(CredentialQueries.UpdateQuery(credentials), true).Wait();
        }

        public void DeleteById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(CredentialQueries.DeleteById(id), true).Wait();
        }

        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(CredentialQueries.DeleteById(tenantId, id), true).Wait();
        }

        private List<Credential> MapCredentials(DataTable dt)
        {
            List<Credential> creds = new List<Credential>();
            if (dt == null || dt.Rows.Count == 0) return creds;

            foreach (DataRow row in dt.Rows)
            {
                Credential cred = new Credential();
                cred.Id = row["id"] != DBNull.Value ? row["id"].ToString() : null;
                cred.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
                cred.UserId = row["user_id"] != DBNull.Value ? row["user_id"].ToString() : null;
                cred.Description = row["description"] != DBNull.Value ? row["description"].ToString() : null;
                cred.AccessKey = row["accesskey"] != DBNull.Value ? row["accesskey"].ToString() : null;
                cred.SecretKey = row["secretkey"] != DBNull.Value ? row["secretkey"].ToString() : null;
                cred.IsBase64 = ControlPlaneDataMapper.BoolValue(row, "isbase64");
                cred.Active = ControlPlaneDataMapper.BoolValue(row, "active");
                cred.LastUsedUtc = ControlPlaneDataMapper.NullableDateValue(row, "lastusedutc");
                cred.LastFailedUtc = ControlPlaneDataMapper.NullableDateValue(row, "lastfailedutc");
                cred.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                creds.Add(cred);
            }

            return creds;
        }
    }
}
