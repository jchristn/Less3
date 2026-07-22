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
    /// Admin API DELETE handler.
    /// </summary>
    public class DeleteHandler
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

        internal DeleteHandler(
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
            else if (ctx.Http.Request.Url.Elements[1].Equals("tenants"))
            {
                await DeleteTenants(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("roles"))
            {
                await DeleteRoles(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("permissions"))
            {
                await DeletePermissions(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("roleassignments"))
            {
                await DeleteRoleAssignments(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("authsessions"))
            {
                await DeleteAuthSessions(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("authorizationaudit"))
            {
                await DeleteAuthorizationAudit(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("requesthistory"))
            {
                await DeleteRequestHistory(ctx);
                return;
            }

            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
        }

        #endregion

        #region Private-Methods

        private async Task DeleteBuckets(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Bucket bucket = _Config.GetBucketById(ctx.Http.Request.Url.Elements[2]);
            if (bucket == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            bool destroy = false;
            if (ctx.Http.Request.Query.Elements.AllKeys.Contains("destroy")) destroy = true;
            _Buckets.Remove(bucket, destroy);

            AdminMutationAuditor.Record(_Config, _Logging, ctx, bucket.TenantId, "Bucket", bucket.Id, "Delete");
            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
        }

        private async Task DeleteUsers(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            User user = _Config.GetUserById(ctx.Http.Request.Url.Elements[2]);
            if (user == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            _Config.DeleteUser(user.Id);

            AdminMutationAuditor.Record(_Config, _Logging, ctx, user.TenantId, "User", user.Id, "Delete");
            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
        }

        private async Task DeleteCredentials(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Credential cred = _Config.GetCredentialById(ctx.Http.Request.Url.Elements[2]);
            if (cred == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            _Config.DeleteCredential(cred.Id);

            AdminMutationAuditor.Record(_Config, _Logging, ctx, cred.TenantId, "Credential", cred.Id, "Delete");
            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
        }

        private async Task DeleteTenants(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            bool deleted = _Config.DeleteTenant(ctx.Http.Request.Url.Elements[2]);
            if (!deleted)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, ctx.Http.Request.Url.Elements[2], "Tenant", ctx.Http.Request.Url.Elements[2], "Delete");
            await SendNoContent(ctx).ConfigureAwait(false);
        }

        private async Task DeleteRoles(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            string tenantId = GetTenantId(ctx);

            try
            {
                bool deleted = _Config.DeleteRole(tenantId, ctx.Http.Request.Url.Elements[2]);
                if (!deleted)
                {
                    await SendNotFound(ctx).ConfigureAwait(false);
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                ctx.Response.StatusCode = 403;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send().ConfigureAwait(false);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, tenantId, "Role", ctx.Http.Request.Url.Elements[2], "Delete");
            await SendNoContent(ctx).ConfigureAwait(false);
        }

        private async Task DeletePermissions(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            string tenantId = GetTenantId(ctx);
            bool deleted = _Config.DeletePermission(tenantId, ctx.Http.Request.Url.Elements[2]);
            if (!deleted)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, tenantId, "Permission", ctx.Http.Request.Url.Elements[2], "Delete");
            await SendNoContent(ctx).ConfigureAwait(false);
        }

        private async Task DeleteRoleAssignments(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            string tenantId = GetTenantId(ctx);
            bool deleted = _Config.DeleteRoleAssignment(tenantId, ctx.Http.Request.Url.Elements[2]);
            if (!deleted)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, tenantId, "RoleAssignment", ctx.Http.Request.Url.Elements[2], "Delete");
            await SendNoContent(ctx).ConfigureAwait(false);
        }

        private async Task DeleteAuthSessions(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            string tenantId = GetTenantId(ctx);
            bool revoked = _Config.RevokeAuthSession(tenantId, ctx.Http.Request.Url.Elements[2]);
            if (!revoked)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, tenantId, "AuthSession", ctx.Http.Request.Url.Elements[2], "Revoke");
            await SendNoContent(ctx).ConfigureAwait(false);
        }

        private async Task DeleteAuthorizationAudit(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            string tenantId = GetTenantId(ctx);
            bool deleted = _Config.DeleteAuthorizationAudit(tenantId, ctx.Http.Request.Url.Elements[2]);
            if (!deleted)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            await SendNoContent(ctx).ConfigureAwait(false);
        }

        private async Task DeleteRequestHistory(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            RequestHistory entry = _Config.GetRequestHistoryById(ctx.Http.Request.Url.Elements[2]);
            if (entry == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            _Config.DeleteRequestHistory(entry.Id);

            AdminMutationAuditor.Record(_Config, _Logging, ctx, entry.TenantId, "RequestHistory", entry.Id, "Delete");
            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
        }

        private static string GetTenantId(S3Context ctx)
        {
            if (ctx.Http.Request.Query.Elements != null
                && ctx.Http.Request.Query.Elements.AllKeys.Contains("tenantId")
                && !String.IsNullOrEmpty(ctx.Http.Request.Query.Elements["tenantId"]))
            {
                return ctx.Http.Request.Query.Elements["tenantId"];
            }

            return "default";
        }

        private static async Task SendNoContent(S3Context ctx)
        {
            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send().ConfigureAwait(false);
        }

        private static async Task SendNotFound(S3Context ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send().ConfigureAwait(false);
        }

        #endregion
    }
}
