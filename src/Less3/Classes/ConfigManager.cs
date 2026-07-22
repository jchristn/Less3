namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Less3.Database;
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

            if (BucketExists(bucket.TenantId, bucket.Name))
            {
                _Logging.Warn("ConfigManager AddBucket bucket " + bucket.Name + " already exists");
                return false;
            }

            _Database.Buckets.Insert(bucket);
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

        internal List<RequestHistory> GetRequestHistoriesInRange(DateTime startUtc, DateTime endUtc)
        {
            return _Database.RequestHistory.GetInRange(startUtc, endUtc);
        }

        #endregion
    }
}
