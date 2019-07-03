using System;
using System.Collections.Generic;
using System.Text;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

using SyslogLogging;
using S3ServerInterface; 

using Less3.Classes;
using Less3.S3Responses;

namespace Less3.Api
{
    /// <summary>
    /// Service API callbacks.
    /// </summary>
    public class ServiceHandler
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings;
        private LoggingModule _Logging;
        private CredentialManager _Credentials;
        private BucketManager _Buckets;
        private AuthManager _Auth;
        private UserManager _Users;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param>
        /// <param name="credentials">CredentialManager.</param>
        /// <param name="buckets">BucketManager.</param>
        /// <param name="auth">AuthManager.</param>
        /// <param name="users">UserManager.</param>
        public ServiceHandler(
            Settings settings,
            LoggingModule logging,
            CredentialManager credentials,
            BucketManager buckets,
            AuthManager auth,
            UserManager users)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            if (buckets == null) throw new ArgumentNullException(nameof(buckets));
            if (auth == null) throw new ArgumentNullException(nameof(auth));
            if (users == null) throw new ArgumentNullException(nameof(users));

            _Settings = settings;
            _Logging = logging;
            _Credentials = credentials;
            _Buckets = buckets;
            _Auth = auth;
            _Users = users;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// List buckets API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ListBuckets(S3Request req)
        { 
            if (String.IsNullOrEmpty(req.AccessKey))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ServiceHandler ListBuckets no access key supplied");
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            Credential cred = null;
            if (!_Credentials.Get(req.AccessKey, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ServiceHandler ListBuckets unable to retrieve credentials for access key " + req.AccessKey);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            User user = null;
            if (!_Users.Get(cred.User, out user))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ServiceHandler ListBuckets unable to retrieve user for access key " + req.AccessKey);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            List<BucketConfiguration> buckets = null;
            _Buckets.GetUserBuckets(user.Name, out buckets);

            ListAllMyBucketsResult resp = new ListAllMyBucketsResult();
            resp.Owner = new S3Responses.Owner();
            resp.Owner.DisplayName = user.Name;
            resp.Owner.ID = user.Name;

            resp.Buckets = new Buckets();
            resp.Buckets.Bucket = new List<Bucket>();

            foreach (BucketConfiguration curr in buckets)
            {
                Bucket b = new Bucket();
                b.Name = curr.Name;
                b.CreationDate = curr.CreatedUtc;
                resp.Buckets.Bucket.Add(b);
            }
              
            return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(resp))); 
        }

        #endregion

        #region Private-Methods

        private string AmazonTimestamp(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffz");
        }

        #endregion
    }
}
