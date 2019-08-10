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
    /// Admin API GET handler.
    /// </summary>
    public class GetHandler
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
        public GetHandler(
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
                return GetBuckets(req);
            }
            else if (req.RawUrlEntries[1].Equals("users"))
            {
                return GetUsers(req);
            }
            else if (req.RawUrlEntries[1].Equals("credentials"))
            {
                return GetCredentials(req);
            }

            return resp;
        }

        #endregion

        #region Private-Methods

        private S3Response GetBuckets(S3Request req)
        {
            if (req.RawUrlEntries.Count >= 3)
            { 
                BucketConfiguration config = null;
                if (!_Buckets.Get(req.RawUrlEntries[2], out config))
                {
                    return new S3Response(req, 404, "text/plain", null, null);
                }
                else
                {
                    return new S3Response(req, 200, "application/json", null, Encoding.UTF8.GetBytes(Common.SerializeJson(config, true)));
                }
            }
            else
            {
                List<BucketConfiguration> configs = null;
                _Config.GetBuckets(out configs);
                return new S3Response(req, 200, "application/json", null, Encoding.UTF8.GetBytes(Common.SerializeJson(configs, true)));
            }
        }

        private S3Response GetUsers(S3Request req)
        {
            if (req.RawUrlEntries.Count >= 3)
            {
                User user = null;
                if (!_Config.GetUserByName(req.RawUrlEntries[2], out user))
                {
                    return new S3Response(req, 404, "text/plain", null, null);
                }
                else
                {
                    return new S3Response(req, 200, "application/json", null, Encoding.UTF8.GetBytes(Common.SerializeJson(user, true)));
                }
            }
            else
            {
                List<User> users = null;
                _Config.GetUsers(out users);
                return new S3Response(req, 200, "application/json", null, Encoding.UTF8.GetBytes(Common.SerializeJson(users, true)));
            }
        }

        private S3Response GetCredentials(S3Request req)
        {
            if (req.RawUrlEntries.Count >= 3)
            {
                Credential cred = null;
                if (!_Config.GetCredentialByAccessKey(req.RawUrlEntries[2], out cred))
                {
                    return new S3Response(req, 404, "text/plain", null, null);
                }
                else
                {
                    return new S3Response(req, 200, "application/json", null, Encoding.UTF8.GetBytes(Common.SerializeJson(cred, true)));
                }
            }
            else
            {
                List<Credential> creds = null;
                _Config.GetCredentials(out creds);
                return new S3Response(req, 200, "application/json", null, Encoding.UTF8.GetBytes(Common.SerializeJson(creds, true)));
            }
        }

        #endregion
    }
}
