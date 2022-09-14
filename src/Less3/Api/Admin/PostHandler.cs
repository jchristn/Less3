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

        internal async Task Process(S3Context ctx)
        { 
            if (ctx.Http.Request.Url.Elements[1].Equals("buckets"))
            {
                await PostBuckets(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("users"))
            {
                await PostUsers(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("credentials"))
            {
                await PostCredentials(ctx);
                return;
            }

            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
        }

        #endregion

        #region Private-Methods

        private async Task PostBuckets(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 2)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            byte[] data = null;
            Bucket bucket = null;

            try
            {
                data = Common.StreamToBytes(ctx.Request.Data);
                bucket = SerializationHelper.DeserializeJson<Bucket>(data);
            }
            catch (Exception)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Bucket tempBucket = _Config.GetBucketByName(bucket.Name);
            if (tempBucket != null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.BucketAlreadyExists);
                return;
            }
             
            _Buckets.Add(bucket);

            ctx.Response.StatusCode = 201;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
        }

        private async Task PostUsers(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 2)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            byte[] data = null;
            User user = null;

            try
            {
                data = Common.StreamToBytes(ctx.Request.Data);
                user = SerializationHelper.DeserializeJson<User>(data);
            }
            catch (Exception)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            User tempUser = _Config.GetUserByEmail(user.Email);
            if (tempUser != null)
            {
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            tempUser = _Config.GetUserByGuid(user.GUID);
            if (tempUser != null)
            {
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            _Config.AddUser(user);

            ctx.Response.StatusCode = 201;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
        }

        private async Task PostCredentials(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 2)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            byte[] data = null;
            Credential cred = null;

            try
            {
                data = Common.StreamToBytes(ctx.Request.Data);
                cred = SerializationHelper.DeserializeJson<Credential>(data);
            }
            catch (Exception)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Credential tempCred = _Config.GetCredentialByAccessKey(cred.AccessKey);
            if (tempCred != null)
            {
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            _Config.AddCredential(cred);

            ctx.Response.StatusCode = 201;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
        }

        #endregion
    }
}
