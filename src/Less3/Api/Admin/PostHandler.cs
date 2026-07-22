namespace Less3.Api.Admin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using S3ServerLibrary;
    using SyslogLogging;

    using Less3.Classes;
    using Less3.Helpers;
    using Less3.Requests;
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
        private CleanupManager _Cleanup;

        #endregion

        #region Constructors-and-Factories
        
        internal PostHandler(
            SettingsBase settings,
            LoggingModule logging,
            ConfigManager config,
            BucketManager buckets,
            AuthManager auth,
            CleanupManager cleanup)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (buckets == null) throw new ArgumentNullException(nameof(buckets));
            if (auth == null) throw new ArgumentNullException(nameof(auth));
            if (cleanup == null) throw new ArgumentNullException(nameof(cleanup));

            _Settings = settings;
            _Logging = logging;
            _Config = config;
            _Buckets = buckets;
            _Auth = auth;
            _Cleanup = cleanup;
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
            else if (ctx.Http.Request.Url.Elements[1].Equals("maintenance"))
            {
                await PostMaintenance(ctx);
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
             
            if (!_Buckets.Add(bucket))
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, bucket.TenantId, "Bucket", bucket.Id, "Create");
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

            AdminMutationAuditor.Record(_Config, _Logging, ctx, user.TenantId, "User", user.Id, "Create");
            ctx.Response.StatusCode = 201;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
        }

        private async Task PostCredentials(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length == 4
                && ctx.Http.Request.Url.Elements[3].Equals("rotate", StringComparison.OrdinalIgnoreCase))
            {
                await RotateCredential(ctx, ctx.Http.Request.Url.Elements[2]).ConfigureAwait(false);
                return;
            }

            if (ctx.Http.Request.Url.Elements.Length == 4
                && ctx.Http.Request.Url.Elements[3].Equals("disable", StringComparison.OrdinalIgnoreCase))
            {
                await SetCredentialActive(ctx, ctx.Http.Request.Url.Elements[2], false).ConfigureAwait(false);
                return;
            }

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

            if (String.IsNullOrEmpty(cred.TenantId)) cred.TenantId = "default";
            if (String.IsNullOrEmpty(cred.AccessKey)) cred.AccessKey = GenerateUniqueAccessKey();
            if (String.IsNullOrEmpty(cred.SecretKey)) cred.SecretKey = CredentialMaterialGenerator.GenerateSecretKey();

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

            AdminMutationAuditor.Record(_Config, _Logging, ctx, cred.TenantId, "Credential", cred.Id, "Create");
            ctx.Response.StatusCode = 201;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(CredentialResponseSanitizer.ForResponse(cred, true), true));
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

            AdminMutationAuditor.Record(_Config, _Logging, ctx, tenant.Id, "Tenant", tenant.Id, "Create");
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

            AdminMutationAuditor.Record(_Config, _Logging, ctx, role.TenantId, "Role", role.Id, "Create");
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

            AdminMutationAuditor.Record(_Config, _Logging, ctx, permission.TenantId, "Permission", permission.Id, "Create");
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

            AdminMutationAuditor.Record(_Config, _Logging, ctx, assignment.TenantId, "RoleAssignment", assignment.Id, "Create");
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

            AdminMutationAuditor.Record(_Config, _Logging, ctx, session.TenantId, "AuthSession", session.Id, "Create");
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

        private async Task RotateCredential(S3Context ctx, string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest).ConfigureAwait(false);
                return;
            }

            Credential existing = _Config.GetCredentialById(id);
            if (existing == null)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            existing.SecretKey = CredentialMaterialGenerator.GenerateSecretKey();
            existing.IsBase64 = false;

            if (!_Config.UpdateCredential(existing))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, existing.TenantId, "Credential", existing.Id, "Rotate");
            await SendJson(ctx, CredentialResponseSanitizer.ForResponse(existing, true)).ConfigureAwait(false);
        }

        private async Task SetCredentialActive(S3Context ctx, string id, bool active)
        {
            if (String.IsNullOrEmpty(id))
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest).ConfigureAwait(false);
                return;
            }

            Credential existing = _Config.GetCredentialById(id);
            if (existing == null)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            existing.Active = active;

            if (!_Config.UpdateCredential(existing))
            {
                await SendConflict(ctx).ConfigureAwait(false);
                return;
            }

            AdminMutationAuditor.Record(_Config, _Logging, ctx, existing.TenantId, "Credential", existing.Id, active ? "Enable" : "Disable");
            await SendJson(ctx, CredentialResponseSanitizer.ForResponse(existing, false)).ConfigureAwait(false);
        }

        private string GenerateUniqueAccessKey()
        {
            for (int i = 0; i < 16; i++)
            {
                string accessKey = CredentialMaterialGenerator.GenerateAccessKey();
                if (_Config.GetCredentialByAccessKey(accessKey) == null) return accessKey;
            }

            throw new InvalidOperationException("Unable to generate a unique access key.");
        }

        private async Task PostMaintenance(S3Context ctx)
        {
            if (ctx.Http.Request.Url.Elements.Length < 3)
            {
                await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest).ConfigureAwait(false);
                return;
            }

            string operation = ctx.Http.Request.Url.Elements[2].ToLowerInvariant();

            if (operation.Equals("purge-request-history"))
            {
                MaintenanceSettingsUpdateRequest request = Deserialize<MaintenanceSettingsUpdateRequest>(ctx);
                DateTime cutoff = request?.OlderThanUtc ?? DateTime.UtcNow.AddDays(-_Settings.RequestHistoryRetentionDays);
                MaintenanceActionResult result = _Cleanup.PurgeRequestHistory(cutoff);
                AdminMutationAuditor.Record(_Config, _Logging, ctx, GetTenantId(ctx), "RequestHistory", null, "Purge");
                await SendJson(ctx, result).ConfigureAwait(false);
                return;
            }

            if (operation.Equals("cleanup-temp-uploads"))
            {
                MaintenanceActionResult result = _Cleanup.CleanupTempUploads();
                AdminMutationAuditor.Record(_Config, _Logging, ctx, GetTenantId(ctx), "Maintenance", null, "CleanupTempUploads");
                await SendJson(ctx, result).ConfigureAwait(false);
                return;
            }

            if (operation.Equals("run-cleanup"))
            {
                MaintenanceActionResult result = _Cleanup.RunCleanupCycle();
                AdminMutationAuditor.Record(_Config, _Logging, ctx, GetTenantId(ctx), "Maintenance", null, "RunCleanup");
                await SendJson(ctx, result).ConfigureAwait(false);
                return;
            }

            if (operation.Equals("verify-objects"))
            {
                string tenantId = GetTenantId(ctx);
                MaintenanceActionResult result = VerifyObjectRows(tenantId);
                AdminMutationAuditor.Record(_Config, _Logging, ctx, tenantId, "Maintenance", null, "VerifyObjects");
                await SendJson(ctx, result).ConfigureAwait(false);
                return;
            }

            if (operation.Equals("settings") || operation.Equals("update-runtime-settings"))
            {
                MaintenanceSettingsUpdateRequest request = Deserialize<MaintenanceSettingsUpdateRequest>(ctx);
                if (request == null)
                {
                    await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest).ConfigureAwait(false);
                    return;
                }

                if (request.RequestHistoryRetentionDays.HasValue)
                {
                    _Settings.RequestHistoryRetentionDays = request.RequestHistoryRetentionDays.Value;
                }

                if (request.CleanupIntervalMs.HasValue)
                {
                    _Cleanup.CleanupIntervalMs = request.CleanupIntervalMs.Value;
                }

                MaintenanceActionResult result = new MaintenanceActionResult();
                result.Action = "update-runtime-settings";
                AdminMutationAuditor.Record(_Config, _Logging, ctx, GetTenantId(ctx), "Maintenance", null, "UpdateSettings");
                await SendJson(ctx, result).ConfigureAwait(false);
                return;
            }

            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest).ConfigureAwait(false);
        }

        private MaintenanceActionResult VerifyObjectRows(string tenantId)
        {
            MaintenanceActionResult result = new MaintenanceActionResult();
            result.Action = "verify-objects";

            foreach (Bucket bucket in _Config.GetBuckets(tenantId))
            {
                List<Obj> objects = _Config.GetObjects(tenantId, bucket.Id, new EnumerationQuery { Limit = 1000 });
                if (objects == null) continue;

                foreach (Obj obj in objects)
                {
                    if (obj.DeleteMarker) continue;

                    result.ObjectRowCount++;
                    if (String.IsNullOrEmpty(obj.BlobFilename))
                    {
                        result.MissingBlobFileCount++;
                        result.MissingBlobFiles.Add(bucket.Name + "/" + obj.Key);
                        continue;
                    }

                    string filePath = Path.GetFullPath(Path.Combine(bucket.DiskDirectory, obj.BlobFilename));
                    if (!File.Exists(filePath))
                    {
                        result.MissingBlobFileCount++;
                        result.MissingBlobFiles.Add(bucket.Name + "/" + obj.Key);
                    }
                }
            }

            result.Success = result.MissingBlobFileCount == 0;
            result.GeneratedUtc = DateTime.UtcNow;
            return result;
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
