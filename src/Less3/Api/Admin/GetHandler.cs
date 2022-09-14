using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using S3ServerLibrary;
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
         
        internal async Task Process(S3Context ctx)
        { 
            if (ctx.Http.Request.Url.Elements[1].Equals("buckets"))
            {
                await GetBuckets(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("users"))
            {
                await GetUsers(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("credentials"))
            {
                await GetCredentials(ctx);
                return;
            }

            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
        }

        #endregion

        #region Private-Methods

        private async Task GetBuckets(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                Bucket bucket = _Buckets.GetByGuid(ctx.Http.Request.Url.Elements[2]);
                if (bucket == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.Send();
                    return;
                }
                else
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(SerializationHelper.SerializeJson(bucket, true));
                    return;
                }
            }
            else
            {
                List<Bucket> buckets = _Config.GetBuckets();
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(SerializationHelper.SerializeJson(buckets, true));
                return;
            }
        }

        private async Task GetUsers(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                User user = _Config.GetUserByGuid(ctx.Http.Request.Url.Elements[2]);
                if (user == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.Send();
                    return;
                }
                else
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(SerializationHelper.SerializeJson(user, true));
                    return;
                }
            }
            else
            {
                List<User> users = _Config.GetUsers(); 
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(SerializationHelper.SerializeJson(users, true));
                return;
            }
        }

        private async Task GetCredentials(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length >= 3)
            {
                Credential cred = _Config.GetCredentialByGuid(ctx.Http.Request.Url.Elements[2]);
                if (cred == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.Send();
                    return;
                }
                else
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send(SerializationHelper.SerializeJson(cred, true));
                    return;
                }
            }
            else
            {
                List<Credential> creds = _Config.GetCredentials(); 
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(SerializationHelper.SerializeJson(creds, true));
                return;
            }
        }

        #endregion
    }
}
