namespace Less3.Api.Admin
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using S3ServerLibrary;
    using SyslogLogging;

    using Less3.Classes;
    using Less3.Helpers;
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
            else if (ctx.Http.Request.Url.Elements[1].Equals("tenants"))
            {
                await PutTenants(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("roles"))
            {
                await PutRoles(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("permissions"))
            {
                await PutPermissions(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("roleassignments"))
            {
                await PutRoleAssignments(ctx);
                return;
            }
            else if (ctx.Http.Request.Url.Elements[1].Equals("authsessions"))
            {
                await PutAuthSessions(ctx);
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

            User existing = _Config.GetUserById(ctx.Http.Request.Url.Elements[2]);
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

            user.Id = existing.Id;
            if (String.IsNullOrEmpty(user.TenantId)) user.TenantId = existing.TenantId;
            user.CreatedUtc = existing.CreatedUtc;

            bool updated = _Config.UpdateUser(user);
            if (!updated)
            {
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, user.TenantId, "User", user.Id, "Update");
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

            Credential existing = _Config.GetCredentialById(ctx.Http.Request.Url.Elements[2]);
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
                || String.IsNullOrEmpty(cred.UserId)
                || String.IsNullOrEmpty(cred.AccessKey))
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            cred.Id = existing.Id;
            if (String.IsNullOrEmpty(cred.TenantId)) cred.TenantId = existing.TenantId;
            if (String.IsNullOrEmpty(cred.SecretKey)) cred.SecretKey = existing.SecretKey;
            cred.CreatedUtc = existing.CreatedUtc;

            bool updated = _Config.UpdateCredential(cred);
            if (!updated)
            {
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "text/plain";
                await ctx.Response.Send();
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, cred.TenantId, "Credential", cred.Id, "Update");
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(CredentialResponseSanitizer.ForResponse(cred, false), true));
        }

        private async Task PutTenants(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            Tenant existing = _Config.GetTenantById(ctx.Http.Request.Url.Elements[2]);
            if (existing == null)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            Tenant tenant = Deserialize<Tenant>(ctx);
            if (tenant == null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            tenant.Id = existing.Id;
            tenant.CreatedUtc = existing.CreatedUtc;

            if (!_Config.UpdateTenant(tenant))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, tenant.Id, "Tenant", tenant.Id, "Update");
            await SendJson(ctx, tenant).ConfigureAwait(false);
        }

        private async Task PutRoles(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            string tenantId = GetTenantId(ctx);
            Role existing = _Config.GetRoleById(tenantId, ctx.Http.Request.Url.Elements[2]);
            if (existing == null)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            Role role = Deserialize<Role>(ctx);
            if (role == null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            role.Id = existing.Id;
            role.TenantId = String.IsNullOrEmpty(role.TenantId) ? tenantId : role.TenantId;
            role.CreatedUtc = existing.CreatedUtc;

            try
            {
                if (!_Config.UpdateRole(role))
                {
                    await SendConflict(ctx).ConfigureAwait(false);
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

            AdminMutationAuditor.Record(_Config, _Logging, ctx, role.TenantId, "Role", role.Id, "Update");
            await SendJson(ctx, role).ConfigureAwait(false);
        }

        private async Task PutPermissions(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            string tenantId = GetTenantId(ctx);
            Permission existing = _Config.GetPermissionById(tenantId, ctx.Http.Request.Url.Elements[2]);
            if (existing == null)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            Permission permission = Deserialize<Permission>(ctx);
            if (permission == null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            permission.Id = existing.Id;
            permission.TenantId = String.IsNullOrEmpty(permission.TenantId) ? tenantId : permission.TenantId;
            permission.CreatedUtc = existing.CreatedUtc;

            if (!_Config.UpdatePermission(permission))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, permission.TenantId, "Permission", permission.Id, "Update");
            await SendJson(ctx, permission).ConfigureAwait(false);
        }

        private async Task PutRoleAssignments(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            string tenantId = GetTenantId(ctx);
            RoleAssignment existing = _Config.GetRoleAssignmentById(tenantId, ctx.Http.Request.Url.Elements[2]);
            if (existing == null)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            RoleAssignment assignment = Deserialize<RoleAssignment>(ctx);
            if (assignment == null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            assignment.Id = existing.Id;
            assignment.TenantId = String.IsNullOrEmpty(assignment.TenantId) ? existing.TenantId : assignment.TenantId;
            assignment.CreatedUtc = existing.CreatedUtc;

            if (!_Config.UpdateRoleAssignment(assignment))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, assignment.TenantId, "RoleAssignment", assignment.Id, "Update");
            await SendJson(ctx, assignment).ConfigureAwait(false);
        }

        private async Task PutAuthSessions(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            string tenantId = GetTenantId(ctx);
            AuthSession existing = _Config.GetAuthSessionById(tenantId, ctx.Http.Request.Url.Elements[2]);
            if (existing == null)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            AuthSession session = Deserialize<AuthSession>(ctx);
            if (session == null)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            session.Id = existing.Id;
            session.TenantId = String.IsNullOrEmpty(session.TenantId) ? existing.TenantId : session.TenantId;
            session.CreatedUtc = existing.CreatedUtc;

            if (!_Config.UpdateAuthSession(session))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, session.TenantId, "AuthSession", session.Id, "Update");
            await SendJson(ctx, session).ConfigureAwait(false);
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

        private static async Task SendJson(S3Context ctx, object obj)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(obj, true)).ConfigureAwait(false);
        }

        private static async Task SendNotFound(S3Context ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send().ConfigureAwait(false);
        }

        private static async Task SendConflict(S3Context ctx)
        {
            ctx.Response.StatusCode = 409;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send().ConfigureAwait(false);
        }
    }
}
