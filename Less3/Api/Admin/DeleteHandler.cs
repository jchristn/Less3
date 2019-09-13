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
    /// Admin API DELETE handler.
    /// </summary>
    public class DeleteHandler
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

        internal DeleteHandler(
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
                await DeleteBuckets(req, resp);
                return;
            }
            else if (req.RawUrlEntries[1].Equals("users"))
            {
                await DeleteUsers(req, resp);
                return;
            }
            else if (req.RawUrlEntries[1].Equals("credentials"))
            {
                await DeleteCredentials(req, resp);
                return;
            }

            await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
        }

        #endregion

        #region Private-Methods

        private async Task DeleteBuckets(S3Request req, S3Response resp)
        {
            if (req.RawUrlEntries.Count != 3)
            {
                await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            BucketConfiguration config = null;
            if (!_Config.GetBucketByName(req.RawUrlEntries[2], out config))
            {
                resp.StatusCode = 404;
                resp.ContentType = "text/plain";
                await resp.Send();
                return;
            }
                
            _Config.DeleteBucket(config.GUID);

            resp.StatusCode = 204;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
        }

        private async Task DeleteUsers(S3Request req, S3Response resp)
        {
            if (req.RawUrlEntries.Count != 3)
            {
                await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            User user = null;
            if (!_Config.GetUserByName(req.RawUrlEntries[2], out user))
            {
                resp.StatusCode = 404;
                resp.ContentType = "text/plain";
                await resp.Send();
                return;
            }

            _Config.DeleteUser(user.GUID);

            resp.StatusCode = 204;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
        }

        private async Task DeleteCredentials(S3Request req, S3Response resp)
        {
            if (req.RawUrlEntries.Count != 3)
            {
                await resp.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Credential cred = null;
            if (!_Config.GetCredentialByAccessKey(req.RawUrlEntries[2], out cred))
            {
                resp.StatusCode = 404;
                resp.ContentType = "text/plain";
                await resp.Send();
                return;
            }

            _Config.DeleteCredential(cred.GUID);

            resp.StatusCode = 204;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
        }

        #endregion
    }
}
