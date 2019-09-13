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
    /// Admin API GET handler.
    /// </summary>
    internal class GetHandler
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

        internal GetHandler(
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
                await GetBuckets(req, resp);
                return;
            }
            else if (req.RawUrlEntries[1].Equals("users"))
            {
                await GetUsers(req, resp);
                return;
            }
            else if (req.RawUrlEntries[1].Equals("credentials"))
            {
                await GetCredentials(req, resp);
                return;
            }

            await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
        }

        #endregion

        #region Private-Methods

        private async Task GetBuckets(S3Request req, S3Response resp)
        {
            if (req.RawUrlEntries.Count >= 3)
            { 
                BucketConfiguration config = null;
                if (!_Buckets.Get(req.RawUrlEntries[2], out config))
                {
                    resp.StatusCode = 404;
                    resp.ContentType = "text/plain";
                    await resp.Send();
                    return;
                }
                else
                {
                    resp.StatusCode = 200;
                    resp.ContentType = "application/json";
                    await resp.Send(Common.SerializeJson(config, true));
                    return;
                }
            }
            else
            {
                List<BucketConfiguration> configs = null;
                _Config.GetBuckets(out configs);

                resp.StatusCode = 200;
                resp.ContentType = "application/json";
                await resp.Send(Common.SerializeJson(configs, true));
                return;
            }
        }

        private async Task GetUsers(S3Request req, S3Response resp)
        {
            if (req.RawUrlEntries.Count >= 3)
            {
                User user = null;
                if (!_Config.GetUserByName(req.RawUrlEntries[2], out user))
                {
                    resp.StatusCode = 404;
                    resp.ContentType = "text/plain";
                    await resp.Send();
                    return;
                }
                else
                {
                    resp.StatusCode = 200;
                    resp.ContentType = "application/json";
                    await resp.Send(Common.SerializeJson(user, true));
                    return;
                }
            }
            else
            {
                List<User> users = null;
                _Config.GetUsers(out users);

                resp.StatusCode = 200;
                resp.ContentType = "application/json";
                await resp.Send(Common.SerializeJson(users, true));
                return;
            }
        }

        private async Task GetCredentials(S3Request req, S3Response resp)
        {
            if (req.RawUrlEntries.Count >= 3)
            {
                Credential cred = null;
                if (!_Config.GetCredentialByAccessKey(req.RawUrlEntries[2], out cred))
                {
                    resp.StatusCode = 404;
                    resp.ContentType = "text/plain";
                    await resp.Send();
                    return;
                }
                else
                {
                    resp.StatusCode = 200;
                    resp.ContentType = "application/json";
                    await resp.Send(Common.SerializeJson(cred, true));
                    return;
                }
            }
            else
            {
                List<Credential> creds = null;
                _Config.GetCredentials(out creds);

                resp.StatusCode = 200;
                resp.ContentType = "application/json";
                await resp.Send(Common.SerializeJson(creds, true));
                return;
            }
        }

        #endregion
    }
}
