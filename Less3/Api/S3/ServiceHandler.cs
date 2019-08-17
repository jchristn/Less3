using System;
using System.Collections.Generic;
using System.Text;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

using SyslogLogging;
using S3ServerInterface;
using S3ServerInterface.S3Objects;

using Less3.Classes; 

namespace Less3.Api.S3
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
        private ConfigManager _Config;
        private BucketManager _Buckets;
        private AuthManager _Auth; 

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param> 
        /// <param name="config">ConfigManager.</param>
        /// <param name="buckets">BucketManager.</param>
        /// <param name="auth">AuthManager.</param> 
        public ServiceHandler(
            Settings settings,
            LoggingModule logging, 
            ConfigManager config,
            BucketManager buckets,
            AuthManager auth)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (buckets == null) throw new ArgumentNullException(nameof(buckets));
            if (auth == null) throw new ArgumentNullException(nameof(auth)); 

            _Settings = settings;
            _Logging = logging;
            _Config = config;
            _Buckets = buckets;
            _Auth = auth; 
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
                _Logging.Warn("ServiceHandler ListBuckets no access key supplied");
                return new S3Response(req, ErrorCode.AccessDenied); 
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeServiceRequest(
                RequestType.ServiceListBuckets,
                req,
                user,
                cred,
                out authResult))
            {
                _Logging.Warn("ServiceHandler ListBuckets authentication or authorization failed");
                return new S3Response(req, ErrorCode.AccessDenied); 
            }

            List<BucketConfiguration> buckets = null;
            _Buckets.GetUserBuckets(user.GUID, out buckets);

            ListAllMyBucketsResult resp = new ListAllMyBucketsResult();
            resp.Owner = new S3ServerInterface.S3Objects.Owner();
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
              
            return new S3Response(req, 200, "application/xml", null, 
                Encoding.UTF8.GetBytes(Common.SerializeXml<ListAllMyBucketsResult>(resp, false))); 
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
