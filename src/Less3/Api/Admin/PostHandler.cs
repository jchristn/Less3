namespace Less3.Api.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using S3ServerLibrary;
    using SyslogLogging;

    using Less3.Classes;
    using Less3.Settings;

    /// <summary>
    /// Admin API POST handler.
    /// </summary>
    internal class PostHandler
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SettingsBase _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;
        private BucketManager _Buckets;
        private AuthManager _Auth;

        #endregion

        #region Constructors-and-Factories
        
        internal PostHandler(
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
            else if (ctx.Http.Request.Url.Elements[1].Equals("tenants"))
            {
                await PostTenants(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("roles"))
            {
                await PostRoles(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("permissions"))
            {
                await PostPermissions(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("roleassignments"))
            {
                await PostRoleAssignments(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("authsessions"))
            {
                await PostAuthSessions(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("authorizationaudit"))
            {
                await PostAuthorizationAudit(ctx);
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

            Bucket bucket = null;

            try
            {
                bucket = SerializationHelper.DeserializeJson<Bucket>(ctx.Request.DataAsString);
            }
            catch (Exception)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Bucket tempBucket = _Config.GetBucketByName(bucket.TenantId, bucket.Name);
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

            User tempUser = _Config.GetUserByEmail(user.TenantId, user.Email);
            if (tempUser != null)
            {
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            tempUser = _Config.GetUserById(user.Id);
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

            Credential tempCred = _Config.GetCredentialByAccessKey(cred.AccessKey);
            if (tempCred != null)
            {
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            User user = _Config.GetUserById(cred.TenantId, cred.UserId);
            if (user == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            _Config.AddCredential(cred);

            ctx.Response.StatusCode = 201;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
        }

        private async Task PostTenants(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 2)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Tenant tenant = Deserialize<Tenant>(ctx);
            if (tenant == null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            if (!_Config.AddTenant(tenant))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            await SendCreated(ctx, tenant).ConfigureAwait(false);
        }

        private async Task PostRoles(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 2)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Role role = Deserialize<Role>(ctx);
            if (role == null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            if (String.IsNullOrEmpty(role.TenantId)) role.TenantId = "default";
            role.IsBuiltIn = false;

            if (!_Config.AddRole(role))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            await SendCreated(ctx, role).ConfigureAwait(false);
        }

        private async Task PostPermissions(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 2)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Permission permission = Deserialize<Permission>(ctx);
            if (permission == null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            if (String.IsNullOrEmpty(permission.TenantId)) permission.TenantId = "default";

            if (!_Config.AddPermission(permission))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            await SendCreated(ctx, permission).ConfigureAwait(false);
        }

        private async Task PostRoleAssignments(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 2)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            RoleAssignment assignment = Deserialize<RoleAssignment>(ctx);
            if (assignment == null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            if (String.IsNullOrEmpty(assignment.TenantId)) assignment.TenantId = "default";

            if (!_Config.AddRoleAssignment(assignment))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            await SendCreated(ctx, assignment).ConfigureAwait(false);
        }

        private async Task PostAuthSessions(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 2)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            AuthSession session = Deserialize<AuthSession>(ctx);
            if (session == null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            if (String.IsNullOrEmpty(session.TenantId)) session.TenantId = "default";

            if (!_Config.AddAuthSession(session))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            await SendCreated(ctx, session).ConfigureAwait(false);
        }

        private async Task PostAuthorizationAudit(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 2)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            AuthorizationAudit audit = Deserialize<AuthorizationAudit>(ctx);
            if (audit == null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            if (!_Config.AddAuthorizationAudit(audit))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            await SendCreated(ctx, audit).ConfigureAwait(false);
        }

        private static T Deserialize<T>(S3Context ctx)
        {
            try
            {
                return SerializationHelper.DeserializeJson<T>(ctx.Request.DataAsString);
            }
            catch
            {
                return default(T);
            }
        }

        private static async Task SendCreated(S3Context ctx, object obj)
        {
            ctx.Response.StatusCode = 201;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(obj, true)).ConfigureAwait(false);
        }

        private static async Task SendConflict(S3Context ctx)
        {
            ctx.Response.StatusCode = 409;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send().ConfigureAwait(false);
        }

        #endregion
    }
}
