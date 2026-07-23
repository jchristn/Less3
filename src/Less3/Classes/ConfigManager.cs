namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    using Less3.Database;
    using Less3.Database.Implementations;
    using Less3.Requests;
    using Less3.Responses;
    using Less3.Settings;
    using SyslogLogging;

    /// <summary>
    /// Configuration manager.
    /// </summary>
    internal class ConfigManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SettingsBase _Settings = null;
        private LoggingModule _Logging = null;
        private DatabaseDriverBase _Database = null;

        #endregion

        #region Constructors-and-Factories

        internal ConfigManager(SettingsBase settings, LoggingModule logging, DatabaseDriverBase database)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Internal-Tenant-Methods

        internal List<Tenant> GetTenants()
        {
            EnumerationResult<Tenant> result = EnumerateTenants(new EnumerationQuery { Limit = 1000 });
            return result.Items;
        }

        internal EnumerationResult<Tenant> EnumerateTenants(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            return _Database.Tenants.EnumerateAsync(query).GetAwaiter().GetResult();
        }

        internal Tenant GetTenantById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Tenants.ReadAsync(id).GetAwaiter().GetResult();
        }

        internal bool TenantExists(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Tenants.ExistsAsync(id).GetAwaiter().GetResult();
        }

        internal bool AddTenant(Tenant tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            if (TenantExists(tenant.Id))
            {
                _Logging.Warn("ConfigManager AddTenant tenant Id " + tenant.Id + " already exists");
                return false;
            }

            _Database.Tenants.CreateAsync(tenant).GetAwaiter().GetResult();
            return true;
        }

        internal bool UpdateTenant(Tenant tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            if (!TenantExists(tenant.Id)) return false;

            _Database.Tenants.UpdateAsync(tenant).GetAwaiter().GetResult();
            return true;
        }

        internal bool DeleteTenant(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Tenants.DeleteAsync(id).GetAwaiter().GetResult();
        }

        #endregion

        #region Internal-Role-Methods

        internal List<Role> GetRoles(string tenantId)
        {
            EnumerationResult<Role> result = EnumerateRoles(new EnumerationQuery { TenantId = tenantId, Limit = 1000 });
            return result.Items;
        }

        internal EnumerationResult<Role> EnumerateRoles(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            return _Database.Roles.EnumerateAsync(query).GetAwaiter().GetResult();
        }

        internal Role GetRoleById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Roles.ReadAsync(tenantId, id).GetAwaiter().GetResult();
        }

        internal bool RoleExists(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Roles.ExistsAsync(tenantId, id).GetAwaiter().GetResult();
        }

        internal bool AddRole(Role role)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));

            if (RoleExists(role.TenantId, role.Id))
            {
                _Logging.Warn("ConfigManager AddRole role Id " + role.Id + " already exists");
                return false;
            }

            _Database.Roles.CreateAsync(role).GetAwaiter().GetResult();
            return true;
        }

        internal bool UpdateRole(Role role)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            Role updated = _Database.Roles.UpdateAsync(role).GetAwaiter().GetResult();
            return updated != null;
        }

        internal bool DeleteRole(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Roles.DeleteAsync(tenantId, id).GetAwaiter().GetResult();
        }

        #endregion

        #region Internal-Permission-Methods

        internal List<Permission> GetPermissions(string tenantId)
        {
            EnumerationResult<Permission> result = EnumeratePermissions(new EnumerationQuery { TenantId = tenantId, Limit = 1000 });
            return result.Items;
        }

        internal EnumerationResult<Permission> EnumeratePermissions(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            return _Database.Permissions.EnumerateAsync(query).GetAwaiter().GetResult();
        }

        internal Permission GetPermissionById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Permissions.ReadAsync(tenantId, id).GetAwaiter().GetResult();
        }

        internal bool PermissionExists(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Permissions.ExistsAsync(tenantId, id).GetAwaiter().GetResult();
        }

        internal bool AddPermission(Permission permission)
        {
            if (permission == null) throw new ArgumentNullException(nameof(permission));

            if (PermissionExists(permission.TenantId, permission.Id))
            {
                _Logging.Warn("ConfigManager AddPermission permission Id " + permission.Id + " already exists");
                return false;
            }

            _Database.Permissions.CreateAsync(permission).GetAwaiter().GetResult();
            return true;
        }

        internal bool UpdatePermission(Permission permission)
        {
            if (permission == null) throw new ArgumentNullException(nameof(permission));
            if (!PermissionExists(permission.TenantId, permission.Id)) return false;

            _Database.Permissions.UpdateAsync(permission).GetAwaiter().GetResult();
            return true;
        }

        internal bool DeletePermission(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Permissions.DeleteAsync(tenantId, id).GetAwaiter().GetResult();
        }

        #endregion

        #region Internal-RoleAssignment-Methods

        internal EnumerationResult<RoleAssignment> EnumerateRoleAssignments(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            return _Database.RoleAssignments.EnumerateAsync(query).GetAwaiter().GetResult();
        }

        internal RoleAssignment GetRoleAssignmentById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.RoleAssignments.ReadAsync(tenantId, id).GetAwaiter().GetResult();
        }

        internal bool AddRoleAssignment(RoleAssignment assignment)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            _Database.RoleAssignments.CreateAsync(assignment).GetAwaiter().GetResult();
            return true;
        }

        internal bool UpdateRoleAssignment(RoleAssignment assignment)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));

            RoleAssignment existing = GetRoleAssignmentById(assignment.TenantId, assignment.Id);
            if (existing == null) return false;

            _Database.RoleAssignments.UpdateAsync(assignment).GetAwaiter().GetResult();
            return true;
        }

        internal bool DeleteRoleAssignment(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.RoleAssignments.DeleteAsync(tenantId, id).GetAwaiter().GetResult();
        }

        #endregion

        #region Internal-AuthSession-Methods

        internal EnumerationResult<AuthSession> EnumerateAuthSessions(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            return _Database.AuthSessions.EnumerateAsync(query).GetAwaiter().GetResult();
        }

        internal AuthSession GetAuthSessionById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.AuthSessions.ReadAsync(tenantId, id).GetAwaiter().GetResult();
        }

        internal AuthSession GetAuthSessionByTokenHash(string tokenHash)
        {
            if (String.IsNullOrEmpty(tokenHash)) throw new ArgumentNullException(nameof(tokenHash));
            return _Database.AuthSessions.ReadByTokenHashAsync(tokenHash).GetAwaiter().GetResult();
        }

        internal bool AddAuthSession(AuthSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            _Database.AuthSessions.CreateAsync(session).GetAwaiter().GetResult();
            return true;
        }

        internal bool UpdateAuthSession(AuthSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            AuthSession existing = GetAuthSessionById(session.TenantId, session.Id);
            if (existing == null) return false;

            _Database.AuthSessions.UpdateAsync(session).GetAwaiter().GetResult();
            return true;
        }

        internal bool RevokeAuthSession(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.AuthSessions.RevokeAsync(tenantId, id).GetAwaiter().GetResult();
        }

        #endregion

        #region Internal-AuthorizationAudit-Methods

        internal EnumerationResult<AuthorizationAudit> EnumerateAuthorizationAudit(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            return _Database.AuthorizationAudit.EnumerateAsync(query).GetAwaiter().GetResult();
        }

        internal AuthorizationAudit GetAuthorizationAuditById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.AuthorizationAudit.ReadAsync(tenantId, id).GetAwaiter().GetResult();
        }

        internal bool AddAuthorizationAudit(AuthorizationAudit audit)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            _Database.AuthorizationAudit.CreateAsync(audit).GetAwaiter().GetResult();
            return true;
        }

        internal bool DeleteAuthorizationAudit(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.AuthorizationAudit.DeleteAsync(tenantId, id).GetAwaiter().GetResult();
        }

        #endregion

        #region Internal-User-Methods

        internal List<User> GetUsers()
        {
            return _Database.Users.GetAll();
        }

        internal List<User> GetUsers(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            return _Database.Users.GetAll(tenantId);
        }

        internal EnumerationResult<User> EnumerateUsers(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            List<string> predicates = TenantPredicates(query.TenantId);
            predicates.Add(StringFilter(query, "id", "id", false));
            predicates.Add(StringFilter(query, "name", "name", false));
            predicates.Add(StringFilter(query, "email", "email", false));
            predicates.Add(BoolFilter(query, "active", "active"));
            predicates.Add(BoolFilter(query, "isAdmin", "isadmin"));
            predicates.Add(BoolFilter(query, "isTenantAdmin", "istenantadmin"));

            return EnumerateTable(
                query,
                "users",
                CombinePredicates(predicates),
                new Dictionary<string, string>
                {
                    { "id", "id" },
                    { "tenantId", "tenant_id" },
                    { "name", "name" },
                    { "email", "email" },
                    { "active", "active" },
                    { "createdUtc", "createdutc" }
                },
                "id",
                ControlPlaneDataMapper.User);
        }

        internal bool UserIdExists(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Users.ExistsById(id);
        }

        internal bool UserEmailExists(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            return _Database.Users.ExistsByEmail(email);
        }

        internal bool UserEmailExists(string tenantId, string email)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            return _Database.Users.ExistsByEmail(tenantId, email);
        }

        internal User GetUserById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Users.GetById(id);
        }

        internal User GetUserById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Users.GetById(tenantId, id);
        }

        internal User GetUserByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _Database.Users.GetByName(name);
        }

        internal User GetUserByEmail(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            return _Database.Users.GetByEmail(email);
        }

        internal User GetUserByEmail(string tenantId, string email)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            return _Database.Users.GetByEmail(tenantId, email);
        }

        internal User GetUserByAccessKey(string accessKey)
        {
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));

            Credential cred = GetCredentialByAccessKey(accessKey);
            if (cred == null)
            {
                _Logging.Warn("ConfigManager GetUserByAccessKey access key " + accessKey + " not found");
                return null;
            }

            User user = GetUserById(cred.UserId);
            if (user == null)
            {
                _Logging.Warn("ConfigManager GetUserByAccessKey user Id " + cred.UserId + " not found, referenced by credential Id " + cred.Id);
                return null;
            }

            return user;
        }

        internal bool AddUser(string id, string name, string email)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            User user = new User(id, name, email);
            return AddUser(user);
        }

        internal bool AddUser(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            User userById = GetUserById(user.Id);
            if (userById != null)
            {
                _Logging.Warn("ConfigManager AddUser user Id " + user.Id + " already exists");
                return false;
            }

            User userByEmail = GetUserByEmail(user.TenantId, user.Email);
            if (userByEmail != null)
            {
                _Logging.Warn("ConfigManager AddUser user email " + user.Email + " already exists");
                return false;
            }

            _Database.Users.Insert(user);
            return true;
        }

        internal void DeleteUser(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.Users.DeleteById(id);
        }

        internal bool UpdateUser(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            User existing = GetUserById(user.Id);
            if (existing == null)
            {
                _Logging.Warn("ConfigManager UpdateUser user Id " + user.Id + " not found");
                return false;
            }

            User userByEmail = GetUserByEmail(user.TenantId, user.Email);
            if (userByEmail != null && !userByEmail.Id.Equals(user.Id))
            {
                _Logging.Warn("ConfigManager UpdateUser user email " + user.Email + " already exists");
                return false;
            }

            user.CreatedUtc = existing.CreatedUtc;
            _Database.Users.Update(user);
            return true;
        }

        #endregion

        #region Internal-Credential-Methods

        internal List<Credential> GetCredentials()
        {
            return _Database.Credentials.GetAll();
        }

        internal List<Credential> GetCredentials(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            return _Database.Credentials.GetAll(tenantId);
        }

        internal EnumerationResult<Credential> EnumerateCredentials(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            List<string> predicates = TenantPredicates(query.TenantId);
            predicates.Add(StringFilter(query, "id", "id", false));
            predicates.Add(StringFilter(query, "userId", "user_id", false));
            predicates.Add(StringFilter(query, "accessKey", "accesskey", false));
            predicates.Add(StringFilter(query, "description", "description", false));
            predicates.Add(BoolFilter(query, "active", "active"));

            return EnumerateTable(
                query,
                "credentials",
                CombinePredicates(predicates),
                new Dictionary<string, string>
                {
                    { "id", "id" },
                    { "tenantId", "tenant_id" },
                    { "userId", "user_id" },
                    { "accessKey", "accesskey" },
                    { "description", "description" },
                    { "active", "active" },
                    { "lastUsedUtc", "lastusedutc" },
                    { "lastFailedUtc", "lastfailedutc" },
                    { "createdUtc", "createdutc" }
                },
                "id",
                ControlPlaneDataMapper.Credential);
        }

        internal bool CredentialIdExists(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Credentials.ExistsById(id);
        }

        internal Credential GetCredentialById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Credentials.GetById(id);
        }

        internal Credential GetCredentialById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Credentials.GetById(tenantId, id);
        }

        internal List<Credential> GetCredentialsByUser(string userId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            return _Database.Credentials.GetByUserId(userId);
        }

        internal List<Credential> GetCredentialsByUser(string tenantId, string userId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            return _Database.Credentials.GetByUserId(tenantId, userId);
        }

        internal Credential GetCredentialByAccessKey(string accessKey)
        {
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            return _Database.Credentials.GetByAccessKey(accessKey);
        }

        internal bool AddCredential(string userId, string description, string accessKey, string secretKey, bool isBase64)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));

            Credential cred = new Credential(userId, description, accessKey, secretKey, isBase64);
            return AddCredential(cred);
        }

        internal bool AddCredential(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));

            Credential credById = GetCredentialById(cred.Id);
            if (credById != null)
            {
                _Logging.Warn("ConfigManager AddCredential credential Id " + cred.Id + " already exists");
                return false;
            }

            Credential credByKey = GetCredentialByAccessKey(cred.AccessKey);
            if (credByKey != null)
            {
                _Logging.Warn("ConfigManager AddCredential access key " + cred.AccessKey + " already exists");
                return false;
            }

            _Database.Credentials.Insert(cred);
            return true;
        }

        internal void DeleteCredential(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.Credentials.DeleteById(id);
        }

        internal bool UpdateCredential(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));

            Credential existing = GetCredentialById(cred.Id);
            if (existing == null)
            {
                _Logging.Warn("ConfigManager UpdateCredential credential Id " + cred.Id + " not found");
                return false;
            }

            Credential credentialByKey = GetCredentialByAccessKey(cred.AccessKey);
            if (credentialByKey != null && !credentialByKey.Id.Equals(cred.Id))
            {
                _Logging.Warn("ConfigManager UpdateCredential access key " + cred.AccessKey + " already exists");
                return false;
            }

            cred.CreatedUtc = existing.CreatedUtc;
            _Database.Credentials.Update(cred);
            return true;
        }

        #endregion

        #region Internal-Bucket-Methods

        internal List<Bucket> GetBuckets()
        {
            return _Database.Buckets.GetAll();
        }

        internal List<Bucket> GetBuckets(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            return _Database.Buckets.GetAll(tenantId);
        }

        internal EnumerationResult<Bucket> EnumerateBuckets(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            List<string> predicates = TenantPredicates(query.TenantId);
            predicates.Add(StringFilter(query, "id", "id", false));
            predicates.Add(StringFilter(query, "name", "name", false));
            predicates.Add(StringFilter(query, "ownerId", "owner_id", false));
            predicates.Add(StringFilter(query, "region", "regionstring", false));
            predicates.Add(BoolFilter(query, "enableVersioning", "enableversioning"));
            predicates.Add(BoolFilter(query, "enablePublicRead", "enablepublicread"));
            predicates.Add(BoolFilter(query, "enablePublicWrite", "enablepublicwrite"));

            return EnumerateTable(
                query,
                "buckets",
                CombinePredicates(predicates),
                new Dictionary<string, string>
                {
                    { "id", "id" },
                    { "tenantId", "tenant_id" },
                    { "ownerId", "owner_id" },
                    { "name", "name" },
                    { "region", "regionstring" },
                    { "createdUtc", "createdutc" }
                },
                "id",
                ControlPlaneDataMapper.Bucket);
        }

        internal bool BucketExists(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _Database.Buckets.ExistsByName(name);
        }

        internal bool BucketExists(string tenantId, string name)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _Database.Buckets.ExistsByName(tenantId, name);
        }

        internal List<Bucket> GetBucketsByUser(string userId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            return _Database.Buckets.GetByOwnerId(userId);
        }

        internal List<Bucket> GetBucketsByUser(string tenantId, string userId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            return _Database.Buckets.GetByOwnerId(tenantId, userId);
        }

        internal Bucket GetBucketById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Buckets.GetById(id);
        }

        internal Bucket GetBucketById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Buckets.GetById(tenantId, id);
        }

        internal Bucket GetBucketByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _Database.Buckets.GetByName(name);
        }

        internal Bucket GetBucketByName(string tenantId, string name)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return _Database.Buckets.GetByName(tenantId, name);
        }

        internal bool AddBucket(string userId, string name)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Bucket bucket = new Bucket(
                Less3.Helpers.IdGenerator.GenerateBucketId(),
                name,
                userId,
                _Settings.Storage.StorageType,
                _Settings.Storage.DiskDirectory + name + "/Objects");

            return AddBucket(bucket);
        }

        internal bool AddBucket(Bucket bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));
            if (BucketNameValidator.IsInvalid(bucket.Name))
            {
                _Logging.Warn("ConfigManager AddBucket invalid bucket name " + bucket.Name);
                return false;
            }

            if (BucketExists(bucket.TenantId, bucket.Name))
            {
                _Logging.Warn("ConfigManager AddBucket bucket " + bucket.Name + " already exists");
                return false;
            }

            _Database.Buckets.Insert(bucket);
            return true;
        }

        internal bool UpdateBucket(Bucket bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));
            if (BucketNameValidator.IsInvalid(bucket.Name))
            {
                _Logging.Warn("ConfigManager UpdateBucket invalid bucket name " + bucket.Name);
                return false;
            }

            Bucket existing = GetBucketById(bucket.TenantId, bucket.Id);
            if (existing == null) return false;

            Bucket bucketByName = GetBucketByName(bucket.TenantId, bucket.Name);
            if (bucketByName != null && !bucketByName.Id.Equals(bucket.Id, StringComparison.Ordinal))
            {
                _Logging.Warn("ConfigManager UpdateBucket bucket " + bucket.Name + " already exists");
                return false;
            }

            bucket.CreatedUtc = existing.CreatedUtc;
            _Database.Buckets.Update(bucket);
            return true;
        }

        internal void DeleteBucket(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.Buckets.DeleteById(id);
        }

        #endregion

        #region Internal-Object-Methods

        internal List<Obj> GetObjects(string tenantId, string bucketId, EnumerationQuery query)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (query == null) query = new EnumerationQuery();

            List<Bucket> buckets = new List<Bucket>();

            if (!String.IsNullOrEmpty(bucketId))
            {
                Bucket bucket = GetBucketById(tenantId, bucketId);
                if (bucket == null) return new List<Obj>();
                buckets.Add(bucket);
            }
            else
            {
                buckets.AddRange(GetBuckets(tenantId));
            }

            int offset = query.Offset < 0 ? 0 : query.Offset;
            int limit = query.Limit < 1 ? 100 : query.Limit;
            int maxResults = offset + limit;
            string prefix = null;
            if (query.Filters != null && query.Filters.ContainsKey("prefix")) prefix = query.Filters["prefix"];

            List<Obj> objects = new List<Obj>();
            foreach (Bucket bucket in buckets)
            {
                objects.AddRange(_Database.Objects.Enumerate(bucket.Id, 0, maxResults, false, prefix)
                    .Where(o => o.TenantId != null && o.TenantId.Equals(tenantId)));
            }

            return objects
                .OrderBy(o => o.Id)
                .ToList();
        }

        internal EnumerationResult<Obj> EnumerateObjects(EnumerationQuery query, string bucketId)
        {
            if (query == null) query = new EnumerationQuery();
            List<string> predicates = TenantPredicates(query.TenantId);

            if (!String.IsNullOrEmpty(bucketId))
            {
                predicates.Add("bucket_id = " + ControlPlaneSql.StringLiteral(bucketId));
            }

            predicates.Add(StringFilter(query, "id", "id", false));
            predicates.Add(StringFilter(query, "objectId", "id", false));
            predicates.Add(StringFilter(query, "bucketId", "bucket_id", false));
            predicates.Add(StringFilter(query, "ownerId", "owner_id", false));
            predicates.Add(StringFilter(query, "authorId", "author_id", false));
            predicates.Add(PrefixFilter(query, "prefix", SqlColumn("key")));
            predicates.Add(StringFilter(query, "key", SqlColumn("key"), false));
            predicates.Add(StringFilter(query, "contentType", "contenttype", false));
            predicates.Add(BoolFilter(query, "isFolder", "isfolder"));
            predicates.Add(BoolFilter(query, "deleteMarker", "deletemarker"));

            return EnumerateTable(
                query,
                "objects",
                CombinePredicates(predicates),
                new Dictionary<string, string>
                {
                    { "id", "id" },
                    { "tenantId", "tenant_id" },
                    { "bucketId", "bucket_id" },
                    { "ownerId", "owner_id" },
                    { "authorId", "author_id" },
                    { "key", SqlColumn("key") },
                    { "contentLength", "contentlength" },
                    { "createdUtc", "createdutc" },
                    { "lastUpdateUtc", "lastupdateutc" },
                    { "lastAccessUtc", "lastaccessutc" }
                },
                "id",
                ControlPlaneDataMapper.Obj);
        }

        internal Obj GetObjectById(string tenantId, string bucketId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(bucketId)) return null;
            if (String.IsNullOrEmpty(id)) return null;
            if (GetBucketById(tenantId, bucketId) == null) return null;

            Obj obj = _Database.Objects.GetById(id, bucketId);
            if (obj == null) return null;
            if (!String.Equals(obj.TenantId, tenantId, StringComparison.Ordinal)) return null;
            return obj;
        }

        internal bool AddObject(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (String.IsNullOrEmpty(obj.TenantId)) throw new ArgumentNullException(nameof(obj.TenantId));
            if (String.IsNullOrEmpty(obj.BucketId)) throw new ArgumentNullException(nameof(obj.BucketId));
            if (String.IsNullOrEmpty(obj.Key)) throw new ArgumentNullException(nameof(obj.Key));
            if (GetBucketById(obj.TenantId, obj.BucketId) == null) return false;
            if (GetObjectById(obj.TenantId, obj.BucketId, obj.Id) != null) return false;

            _Database.Objects.Insert(obj);
            return true;
        }

        internal bool UpdateObject(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (String.IsNullOrEmpty(obj.TenantId)) throw new ArgumentNullException(nameof(obj.TenantId));
            if (String.IsNullOrEmpty(obj.BucketId)) throw new ArgumentNullException(nameof(obj.BucketId));

            Obj existing = GetObjectById(obj.TenantId, obj.BucketId, obj.Id);
            if (existing == null) return false;

            obj.CreatedUtc = existing.CreatedUtc;
            _Database.Objects.Update(obj);
            return true;
        }

        internal bool DeleteObject(string tenantId, string bucketId, string id)
        {
            Obj obj = GetObjectById(tenantId, bucketId, id);
            if (obj == null) return false;
            _Database.Objects.Delete(obj);
            return true;
        }

        #endregion

        #region Internal-Tag-and-Acl-Methods

        internal List<BucketTag> GetBucketTags(string tenantId, string bucketId, EnumerationQuery query)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));

            List<BucketTag> tags = new List<BucketTag>();
            if (!String.IsNullOrEmpty(bucketId))
            {
                if (GetBucketById(tenantId, bucketId) == null) return tags;
                tags.AddRange(_Database.BucketTags.GetByBucketId(tenantId, bucketId));
            }
            else
            {
                foreach (Bucket bucket in GetBuckets(tenantId))
                {
                    tags.AddRange(_Database.BucketTags.GetByBucketId(tenantId, bucket.Id));
                }
            }

            return tags.OrderBy(t => t.Id).ToList();
        }

        internal EnumerationResult<BucketTag> EnumerateBucketTags(EnumerationQuery query, string bucketId)
        {
            if (query == null) query = new EnumerationQuery();
            List<string> predicates = TenantPredicates(query.TenantId);

            if (!String.IsNullOrEmpty(bucketId))
            {
                predicates.Add("bucket_id = " + ControlPlaneSql.StringLiteral(bucketId));
            }

            predicates.Add(StringFilter(query, "id", "id", false));
            predicates.Add(StringFilter(query, "bucketId", "bucket_id", false));
            predicates.Add(StringFilter(query, "key", SqlColumn("key"), false));
            predicates.Add(StringFilter(query, "value", SqlColumn("value"), false));

            return EnumerateTable(
                query,
                "buckettags",
                CombinePredicates(predicates),
                TagSortFields(),
                "id",
                ControlPlaneDataMapper.BucketTag);
        }

        internal BucketTag GetBucketTagById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.BucketTags.GetById(tenantId, id);
        }

        internal bool AddBucketTag(BucketTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (String.IsNullOrEmpty(tag.TenantId)) throw new ArgumentNullException(nameof(tag.TenantId));
            if (String.IsNullOrEmpty(tag.BucketId)) throw new ArgumentNullException(nameof(tag.BucketId));
            if (String.IsNullOrEmpty(tag.Key)) throw new ArgumentNullException(nameof(tag.Key));
            if (GetBucketById(tag.TenantId, tag.BucketId) == null) return false;
            if (_Database.BucketTags.ExistsById(tag.TenantId, tag.Id)) return false;
            _Database.BucketTags.Insert(tag);
            return true;
        }

        internal bool UpdateBucketTag(BucketTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            BucketTag existing = GetBucketTagById(tag.TenantId, tag.Id);
            if (existing == null) return false;
            if (GetBucketById(tag.TenantId, tag.BucketId) == null) return false;
            tag.CreatedUtc = existing.CreatedUtc;
            _Database.BucketTags.Update(tag);
            return true;
        }

        internal bool DeleteBucketTag(string tenantId, string id)
        {
            if (GetBucketTagById(tenantId, id) == null) return false;
            _Database.BucketTags.DeleteById(tenantId, id);
            return true;
        }

        internal List<ObjectTag> GetObjectTags(string tenantId, string bucketId, string objectId, EnumerationQuery query)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));

            List<ObjectTag> tags = new List<ObjectTag>();
            if (!String.IsNullOrEmpty(objectId))
            {
                Obj obj = FindObjectById(tenantId, bucketId, objectId);
                if (obj == null) return tags;
                tags.AddRange(_Database.ObjectTags.GetByObjectId(tenantId, obj.Id, obj.BucketId));
            }
            else if (!String.IsNullOrEmpty(bucketId))
            {
                if (GetBucketById(tenantId, bucketId) == null) return tags;
                foreach (Obj obj in GetObjects(tenantId, bucketId, query))
                {
                    tags.AddRange(_Database.ObjectTags.GetByObjectId(tenantId, obj.Id, bucketId));
                }
            }
            else
            {
                foreach (Bucket bucket in GetBuckets(tenantId))
                {
                    foreach (Obj obj in GetObjects(tenantId, bucket.Id, query))
                    {
                        tags.AddRange(_Database.ObjectTags.GetByObjectId(tenantId, obj.Id, bucket.Id));
                    }
                }
            }

            return tags.OrderBy(t => t.Id).ToList();
        }

        internal EnumerationResult<ObjectTag> EnumerateObjectTags(EnumerationQuery query, string bucketId, string objectId)
        {
            if (query == null) query = new EnumerationQuery();
            List<string> predicates = TenantPredicates(query.TenantId);

            if (!String.IsNullOrEmpty(bucketId))
            {
                predicates.Add("bucket_id = " + ControlPlaneSql.StringLiteral(bucketId));
            }

            if (!String.IsNullOrEmpty(objectId))
            {
                predicates.Add("object_id = " + ControlPlaneSql.StringLiteral(objectId));
            }

            predicates.Add(StringFilter(query, "id", "id", false));
            predicates.Add(StringFilter(query, "bucketId", "bucket_id", false));
            predicates.Add(StringFilter(query, "objectId", "object_id", false));
            predicates.Add(StringFilter(query, "key", SqlColumn("key"), false));
            predicates.Add(StringFilter(query, "value", SqlColumn("value"), false));

            return EnumerateTable(
                query,
                "objecttags",
                CombinePredicates(predicates),
                new Dictionary<string, string>
                {
                    { "id", "id" },
                    { "tenantId", "tenant_id" },
                    { "bucketId", "bucket_id" },
                    { "objectId", "object_id" },
                    { "key", SqlColumn("key") },
                    { "value", SqlColumn("value") },
                    { "createdUtc", "createdutc" }
                },
                "id",
                ControlPlaneDataMapper.ObjectTag);
        }

        internal ObjectTag GetObjectTagById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.ObjectTags.GetById(tenantId, id);
        }

        internal bool AddObjectTag(ObjectTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (String.IsNullOrEmpty(tag.TenantId)) throw new ArgumentNullException(nameof(tag.TenantId));
            if (String.IsNullOrEmpty(tag.BucketId)) throw new ArgumentNullException(nameof(tag.BucketId));
            if (String.IsNullOrEmpty(tag.ObjectId)) throw new ArgumentNullException(nameof(tag.ObjectId));
            if (String.IsNullOrEmpty(tag.Key)) throw new ArgumentNullException(nameof(tag.Key));
            if (GetObjectById(tag.TenantId, tag.BucketId, tag.ObjectId) == null) return false;
            if (_Database.ObjectTags.ExistsById(tag.TenantId, tag.Id)) return false;
            _Database.ObjectTags.Insert(tag);
            return true;
        }

        internal bool UpdateObjectTag(ObjectTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            ObjectTag existing = GetObjectTagById(tag.TenantId, tag.Id);
            if (existing == null) return false;
            if (GetObjectById(tag.TenantId, tag.BucketId, tag.ObjectId) == null) return false;
            tag.CreatedUtc = existing.CreatedUtc;
            _Database.ObjectTags.Update(tag);
            return true;
        }

        internal bool DeleteObjectTag(string tenantId, string id)
        {
            if (GetObjectTagById(tenantId, id) == null) return false;
            _Database.ObjectTags.DeleteById(tenantId, id);
            return true;
        }

        internal List<BucketAcl> GetBucketAcls(string tenantId, string bucketId, EnumerationQuery query)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));

            List<BucketAcl> acls = new List<BucketAcl>();
            if (!String.IsNullOrEmpty(bucketId))
            {
                if (GetBucketById(tenantId, bucketId) == null) return acls;
                acls.AddRange(_Database.BucketAcls.GetByBucketId(tenantId, bucketId));
            }
            else
            {
                foreach (Bucket bucket in GetBuckets(tenantId))
                {
                    acls.AddRange(_Database.BucketAcls.GetByBucketId(tenantId, bucket.Id));
                }
            }

            return acls.OrderBy(a => a.Id).ToList();
        }

        internal EnumerationResult<BucketAcl> EnumerateBucketAcls(EnumerationQuery query, string bucketId)
        {
            if (query == null) query = new EnumerationQuery();
            List<string> predicates = TenantPredicates(query.TenantId);

            if (!String.IsNullOrEmpty(bucketId))
            {
                predicates.Add("bucket_id = " + ControlPlaneSql.StringLiteral(bucketId));
            }

            predicates.Add(StringFilter(query, "id", "id", false));
            predicates.Add(StringFilter(query, "bucketId", "bucket_id", false));
            predicates.Add(StringFilter(query, "userId", "user_id", false));
            predicates.Add(StringFilter(query, "userGroup", "usergroup", false));
            predicates.Add(StringFilter(query, "issuedByUserId", "issued_by_user_id", false));
            predicates.Add(BoolFilter(query, "permitRead", "permitread"));
            predicates.Add(BoolFilter(query, "permitWrite", "permitwrite"));
            predicates.Add(BoolFilter(query, "fullControl", "fullcontrol"));

            return EnumerateTable(
                query,
                "bucketacls",
                CombinePredicates(predicates),
                AclSortFields(false),
                "id",
                ControlPlaneDataMapper.BucketAcl);
        }

        internal BucketAcl GetBucketAclById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.BucketAcls.GetById(tenantId, id);
        }

        internal bool AddBucketAcl(BucketAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            if (String.IsNullOrEmpty(acl.TenantId)) throw new ArgumentNullException(nameof(acl.TenantId));
            if (String.IsNullOrEmpty(acl.BucketId)) throw new ArgumentNullException(nameof(acl.BucketId));
            if (GetBucketById(acl.TenantId, acl.BucketId) == null) return false;
            if (_Database.BucketAcls.ExistsById(acl.TenantId, acl.Id)) return false;
            _Database.BucketAcls.Insert(acl);
            return true;
        }

        internal bool UpdateBucketAcl(BucketAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            BucketAcl existing = GetBucketAclById(acl.TenantId, acl.Id);
            if (existing == null) return false;
            if (GetBucketById(acl.TenantId, acl.BucketId) == null) return false;
            acl.CreatedUtc = existing.CreatedUtc;
            _Database.BucketAcls.Update(acl);
            return true;
        }

        internal bool DeleteBucketAcl(string tenantId, string id)
        {
            if (GetBucketAclById(tenantId, id) == null) return false;
            _Database.BucketAcls.DeleteById(tenantId, id);
            return true;
        }

        internal List<ObjectAcl> GetObjectAcls(string tenantId, string bucketId, string objectId, EnumerationQuery query)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));

            List<ObjectAcl> acls = new List<ObjectAcl>();
            if (!String.IsNullOrEmpty(objectId))
            {
                Obj obj = FindObjectById(tenantId, bucketId, objectId);
                if (obj == null) return acls;
                acls.AddRange(_Database.ObjectAcls.GetByObjectId(tenantId, obj.Id, obj.BucketId));
            }
            else if (!String.IsNullOrEmpty(bucketId))
            {
                if (GetBucketById(tenantId, bucketId) == null) return acls;
                acls.AddRange(_Database.ObjectAcls.GetByBucketId(tenantId, bucketId));
            }
            else
            {
                foreach (Bucket bucket in GetBuckets(tenantId))
                {
                    acls.AddRange(_Database.ObjectAcls.GetByBucketId(tenantId, bucket.Id));
                }
            }

            return acls.OrderBy(a => a.Id).ToList();
        }

        internal EnumerationResult<ObjectAcl> EnumerateObjectAcls(EnumerationQuery query, string bucketId, string objectId)
        {
            if (query == null) query = new EnumerationQuery();
            List<string> predicates = TenantPredicates(query.TenantId);

            if (!String.IsNullOrEmpty(bucketId))
            {
                predicates.Add("bucket_id = " + ControlPlaneSql.StringLiteral(bucketId));
            }

            if (!String.IsNullOrEmpty(objectId))
            {
                predicates.Add("object_id = " + ControlPlaneSql.StringLiteral(objectId));
            }

            predicates.Add(StringFilter(query, "id", "id", false));
            predicates.Add(StringFilter(query, "bucketId", "bucket_id", false));
            predicates.Add(StringFilter(query, "objectId", "object_id", false));
            predicates.Add(StringFilter(query, "userId", "user_id", false));
            predicates.Add(StringFilter(query, "userGroup", "usergroup", false));
            predicates.Add(StringFilter(query, "issuedByUserId", "issued_by_user_id", false));
            predicates.Add(BoolFilter(query, "permitRead", "permitread"));
            predicates.Add(BoolFilter(query, "permitWrite", "permitwrite"));
            predicates.Add(BoolFilter(query, "fullControl", "fullcontrol"));

            return EnumerateTable(
                query,
                "objectacls",
                CombinePredicates(predicates),
                AclSortFields(true),
                "id",
                ControlPlaneDataMapper.ObjectAcl);
        }

        internal ObjectAcl GetObjectAclById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.ObjectAcls.GetById(tenantId, id);
        }

        internal bool AddObjectAcl(ObjectAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            if (String.IsNullOrEmpty(acl.TenantId)) throw new ArgumentNullException(nameof(acl.TenantId));
            if (String.IsNullOrEmpty(acl.BucketId)) throw new ArgumentNullException(nameof(acl.BucketId));
            if (String.IsNullOrEmpty(acl.ObjectId)) throw new ArgumentNullException(nameof(acl.ObjectId));
            if (GetObjectById(acl.TenantId, acl.BucketId, acl.ObjectId) == null) return false;
            if (_Database.ObjectAcls.ExistsById(acl.TenantId, acl.Id)) return false;
            _Database.ObjectAcls.Insert(acl);
            return true;
        }

        internal bool UpdateObjectAcl(ObjectAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            ObjectAcl existing = GetObjectAclById(acl.TenantId, acl.Id);
            if (existing == null) return false;
            if (GetObjectById(acl.TenantId, acl.BucketId, acl.ObjectId) == null) return false;
            acl.CreatedUtc = existing.CreatedUtc;
            _Database.ObjectAcls.Update(acl);
            return true;
        }

        internal bool DeleteObjectAcl(string tenantId, string id)
        {
            if (GetObjectAclById(tenantId, id) == null) return false;
            _Database.ObjectAcls.DeleteById(tenantId, id);
            return true;
        }

        private Obj FindObjectById(string tenantId, string bucketId, string objectId)
        {
            if (String.IsNullOrEmpty(objectId)) return null;
            if (!String.IsNullOrEmpty(bucketId)) return GetObjectById(tenantId, bucketId, objectId);

            foreach (Bucket bucket in GetBuckets(tenantId))
            {
                Obj obj = GetObjectById(tenantId, bucket.Id, objectId);
                if (obj != null) return obj;
            }

            return null;
        }

        #endregion

        #region Internal-Upload-Methods

        internal Less3.Classes.Upload GetUploadById(string uploadId)
        {
            if (String.IsNullOrEmpty(uploadId)) return null;
            return _Database.Uploads.GetById(uploadId);
        }

        internal Less3.Classes.Upload GetUploadById(string tenantId, string uploadId)
        {
            if (String.IsNullOrEmpty(tenantId)) return null;
            if (String.IsNullOrEmpty(uploadId)) return null;
            return _Database.Uploads.GetById(tenantId, uploadId);
        }

        internal List<Less3.Classes.Upload> GetUploads()
        {
            return _Database.Uploads.GetAll();
        }

        internal List<Less3.Classes.Upload> GetUploadsByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) return new List<Less3.Classes.Upload>();
            return _Database.Uploads.GetByBucketId(bucketId);
        }

        internal List<Less3.Classes.Upload> GetUploadsByBucketId(string tenantId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) return new List<Less3.Classes.Upload>();
            if (String.IsNullOrEmpty(bucketId)) return new List<Less3.Classes.Upload>();
            return _Database.Uploads.GetByBucketId(tenantId, bucketId);
        }

        internal EnumerationResult<Less3.Classes.Upload> EnumerateUploads(EnumerationQuery query, string bucketId)
        {
            if (query == null) query = new EnumerationQuery();
            List<string> predicates = TenantPredicates(query.TenantId);

            if (!String.IsNullOrEmpty(bucketId))
            {
                predicates.Add("bucket_id = " + ControlPlaneSql.StringLiteral(bucketId));
            }

            predicates.Add(StringFilter(query, "id", "id", false));
            predicates.Add(StringFilter(query, "bucketId", "bucket_id", false));
            predicates.Add(StringFilter(query, "ownerId", "owner_id", false));
            predicates.Add(StringFilter(query, "authorId", "author_id", false));
            predicates.Add(PrefixFilter(query, "prefix", SqlColumn("key")));
            predicates.Add(StringFilter(query, "key", SqlColumn("key"), false));

            return EnumerateTable(
                query,
                "uploads",
                CombinePredicates(predicates),
                new Dictionary<string, string>
                {
                    { "id", "id" },
                    { "tenantId", "tenant_id" },
                    { "bucketId", "bucket_id" },
                    { "ownerId", "owner_id" },
                    { "authorId", "author_id" },
                    { "key", SqlColumn("key") },
                    { "createdUtc", "createdutc" },
                    { "lastAccessUtc", "lastaccessutc" },
                    { "expirationUtc", "expirationutc" }
                },
                "id",
                ControlPlaneDataMapper.Upload);
        }

        internal void AddUpload(Less3.Classes.Upload upload)
        {
            if (upload == null) throw new ArgumentNullException(nameof(upload));
            _Database.Uploads.Insert(upload);
        }

        internal void DeleteUpload(string uploadId)
        {
            if (String.IsNullOrEmpty(uploadId)) return;
            _Database.Uploads.DeleteById(uploadId);
        }

        internal void DeleteUpload(string tenantId, string uploadId)
        {
            if (String.IsNullOrEmpty(tenantId)) return;
            if (String.IsNullOrEmpty(uploadId)) return;
            _Database.Uploads.DeleteById(tenantId, uploadId);
        }

        internal void AddUploadPart(UploadPart part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            _Database.UploadParts.Insert(part);
        }

        internal List<UploadPart> GetUploadPartsByUploadId(string uploadId)
        {
            if (String.IsNullOrEmpty(uploadId)) return null;
            return _Database.UploadParts.GetByUploadId(uploadId);
        }

        internal List<UploadPart> GetUploadPartsByUploadId(string tenantId, string uploadId)
        {
            if (String.IsNullOrEmpty(tenantId)) return null;
            if (String.IsNullOrEmpty(uploadId)) return null;
            return _Database.UploadParts.GetByUploadId(tenantId, uploadId);
        }

        internal void DeleteUploadParts(string uploadId)
        {
            if (String.IsNullOrEmpty(uploadId)) return;
            _Database.UploadParts.DeleteByUploadId(uploadId);
        }

        internal void DeleteUploadParts(string tenantId, string uploadId)
        {
            if (String.IsNullOrEmpty(tenantId)) return;
            if (String.IsNullOrEmpty(uploadId)) return;
            _Database.UploadParts.DeleteByUploadId(tenantId, uploadId);
        }

        internal void DeleteUploadPart(string uploadId, int partNumber)
        {
            if (String.IsNullOrEmpty(uploadId)) return;
            if (partNumber < 1) return;
            _Database.UploadParts.DeleteByUploadIdAndPartNumber(uploadId, partNumber);
        }

        internal void DeleteUploadPart(string tenantId, string uploadId, int partNumber)
        {
            if (String.IsNullOrEmpty(tenantId)) return;
            if (String.IsNullOrEmpty(uploadId)) return;
            if (partNumber < 1) return;
            _Database.UploadParts.DeleteByUploadIdAndPartNumber(tenantId, uploadId, partNumber);
        }

        #endregion

        #region Internal-RequestHistory-Methods

        internal List<RequestHistory> GetRequestHistories()
        {
            return _Database.RequestHistory.GetAll();
        }

        internal RequestHistory GetRequestHistoryById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.RequestHistory.GetById(id);
        }

        internal void AddRequestHistory(RequestHistory entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            _Database.RequestHistory.Insert(entry);
        }

        internal void DeleteRequestHistory(string id)
        {
            if (String.IsNullOrEmpty(id)) return;
            _Database.RequestHistory.DeleteById(id);
        }

        internal void DeleteRequestHistoriesOlderThan(DateTime cutoff)
        {
            _Database.RequestHistory.DeleteOlderThan(cutoff);
        }

        internal int CountRequestHistoriesOlderThan(DateTime cutoff)
        {
            DataTable countTable = _Database.ExecuteQuery(
                ControlPlaneSql.CountString(
                    "requesthistory",
                    "createdutc < " + ControlPlaneSql.DateLiteral(cutoff)),
                false).GetAwaiter().GetResult();

            long count = ControlPlaneDataMapper.Count(countTable);
            if (count > Int32.MaxValue) return Int32.MaxValue;
            return (int)count;
        }

        internal List<RequestHistory> GetRequestHistoriesInRange(DateTime startUtc, DateTime endUtc)
        {
            return _Database.RequestHistory.GetInRange(startUtc, endUtc);
        }

        internal EnumerationResult<RequestHistory> EnumerateRequestHistories(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            List<string> predicates = TenantPredicates(query.TenantId);

            if (query.StartUtc.HasValue)
            {
                predicates.Add("createdutc >= " + ControlPlaneSql.DateLiteral(query.StartUtc.Value));
            }

            if (query.EndUtc.HasValue)
            {
                predicates.Add("createdutc <= " + ControlPlaneSql.DateLiteral(query.EndUtc.Value));
            }

            predicates.Add(StringFilter(query, "id", "id", false));
            predicates.Add(StringFilter(query, "method", "httpmethod", true));
            predicates.Add(StringFilter(query, "sourceIp", "sourceip", true));
            predicates.Add(StringFilter(query, "requestType", "requesttype", true));
            predicates.Add(StringFilter(query, "userId", "user_id", false));
            predicates.Add(StringFilter(query, "accessKey", "accesskey", false));
            predicates.Add(IntFilter(query, "status", "statuscode"));
            predicates.Add(BoolFilter(query, "success", "success"));

            return EnumerateTable(
                query,
                "requesthistory",
                CombinePredicates(predicates),
                new Dictionary<string, string>
                {
                    { "id", "id" },
                    { "tenantId", "tenant_id" },
                    { "method", "httpmethod" },
                    { "status", "statuscode" },
                    { "success", "success" },
                    { "durationMs", "durationms" },
                    { "requestType", "requesttype" },
                    { "userId", "user_id" },
                    { "accessKey", "accesskey" },
                    { "createdUtc", "createdutc" }
                },
                "createdutc",
                ControlPlaneDataMapper.RequestHistory);
        }

        #endregion

        #region Private-Enumeration-Methods

        private EnumerationResult<T> EnumerateTable<T>(
            EnumerationQuery query,
            string table,
            string where,
            Dictionary<string, string> allowedSortFields,
            string defaultSortColumn,
            Func<DataRow, T> mapper)
        {
            if (query == null) query = new EnumerationQuery();
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            DataTable countTable = _Database.ExecuteQuery(
                ControlPlaneSql.CountString(table, where),
                false).GetAwaiter().GetResult();

            DataTable dataTable = _Database.ExecuteQuery(
                ControlPlaneSql.QueryString(query, table, where, allowedSortFields, defaultSortColumn, _Database.Dialect),
                false).GetAwaiter().GetResult();

            List<T> items = ControlPlaneDataMapper.List(dataTable, mapper);
            long total = ControlPlaneDataMapper.Count(countTable);

            EnumerationResult<T> result = new EnumerationResult<T>();
            result.Items = items;
            result.Total = total;
            result.Limit = query.Limit;
            result.Offset = query.Offset;
            result.HasMore = query.Offset + items.Count < total;
            if (result.HasMore) result.NextContinuationToken = (query.Offset + items.Count).ToString();
            return result;
        }

        private List<string> TenantPredicates(string tenantId)
        {
            List<string> predicates = new List<string>();
            if (!String.IsNullOrEmpty(tenantId))
            {
                predicates.Add("tenant_id = " + ControlPlaneSql.StringLiteral(tenantId));
            }
            return predicates;
        }

        private string CombinePredicates(IEnumerable<string> predicates)
        {
            if (predicates == null) return null;
            List<string> filtered = predicates
                .Where(p => !String.IsNullOrWhiteSpace(p))
                .ToList();
            if (filtered.Count < 1) return null;
            return String.Join(" AND ", filtered);
        }

        private string StringFilter(EnumerationQuery query, string filterName, string column, bool ignoreCase)
        {
            string value = FilterValue(query, filterName);
            if (String.IsNullOrEmpty(value)) return null;

            string literal = ControlPlaneSql.StringLiteral(value);
            if (ignoreCase)
            {
                return "LOWER(" + column + ") = LOWER(" + literal + ")";
            }

            return column + " = " + literal;
        }

        private string PrefixFilter(EnumerationQuery query, string filterName, string column)
        {
            string value = FilterValue(query, filterName);
            if (String.IsNullOrEmpty(value)) return null;
            string escaped = EscapeLike(value) + "%";
            return column + " LIKE " + ControlPlaneSql.StringLiteral(escaped) + " ESCAPE '\\'";
        }

        private string BoolFilter(EnumerationQuery query, string filterName, string column)
        {
            string value = FilterValue(query, filterName);
            if (String.IsNullOrEmpty(value)) return null;
            if (!Boolean.TryParse(value, out bool parsed)) return null;
            return column + " = " + ControlPlaneSql.BoolLiteral(parsed, _Database.Dialect);
        }

        private string IntFilter(EnumerationQuery query, string filterName, string column)
        {
            string value = FilterValue(query, filterName);
            if (String.IsNullOrEmpty(value)) return null;
            if (!Int32.TryParse(value, out int parsed)) return null;
            return column + " = " + parsed;
        }

        private static string FilterValue(EnumerationQuery query, string filterName)
        {
            if (query == null || query.Filters == null) return null;
            if (!query.Filters.ContainsKey(filterName)) return null;
            return query.Filters[filterName];
        }

        private static string EscapeLike(string value)
        {
            if (value == null) return null;
            return value
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }

        private string SqlColumn(string column)
        {
            if (_Database.Dialect == SqlDialect.SqlServer
                && (String.Equals(column, "key", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(column, "value", StringComparison.OrdinalIgnoreCase)))
            {
                return "[" + column + "]";
            }

            return column;
        }

        private Dictionary<string, string> TagSortFields()
        {
            return new Dictionary<string, string>
            {
                { "id", "id" },
                { "tenantId", "tenant_id" },
                { "bucketId", "bucket_id" },
                { "key", SqlColumn("key") },
                { "value", SqlColumn("value") },
                { "createdUtc", "createdutc" }
            };
        }

        private Dictionary<string, string> AclSortFields(bool includeObject)
        {
            Dictionary<string, string> fields = new Dictionary<string, string>
            {
                { "id", "id" },
                { "tenantId", "tenant_id" },
                { "bucketId", "bucket_id" },
                { "userId", "user_id" },
                { "userGroup", "usergroup" },
                { "issuedByUserId", "issued_by_user_id" },
                { "createdUtc", "createdutc" }
            };

            if (includeObject) fields["objectId"] = "object_id";
            return fields;
        }

        #endregion
    }
}
