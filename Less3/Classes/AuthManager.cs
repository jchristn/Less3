using System;
using System.Collections.Generic;
using System.Text;

using S3ServerInterface;
using SyslogLogging;

namespace Less3.Classes
{
    /// <summary>
    /// Authentication manager.
    /// </summary>
    public class AuthManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings;
        private LoggingModule _Logging;
        private UserManager _Users;
        private CredentialManager _Credentials;
        private BucketManager _Buckets;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public AuthManager()
        {

        }

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param>
        /// <param name="users">UserManager.</param>
        /// <param name="credentials">CredentialManager.</param>
        /// <param name="buckets">BucketManager.</param>
        public AuthManager(
            Settings settings, 
            LoggingModule logging, 
            UserManager users, 
            CredentialManager credentials,
            BucketManager buckets)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (users == null) throw new ArgumentNullException(nameof(users));
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            if (buckets == null) throw new ArgumentNullException(nameof(buckets));

            _Settings = settings;
            _Logging = logging;
            _Users = users;
            _Credentials = credentials;
            _Buckets = buckets;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Authenticate the operation.  This should not be used to authorize access to the requested resource but rather to authenticate the user.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <param name="cred">Credential.</param>
        /// <param name="user">User.</param>
        /// <returns>True if authenticated and the requested operation is supported for this user.</returns>
        public bool Authenticate(S3Request req, out User user, out Credential cred)
        {
            user = null;
            cred = null;
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (String.IsNullOrEmpty(req.AccessKey))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "AuthManager Authenticate no access key supplied");
                return false;
            }

            string userName = null;
            if (!_Credentials.GetUser(req.AccessKey, out userName))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "AuthManager Authenticate unable to find user for access key " + req.AccessKey);
                return false;
            }
             
            if (!_Users.Get(userName, out user))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "AuthManager Authenticate unable to retrieve user for access key " + req.AccessKey);
                return false;
            }

            if (!_Credentials.Get(req.AccessKey, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "AuthManager Authenticate unable to find credentials for access key " + req.AccessKey);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Authorize access to a resource.  Should be performed after authentication and dictate whether or not the resource can be accessed by the requestor.
        /// </summary>
        /// <param name="reqType">RequestType.</param>
        /// <param name="req">S3Request.</param>
        /// <param name="user">User.</param>
        /// <param name="cred">Credential.</param>
        /// <returns>True if authorized against the supplied bucket.</returns>
        public bool Authorize(RequestType reqType, S3Request req, User user, Credential cred)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (cred == null) throw new ArgumentNullException(nameof(cred));

            if (cred.Permit == null || cred.Permit.Count < 1)
            {
                _Logging.Log(LoggingModule.Severity.Warn, "AuthManager Authorize no permitted operations for access key " + req.AccessKey);
                return false;
            }

            if (cred.Permit.Contains(RequestType.Admin)) return true;
            if (cred.Permit.Contains(reqType)) return true;

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "AuthManager Authorize unable to find bucket " + req.Bucket);
                return false;
            }

            if (bucket.Owner.Equals(req.AccessKey)) return true;

            if (bucket.PermittedAccessKeys != null && bucket.PermittedAccessKeys.Count > 0)
            {
                if (bucket.PermittedAccessKeys.Contains(req.AccessKey)) return true;
            }

            _Logging.Log(LoggingModule.Severity.Warn, "AuthManager Authorize unable to authorize access key " + req.AccessKey + ": " + reqType.ToString() + " " + req.Bucket);
            return false;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
