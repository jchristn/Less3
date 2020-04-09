using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

using SqliteWrapper;
using SyslogLogging;

using Less3.Database.Configuration;

namespace Less3.Classes
{
    /// <summary>
    /// Configuration manager.
    /// </summary>
    internal class ConfigManager : IDisposable
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private DatabaseClient _Database = null;
        private DatabaseQueries _Queries = null;

        #endregion

        #region Constructors-and-Factories
         
        internal ConfigManager(Settings settings, LoggingModule logging)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _Settings = settings;
            _Logging = logging;

            InitializeDatabase();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Tear down the client and dispose of background workers.
        /// </summary>
        public void Dispose()
        {
            if (_Database != null)
            {
                _Database.Dispose();
                _Database = null;
            }
        }

        #endregion

        #region Internal-User-Methods

        internal void GetUsers(out List<User> users)
        {
            users = new List<User>();
            string query = _Queries.GetUsers();
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    users.Add(User.FromDataRow(row));
                }
            }

            return;
        }

        internal bool UserGuidExists(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = _Queries.GetUserByGuid(guid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        internal bool UserEmailExists(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            string query = _Queries.GetUserByEmail(email);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        internal bool GetUserByGuid(string guid, out User user)
        {
            user = null;
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = _Queries.GetUserByGuid(guid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                user = User.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        internal bool GetUserByName(string name, out User user)
        {
            user = null;
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            string query = _Queries.GetUserByName(name);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                user = User.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        internal bool GetUserByEmail(string email, out User user)
        {
            user = null;
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            string query = _Queries.GetUserByEmail(email);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                user = User.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        internal bool GetUserByAccessKey(string accessKey, out User user)
        {
            user = null;
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));

            Credential cred = null;
            if (!GetCredentialByAccessKey(accessKey, out cred))
            {
                _Logging.Warn("ConfigManager GetUserByAccessKey access key " + accessKey + " not found");
                return false;
            }

            if (!GetUserByGuid(cred.UserGUID, out user))
            {
                _Logging.Warn("ConfigManager GetUserByAccessKey user GUID " + cred.UserGUID + " not found, referenced by credential GUID " + cred.GUID);
                return false;
            }

            return true;
        }

        internal bool AddUser(string guid, string name, string email)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            User user = new User(guid, name, email);
            return AddUser(user);
        }

        internal bool AddUser(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            User tempUser = null;

            if (GetUserByGuid(user.GUID, out tempUser))
            {
                _Logging.Warn("ConfigManager AddUser user GUID " + user.GUID + " already exists");
                return false;
            }

            if (GetUserByEmail(user.Email, out tempUser))
            {
                _Logging.Warn("ConfigManager AddUser user email " + user.Email + " already exists");
                return false;
            }

            string query = _Queries.InsertUser(user);
            DataTable result = _Database.Query(query);
            return true;
        }

        internal void DeleteUser(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid)); 
              
            string query = _Queries.DeleteUser(guid);
            DataTable result = _Database.Query(query);
            return;
        }

        internal void SetUserValue(string guid, string field, string val)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(field)) throw new ArgumentNullException(nameof(field));
            if (String.IsNullOrEmpty(val)) throw new ArgumentNullException(nameof(val));

            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add(field, val);

            string query = _Queries.UpdateRecord("Users", "GUID", guid, vals);
            DataTable result = _Database.Query(query);
            return;
        }

        #endregion

        #region Internal-Credential-Methods

        internal void GetCredentials(out List<Credential> creds)
        {
            creds = new List<Credential>();
            string query = _Queries.GetCredentials();
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    creds.Add(Credential.FromDataRow(row));
                }
            }

            return;
        }

        internal bool CredentialGuidExists(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = _Queries.GetCredentialsByGuid(guid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        internal bool GetCredentialByGuid(string guid, out Credential cred)
        {
            cred = null;
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = _Queries.GetCredentialsByGuid(guid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                cred = Credential.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        internal bool GetCredentialsByUser(string userGuid, out List<Credential> creds)
        {
            creds = new List<Credential>();
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));

            string query = _Queries.GetCredentialsByUser(userGuid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    creds.Add(Credential.FromDataRow(row));
                }

                return true;
            }

            return false;
        }

        internal bool GetCredentialByAccessKey(string accessKey, out Credential cred)
        {
            cred = null;
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));

            string query = _Queries.GetCredentialsByAccessKey(accessKey);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                cred = Credential.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        internal bool AddCredential(string userGuid, string description, string accessKey, string secretKey)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));

            Credential cred = new Credential(userGuid, description, accessKey, secretKey);
            return AddCredential(cred);
        }

        internal bool AddCredential(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));

            Credential tempCred = null;
            if (GetCredentialByGuid(cred.GUID, out tempCred))
            {
                _Logging.Warn("ConfigManager AddCredential credential GUID " + cred.GUID + " already exists");
                return false;
            }

            if (GetCredentialByAccessKey(cred.AccessKey, out tempCred)) 
            {
                _Logging.Warn("ConfigManager AddCredential access key " + cred.AccessKey + " already exists");
                return false;
            }

            string query = _Queries.InsertCredentials(cred);
            DataTable result = _Database.Query(query);
            return true;
        }

        internal void DeleteCredential(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = _Queries.DeleteCredentials(guid);
            DataTable result = _Database.Query(query);
            return;
        }

        internal void SetCredentialValue(string guid, string field, string val)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(field)) throw new ArgumentNullException(nameof(field));
            if (String.IsNullOrEmpty(val)) throw new ArgumentNullException(nameof(val));

            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add(field, val);

            string query = _Queries.UpdateRecord("Credentials", "GUID", guid, vals);
            DataTable result = _Database.Query(query);
            return;
        }

        #endregion

        #region Internal-Bucket-Methods

        internal void GetBuckets(out List<BucketConfiguration> buckets)
        {
            buckets = new List<BucketConfiguration>();
            string query = _Queries.GetBuckets();
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    buckets.Add(BucketConfiguration.FromDataRow(row));
                }
            }

            return;
        }

        internal bool BucketExists(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            string query = _Queries.GetBucketByName(name);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        internal void GetBucketsByUser(string userGuid, out List<BucketConfiguration> buckets)
        {
            buckets = new List<BucketConfiguration>();
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));

            string query = _Queries.GetBucketsByUser(userGuid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    buckets.Add(BucketConfiguration.FromDataRow(row));
                } 
            }

            return;
        }

        internal bool GetBucketByGuid(string guid, out BucketConfiguration bucket)
        {
            bucket = null;
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = _Queries.GetBucketByGuid(guid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                bucket = BucketConfiguration.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        internal bool GetBucketByName(string name, out BucketConfiguration bucket)
        {
            bucket = null;
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            string query = _Queries.GetBucketByName(name);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                bucket = BucketConfiguration.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        internal bool AddBucket(string userGuid, string name)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            BucketConfiguration bucket = new BucketConfiguration(
                name,
                userGuid,
                _Settings.Storage.Directory + name + "/" + name + ".db",
                _Settings.Storage.Directory + name + "/Objects");

            return AddBucket(bucket);
        }

        internal bool AddBucket(BucketConfiguration bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            if (BucketExists(bucket.Name))
            {
                _Logging.Warn("ConfigManager AddBucket bucket " + bucket.Name + " already exists");
                return false;
            }

            string query = _Queries.InsertBucket(bucket);
            DataTable result = _Database.Query(query);
            return true;
        }

        internal void DeleteBucket(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = _Queries.DeleteBucket(guid);
            DataTable result = _Database.Query(query);
            return;
        }

        internal void SetBucketValue(string guid, string field, string val)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(field)) throw new ArgumentNullException(nameof(field));
            if (String.IsNullOrEmpty(val)) throw new ArgumentNullException(nameof(val));

            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add(field, val);

            string query = _Queries.UpdateRecord("Buckets", "GUID", guid, vals);
            DataTable result = _Database.Query(query);
            return;
        }

        #endregion

        #region Private-Methods
         
        private void InitializeDatabase()
        {
            _Database = new DatabaseClient(_Settings.Files.Database);

            _Database.Logger = Logger;
            _Database.LogQueries = _Settings.Debug.DatabaseQueries;
            _Database.LogResults = _Settings.Debug.DatabaseResults;

            _Queries = new DatabaseQueries(_Database);

            string query = null;
            DataTable result = null;

            query = _Queries.CreateUsersTable();
            result = _Database.Query(query);

            query = _Queries.CreateCredentialsTable();
            result = _Database.Query(query);

            query = _Queries.CreateBucketsTable();
            result = _Database.Query(query);
        }

        private void Logger(string msg)
        {
            _Logging.Debug(msg);
            return;
        }
        
        #endregion
    }
}
