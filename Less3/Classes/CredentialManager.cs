using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
 
using SyslogLogging;

namespace Less3.Classes
{
    public class CredentialManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings;
        private LoggingModule _Logging;

        private readonly object _CredentialLock = new object();
        private List<Credential> _Credentials = new List<Credential>();

        #endregion

        #region Constructors-and-Factories

        public CredentialManager(Settings settings, LoggingModule logging)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _Settings = settings;
            _Logging = logging;

            Load();
        }

        #endregion

        #region Public-Methods

        public void Load()
        {
            lock (_CredentialLock)
            {
                _Credentials = Common.DeserializeJson<List<Credential>>(Common.ReadTextFile(_Settings.Files.Credentials));
            }
        }

        public void Save()
        {
            lock (_CredentialLock)
            {
                Common.WriteFile(_Settings.Files.Credentials, Encoding.UTF8.GetBytes(Common.SerializeJson(_Credentials, true)));
            }
        }

        public bool Exists(string accessKey, string secretKey)
        {
            if (String.IsNullOrEmpty(accessKey)) return false;
            if (String.IsNullOrEmpty(secretKey)) return false;

            lock (_CredentialLock)
            {
                return _Credentials.Exists(c => c.AccessKey.Equals(accessKey) && c.SecretKey.Equals(secretKey));
            }
        }

        public bool GetUser(string accessKey, out string user)
        {
            user = null;
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey)); 

            lock (_CredentialLock)
            {
                bool exists = _Credentials.Exists(c => c.AccessKey.Equals(accessKey));
                if (!exists) return false;
                Credential cred = _Credentials.Where(c => c.AccessKey.Equals(accessKey)).First();
                user = cred.User;
                return true;
            } 
        }

        public bool Get(string accessKey, out Credential credential)
        {
            credential = null;
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));

            lock (_CredentialLock)
            {
                bool exists = _Credentials.Exists(c => c.AccessKey.Equals(accessKey));
                if (!exists) return false;
                credential = _Credentials.Where(c => c.AccessKey.Equals(accessKey)).First();
                return true;
            }
        }

        public void Add(string user, string name, string accessKey, string secretKey, List<RequestType> permit)
        {
            if (String.IsNullOrEmpty(user)) throw new ArgumentNullException(nameof(user));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));
            if (permit == null || permit.Count < 1) throw new ArgumentException("At least one permission must be specified.");

            Credential newCred = new Credential(user, name, accessKey, secretKey, permit);
            bool added = false;

            lock (_CredentialLock)
            {
                bool exists = _Credentials.Exists(c => c.AccessKey.Equals(accessKey) && c.SecretKey.Equals(secretKey));
                if (!exists)
                {
                    _Credentials.Add(newCred);
                    added = true;
                }
            }

            if (added) Save();
        }

        public void Remove(string name, string accessKey, string secretKey)
        {
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));
             
            bool removed = false;

            lock (_CredentialLock)
            {
                bool exists = _Credentials.Exists(c => c.AccessKey.Equals(accessKey) && c.SecretKey.Equals(secretKey));
                if (exists)
                {
                    Credential cred = _Credentials.Where(c => c.AccessKey.Equals(accessKey) && c.SecretKey.Equals(secretKey)).First();
                    _Credentials.Remove(cred);
                    removed = true;
                }
            }

            if (removed) Save();
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
