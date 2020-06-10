using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using S3ServerInterface;
using SyslogLogging;

using Less3.Classes;

namespace Less3.Api.Admin
{
    /// <summary>
    /// Admin API POST handler.
    /// </summary>
    internal class PostHandler
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
        
        internal PostHandler(
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

        internal async Task Process(S3Request req, S3Response resp)
        { 
            if (req.RawUrlEntries[1].Equals("buckets"))
            {
                await PostBuckets(req, resp);
                return;
            }
            else if (req.RawUrlEntries[1].Equals("users"))
            {
                await PostUsers(req, resp);
                return;
            }
            else if (req.RawUrlEntries[1].Equals("credentials"))
            {
                await PostCredentials(req, resp);
                return;
            }

            await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
        }

        #endregion

        #region Private-Methods

        private async Task PostBuckets(S3Request req, S3Response resp)
        {
            if (req.RawUrlEntries.Count != 2)
            {
                await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            byte[] data = null;
            Bucket bucket = null;

            try
            {
                data = Common.StreamToBytes(req.Data);
                bucket = Common.DeserializeJson<Bucket>(data);
            }
            catch (Exception)
            {
                await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Bucket tempBucket = _Config.GetBucketByName(bucket.Name);
            if (tempBucket != null)
            {
                await resp.Send(S3ServerInterface.S3Objects.ErrorCode.BucketAlreadyExists);
                return;
            }

            _Config.AddBucket(bucket); 
            resp.StatusCode = 201;
            resp.ContentType = "text/plain";
            await resp.Send();
        }

        private async Task PostUsers(S3Request req, S3Response resp)
        {
            if (req.RawUrlEntries.Count != 2)
            {
                await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            byte[] data = null;
            User user = null;

            try
            {
                data = Common.StreamToBytes(req.Data);
                user = Common.DeserializeJson<User>(data);
            }
            catch (Exception)
            {
                await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            User tempUser = _Config.GetUserByName(user.Name);
            if (tempUser != null)
            {
                resp.StatusCode = 409;
                resp.ContentType = "text/plain";
                await resp.Send();
                return;
            }

            _Config.AddUser(user);

            resp.StatusCode = 201;
            resp.ContentType = "text/plain";
            await resp.Send();
        }

        private async Task PostCredentials(S3Request req, S3Response resp)
        {
            if (req.RawUrlEntries.Count != 2)
            {
                await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            byte[] data = null;
            Credential cred = null;

            try
            {
                data = Common.StreamToBytes(req.Data);
                cred = Common.DeserializeJson<Credential>(data);
            }
            catch (Exception)
            {
                await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Credential tempCred = _Config.GetCredentialByAccessKey(cred.AccessKey);
            if (tempCred != null)
            {
                resp.StatusCode = 409;
                resp.ContentType = "text/plain";
                await resp.Send();
                return;
            }

            _Config.AddCredential(cred);

            resp.StatusCode = 201;
            resp.ContentType = "text/plain";
            await resp.Send();
        }

        #endregion
    }
}
