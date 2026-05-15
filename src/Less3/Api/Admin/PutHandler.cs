namespace Less3.Api.Admin
{
    using System;
    using System.Threading.Tasks;

    using S3ServerLibrary;
    using SyslogLogging;

    using Less3.Classes;
    using Less3.Settings;

    /// <summary>
    /// Admin API PUT handler.
    /// </summary>
    internal class PutHandler
    {
        private SettingsBase _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;
        private BucketManager _Buckets;
        private AuthManager _Auth;

        internal PutHandler(
            SettingsBase settings,
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

        internal async Task Process(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements[1].Equals("users"))
            {
                await PutUsers(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("credentials"))
            {
                await PutCredentials(ctx);
                return;
            }

            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
        }

        private async Task PutUsers(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            User existing = _Config.GetUserByGuid(ctx.Http.Request.Url.Elements[2]);
            if (existing == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            User user = null;

            try
            {
                user = SerializationHelper.DeserializeJson<User>(ctx.Request.DataAsString);
            }
            catch (Exception)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            if (user == null || String.IsNullOrEmpty(user.Name) || String.IsNullOrEmpty(user.Email))
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            user.GUID = existing.GUID;
            user.CreatedUtc = existing.CreatedUtc;

            bool updated = _Config.UpdateUser(user);
            if (!updated)
            {
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(user, true));
        }

        private async Task PutCredentials(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Credential existing = _Config.GetCredentialByGuid(ctx.Http.Request.Url.Elements[2]);
            if (existing == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            Credential cred = null;

            try
            {
                cred = SerializationHelper.DeserializeJson<Credential>(ctx.Request.DataAsString);
            }
            catch (Exception)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            if (cred == null
                || String.IsNullOrEmpty(cred.UserGUID)
                || String.IsNullOrEmpty(cred.AccessKey)
                || String.IsNullOrEmpty(cred.SecretKey))
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            cred.GUID = existing.GUID;
            cred.CreatedUtc = existing.CreatedUtc;

            bool updated = _Config.UpdateCredential(cred);
            if (!updated)
            {
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(cred, true));
        }
    }
}
