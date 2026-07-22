namespace Less3.Api.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    using S3ServerLibrary;
    using SyslogLogging;

    using Less3.Classes;
    using Less3.Requests;
    using Less3.Responses;
    using Less3.Settings;
    using WatsonWebserver.Core;

    /// <summary>
    /// Less3 REST API handler for /api/v1 routes.
    /// </summary>
    internal class RestApiHandler
    {
        #region Private-Members

        private SettingsBase _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;
        private BucketManager _Buckets;
        private AuthManager _Auth;

        #endregion

        #region Constructors-and-Factories

        internal RestApiHandler(
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
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            if (ctx.Http.Request.Url.Elements == null || ctx.Http.Request.Url.Elements.Length < 3)
            {
                await SendInvalidRequest(ctx).ConfigureAwait(false);
                return;
            }

            string resourceType = NormalizeResourceType(ctx.Http.Request.Url.Elements[2]);

            switch (ctx.Http.Request.Method)
            {
                case HttpMethod.GET:
                    await Get(ctx, resourceType).ConfigureAwait(false);
                    return;
                case HttpMethod.POST:
                    await Post(ctx, resourceType).ConfigureAwait(false);
                    return;
                case HttpMethod.PUT:
                    await Put(ctx, resourceType).ConfigureAwait(false);
                    return;
                case HttpMethod.DELETE:
                    await Delete(ctx, resourceType).ConfigureAwait(false);
                    return;
            }

            await SendInvalidRequest(ctx).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private async Task Get(S3Context ctx, string resourceType)
        {
            if (IsExistsRequest(ctx))
            {
                await SendExists(ctx, Exists(ctx, resourceType, ctx.Http.Request.Url.Elements[3])).ConfigureAwait(false);
                return;
            }

            if (ctx.Http.Request.Url.Elements.Length == 3)
            {
                await Enumerate(ctx, resourceType, QueryFromRequest(ctx)).ConfigureAwait(false);
                return;
            }

            if (ctx.Http.Request.Url.Elements.Length == 4)
            {
                object item = Read(ctx, resourceType, ctx.Http.Request.Url.Elements[3]);
                if (item == null)
                {
                    await SendNotFound(ctx).ConfigureAwait(false);
                    return;
                }

                await SendJson(ctx, item, 200).ConfigureAwait(false);
                return;
            }

            await SendInvalidRequest(ctx).ConfigureAwait(false);
        }

        private async Task Post(S3Context ctx, string resourceType)
        {
            if (resourceType.Equals("authsessions", StringComparison.OrdinalIgnoreCase)
                && ctx.Http.Request.Url.Elements.Length == 4)
            {
                if (ctx.Http.Request.Url.Elements[3].Equals("login", StringComparison.OrdinalIgnoreCase))
                {
                    await Login(ctx).ConfigureAwait(false);
                    return;
                }

                if (ctx.Http.Request.Url.Elements[3].Equals("validate", StringComparison.OrdinalIgnoreCase))
                {
                    await ValidateSession(ctx).ConfigureAwait(false);
                    return;
                }

                if (ctx.Http.Request.Url.Elements[3].Equals("revoke", StringComparison.OrdinalIgnoreCase))
                {
                    await RevokeSessionByToken(ctx).ConfigureAwait(false);
                    return;
                }
            }

            if (ctx.Http.Request.Url.Elements.Length == 4 && ctx.Http.Request.Url.Elements[3].Equals("enumerate"))
            {
                EnumerationQuery query = Deserialize<EnumerationQuery>(ctx);
                if (query == null) query = new EnumerationQuery();
                await Enumerate(ctx, resourceType, query).ConfigureAwait(false);
                return;
            }

            if (ctx.Http.Request.Url.Elements.Length == 4 && ctx.Http.Request.Url.Elements[3].Equals("exists"))
            {
                string id = GetQueryValue(ctx, "id");
                if (String.IsNullOrEmpty(id))
                {
                    await SendInvalidRequest(ctx).ConfigureAwait(false);
                    return;
                }

                await SendExists(ctx, Exists(ctx, resourceType, id)).ConfigureAwait(false);
                return;
            }

            if (ctx.Http.Request.Url.Elements.Length != 3)
            {
                await SendInvalidRequest(ctx).ConfigureAwait(false);
                return;
            }

            object created = Create(ctx, resourceType);
            if (created == null)
            {
                await SendInvalidRequest(ctx).ConfigureAwait(false);
                return;
            }

            await SendJson(ctx, created, 201).ConfigureAwait(false);
        }

        private async Task Put(S3Context ctx, string resourceType)
        {
            if (ctx.Http.Request.Url.Elements.Length != 4)
            {
                await SendInvalidRequest(ctx).ConfigureAwait(false);
                return;
            }

            object updated = Update(ctx, resourceType, ctx.Http.Request.Url.Elements[3]);
            if (updated == null)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            await SendJson(ctx, updated, 200).ConfigureAwait(false);
        }

        private async Task Delete(S3Context ctx, string resourceType)
        {
            if (ctx.Http.Request.Url.Elements.Length != 4)
            {
                await SendInvalidRequest(ctx).ConfigureAwait(false);
                return;
            }

            bool deleted = DeleteResource(ctx, resourceType, ctx.Http.Request.Url.Elements[3]);
            if (!deleted)
            {
                await SendNotFound(ctx).ConfigureAwait(false);
                return;
            }

            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send().ConfigureAwait(false);
        }

        private async Task Enumerate(S3Context ctx, string resourceType, EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            if (String.IsNullOrEmpty(query.TenantId)) query.TenantId = GetTenantId(ctx);

            switch (resourceType)
            {
                case "tenants":
                    await SendJson(ctx, _Config.EnumerateTenants(query), 200).ConfigureAwait(false);
                    return;
                case "roles":
                    await SendJson(ctx, _Config.EnumerateRoles(query), 200).ConfigureAwait(false);
                    return;
                case "permissions":
                    await SendJson(ctx, _Config.EnumeratePermissions(query), 200).ConfigureAwait(false);
                    return;
                case "roleassignments":
                    await SendJson(ctx, _Config.EnumerateRoleAssignments(query), 200).ConfigureAwait(false);
                    return;
                case "authsessions":
                    await SendJson(ctx, _Config.EnumerateAuthSessions(query), 200).ConfigureAwait(false);
                    return;
                case "authorizationaudit":
                    await SendJson(ctx, _Config.EnumerateAuthorizationAudit(query), 200).ConfigureAwait(false);
                    return;
                case "users":
                    await SendJson(ctx, BuildResult(_Config.GetUsers(query.TenantId), query), 200).ConfigureAwait(false);
                    return;
                case "credentials":
                    await SendJson(ctx, BuildResult(_Config.GetCredentials(query.TenantId), query), 200).ConfigureAwait(false);
                    return;
                case "buckets":
                    await SendJson(ctx, BuildResult(_Config.GetBuckets(query.TenantId), query), 200).ConfigureAwait(false);
                    return;
                case "objects":
                    await SendJson(ctx, BuildResult(_Config.GetObjects(query.TenantId, GetBucketId(ctx), query), query), 200).ConfigureAwait(false);
                    return;
                case "buckettags":
                    await SendJson(ctx, BuildResult(_Config.GetBucketTags(query.TenantId, GetBucketId(ctx), query), query), 200).ConfigureAwait(false);
                    return;
                case "objecttags":
                    await SendJson(ctx, BuildResult(_Config.GetObjectTags(query.TenantId, GetBucketId(ctx), GetObjectId(ctx), query), query), 200).ConfigureAwait(false);
                    return;
                case "bucketacls":
                    await SendJson(ctx, BuildResult(_Config.GetBucketAcls(query.TenantId, GetBucketId(ctx), query), query), 200).ConfigureAwait(false);
                    return;
                case "objectacls":
                    await SendJson(ctx, BuildResult(_Config.GetObjectAcls(query.TenantId, GetBucketId(ctx), GetObjectId(ctx), query), query), 200).ConfigureAwait(false);
                    return;
                case "requesthistory":
                    await SendJson(ctx, BuildResult(FilterRequestHistory(_Config.GetRequestHistories(), query), query), 200).ConfigureAwait(false);
                    return;
            }

            await SendInvalidRequest(ctx).ConfigureAwait(false);
        }

        private object Read(S3Context ctx, string resourceType, string id)
        {
            string tenantId = GetTenantId(ctx);

            switch (resourceType)
            {
                case "tenants":
                    return _Config.GetTenantById(id);
                case "roles":
                    return _Config.GetRoleById(tenantId, id);
                case "permissions":
                    return _Config.GetPermissionById(tenantId, id);
                case "roleassignments":
                    return _Config.GetRoleAssignmentById(tenantId, id);
                case "authsessions":
                    return _Config.GetAuthSessionById(tenantId, id);
                case "authorizationaudit":
                    return _Config.GetAuthorizationAuditById(tenantId, id);
                case "users":
                    return _Config.GetUserById(tenantId, id);
                case "credentials":
                    return _Config.GetCredentialById(tenantId, id);
                case "buckets":
                    return _Config.GetBucketById(tenantId, id);
                case "objects":
                    return _Config.GetObjectById(tenantId, GetBucketId(ctx), id);
                case "buckettags":
                    return _Config.GetBucketTagById(tenantId, id);
                case "objecttags":
                    return _Config.GetObjectTagById(tenantId, id);
                case "bucketacls":
                    return _Config.GetBucketAclById(tenantId, id);
                case "objectacls":
                    return _Config.GetObjectAclById(tenantId, id);
                case "requesthistory":
                    return _Config.GetRequestHistoryById(id);
            }

            return null;
        }

        private object Create(S3Context ctx, string resourceType)
        {
            switch (resourceType)
            {
                case "tenants":
                    return CreateTenant(ctx);
                case "roles":
                    return CreateRole(ctx);
                case "permissions":
                    return CreatePermission(ctx);
                case "roleassignments":
                    return CreateRoleAssignment(ctx);
                case "authsessions":
                    return CreateAuthSession(ctx);
                case "authorizationaudit":
                    return CreateAuthorizationAudit(ctx);
                case "users":
                    return CreateUser(ctx);
                case "credentials":
                    return CreateCredential(ctx);
                case "buckets":
                    return CreateBucket(ctx);
                case "objects":
                    return CreateObject(ctx);
                case "buckettags":
                    return CreateBucketTag(ctx);
                case "objecttags":
                    return CreateObjectTag(ctx);
                case "bucketacls":
                    return CreateBucketAcl(ctx);
                case "objectacls":
                    return CreateObjectAcl(ctx);
            }

            return null;
        }

        private object Update(S3Context ctx, string resourceType, string id)
        {
            switch (resourceType)
            {
                case "tenants":
                    return UpdateTenant(ctx, id);
                case "roles":
                    return UpdateRole(ctx, id);
                case "permissions":
                    return UpdatePermission(ctx, id);
                case "roleassignments":
                    return UpdateRoleAssignment(ctx, id);
                case "authsessions":
                    return UpdateAuthSession(ctx, id);
                case "users":
                    return UpdateUser(ctx, id);
                case "credentials":
                    return UpdateCredential(ctx, id);
                case "buckets":
                    return UpdateBucket(ctx, id);
                case "objects":
                    return UpdateObject(ctx, id);
                case "buckettags":
                    return UpdateBucketTag(ctx, id);
                case "objecttags":
                    return UpdateObjectTag(ctx, id);
                case "bucketacls":
                    return UpdateBucketAcl(ctx, id);
                case "objectacls":
                    return UpdateObjectAcl(ctx, id);
            }

            return null;
        }

        private bool DeleteResource(S3Context ctx, string resourceType, string id)
        {
            string tenantId = GetTenantId(ctx);

            switch (resourceType)
            {
                case "tenants":
                    return _Config.DeleteTenant(id);
                case "roles":
                    return _Config.DeleteRole(tenantId, id);
                case "permissions":
                    return _Config.DeletePermission(tenantId, id);
                case "roleassignments":
                    return _Config.DeleteRoleAssignment(tenantId, id);
                case "authsessions":
                    return _Config.RevokeAuthSession(tenantId, id);
                case "authorizationaudit":
                    return _Config.DeleteAuthorizationAudit(tenantId, id);
                case "users":
                    if (_Config.GetUserById(tenantId, id) == null) return false;
                    _Config.DeleteUser(id);
                    return true;
                case "credentials":
                    if (_Config.GetCredentialById(tenantId, id) == null) return false;
                    _Config.DeleteCredential(id);
                    return true;
                case "buckets":
                    Bucket bucket = _Config.GetBucketById(tenantId, id);
                    if (bucket == null) return false;
                    _Buckets.Remove(bucket, ShouldDestroyBucket(ctx));
                    return true;
                case "objects":
                    return _Config.DeleteObject(tenantId, GetBucketId(ctx), id);
                case "buckettags":
                    return _Config.DeleteBucketTag(tenantId, id);
                case "objecttags":
                    return _Config.DeleteObjectTag(tenantId, id);
                case "bucketacls":
                    return _Config.DeleteBucketAcl(tenantId, id);
                case "objectacls":
                    return _Config.DeleteObjectAcl(tenantId, id);
                case "requesthistory":
                    if (_Config.GetRequestHistoryById(id) == null) return false;
                    _Config.DeleteRequestHistory(id);
                    return true;
            }

            return false;
        }

        private bool Exists(S3Context ctx, string resourceType, string id)
        {
            string tenantId = GetTenantId(ctx);

            switch (resourceType)
            {
                case "tenants":
                    return _Config.TenantExists(id);
                case "roles":
                    return _Config.RoleExists(tenantId, id);
                case "permissions":
                    return _Config.PermissionExists(tenantId, id);
                case "roleassignments":
                    return _Config.GetRoleAssignmentById(tenantId, id) != null;
                case "authsessions":
                    return _Config.GetAuthSessionById(tenantId, id) != null;
                case "authorizationaudit":
                    return _Config.GetAuthorizationAuditById(tenantId, id) != null;
                case "users":
                    return _Config.GetUserById(tenantId, id) != null;
                case "credentials":
                    return _Config.GetCredentialById(tenantId, id) != null;
                case "buckets":
                    return _Config.GetBucketById(tenantId, id) != null;
                case "objects":
                    return _Config.GetObjectById(tenantId, GetBucketId(ctx), id) != null;
                case "buckettags":
                    return _Config.GetBucketTagById(tenantId, id) != null;
                case "objecttags":
                    return _Config.GetObjectTagById(tenantId, id) != null;
                case "bucketacls":
                    return _Config.GetBucketAclById(tenantId, id) != null;
                case "objectacls":
                    return _Config.GetObjectAclById(tenantId, id) != null;
                case "requesthistory":
                    return _Config.GetRequestHistoryById(id) != null;
            }

            return false;
        }

        private Tenant CreateTenant(S3Context ctx)
        {
            Tenant tenant = Deserialize<Tenant>(ctx);
            if (tenant == null) return null;
            if (!_Config.AddTenant(tenant)) return null;
            return tenant;
        }

        private Role CreateRole(S3Context ctx)
        {
            Role role = Deserialize<Role>(ctx);
            if (role == null) return null;
            if (String.IsNullOrEmpty(role.TenantId)) role.TenantId = GetTenantId(ctx);
            role.IsBuiltIn = false;
            if (!_Config.AddRole(role)) return null;
            return role;
        }

        private Permission CreatePermission(S3Context ctx)
        {
            Permission permission = Deserialize<Permission>(ctx);
            if (permission == null) return null;
            if (String.IsNullOrEmpty(permission.TenantId)) permission.TenantId = GetTenantId(ctx);
            if (!_Config.AddPermission(permission)) return null;
            return permission;
        }

        private RoleAssignment CreateRoleAssignment(S3Context ctx)
        {
            RoleAssignment assignment = Deserialize<RoleAssignment>(ctx);
            if (assignment == null) return null;
            if (String.IsNullOrEmpty(assignment.TenantId)) assignment.TenantId = GetTenantId(ctx);
            if (!_Config.AddRoleAssignment(assignment)) return null;
            return assignment;
        }

        private AuthSession CreateAuthSession(S3Context ctx)
        {
            AuthSession session = Deserialize<AuthSession>(ctx);
            if (session == null) return null;
            if (String.IsNullOrEmpty(session.TenantId)) session.TenantId = GetTenantId(ctx);
            if (!_Config.AddAuthSession(session)) return null;
            return session;
        }

        private AuthorizationAudit CreateAuthorizationAudit(S3Context ctx)
        {
            AuthorizationAudit audit = Deserialize<AuthorizationAudit>(ctx);
            if (audit == null) return null;
            if (String.IsNullOrEmpty(audit.TenantId)) audit.TenantId = GetTenantId(ctx);
            if (!_Config.AddAuthorizationAudit(audit)) return null;
            return audit;
        }

        private User CreateUser(S3Context ctx)
        {
            User user = Deserialize<User>(ctx);
            if (user == null) return null;
            if (String.IsNullOrEmpty(user.TenantId)) user.TenantId = GetTenantId(ctx);
            if (!_Config.AddUser(user)) return null;
            return user;
        }

        private Credential CreateCredential(S3Context ctx)
        {
            Credential credential = Deserialize<Credential>(ctx);
            if (credential == null) return null;
            if (String.IsNullOrEmpty(credential.TenantId)) credential.TenantId = GetTenantId(ctx);
            if (_Config.GetUserById(credential.TenantId, credential.UserId) == null) return null;
            if (!_Config.AddCredential(credential)) return null;
            return credential;
        }

        private async Task Login(S3Context ctx)
        {
            AuthSessionLoginRequest request = Deserialize<AuthSessionLoginRequest>(ctx);
            if (request == null
                || String.IsNullOrEmpty(request.Email)
                || String.IsNullOrEmpty(request.Password))
            {
                await SendInvalidRequest(ctx).ConfigureAwait(false);
                return;
            }

            string tenantId = String.IsNullOrEmpty(request.TenantId) ? "default" : request.TenantId;
            Tenant tenant = _Config.GetTenantById(tenantId);
            if (tenant == null || !tenant.Active)
            {
                await SendUnauthorized(ctx, "Tenant is not active.").ConfigureAwait(false);
                return;
            }

            User user = _Config.GetUserByEmail(tenantId, request.Email);
            if (user == null || !user.Active || !PasswordMatches(user, request.Password))
            {
                await SendUnauthorized(ctx, "Invalid email or password.").ConfigureAwait(false);
                return;
            }

            string rawToken = CreateSessionToken();
            AuthSession session = new AuthSession();
            session.TenantId = tenantId;
            session.PrincipalType = "User";
            session.PrincipalId = user.Id;
            session.TokenHash = HashToken(rawToken);
            session.CreatedUtc = DateTime.UtcNow;
            session.ExpirationUtc = DateTime.UtcNow.AddMinutes(NormalizeExpirationMinutes(request.ExpirationMinutes));
            session.SourceIp = ctx.Http.Request.Source.IpAddress;

            if (!_Config.AddAuthSession(session))
            {
                await SendInvalidRequest(ctx).ConfigureAwait(false);
                return;
            }

            AuthSessionLoginResponse response = new AuthSessionLoginResponse();
            response.Session = session;
            response.Token = rawToken;
            await SendJson(ctx, response, 201).ConfigureAwait(false);
        }

        private async Task ValidateSession(S3Context ctx)
        {
            AuthSessionTokenRequest request = Deserialize<AuthSessionTokenRequest>(ctx);
            AuthSessionValidationResponse response = ResolveSession(request?.Token);
            await SendJson(ctx, response, response.Valid ? 200 : 401).ConfigureAwait(false);
        }

        private async Task RevokeSessionByToken(S3Context ctx)
        {
            AuthSessionTokenRequest request = Deserialize<AuthSessionTokenRequest>(ctx);
            AuthSessionValidationResponse validation = ResolveSession(request?.Token);
            if (!validation.Valid || validation.Session == null)
            {
                await SendJson(ctx, validation, 401).ConfigureAwait(false);
                return;
            }

            _Config.RevokeAuthSession(validation.Session.TenantId, validation.Session.Id);
            AuthSessionValidationResponse response = new AuthSessionValidationResponse();
            response.Valid = false;
            response.Reason = "Session revoked.";
            await SendJson(ctx, response, 200).ConfigureAwait(false);
        }

        private AuthSessionValidationResponse ResolveSession(string rawToken)
        {
            AuthSessionValidationResponse response = new AuthSessionValidationResponse();

            if (String.IsNullOrWhiteSpace(rawToken))
            {
                response.Reason = "Token is required.";
                return response;
            }

            AuthSession session = _Config.GetAuthSessionByTokenHash(HashToken(rawToken));
            if (session == null)
            {
                response.Reason = "Session was not found.";
                return response;
            }

            if (!session.Active || session.RevokedUtc.HasValue)
            {
                response.Session = session;
                response.Reason = "Session has been revoked.";
                return response;
            }

            if (session.ExpirationUtc <= DateTime.UtcNow)
            {
                response.Session = session;
                response.Reason = "Session has expired.";
                return response;
            }

            Tenant tenant = _Config.GetTenantById(session.TenantId);
            if (tenant == null || !tenant.Active)
            {
                response.Session = session;
                response.Reason = "Tenant is not active.";
                return response;
            }

            response.Valid = true;
            response.Session = session;
            return response;
        }

        private static bool PasswordMatches(User user, string password)
        {
            if (user == null || String.IsNullOrEmpty(password)) return false;
            if (String.IsNullOrEmpty(user.PasswordHash)) return false;
            return user.PasswordHash.Equals(password, StringComparison.Ordinal);
        }

        private static int NormalizeExpirationMinutes(int minutes)
        {
            if (minutes < 1) return 480;
            if (minutes > 1440) return 1440;
            return minutes;
        }

        private static string CreateSessionToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
        }

        private static string HashToken(string token)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private Bucket CreateBucket(S3Context ctx)
        {
            Bucket bucket = Deserialize<Bucket>(ctx);
            if (bucket == null) return null;
            if (String.IsNullOrEmpty(bucket.TenantId)) bucket.TenantId = GetTenantId(ctx);
            if (String.IsNullOrEmpty(bucket.DiskDirectory))
            {
                bucket.DiskDirectory = _Settings.Storage.DiskDirectory + bucket.Name + "/Objects/";
            }

            if (!_Buckets.Add(bucket)) return null;
            return bucket;
        }

        private Obj CreateObject(S3Context ctx)
        {
            Obj obj = Deserialize<Obj>(ctx);
            if (obj == null) return null;

            string tenantId = GetTenantId(ctx);
            string bucketId = GetBucketId(ctx);
            if (String.IsNullOrEmpty(obj.TenantId)) obj.TenantId = tenantId;
            if (String.IsNullOrEmpty(obj.BucketId)) obj.BucketId = bucketId;
            if (String.IsNullOrEmpty(obj.OwnerId)) obj.OwnerId = "usr_default_admin";
            if (String.IsNullOrEmpty(obj.AuthorId)) obj.AuthorId = obj.OwnerId;
            if (String.IsNullOrEmpty(obj.Etag)) obj.Etag = String.Empty;
            if (String.IsNullOrEmpty(obj.BlobFilename)) obj.BlobFilename = String.Empty;
            if (String.IsNullOrEmpty(obj.Md5)) obj.Md5 = String.Empty;

            if (!_Config.AddObject(obj)) return null;
            return obj;
        }

        private BucketTag CreateBucketTag(S3Context ctx)
        {
            BucketTag tag = Deserialize<BucketTag>(ctx);
            if (tag == null) return null;

            string tenantId = GetTenantId(ctx);
            string bucketId = GetBucketId(ctx);
            if (String.IsNullOrEmpty(tag.TenantId)) tag.TenantId = tenantId;
            if (!String.IsNullOrEmpty(bucketId)) tag.BucketId = bucketId;

            if (!_Config.AddBucketTag(tag)) return null;
            return tag;
        }

        private ObjectTag CreateObjectTag(S3Context ctx)
        {
            ObjectTag tag = Deserialize<ObjectTag>(ctx);
            if (tag == null) return null;

            string tenantId = GetTenantId(ctx);
            string bucketId = GetBucketId(ctx);
            string objectId = GetObjectId(ctx);
            if (String.IsNullOrEmpty(tag.TenantId)) tag.TenantId = tenantId;
            if (!String.IsNullOrEmpty(bucketId)) tag.BucketId = bucketId;
            if (!String.IsNullOrEmpty(objectId)) tag.ObjectId = objectId;

            if (!_Config.AddObjectTag(tag)) return null;
            return tag;
        }

        private BucketAcl CreateBucketAcl(S3Context ctx)
        {
            BucketAcl acl = Deserialize<BucketAcl>(ctx);
            if (acl == null) return null;

            string tenantId = GetTenantId(ctx);
            string bucketId = GetBucketId(ctx);
            if (String.IsNullOrEmpty(acl.TenantId)) acl.TenantId = tenantId;
            if (!String.IsNullOrEmpty(bucketId)) acl.BucketId = bucketId;

            if (!_Config.AddBucketAcl(acl)) return null;
            return acl;
        }

        private ObjectAcl CreateObjectAcl(S3Context ctx)
        {
            ObjectAcl acl = Deserialize<ObjectAcl>(ctx);
            if (acl == null) return null;

            string tenantId = GetTenantId(ctx);
            string bucketId = GetBucketId(ctx);
            string objectId = GetObjectId(ctx);
            if (String.IsNullOrEmpty(acl.TenantId)) acl.TenantId = tenantId;
            if (!String.IsNullOrEmpty(bucketId)) acl.BucketId = bucketId;
            if (!String.IsNullOrEmpty(objectId)) acl.ObjectId = objectId;

            if (!_Config.AddObjectAcl(acl)) return null;
            return acl;
        }

        private Tenant UpdateTenant(S3Context ctx, string id)
        {
            Tenant existing = _Config.GetTenantById(id);
            if (existing == null) return null;

            Tenant tenant = Deserialize<Tenant>(ctx);
            if (tenant == null) return null;
            tenant.Id = existing.Id;
            tenant.CreatedUtc = existing.CreatedUtc;
            if (!_Config.UpdateTenant(tenant)) return null;
            return tenant;
        }

        private Role UpdateRole(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            Role existing = _Config.GetRoleById(tenantId, id);
            if (existing == null) return null;

            Role role = Deserialize<Role>(ctx);
            if (role == null) return null;
            role.Id = existing.Id;
            role.TenantId = String.IsNullOrEmpty(role.TenantId) ? tenantId : role.TenantId;
            role.CreatedUtc = existing.CreatedUtc;
            if (!_Config.UpdateRole(role)) return null;
            return role;
        }

        private Permission UpdatePermission(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            Permission existing = _Config.GetPermissionById(tenantId, id);
            if (existing == null) return null;

            Permission permission = Deserialize<Permission>(ctx);
            if (permission == null) return null;
            permission.Id = existing.Id;
            permission.TenantId = String.IsNullOrEmpty(permission.TenantId) ? tenantId : permission.TenantId;
            permission.CreatedUtc = existing.CreatedUtc;
            if (!_Config.UpdatePermission(permission)) return null;
            return permission;
        }

        private RoleAssignment UpdateRoleAssignment(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            RoleAssignment existing = _Config.GetRoleAssignmentById(tenantId, id);
            if (existing == null) return null;

            RoleAssignment assignment = Deserialize<RoleAssignment>(ctx);
            if (assignment == null) return null;
            assignment.Id = existing.Id;
            assignment.TenantId = String.IsNullOrEmpty(assignment.TenantId) ? tenantId : assignment.TenantId;
            assignment.CreatedUtc = existing.CreatedUtc;
            if (!_Config.UpdateRoleAssignment(assignment)) return null;
            return assignment;
        }

        private AuthSession UpdateAuthSession(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            AuthSession existing = _Config.GetAuthSessionById(tenantId, id);
            if (existing == null) return null;

            AuthSession session = Deserialize<AuthSession>(ctx);
            if (session == null) return null;
            session.Id = existing.Id;
            session.TenantId = String.IsNullOrEmpty(session.TenantId) ? tenantId : session.TenantId;
            session.CreatedUtc = existing.CreatedUtc;
            if (!_Config.UpdateAuthSession(session)) return null;
            return session;
        }

        private User UpdateUser(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            User existing = _Config.GetUserById(tenantId, id);
            if (existing == null) return null;

            User user = Deserialize<User>(ctx);
            if (user == null) return null;
            user.Id = existing.Id;
            user.TenantId = String.IsNullOrEmpty(user.TenantId) ? tenantId : user.TenantId;
            user.CreatedUtc = existing.CreatedUtc;
            if (!_Config.UpdateUser(user)) return null;
            return user;
        }

        private Credential UpdateCredential(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            Credential existing = _Config.GetCredentialById(tenantId, id);
            if (existing == null) return null;

            Credential credential = Deserialize<Credential>(ctx);
            if (credential == null) return null;
            credential.Id = existing.Id;
            credential.TenantId = String.IsNullOrEmpty(credential.TenantId) ? tenantId : credential.TenantId;
            credential.CreatedUtc = existing.CreatedUtc;
            if (!_Config.UpdateCredential(credential)) return null;
            return credential;
        }

        private Bucket UpdateBucket(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            Bucket existing = _Config.GetBucketById(tenantId, id);
            if (existing == null) return null;

            Bucket bucket = Deserialize<Bucket>(ctx);
            if (bucket == null) return null;
            bucket.Id = existing.Id;
            bucket.TenantId = String.IsNullOrEmpty(bucket.TenantId) ? tenantId : bucket.TenantId;
            if (String.IsNullOrEmpty(bucket.OwnerId)) bucket.OwnerId = existing.OwnerId;
            if (String.IsNullOrEmpty(bucket.DiskDirectory)) bucket.DiskDirectory = existing.DiskDirectory;
            bucket.CreatedUtc = existing.CreatedUtc;
            if (!_Config.UpdateBucket(bucket)) return null;
            return bucket;
        }

        private Obj UpdateObject(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            string bucketId = GetBucketId(ctx);
            Obj existing = _Config.GetObjectById(tenantId, bucketId, id);
            if (existing == null) return null;

            Obj obj = Deserialize<Obj>(ctx);
            if (obj == null) return null;
            obj.Id = existing.Id;
            obj.TenantId = String.IsNullOrEmpty(obj.TenantId) ? tenantId : obj.TenantId;
            obj.BucketId = String.IsNullOrEmpty(obj.BucketId) ? bucketId : obj.BucketId;
            if (String.IsNullOrEmpty(obj.OwnerId)) obj.OwnerId = existing.OwnerId;
            if (String.IsNullOrEmpty(obj.AuthorId)) obj.AuthorId = existing.AuthorId;
            if (String.IsNullOrEmpty(obj.Etag)) obj.Etag = existing.Etag ?? String.Empty;
            if (String.IsNullOrEmpty(obj.BlobFilename)) obj.BlobFilename = existing.BlobFilename ?? String.Empty;
            if (String.IsNullOrEmpty(obj.Md5)) obj.Md5 = existing.Md5 ?? String.Empty;

            if (!_Config.UpdateObject(obj)) return null;
            return obj;
        }

        private BucketTag UpdateBucketTag(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            BucketTag existing = _Config.GetBucketTagById(tenantId, id);
            if (existing == null) return null;

            BucketTag tag = Deserialize<BucketTag>(ctx);
            if (tag == null) return null;
            tag.Id = existing.Id;
            tag.TenantId = String.IsNullOrEmpty(tag.TenantId) ? tenantId : tag.TenantId;
            if (String.IsNullOrEmpty(tag.BucketId)) tag.BucketId = existing.BucketId;
            if (String.IsNullOrEmpty(tag.Key)) tag.Key = existing.Key;
            tag.CreatedUtc = existing.CreatedUtc;

            if (!_Config.UpdateBucketTag(tag)) return null;
            return tag;
        }

        private ObjectTag UpdateObjectTag(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            ObjectTag existing = _Config.GetObjectTagById(tenantId, id);
            if (existing == null) return null;

            ObjectTag tag = Deserialize<ObjectTag>(ctx);
            if (tag == null) return null;
            tag.Id = existing.Id;
            tag.TenantId = String.IsNullOrEmpty(tag.TenantId) ? tenantId : tag.TenantId;
            if (String.IsNullOrEmpty(tag.BucketId)) tag.BucketId = existing.BucketId;
            if (String.IsNullOrEmpty(tag.ObjectId)) tag.ObjectId = existing.ObjectId;
            if (String.IsNullOrEmpty(tag.Key)) tag.Key = existing.Key;
            tag.CreatedUtc = existing.CreatedUtc;

            if (!_Config.UpdateObjectTag(tag)) return null;
            return tag;
        }

        private BucketAcl UpdateBucketAcl(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            BucketAcl existing = _Config.GetBucketAclById(tenantId, id);
            if (existing == null) return null;

            BucketAcl acl = Deserialize<BucketAcl>(ctx);
            if (acl == null) return null;
            acl.Id = existing.Id;
            acl.TenantId = String.IsNullOrEmpty(acl.TenantId) ? tenantId : acl.TenantId;
            if (String.IsNullOrEmpty(acl.BucketId)) acl.BucketId = existing.BucketId;
            if (String.IsNullOrEmpty(acl.IssuedByUserId)) acl.IssuedByUserId = existing.IssuedByUserId;
            acl.CreatedUtc = existing.CreatedUtc;

            if (!_Config.UpdateBucketAcl(acl)) return null;
            return acl;
        }

        private ObjectAcl UpdateObjectAcl(S3Context ctx, string id)
        {
            string tenantId = GetTenantId(ctx);
            ObjectAcl existing = _Config.GetObjectAclById(tenantId, id);
            if (existing == null) return null;

            ObjectAcl acl = Deserialize<ObjectAcl>(ctx);
            if (acl == null) return null;
            acl.Id = existing.Id;
            acl.TenantId = String.IsNullOrEmpty(acl.TenantId) ? tenantId : acl.TenantId;
            if (String.IsNullOrEmpty(acl.BucketId)) acl.BucketId = existing.BucketId;
            if (String.IsNullOrEmpty(acl.ObjectId)) acl.ObjectId = existing.ObjectId;
            if (String.IsNullOrEmpty(acl.IssuedByUserId)) acl.IssuedByUserId = existing.IssuedByUserId;
            acl.CreatedUtc = existing.CreatedUtc;

            if (!_Config.UpdateObjectAcl(acl)) return null;
            return acl;
        }

        private List<RequestHistory> FilterRequestHistory(List<RequestHistory> entries, EnumerationQuery query)
        {
            IEnumerable<RequestHistory> filtered = entries;

            if (!String.IsNullOrEmpty(query.TenantId))
            {
                filtered = filtered.Where(e => e.TenantId != null && e.TenantId.Equals(query.TenantId));
            }

            if (query.StartUtc.HasValue)
            {
                filtered = filtered.Where(e => e.CreatedUtc >= query.StartUtc.Value);
            }

            if (query.EndUtc.HasValue)
            {
                filtered = filtered.Where(e => e.CreatedUtc <= query.EndUtc.Value);
            }

            if (query.Filters != null)
            {
                filtered = ApplyStringFilter(filtered, query, "method", e => e.HttpMethod);
                filtered = ApplyStringFilter(filtered, query, "sourceIp", e => e.SourceIp);
                filtered = ApplyStringFilter(filtered, query, "requestType", e => e.RequestType);
                filtered = ApplyStringFilter(filtered, query, "userId", e => e.UserId);
                filtered = ApplyStringFilter(filtered, query, "accessKey", e => e.AccessKey);

                if (query.Filters.ContainsKey("status") && Int32.TryParse(query.Filters["status"], out int status))
                {
                    filtered = filtered.Where(e => e.StatusCode == status);
                }

                if (query.Filters.ContainsKey("success") && Boolean.TryParse(query.Filters["success"], out bool success))
                {
                    filtered = filtered.Where(e => e.Success == success);
                }
            }

            return filtered.ToList();
        }

        private static IEnumerable<RequestHistory> ApplyStringFilter(
            IEnumerable<RequestHistory> entries,
            EnumerationQuery query,
            string filterName,
            Func<RequestHistory, string> accessor)
        {
            if (query.Filters == null) return entries;
            if (!query.Filters.ContainsKey(filterName)) return entries;
            if (String.IsNullOrEmpty(query.Filters[filterName])) return entries;

            string value = query.Filters[filterName];
            return entries.Where(e =>
            {
                string current = accessor(e);
                return current != null && current.Equals(value, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static EnumerationResult<T> BuildResult<T>(List<T> items, EnumerationQuery query)
        {
            if (items == null) items = new List<T>();
            if (query == null) query = new EnumerationQuery();

            List<T> page = items
                .Skip(query.Offset)
                .Take(query.Limit)
                .ToList();

            EnumerationResult<T> result = new EnumerationResult<T>();
            result.Items = page;
            result.Total = items.Count;
            result.Limit = query.Limit;
            result.Offset = query.Offset;
            result.HasMore = query.Offset + page.Count < items.Count;
            if (result.HasMore) result.NextContinuationToken = (query.Offset + page.Count).ToString();
            return result;
        }

        private static EnumerationQuery QueryFromRequest(S3Context ctx)
        {
            EnumerationQuery query = new EnumerationQuery();
            query.TenantId = GetTenantId(ctx);

            string limit = GetQueryValue(ctx, "limit");
            if (!String.IsNullOrEmpty(limit) && Int32.TryParse(limit, out int parsedLimit)) query.Limit = parsedLimit;

            string offset = GetQueryValue(ctx, "offset");
            if (!String.IsNullOrEmpty(offset) && Int32.TryParse(offset, out int parsedOffset)) query.Offset = parsedOffset;

            string sortField = GetQueryValue(ctx, "sortField");
            if (!String.IsNullOrEmpty(sortField)) query.SortField = sortField;

            string sortDirection = GetQueryValue(ctx, "sortDirection");
            if (!String.IsNullOrEmpty(sortDirection)) query.SortDirection = sortDirection;

            foreach (string filterName in new string[] { "method", "status", "success", "sourceIp", "requestType", "userId", "accessKey", "prefix", "objectId" })
            {
                string value = GetQueryValue(ctx, filterName);
                if (!String.IsNullOrEmpty(value)) query.Filters[filterName] = value;
            }

            return query;
        }

        private static string GetTenantId(S3Context ctx)
        {
            string tenantId = GetQueryValue(ctx, "tenantId");
            if (String.IsNullOrEmpty(tenantId) && ctx.Metadata is RequestContext requestContext)
            {
                tenantId = requestContext.TenantId;
            }

            if (String.IsNullOrEmpty(tenantId) && ctx.Metadata is RequestMetadata requestMetadata)
            {
                tenantId = requestMetadata.TenantId;
            }

            if (String.IsNullOrEmpty(tenantId)) tenantId = "default";
            return tenantId;
        }

        private static string GetBucketId(S3Context ctx)
        {
            return GetQueryValue(ctx, "bucketId");
        }

        private static string GetObjectId(S3Context ctx)
        {
            return GetQueryValue(ctx, "objectId");
        }

        private static string GetQueryValue(S3Context ctx, string name)
        {
            if (ctx.Http.Request.Query.Elements == null) return null;
            if (!ctx.Http.Request.Query.Elements.AllKeys.Contains(name)) return null;
            return ctx.Http.Request.Query.Elements[name];
        }

        private static bool IsExistsRequest(S3Context ctx)
        {
            return ctx.Http.Request.Url.Elements.Length == 5
                && ctx.Http.Request.Url.Elements[4].Equals("exists", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldDestroyBucket(S3Context ctx)
        {
            string value = GetQueryValue(ctx, "destroy");
            if (String.IsNullOrEmpty(value)) return false;
            if (String.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (String.Equals(value, "1", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string NormalizeResourceType(string resourceType)
        {
            if (String.IsNullOrEmpty(resourceType)) return String.Empty;

            string normalized = resourceType.ToLowerInvariant().Replace("-", String.Empty);

            if (normalized.Equals("assignments")) return "roleassignments";
            if (normalized.Equals("sessions")) return "authsessions";
            if (normalized.Equals("audit")) return "authorizationaudit";
            if (normalized.Equals("requesthistories")) return "requesthistory";
            if (normalized.Equals("buckettag")) return "buckettags";
            if (normalized.Equals("objecttag")) return "objecttags";
            if (normalized.Equals("bucketacl")) return "bucketacls";
            if (normalized.Equals("objectacl")) return "objectacls";

            return normalized;
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

        private static async Task SendJson(S3Context ctx, object obj, int statusCode)
        {
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send(SerializationHelper.SerializeJson(obj, true)).ConfigureAwait(false);
        }

        private static async Task SendExists(S3Context ctx, bool exists)
        {
            await SendJson(ctx, new ExistsResponse(exists), 200).ConfigureAwait(false);
        }

        private static async Task SendNotFound(S3Context ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send().ConfigureAwait(false);
        }

        private static async Task SendUnauthorized(S3Context ctx, string message)
        {
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send(message).ConfigureAwait(false);
        }

        private static async Task SendInvalidRequest(S3Context ctx)
        {
            await ctx.Response.Send(S3ServerLibrary.S3Objects.ErrorCode.InvalidRequest).ConfigureAwait(false);
        }

        #endregion
    }
}
