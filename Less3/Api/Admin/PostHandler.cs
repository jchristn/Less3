using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using S3ServerInterface;
using SyslogLogging;

using Less3.Classes;

namespace Less3.Api.Admin
{
    /// <summary>
    /// Admin API POST handler.
    /// </summary>
    public class PostHandler
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
        public PostHandler(
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
        /// Process the API request.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response Process(S3Request req)
        {
            S3Response resp = new S3Response(req, 400, "text/plain", null, null);

            if (req.RawUrlEntries[1].Equals("buckets"))
            {
                return PostBuckets(req);
            }
            else if (req.RawUrlEntries[1].Equals("users"))
            {
                return PostUsers(req);
            }
            else if (req.RawUrlEntries[1].Equals("credentials"))
            {
                return PostCredentials(req);
            }

            return resp;
        }

        #endregion

        #region Private-Methods

        private S3Response PostBuckets(S3Request req)
        {
            S3Response resp = new S3Response(req, 400, "text/plain", null, null);
            if (req.RawUrlEntries.Count != 2) return resp;

            BucketConfiguration config = null;

            try
            {
                req.Data = Common.StreamToBytes(req.DataStream);
                config = Common.DeserializeJson<BucketConfiguration>(req.Data);
            }
            catch (Exception)
            {
                return resp;
            }

            BucketConfiguration tempConfig = null;
            if (_Config.GetBucketByName(config.Name, out tempConfig))
                return new S3Response(req, 409, "text/plain", null, null);

            _Config.AddBucket(config);
            return new S3Response(req, 204, "text/plain", null, null);
        }

        private S3Response PostUsers(S3Request req)
        {
            S3Response resp = new S3Response(req, 400, "text/plain", null, null);
            if (req.RawUrlEntries.Count != 2) return resp;

            User user = null;

            try
            {
                req.Data = Common.StreamToBytes(req.DataStream);
                user = Common.DeserializeJson<User>(req.Data);
            }
            catch (Exception)
            {
                return resp;
            }

            User tempUser = null;
            if (_Config.GetUserByName(user.Name, out tempUser))
                return new S3Response(req, 409, "text/plain", null, null);

            _Config.AddUser(user);
            return new S3Response(req, 204, "text/plain", null, null);
        }

        private S3Response PostCredentials(S3Request req)
        {
            S3Response resp = new S3Response(req, 400, "text/plain", null, null);
            if (req.RawUrlEntries.Count != 2) return resp;

            Credential cred = null;

            try
            {
                req.Data = Common.StreamToBytes(req.DataStream);
                cred = Common.DeserializeJson<Credential>(req.Data);
            }
            catch (Exception)
            {
                return resp;
            }

            Credential tempCred = null;
            if (_Config.GetCredentialByAccessKey(cred.AccessKey, out tempCred))
                return new S3Response(req, 409, "text/plain", null, null);

            _Config.AddCredential(cred);
            return new S3Response(req, 204, "text/plain", null, null);
        }

        #endregion
    }
}
