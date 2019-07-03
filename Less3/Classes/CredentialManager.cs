using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
 
using SyslogLogging;

namespace Less3.Classes
{
    /// <summary>
    /// Credential manager.
    /// </summary>
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

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param>
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

        /// <summary>
        /// Load credentials from filesystem.
        /// </summary>
        public void Load()
        {
            lock (_CredentialLock)
            {
                _Credentials = Common.DeserializeJson<List<Credential>>(Common.ReadTextFile(_Settings.Files.Credentials));
            }
        }

        /// <summary>
        /// Save credentials to filesystem.
        /// </summary>
        public void Save()
        {
            lock (_CredentialLock)
            {
                Common.WriteFile(_Settings.Files.Credentials, Encoding.UTF8.GetBytes(Common.SerializeJson(_Credentials, true)));
            }
        }

        /// <summary>
        /// Check of credentials exist.
        /// </summary>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param>
        /// <returns>True if exists.</returns>
        public bool Exists(string accessKey, string secretKey)
        {
            if (String.IsNullOrEmpty(accessKey)) return false;
            if (String.IsNullOrEmpty(secretKey)) return false;

            lock (_CredentialLock)
            {
                return _Credentials.Exists(c => c.AccessKey.Equals(accessKey) && c.SecretKey.Equals(secretKey));
            }
        }

        /// <summary>
        /// Retrieve user by access key.
        /// </summary>
        /// <param name="accessKey">Access key.</param>
        /// <param name="user">User.</param>
        /// <returns>True if successful.</returns>
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

        /// <summary>
        /// Retrieve credential by access key.
        /// </summary>
        /// <param name="accessKey">Access key.</param>
        /// <param name="credential">Credential.</param>
        /// <returns>True if successful.</returns>
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

        /// <summary>
        /// Add a credential.
        /// </summary>
        /// <param name="user">User name.</param>
        /// <param name="name">Name of the credential or description.</param>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param>
        /// <param name="permit">List of permitted actions.</param>
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

        /// <summary>
        /// Remove a credential.
        /// </summary>
        /// <param name="name">User name.</param>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param>
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
