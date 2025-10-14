namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;

    using DatabaseWrapper.Core;
    using ExpressionTree;
    using Less3.Settings;
    using SyslogLogging;
    using Watson.ORM;
    using Watson.ORM.Core;

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
        private WatsonORM _ORM = null;

        #endregion

        #region Constructors-and-Factories
         
        internal ConfigManager(SettingsBase settings, LoggingModule logging, WatsonORM orm)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _ORM = orm ?? throw new ArgumentNullException(nameof(orm));
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Internal-User-Methods

        internal List<User> GetUsers()
        {
            Expr e = new Expr(
                _ORM.GetColumnName<User>(nameof(User.Id)),
                OperatorEnum.GreaterThan,
                0);
            return _ORM.SelectMany<User>(e);
        }

        internal bool UserGuidExists(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            Expr e = new Expr(
                _ORM.GetColumnName<User>(nameof(User.GUID)),
                OperatorEnum.Equals,
                guid);

            User user = _ORM.SelectFirst<User>(e);
            if (user != null) return true;
            return false;
        }

        internal bool UserEmailExists(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            Expr e = new Expr(
                _ORM.GetColumnName<User>(nameof(User.Email)),
                OperatorEnum.Equals,
                email);

            User user = _ORM.SelectFirst<User>(e);
            if (user != null) return true;
            return false;
        }

        internal User GetUserByGuid(string guid)
        { 
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            Expr e = new Expr(
                _ORM.GetColumnName<User>(nameof(User.GUID)),
                OperatorEnum.Equals,
                guid);

            return _ORM.SelectFirst<User>(e);
        }

        internal User GetUserByName(string name)
        { 
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Expr e = new Expr(
                _ORM.GetColumnName<User>(nameof(User.Name)),
                OperatorEnum.Equals,
                name);

            return _ORM.SelectFirst<User>(e);
        }

        internal User GetUserByEmail(string email)
        { 
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            Expr e = new Expr(
                _ORM.GetColumnName<User>(nameof(User.Email)),
                OperatorEnum.Equals,
                email);

            return _ORM.SelectFirst<User>(e);
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

            _ORM.Insert<User>(user);
            return true;
        }

        internal void DeleteUser(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            User tempUser = GetUserByGuid(guid);
            if (tempUser != null)
            {
                _ORM.Delete<User>(tempUser);
            }
        }
         
        #endregion

        #region Internal-Credential-Methods

        internal List<Credential> GetCredentials()
        {
            Expr e = new Expr(
                _ORM.GetColumnName<Credential>(nameof(Credential.Id)),
                OperatorEnum.GreaterThan,
                0);
            return _ORM.SelectMany<Credential>(e); 
        }

        internal bool CredentialGuidExists(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            Expr e = new Expr(
                _ORM.GetColumnName<Credential>(nameof(Credential.GUID)),
                OperatorEnum.Equals,
                guid);

            Credential cred = _ORM.SelectFirst<Credential>(e);
            if (cred != null) return true;
            return false;
        }

        internal Credential GetCredentialByGuid(string guid)
        { 
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            Expr e = new Expr(
                _ORM.GetColumnName<Credential>(nameof(Credential.GUID)),
                OperatorEnum.Equals,
                guid);

            return _ORM.SelectFirst<Credential>(e);
        }

        internal List<Credential> GetCredentialsByUser(string userGuid)
        { 
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));

            Expr e = new Expr(
                _ORM.GetColumnName<Credential>(nameof(Credential.UserGUID)),
                OperatorEnum.Equals,
                userGuid);

            return _ORM.SelectMany<Credential>(e);
        }

        internal Credential GetCredentialByAccessKey(string accessKey)
        { 
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
             
            Expr e = new Expr(
                _ORM.GetColumnName<Credential>(nameof(Credential.AccessKey)),
                OperatorEnum.Equals,
                accessKey);

            return _ORM.SelectFirst<Credential>(e);
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

            _ORM.Insert<Credential>(cred); 
            return true;
        }

        internal void DeleteCredential(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            Credential tempCred = GetCredentialByGuid(guid);
            if (tempCred != null)
            {
                _ORM.Delete<Credential>(tempCred);
            }
        }
         
        #endregion

        #region Internal-Bucket-Methods

        internal List<Bucket> GetBuckets()
        {
            Expr e = new Expr(
                _ORM.GetColumnName<Bucket>(nameof(Bucket.Id)),
                OperatorEnum.GreaterThan,
                0);

            return _ORM.SelectMany<Bucket>(e); 
        }

        internal bool BucketExists(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Expr e = new Expr(
                _ORM.GetColumnName<Bucket>(nameof(Bucket.Name)),
                OperatorEnum.Equals,
                name);

            Bucket bucket = _ORM.SelectFirst<Bucket>(e);
            if (bucket != null) return true;
            return false;
        }

        internal List<Bucket> GetBucketsByUser(string userGuid)
        { 
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));

            Expr e = new Expr(
                _ORM.GetColumnName<Bucket>(nameof(Bucket.OwnerGUID)),
                OperatorEnum.Equals,
                userGuid);

            return _ORM.SelectMany<Bucket>(e); 
        }

        internal Bucket GetBucketByGuid(string guid)
        { 
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            Expr e = new Expr(
                _ORM.GetColumnName<Bucket>(nameof(Bucket.GUID)),
                OperatorEnum.Equals,
                guid);

            return _ORM.SelectFirst<Bucket>(e);
        }

        internal Bucket GetBucketByName(string name)
        { 
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Expr e = new Expr(
                _ORM.GetColumnName<Bucket>(nameof(Bucket.Name)),
                OperatorEnum.Equals,
                name);

            return _ORM.SelectFirst<Bucket>(e);
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

            _ORM.Insert<Bucket>(bucket);
            return true;
        }

        internal void DeleteBucket(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            Bucket bucket = GetBucketByGuid(guid);
            if (bucket != null)
            {
                _ORM.Delete<Bucket>(bucket);
            }
        }
         
        #endregion

        #region Internal-Upload-Methods

        internal Less3.Classes.Upload GetUploadByGuid(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) return null;

            Expr e = new Expr(
                _ORM.GetColumnName<Less3.Classes.Upload>(nameof(Less3.Classes.Upload.GUID)),
                OperatorEnum.Equals,
                uploadGuid);

            return _ORM.SelectFirst<Less3.Classes.Upload>(e);
        }

        internal List<Less3.Classes.Upload> GetUploads()
        {
            Expr e = new Expr(
                _ORM.GetColumnName<Less3.Classes.Upload>(nameof(Less3.Classes.Upload.Id)),
                OperatorEnum.GreaterThan,
                0);

            return _ORM.SelectMany<Less3.Classes.Upload>(e);
        }

        internal List<Less3.Classes.Upload> GetUploadsByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) return new List<Less3.Classes.Upload>();

            Expr e = new Expr(
                _ORM.GetColumnName<Less3.Classes.Upload>(nameof(Less3.Classes.Upload.BucketGUID)),
                OperatorEnum.Equals,
                bucketGuid);

            return _ORM.SelectMany<Less3.Classes.Upload>(e);
        }

        internal void AddUpload(Less3.Classes.Upload upload)
        {
            if (upload == null) throw new ArgumentNullException(nameof(upload));
            _ORM.Insert<Less3.Classes.Upload>(upload);
        }

        internal void DeleteUpload(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) return;

            Expr e = new Expr(
                _ORM.GetColumnName<Less3.Classes.Upload>(nameof(Less3.Classes.Upload.GUID)),
                OperatorEnum.Equals,
                uploadGuid);

            _ORM.DeleteMany<Less3.Classes.Upload>(e);
        }

        internal void AddUploadPart(UploadPart part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            _ORM.Insert<UploadPart>(part);
        }

        internal List<UploadPart> GetUploadPartsByUploadGuid(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) return null;

            Expr e = new Expr(
                _ORM.GetColumnName<UploadPart>(nameof(UploadPart.UploadGUID)),
                OperatorEnum.Equals,
                Guid.Parse(uploadGuid));

            return _ORM.SelectMany<UploadPart>(e);
        }

        internal void DeleteUploadParts(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) return;

            Expr e = new Expr(
                _ORM.GetColumnName<UploadPart>(nameof(UploadPart.UploadGUID)),
                OperatorEnum.Equals,
                Guid.Parse(uploadGuid));

            _ORM.DeleteMany<UploadPart>(e);
        }

        #endregion
    }
}
