using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        internal ServiceHandler(
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

        #region Internal-Methods
         
        internal async Task ListBuckets(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            if (String.IsNullOrEmpty(req.AccessKey))
            {
                _Logging.Warn(header + "ListBuckets no access key supplied");
                await resp.Send(ErrorCode.AccessDenied);
                return;
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
                _Logging.Warn(header + "ListBuckets authentication or authorization failed");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            List<BucketConfiguration> buckets = null;
            _Buckets.GetUserBuckets(user.GUID, out buckets);

            ListAllMyBucketsResult listBucketsResult = new ListAllMyBucketsResult();
            listBucketsResult.Owner = new S3ServerInterface.S3Objects.Owner();
            listBucketsResult.Owner.DisplayName = user.Name;
            listBucketsResult.Owner.ID = user.Name;

            listBucketsResult.Buckets = new Buckets();
            listBucketsResult.Buckets.Bucket = new List<Bucket>();

            foreach (BucketConfiguration curr in buckets)
            {
                Bucket b = new Bucket();
                b.Name = curr.Name;
                b.CreationDate = curr.CreatedUtc;
                listBucketsResult.Buckets.Bucket.Add(b);
            }
             
            resp.StatusCode = 200;
            resp.ContentType = "application/xml";
            await resp.Send(Common.SerializeXml<ListAllMyBucketsResult>(listBucketsResult, false));
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
