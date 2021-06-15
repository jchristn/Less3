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

        internal async Task Process(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements[1].Equals("buckets"))
            {
                await DeleteBuckets(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("users"))
            {
                await DeleteUsers(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("credentials"))
            {
                await DeleteCredentials(ctx);
                return;
            }

            await ctx.Response.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
        }

        #endregion

        #region Private-Methods

        private async Task DeleteBuckets(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Bucket bucket = _Config.GetBucketByName(ctx.Http.Request.Url.Elements[2]);
            if (bucket == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            bool destroy = false;
            if (ctx.Http.Request.Query.Elements.ContainsKey("destroy")) destroy = true; 
            _Buckets.Remove(bucket, destroy); 

            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
        }

        private async Task DeleteUsers(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            User user = _Config.GetUserByGuid(ctx.Http.Request.Url.Elements[2]);
            if (user == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            _Config.DeleteUser(user.GUID);

            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
        }

        private async Task DeleteCredentials(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerInterface.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Credential cred = _Config.GetCredentialByAccessKey(ctx.Http.Request.Url.Elements[2]);
            if (cred == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            _Config.DeleteCredential(cred.GUID);

            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
        }

        #endregion
    }
}
