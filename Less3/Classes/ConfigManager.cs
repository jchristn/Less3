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
    public class ConfigManager : IDisposable
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private bool _Disposed = false;

        private Settings _Settings;
        private LoggingModule _Logging;
        private DatabaseClient _Database;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param>
        public ConfigManager(Settings settings, LoggingModule logging)
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
            Dispose(true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        #endregion

        #region Public-User-Methods

        /// <summary>
        /// Retrieve a list of configured users.
        /// </summary>
        /// <param name="users">Users.</param>
        public void GetUsers(out List<User> users)
        {
            users = new List<User>();
            string query = DatabaseQueries.GetUsers();
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

        /// <summary>
        /// Check if a user exists by GUID.
        /// </summary>
        /// <param name="guid">User GUID.</param>
        /// <returns>True if exists.</returns>
        public bool UserGuidExists(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = DatabaseQueries.GetUserByGuid(guid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        /// <summary>
        /// Check if a user exists by email address.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <returns>True if exists.</returns>
        public bool UserEmailExists(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            string query = DatabaseQueries.GetUserByEmail(email);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        /// <summary>
        /// Retrieve a user by GUID.
        /// </summary>
        /// <param name="guid">User GUID.</param>
        /// <param name="user">User.</param>
        /// <returns>True if successful.</returns>
        public bool GetUserByGuid(string guid, out User user)
        {
            user = null;
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = DatabaseQueries.GetUserByGuid(guid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                user = User.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieve a user by name.
        /// </summary>
        /// <param name="name">User name.</param>
        /// <param name="user">User.</param>
        /// <returns>True if successful.</returns>
        public bool GetUserByName(string name, out User user)
        {
            user = null;
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            string query = DatabaseQueries.GetUserByName(name);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                user = User.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieve a user by email address.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="user">User.</param>
        /// <returns>True if successful.</returns>
        public bool GetUserByEmail(string email, out User user)
        {
            user = null;
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            string query = DatabaseQueries.GetUserByEmail(email);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                user = User.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieve a user by access key.
        /// </summary>
        /// <param name="accessKey">Access key.</param>
        /// <param name="user">User.</param>
        /// <returns>True if successful.</returns>
        public bool GetUserByAccessKey(string accessKey, out User user)
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

        /// <summary>
        /// Add a user.
        /// </summary>
        /// <param name="guid">User GUID.</param>
        /// <param name="name">User name.</param>
        /// <param name="email">Email address.</param>
        /// <returns>True if successful.</returns>
        public bool AddUser(string guid, string name, string email)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            User user = new User(guid, name, email);
            return AddUser(user);
        }

        /// <summary>
        /// Add a user.
        /// </summary>
        /// <param name="user">User.</param>
        /// <returns>True if successful.</returns>
        public bool AddUser(User user)
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

            string query = DatabaseQueries.InsertUser(user);
            DataTable result = _Database.Query(query);
            return true;
        }

        /// <summary>
        /// Delete a user.
        /// </summary>
        /// <param name="guid">User GUID.</param>
        public void DeleteUser(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid)); 
              
            string query = DatabaseQueries.DeleteUser(guid);
            DataTable result = _Database.Query(query);
            return;
        }

        /// <summary>
        /// Set a value on a user account.
        /// </summary>
        /// <param name="guid">User GUID.</param>
        /// <param name="field">Field to set.</param>
        /// <param name="val">Value to set in the field.</param>
        public void SetUserValue(string guid, string field, string val)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(field)) throw new ArgumentNullException(nameof(field));
            if (String.IsNullOrEmpty(val)) throw new ArgumentNullException(nameof(val));

            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add(field, val);

            string query = DatabaseQueries.UpdateRecord("Users", "GUID", guid, vals);
            DataTable result = _Database.Query(query);
            return;
        }

        #endregion

        #region Public-Credential-Methods

        /// <summary>
        /// Retrieve a list of credentials.
        /// </summary>
        /// <param name="creds">Credentials.</param>
        public void GetCredentials(out List<Credential> creds)
        {
            creds = new List<Credential>();
            string query = DatabaseQueries.GetCredentials();
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

        /// <summary>
        /// Check if a credential exists by GUID.
        /// </summary>
        /// <param name="guid">Credential GUID.</param>
        /// <returns>True if exists.</returns>
        public bool CredentialGuidExists(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = DatabaseQueries.GetCredentialsByGuid(guid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        /// <summary>
        /// Retrieve credential by GUID.
        /// </summary>
        /// <param name="guid">Credential GUID.</param>
        /// <param name="cred">Credential.</param>
        /// <returns>True if successful.</returns>
        public bool GetCredentialByGuid(string guid, out Credential cred)
        {
            cred = null;
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = DatabaseQueries.GetCredentialsByGuid(guid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                cred = Credential.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieve credentials by user.
        /// </summary>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="creds">Credentials.</param>
        /// <returns>True if successful.</returns>
        public bool GetCredentialsByUser(string userGuid, out List<Credential> creds)
        {
            creds = new List<Credential>();
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));

            string query = DatabaseQueries.GetCredentialsByUser(userGuid);
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

        /// <summary>
        /// Retrieve credentials by access key.
        /// </summary>
        /// <param name="accessKey">Access key.</param>
        /// <param name="cred">Credential.</param>
        /// <returns>True if successful.</returns>
        public bool GetCredentialByAccessKey(string accessKey, out Credential cred)
        {
            cred = null;
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));

            string query = DatabaseQueries.GetCredentialsByAccessKey(accessKey);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                cred = Credential.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add credentials.
        /// </summary>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="description">Description of the credentials.</param>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param>
        /// <returns>True if successful.</returns>
        public bool AddCredential(string userGuid, string description, string accessKey, string secretKey)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));

            Credential cred = new Credential(userGuid, description, accessKey, secretKey);
            return AddCredential(cred);
        }

        /// <summary>
        /// Add credentials.
        /// </summary>
        /// <param name="cred">Credential.</param>
        /// <returns>True if successful.</returns>
        public bool AddCredential(Credential cred)
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

            string query = DatabaseQueries.InsertCredentials(cred);
            DataTable result = _Database.Query(query);
            return true;
        }

        /// <summary>
        /// Delete credentials.
        /// </summary>
        /// <param name="guid">Credential GUID.</param>
        public void DeleteCredential(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = DatabaseQueries.DeleteCredentials(guid);
            DataTable result = _Database.Query(query);
            return;
        }

        /// <summary>
        /// Set values for a credential.
        /// </summary>
        /// <param name="guid">Credential GUID.</param>
        /// <param name="field">Field to set.</param>
        /// <param name="val">Value to set.</param>
        public void SetCredentialValue(string guid, string field, string val)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(field)) throw new ArgumentNullException(nameof(field));
            if (String.IsNullOrEmpty(val)) throw new ArgumentNullException(nameof(val));

            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add(field, val);

            string query = DatabaseQueries.UpdateRecord("Credentials", "GUID", guid, vals);
            DataTable result = _Database.Query(query);
            return;
        }

        #endregion

        #region Public-Bucket-Methods

        /// <summary>
        /// Retrieve buckets.
        /// </summary>
        /// <param name="buckets">Buckets.</param>
        public void GetBuckets(out List<BucketConfiguration> buckets)
        {
            buckets = new List<BucketConfiguration>();
            string query = DatabaseQueries.GetBuckets();
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

        /// <summary>
        /// Check if a bucket exists.
        /// </summary>
        /// <param name="name">Name of the bucket.</param>
        /// <returns>True if exists.</returns>
        public bool BucketExists(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            string query = DatabaseQueries.GetBucketByName(name);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        /// <summary>
        /// Get buckets by user GUID.
        /// </summary>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="buckets">Buckets.</param>
        public void GetBucketsByUser(string userGuid, out List<BucketConfiguration> buckets)
        {
            buckets = new List<BucketConfiguration>();
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));

            string query = DatabaseQueries.GetBucketsByUser(userGuid);
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

        /// <summary>
        /// Get bucket by GUID.
        /// </summary>
        /// <param name="guid">Bucket GUID.</param>
        /// <param name="bucket">Bucket.</param>
        /// <returns>True if successful.</returns>
        public bool GetBucketByGuid(string guid, out BucketConfiguration bucket)
        {
            bucket = null;
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = DatabaseQueries.GetBucketByGuid(guid);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                bucket = BucketConfiguration.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get bucket by name.
        /// </summary>
        /// <param name="name">Bucket name.</param>
        /// <param name="bucket">Bucket.</param>
        /// <returns>True if successful.</returns>
        public bool GetBucketByName(string name, out BucketConfiguration bucket)
        {
            bucket = null;
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            string query = DatabaseQueries.GetBucketByName(name);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                bucket = BucketConfiguration.FromDataRow(result.Rows[0]);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add a bucket.
        /// </summary>
        /// <param name="userGuid">Owning user GUID.</param>
        /// <param name="name">Bucket name.</param>
        /// <returns>True if successful.</returns>
        public bool AddBucket(string userGuid, string name)
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

        /// <summary>
        /// Add a bucket.
        /// </summary>
        /// <param name="bucket">Bucket.</param>
        /// <returns>True if successful.</returns>
        public bool AddBucket(BucketConfiguration bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            if (BucketExists(bucket.Name))
            {
                _Logging.Warn("ConfigManager AddBucket bucket " + bucket.Name + " already exists");
                return false;
            }

            string query = DatabaseQueries.InsertBucket(bucket);
            DataTable result = _Database.Query(query);
            return true;
        }

        /// <summary>
        /// Delete a bucket.
        /// </summary>
        /// <param name="guid">Bucket GUID.</param>
        public void DeleteBucket(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            string query = DatabaseQueries.DeleteBucket(guid);
            DataTable result = _Database.Query(query);
            return;
        }

        /// <summary>
        /// Set value for a bucket.
        /// </summary>
        /// <param name="guid">Bucket GUID.</param>
        /// <param name="field">Field to set.</param>
        /// <param name="val">Value to set.</param>
        public void SetBucketValue(string guid, string field, string val)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(field)) throw new ArgumentNullException(nameof(field));
            if (String.IsNullOrEmpty(val)) throw new ArgumentNullException(nameof(val));

            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add(field, val);

            string query = DatabaseQueries.UpdateRecord("Buckets", "GUID", guid, vals);
            DataTable result = _Database.Query(query);
            return;
        }

        #endregion

        #region Private-Methods

        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
            {
                return;
            }

            if (disposing)
            {
                try
                {
                    _Database.Dispose();
                    _Database = null;
                }
                catch (Exception e)
                {
                    _Logging.LogException("ConfigManager", "Dispose", e);
                }
            }

            _Disposed = true;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void InitializeDatabase()
        {
            _Database = new DatabaseClient(_Settings.Files.ConfigDatabase, _Settings.Debug.Database);

            string query = null;
            DataTable result = null;

            query = DatabaseQueries.CreateUsersTable();
            result = _Database.Query(query);

            query = DatabaseQueries.CreateCredentialsTable();
            result = _Database.Query(query);

            query = DatabaseQueries.CreateBucketsTable();
            result = _Database.Query(query);
        }

        #endregion
    }
}
