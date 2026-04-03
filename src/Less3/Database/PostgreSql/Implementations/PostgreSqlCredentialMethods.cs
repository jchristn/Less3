namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
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

        public bool ExistsByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.ExistsByGuid(guid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public Credential GetByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.SelectByGuid(guid)).Result;
            List<Credential> creds = MapCredentials(result);
            if (creds.Count > 0) return creds[0];
            return null;
        }

        public List<Credential> GetByUserGuid(string userGuid)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            DataTable result = _Driver.ExecuteQuery(CredentialQueries.SelectByUserGuid(userGuid)).Result;
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

        public void Insert(Credential credential)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));
            _Driver.ExecuteQuery(CredentialQueries.InsertQuery(credential), true).Wait();
        }

        public void DeleteByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Driver.ExecuteQuery(CredentialQueries.DeleteByGuid(guid), true).Wait();
        }

        private List<Credential> MapCredentials(DataTable dt)
        {
            List<Credential> creds = new List<Credential>();
            if (dt == null || dt.Rows.Count == 0) return creds;

            foreach (DataRow row in dt.Rows)
            {
                Credential cred = new Credential();
                cred.Id = Convert.ToInt32(row["id"]);
                cred.GUID = row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
                cred.UserGUID = row["userguid"] != DBNull.Value ? row["userguid"].ToString() : null;
                cred.Description = row["description"] != DBNull.Value ? row["description"].ToString() : null;
                cred.AccessKey = row["accesskey"] != DBNull.Value ? row["accesskey"].ToString() : null;
                cred.SecretKey = row["secretkey"] != DBNull.Value ? row["secretkey"].ToString() : null;
                cred.IsBase64 = Convert.ToBoolean(row["isbase64"]);
                cred.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                creds.Add(cred);
            }

            return creds;
        }
    }
}
