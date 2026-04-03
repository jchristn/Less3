namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;

    using Less3.Database;
    using Less3.Settings;
    using SyslogLogging;

    /// <summary>
    /// Configuration manager.
    /// </summary>
    internal class ConfigManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SettingsBase _Settings = null;
        private LoggingModule _Logging = null;
        private DatabaseDriverBase _Database = null;

        #endregion

        #region Constructors-and-Factories

        internal ConfigManager(SettingsBase settings, LoggingModule logging, DatabaseDriverBase database)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Internal-User-Methods

        internal List<User> GetUsers()
        {
            return _Database.Users.GetAll();
        }

        internal bool UserGuidExists(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            return _Database.Users.ExistsByGuid(guid);
        }

        internal bool UserEmailExists(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            return _Database.Users.ExistsByEmail(email);
        }

        internal User GetUserByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            return _Database.Users.GetByGuid(guid);
        }

        internal User GetUserByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _Database.Users.GetByName(name);
        }

        internal User GetUserByEmail(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            return _Database.Users.GetByEmail(email);
        }

        internal User GetUserByAccessKey(string accessKey)
        {
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));

            Credential cred = GetCredentialByAccessKey(accessKey);
            if (cred == null)
            {
                _Logging.Warn("ConfigManager GetUserByAccessKey access key " + accessKey + " not found");
                return null;
            }

            User user = GetUserByGuid(cred.UserGUID);
            if (user == null)
            {
                _Logging.Warn("ConfigManager GetUserByAccessKey user GUID " + cred.UserGUID + " not found, referenced by credential GUID " + cred.GUID);
                return null;
            }

            return user;
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

            User userByGuid = GetUserByGuid(user.GUID);
            if (userByGuid != null)
            {
                _Logging.Warn("ConfigManager AddUser user GUID " + user.GUID + " already exists");
                return false;
            }

            User userByEmail = GetUserByEmail(user.Email);
            if (userByEmail != null)
            {
                _Logging.Warn("ConfigManager AddUser user email " + user.Email + " already exists");
                return false;
            }

            _Database.Users.Insert(user);
            return true;
        }

        internal void DeleteUser(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Database.Users.DeleteByGuid(guid);
        }

        #endregion

        #region Internal-Credential-Methods

        internal List<Credential> GetCredentials()
        {
            return _Database.Credentials.GetAll();
        }

        internal bool CredentialGuidExists(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            return _Database.Credentials.ExistsByGuid(guid);
        }

        internal Credential GetCredentialByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            return _Database.Credentials.GetByGuid(guid);
        }

        internal List<Credential> GetCredentialsByUser(string userGuid)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            return _Database.Credentials.GetByUserGuid(userGuid);
        }

        internal Credential GetCredentialByAccessKey(string accessKey)
        {
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            return _Database.Credentials.GetByAccessKey(accessKey);
        }

        internal bool AddCredential(string userGuid, string description, string accessKey, string secretKey, bool isBase64)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));

            Credential cred = new Credential(userGuid, description, accessKey, secretKey, isBase64);
            return AddCredential(cred);
        }

        internal bool AddCredential(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));

            Credential credByGuid = GetCredentialByGuid(cred.GUID);
            if (credByGuid != null)
            {
                _Logging.Warn("ConfigManager AddCredential credential GUID " + cred.GUID + " already exists");
                return false;
            }

            Credential credByKey = GetCredentialByAccessKey(cred.AccessKey);
            if (credByKey != null)
            {
                _Logging.Warn("ConfigManager AddCredential access key " + cred.AccessKey + " already exists");
                return false;
            }

            _Database.Credentials.Insert(cred);
            return true;
        }

        internal void DeleteCredential(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Database.Credentials.DeleteByGuid(guid);
        }

        #endregion

        #region Internal-Bucket-Methods

        internal List<Bucket> GetBuckets()
        {
            return _Database.Buckets.GetAll();
        }

        internal bool BucketExists(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _Database.Buckets.ExistsByName(name);
        }

        internal List<Bucket> GetBucketsByUser(string userGuid)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            return _Database.Buckets.GetByOwnerGuid(userGuid);
        }

        internal Bucket GetBucketByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            return _Database.Buckets.GetByGuid(guid);
        }

        internal Bucket GetBucketByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _Database.Buckets.GetByName(name);
        }

        internal bool AddBucket(string userGuid, string name)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Bucket bucket = new Bucket(
                Guid.NewGuid().ToString(),
                name,
                userGuid,
                _Settings.Storage.StorageType,
                _Settings.Storage.DiskDirectory + name + "/Objects");

            return AddBucket(bucket);
        }

        internal bool AddBucket(Bucket bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            if (BucketExists(bucket.Name))
            {
                _Logging.Warn("ConfigManager AddBucket bucket " + bucket.Name + " already exists");
                return false;
            }

            _Database.Buckets.Insert(bucket);
            return true;
        }

        internal void DeleteBucket(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Database.Buckets.DeleteByGuid(guid);
        }

        #endregion

        #region Internal-Upload-Methods

        internal Less3.Classes.Upload GetUploadByGuid(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) return null;
            return _Database.Uploads.GetByGuid(uploadGuid);
        }

        internal List<Less3.Classes.Upload> GetUploads()
        {
            return _Database.Uploads.GetAll();
        }

        internal List<Less3.Classes.Upload> GetUploadsByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) return new List<Less3.Classes.Upload>();
            return _Database.Uploads.GetByBucketGuid(bucketGuid);
        }

        internal void AddUpload(Less3.Classes.Upload upload)
        {
            if (upload == null) throw new ArgumentNullException(nameof(upload));
            _Database.Uploads.Insert(upload);
        }

        internal void DeleteUpload(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) return;
            _Database.Uploads.DeleteByGuid(uploadGuid);
        }

        internal void AddUploadPart(UploadPart part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            _Database.UploadParts.Insert(part);
        }

        internal List<UploadPart> GetUploadPartsByUploadGuid(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) return null;
            return _Database.UploadParts.GetByUploadGuid(uploadGuid);
        }

        internal void DeleteUploadParts(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) return;
            _Database.UploadParts.DeleteByUploadGuid(uploadGuid);
        }

        #endregion

        #region Internal-RequestHistory-Methods

        internal List<RequestHistory> GetRequestHistories()
        {
            return _Database.RequestHistory.GetAll();
        }

        internal RequestHistory GetRequestHistoryByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            return _Database.RequestHistory.GetByGuid(guid);
        }

        internal void AddRequestHistory(RequestHistory entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            _Database.RequestHistory.Insert(entry);
        }

        internal void DeleteRequestHistory(string guid)
        {
            if (String.IsNullOrEmpty(guid)) return;
            _Database.RequestHistory.DeleteByGuid(guid);
        }

        internal void DeleteRequestHistoriesOlderThan(DateTime cutoff)
        {
            _Database.RequestHistory.DeleteOlderThan(cutoff);
        }

        internal List<RequestHistory> GetRequestHistoriesInRange(DateTime startUtc, DateTime endUtc)
        {
            return _Database.RequestHistory.GetInRange(startUtc, endUtc);
        }

        #endregion
    }
}
