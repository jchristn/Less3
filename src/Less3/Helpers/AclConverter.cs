namespace Less3.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    using S3ServerLibrary;
    using S3ServerLibrary.S3Objects;

    using Less3.Classes;

    using SyslogLogging;

    /// <summary>
    /// ACL conversion helper methods.
    /// Provides bidirectional conversion between internal ACL representations and S3 API objects.
    /// </summary>
    internal static class AclConverter
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Converts bucket ACLs to an AccessControlPolicy for API responses.
        /// </summary>
        /// <param name="acls">List of bucket ACLs to convert.</param>
        /// <param name="owner">Bucket owner user.</param>
        /// <param name="config">Configuration manager for user lookups.</param>
        /// <param name="logging">Logging module for warnings about misconfigured ACLs.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <returns>AccessControlPolicy containing owner information and grants.</returns>
        /// <exception cref="ArgumentNullException">Thrown when owner, config, or logging is null.</exception>
        internal static AccessControlPolicy BucketAclsToPolicy(
            List<BucketAcl> acls,
            User owner,
            ConfigManager config,
            LoggingModule logging,
            string header)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            AccessControlPolicy acp = new AccessControlPolicy();
            acp.Owner = new Owner();
            acp.Owner.DisplayName = owner.Name;
            acp.Owner.ID = owner.Id;

            acp.Acl = new AccessControlList();
            acp.Acl.Grants = new List<Grant>();

            if (acls == null || acls.Count == 0)
            {
                return acp;
            }

            foreach (BucketAcl curr in acls)
            {
                if (!String.IsNullOrEmpty(curr.UserId))
                {
                    AddUserGrantsToList(curr.UserId, curr, acp.Acl.Grants, config, logging, header);
                }
                else if (!String.IsNullOrEmpty(curr.UserGroup))
                {
                    AddGroupGrantsToList(curr.UserGroup, curr, acp.Acl.Grants);
                }
                else
                {
                    logging.Warn(header + "incorrectly configured bucket ACL ID " + curr.Id + " (not user or group)");
                }
            }

            return acp;
        }

        /// <summary>
        /// Converts object ACLs to an AccessControlPolicy for API responses.
        /// </summary>
        /// <param name="acls">List of object ACLs to convert.</param>
        /// <param name="owner">Object owner user.</param>
        /// <param name="config">Configuration manager for user lookups.</param>
        /// <param name="logging">Logging module for warnings about misconfigured ACLs.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <returns>AccessControlPolicy containing owner information and grants.</returns>
        /// <exception cref="ArgumentNullException">Thrown when owner, config, or logging is null.</exception>
        internal static AccessControlPolicy ObjectAclsToPolicy(
            List<ObjectAcl> acls,
            User owner,
            ConfigManager config,
            LoggingModule logging,
            string header)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            AccessControlPolicy acp = new AccessControlPolicy();
            acp.Owner = new Owner();
            acp.Owner.DisplayName = owner.Name;
            acp.Owner.ID = owner.Id;

            acp.Acl = new AccessControlList();
            acp.Acl.Grants = new List<Grant>();

            if (acls == null || acls.Count == 0)
            {
                return acp;
            }

            foreach (ObjectAcl curr in acls)
            {
                if (!String.IsNullOrEmpty(curr.UserId))
                {
                    AddUserGrantsToList(curr.UserId, curr, acp.Acl.Grants, config, logging, header);
                }
                else if (!String.IsNullOrEmpty(curr.UserGroup))
                {
                    AddGroupGrantsToList(curr.UserGroup, curr, acp.Acl.Grants);
                }
                else
                {
                    logging.Warn(header + "incorrectly configured object ACL in ID " + curr.Id);
                }
            }

            return acp;
        }

        /// <summary>
        /// Converts an AccessControlPolicy and request headers to a list of bucket ACLs.
        /// </summary>
        /// <param name="acp">AccessControlPolicy from request body. May be null.</param>
        /// <param name="headers">HTTP request headers containing ACL grants. May be null.</param>
        /// <param name="currentUser">User making the request.</param>
        /// <param name="bucketId">Id of the bucket.</param>
        /// <param name="ownerId">Id of the bucket owner.</param>
        /// <param name="config">Configuration manager for user lookups.</param>
        /// <param name="logging">Logging module for warnings about invalid grants.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <returns>List of BucketAcl objects ready for database insertion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when currentUser, config, or logging is null.</exception>
        internal static List<BucketAcl> PolicyToBucketAcls(
            AccessControlPolicy acp,
            NameValueCollection headers,
            User currentUser,
            string bucketId,
            string ownerId,
            ConfigManager config,
            LoggingModule logging,
            string header)
        {
            if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            List<Grant> allGrants = new List<Grant>();

            List<Grant> headerGrants = GrantsFromHeaders(currentUser, headers, config);
            if (headerGrants != null && headerGrants.Count > 0)
            {
                allGrants.AddRange(headerGrants);
            }

            if (acp != null && acp.Acl != null && acp.Acl.Grants != null && acp.Acl.Grants.Count > 0)
            {
                allGrants.AddRange(acp.Acl.Grants);
            }

            List<BucketAcl> acls = new List<BucketAcl>();

            foreach (Grant curr in allGrants)
            {
                BucketAcl acl = null;

                if (!String.IsNullOrEmpty(curr.Grantee.ID))
                {
                    User tempUser = config.GetUserById(currentUser.TenantId, curr.Grantee.ID);
                    if (tempUser == null)
                    {
                        logging.Warn(header + "unable to find user Id " + curr.Grantee.ID + " in tenant " + currentUser.TenantId);
                        throw new S3Exception(new Error(ErrorCode.InvalidRequest));
                    }

                    acl = GrantToBucketUserAcl(curr, curr.Grantee.ID, ownerId, bucketId);
                }
                else if (!String.IsNullOrEmpty(curr.Grantee.URI))
                {
                    acl = GrantToBucketGroupAcl(curr, curr.Grantee.URI, ownerId, bucketId);
                }

                if (acl != null)
                {
                    acls.Add(acl);
                }
            }

            return acls;
        }

        /// <summary>
        /// Converts an AccessControlPolicy and request headers to a list of object ACLs.
        /// </summary>
        /// <param name="acp">AccessControlPolicy from request body. May be null.</param>
        /// <param name="headers">HTTP request headers containing ACL grants. May be null.</param>
        /// <param name="currentUser">User making the request.</param>
        /// <param name="bucketId">Id of the bucket containing the object.</param>
        /// <param name="objectId">Id of the object.</param>
        /// <param name="ownerId">Id of the object owner.</param>
        /// <param name="config">Configuration manager for user lookups.</param>
        /// <param name="logging">Logging module for warnings about invalid grants.</param>
        /// <param name="header">Log header prefix for consistent log formatting.</param>
        /// <returns>List of ObjectAcl objects ready for database insertion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when currentUser, config, or logging is null.</exception>
        internal static List<ObjectAcl> PolicyToObjectAcls(
            AccessControlPolicy acp,
            NameValueCollection headers,
            User currentUser,
            string bucketId,
            string objectId,
            string ownerId,
            ConfigManager config,
            LoggingModule logging,
            string header)
        {
            if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            List<Grant> allGrants = new List<Grant>();

            List<Grant> headerGrants = GrantsFromHeaders(currentUser, headers, config);
            if (headerGrants != null && headerGrants.Count > 0)
            {
                allGrants.AddRange(headerGrants);
            }

            if (acp != null && acp.Acl != null && acp.Acl.Grants != null && acp.Acl.Grants.Count > 0)
            {
                allGrants.AddRange(acp.Acl.Grants);
            }

            List<ObjectAcl> acls = new List<ObjectAcl>();

            foreach (Grant curr in allGrants)
            {
                ObjectAcl acl = null;

                if (!String.IsNullOrEmpty(curr.Grantee.ID))
                {
                    User tempUser = config.GetUserById(currentUser.TenantId, curr.Grantee.ID);
                    if (tempUser == null)
                    {
                        logging.Warn(header + "unable to find user Id " + curr.Grantee.ID + " in tenant " + currentUser.TenantId);
                        throw new S3Exception(new Error(ErrorCode.InvalidRequest));
                    }

                    acl = GrantToObjectUserAcl(curr, curr.Grantee.ID, ownerId, bucketId, objectId);
                }
                else if (!String.IsNullOrEmpty(curr.Grantee.URI))
                {
                    acl = GrantToObjectGroupAcl(curr, curr.Grantee.URI, ownerId, bucketId, objectId);
                }

                if (acl != null)
                {
                    acls.Add(acl);
                }
            }

            return acls;
        }

        /// <summary>
        /// Extracts ACL grants from HTTP request headers.
        /// Supports canned ACLs and individual grant headers.
        /// </summary>
        /// <param name="user">User making the request. Used for 'private' canned ACL.</param>
        /// <param name="headers">HTTP request headers to parse. May be null.</param>
        /// <param name="config">Configuration manager for user email/Id lookups.</param>
        /// <returns>List of Grant objects parsed from headers. Empty list if no grants found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when user or config is null.</exception>
        internal static List<Grant> GrantsFromHeaders(User user, NameValueCollection headers, ConfigManager config)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (config == null) throw new ArgumentNullException(nameof(config));

            List<Grant> ret = new List<Grant>();
            if (headers == null || headers.Count < 1) return ret;

            string headerVal = null;
            string[] grantees = null;
            Grant grant = null;

            if (headers.AllKeys.Contains(Constants.Headers.AccessControlList.ToLower()))
            {
                headerVal = headers[Constants.Headers.AccessControlList.ToLower()];

                switch (headerVal)
                {
                    case "private":
                        grant = new Grant();
                        grant.Permission = PermissionEnum.FullControl;
                        grant.Grantee = new CanonicalUser();
                        grant.Grantee.ID = user.Id;
                        grant.Grantee.DisplayName = user.Name;
                        ret.Add(grant);
                        break;

                    case "public-read":
                        grant = new Grant();
                        grant.Permission = PermissionEnum.FullControl;
                        grant.Grantee = new CanonicalUser();
                        grant.Grantee.ID = user.Id;
                        grant.Grantee.DisplayName = user.Name;
                        ret.Add(grant);

                        grant = new Grant();
                        grant.Permission = PermissionEnum.Read;
                        grant.Grantee = new Group();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        ret.Add(grant);
                        break;

                    case "public-read-write":
                        grant = new Grant();
                        grant.Permission = PermissionEnum.FullControl;
                        grant.Grantee = new CanonicalUser();
                        grant.Grantee.ID = user.Id;
                        grant.Grantee.DisplayName = user.Name;
                        ret.Add(grant);

                        grant = new Grant();
                        grant.Permission = PermissionEnum.Read;
                        grant.Grantee = new Group();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        ret.Add(grant);

                        grant = new Grant();
                        grant.Permission = PermissionEnum.Write;
                        grant.Grantee = new Group();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        ret.Add(grant);
                        break;

                    case "authenticated-read":
                        grant = new Grant();
                        grant.Permission = PermissionEnum.FullControl;
                        grant.Grantee = new CanonicalUser();
                        grant.Grantee.ID = user.Id;
                        grant.Grantee.DisplayName = user.Name;
                        ret.Add(grant);

                        grant = new Grant();
                        grant.Permission = PermissionEnum.Read;
                        grant.Grantee = new Group();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AuthenticatedUsers";
                        ret.Add(grant);
                        break;
                }
            }

            if (headers.AllKeys.Contains(Constants.Headers.AclGrantRead.ToLower()))
            {
                headerVal = headers[Constants.Headers.AclGrantRead.ToLower()];
                grantees = headerVal.Split(',');
                if (grantees.Length > 0)
                {
                    foreach (string curr in grantees)
                    {
                        grant = null;
                        if (!GrantFromString(curr, PermissionEnum.Read, user.TenantId, config, out grant))
                        {
                            throw new S3Exception(new Error(ErrorCode.InvalidRequest));
                        }
                        ret.Add(grant);
                    }
                }
            }

            if (headers.AllKeys.Contains(Constants.Headers.AclGrantWrite.ToLower()))
            {
                headerVal = headers[Constants.Headers.AclGrantWrite.ToLower()];
                grantees = headerVal.Split(',');
                if (grantees.Length > 0)
                {
                    foreach (string curr in grantees)
                    {
                        grant = null;
                        if (!GrantFromString(curr, PermissionEnum.Write, user.TenantId, config, out grant))
                        {
                            throw new S3Exception(new Error(ErrorCode.InvalidRequest));
                        }
                        ret.Add(grant);
                    }
                }
            }

            if (headers.AllKeys.Contains(Constants.Headers.AclGrantReadAcp.ToLower()))
            {
                headerVal = headers[Constants.Headers.AclGrantReadAcp.ToLower()];
                grantees = headerVal.Split(',');
                if (grantees.Length > 0)
                {
                    foreach (string curr in grantees)
                    {
                        grant = null;
                        if (!GrantFromString(curr, PermissionEnum.ReadAcp, user.TenantId, config, out grant))
                        {
                            throw new S3Exception(new Error(ErrorCode.InvalidRequest));
                        }
                        ret.Add(grant);
                    }
                }
            }

            if (headers.AllKeys.Contains(Constants.Headers.AclGrantWriteAcp.ToLower()))
            {
                headerVal = headers[Constants.Headers.AclGrantWriteAcp.ToLower()];
                grantees = headerVal.Split(',');
                if (grantees.Length > 0)
                {
                    foreach (string curr in grantees)
                    {
                        grant = null;
                        if (!GrantFromString(curr, PermissionEnum.WriteAcp, user.TenantId, config, out grant))
                        {
                            throw new S3Exception(new Error(ErrorCode.InvalidRequest));
                        }
                        ret.Add(grant);
                    }
                }
            }

            if (headers.AllKeys.Contains(Constants.Headers.AclGrantFullControl.ToLower()))
            {
                headerVal = headers[Constants.Headers.AclGrantFullControl.ToLower()];
                grantees = headerVal.Split(',');
                if (grantees.Length > 0)
                {
                    foreach (string curr in grantees)
                    {
                        grant = null;
                        if (!GrantFromString(curr, PermissionEnum.FullControl, user.TenantId, config, out grant))
                        {
                            throw new S3Exception(new Error(ErrorCode.InvalidRequest));
                        }
                        ret.Add(grant);
                    }
                }
            }

            return ret;
        }

        #endregion

        #region Private-Methods

        private static void AddUserGrantsToList<T>(
            string userId,
            T acl,
            List<Grant> grants,
            ConfigManager config,
            LoggingModule logging,
            string header) where T : class
        {
            string tenantId = null;
            if (acl is BucketAcl tenantBucketAcl)
            {
                tenantId = tenantBucketAcl.TenantId;
            }
            else if (acl is ObjectAcl tenantObjectAcl)
            {
                tenantId = tenantObjectAcl.TenantId;
            }

            User tempUser = String.IsNullOrEmpty(tenantId)
                ? config.GetUserById(userId)
                : config.GetUserById(tenantId, userId);
            if (tempUser == null)
            {
                string aclId = null;
                if (acl is BucketAcl bucketAcl)
                {
                    aclId = bucketAcl.Id;
                }
                else if (acl is ObjectAcl objectAcl)
                {
                    aclId = objectAcl.Id;
                }

                logging.Warn(header + "unlinked ACL ID " + aclId + ", could not find user Id " + userId);
                return;
            }

            bool permitRead = false;
            bool permitWrite = false;
            bool permitReadAcp = false;
            bool permitWriteAcp = false;
            bool fullControl = false;

            if (acl is BucketAcl ba)
            {
                permitRead = ba.PermitRead;
                permitWrite = ba.PermitWrite;
                permitReadAcp = ba.PermitReadAcp;
                permitWriteAcp = ba.PermitWriteAcp;
                fullControl = ba.FullControl;
            }
            else if (acl is ObjectAcl oa)
            {
                permitRead = oa.PermitRead;
                permitWrite = oa.PermitWrite;
                permitReadAcp = oa.PermitReadAcp;
                permitWriteAcp = oa.PermitWriteAcp;
                fullControl = oa.FullControl;
            }

            if (permitRead)
            {
                Grant grant = new Grant();
                grant.Grantee = new CanonicalUser();
                grant.Grantee.DisplayName = tempUser.Name;
                grant.Grantee.ID = userId;
                grant.Permission = PermissionEnum.Read;
                grants.Add(grant);
            }

            if (permitReadAcp)
            {
                Grant grant = new Grant();
                grant.Grantee = new CanonicalUser();
                grant.Grantee.DisplayName = tempUser.Name;
                grant.Grantee.ID = userId;
                grant.Permission = PermissionEnum.ReadAcp;
                grants.Add(grant);
            }

            if (permitWrite)
            {
                Grant grant = new Grant();
                grant.Grantee = new CanonicalUser();
                grant.Grantee.DisplayName = tempUser.Name;
                grant.Grantee.ID = userId;
                grant.Permission = PermissionEnum.Write;
                grants.Add(grant);
            }

            if (permitWriteAcp)
            {
                Grant grant = new Grant();
                grant.Grantee = new CanonicalUser();
                grant.Grantee.DisplayName = tempUser.Name;
                grant.Grantee.ID = userId;
                grant.Permission = PermissionEnum.WriteAcp;
                grants.Add(grant);
            }

            if (fullControl)
            {
                Grant grant = new Grant();
                grant.Grantee = new CanonicalUser();
                grant.Grantee.DisplayName = tempUser.Name;
                grant.Grantee.ID = userId;
                grant.Permission = PermissionEnum.FullControl;
                grants.Add(grant);
            }
        }

        private static void AddGroupGrantsToList<T>(string userGroup, T acl, List<Grant> grants) where T : class
        {
            bool permitRead = false;
            bool permitWrite = false;
            bool permitReadAcp = false;
            bool permitWriteAcp = false;
            bool fullControl = false;

            if (acl is BucketAcl ba)
            {
                permitRead = ba.PermitRead;
                permitWrite = ba.PermitWrite;
                permitReadAcp = ba.PermitReadAcp;
                permitWriteAcp = ba.PermitWriteAcp;
                fullControl = ba.FullControl;
            }
            else if (acl is ObjectAcl oa)
            {
                permitRead = oa.PermitRead;
                permitWrite = oa.PermitWrite;
                permitReadAcp = oa.PermitReadAcp;
                permitWriteAcp = oa.PermitWriteAcp;
                fullControl = oa.FullControl;
            }

            if (permitRead)
            {
                Grant grant = new Grant();
                grant.Grantee = new Group();
                grant.Grantee.URI = userGroup;
                grant.Permission = PermissionEnum.Read;
                grants.Add(grant);
            }

            if (permitReadAcp)
            {
                Grant grant = new Grant();
                grant.Grantee = new Group();
                grant.Grantee.URI = userGroup;
                grant.Permission = PermissionEnum.ReadAcp;
                grants.Add(grant);
            }

            if (permitWrite)
            {
                Grant grant = new Grant();
                grant.Grantee = new Group();
                grant.Grantee.URI = userGroup;
                grant.Permission = PermissionEnum.Write;
                grants.Add(grant);
            }

            if (permitWriteAcp)
            {
                Grant grant = new Grant();
                grant.Grantee = new Group();
                grant.Grantee.URI = userGroup;
                grant.Permission = PermissionEnum.WriteAcp;
                grants.Add(grant);
            }

            if (fullControl)
            {
                Grant grant = new Grant();
                grant.Grantee = new Group();
                grant.Grantee.URI = userGroup;
                grant.Permission = PermissionEnum.FullControl;
                grants.Add(grant);
            }
        }

        private static BucketAcl GrantToBucketUserAcl(Grant grant, string userId, string ownerId, string bucketId)
        {
            if (grant.Permission == PermissionEnum.Read)
            {
                return BucketAcl.UserAcl(userId, ownerId, bucketId, true, false, false, false, false);
            }
            else if (grant.Permission == PermissionEnum.Write)
            {
                return BucketAcl.UserAcl(userId, ownerId, bucketId, false, true, false, false, false);
            }
            else if (grant.Permission == PermissionEnum.ReadAcp)
            {
                return BucketAcl.UserAcl(userId, ownerId, bucketId, false, false, true, false, false);
            }
            else if (grant.Permission == PermissionEnum.WriteAcp)
            {
                return BucketAcl.UserAcl(userId, ownerId, bucketId, false, false, false, true, false);
            }
            else if (grant.Permission == PermissionEnum.FullControl)
            {
                return BucketAcl.UserAcl(userId, ownerId, bucketId, false, false, false, false, true);
            }

            return null;
        }

        private static BucketAcl GrantToBucketGroupAcl(Grant grant, string userGroup, string ownerId, string bucketId)
        {
            if (grant.Permission == PermissionEnum.Read)
            {
                return BucketAcl.GroupAcl(userGroup, ownerId, bucketId, true, false, false, false, false);
            }
            else if (grant.Permission == PermissionEnum.Write)
            {
                return BucketAcl.GroupAcl(userGroup, ownerId, bucketId, false, true, false, false, false);
            }
            else if (grant.Permission == PermissionEnum.ReadAcp)
            {
                return BucketAcl.GroupAcl(userGroup, ownerId, bucketId, false, false, true, false, false);
            }
            else if (grant.Permission == PermissionEnum.WriteAcp)
            {
                return BucketAcl.GroupAcl(userGroup, ownerId, bucketId, false, false, false, true, false);
            }
            else if (grant.Permission == PermissionEnum.FullControl)
            {
                return BucketAcl.GroupAcl(userGroup, ownerId, bucketId, false, false, false, false, true);
            }

            return null;
        }

        private static ObjectAcl GrantToObjectUserAcl(Grant grant, string userId, string ownerId, string bucketId, string objectId)
        {
            if (grant.Permission == PermissionEnum.Read)
            {
                return ObjectAcl.UserAcl(userId, ownerId, bucketId, objectId, true, false, false, false, false);
            }
            else if (grant.Permission == PermissionEnum.Write)
            {
                return ObjectAcl.UserAcl(userId, ownerId, bucketId, objectId, false, true, false, false, false);
            }
            else if (grant.Permission == PermissionEnum.ReadAcp)
            {
                return ObjectAcl.UserAcl(userId, ownerId, bucketId, objectId, false, false, true, false, false);
            }
            else if (grant.Permission == PermissionEnum.WriteAcp)
            {
                return ObjectAcl.UserAcl(userId, ownerId, bucketId, objectId, false, false, false, true, false);
            }
            else if (grant.Permission == PermissionEnum.FullControl)
            {
                return ObjectAcl.UserAcl(userId, ownerId, bucketId, objectId, false, false, false, false, true);
            }

            return null;
        }

        private static ObjectAcl GrantToObjectGroupAcl(Grant grant, string userGroup, string ownerId, string bucketId, string objectId)
        {
            if (grant.Permission == PermissionEnum.Read)
            {
                return ObjectAcl.GroupAcl(userGroup, ownerId, bucketId, objectId, true, false, false, false, false);
            }
            else if (grant.Permission == PermissionEnum.Write)
            {
                return ObjectAcl.GroupAcl(userGroup, ownerId, bucketId, objectId, false, true, false, false, false);
            }
            else if (grant.Permission == PermissionEnum.ReadAcp)
            {
                return ObjectAcl.GroupAcl(userGroup, ownerId, bucketId, objectId, false, false, true, false, false);
            }
            else if (grant.Permission == PermissionEnum.WriteAcp)
            {
                return ObjectAcl.GroupAcl(userGroup, ownerId, bucketId, objectId, false, false, false, true, false);
            }
            else if (grant.Permission == PermissionEnum.FullControl)
            {
                return ObjectAcl.GroupAcl(userGroup, ownerId, bucketId, objectId, false, false, false, false, true);
            }

            return null;
        }

        private static bool GrantFromString(string str, PermissionEnum permType, string tenantId, ConfigManager config, out Grant grant)
        {
            grant = null;
            if (String.IsNullOrEmpty(str)) return false;

            string[] parts = str.Split('=');
            if (parts.Length != 2) return false;
            string granteeType = parts[0];
            string grantee = parts[1].Trim().Trim('"');

            grant = new Grant();
            grant.Permission = permType;

            if (granteeType.Equals("emailAddress"))
            {
                User user = config.GetUserByEmail(tenantId, grantee);
                if (user == null)
                {
                    return false;
                }
                else
                {
                    grant.Grantee = new CanonicalUser();
                    grant.Grantee.ID = user.Id;
                    grant.Grantee.DisplayName = user.Name;
                    return true;
                }
            }
            else if (granteeType.Equals("id"))
            {
                User user = config.GetUserById(tenantId, grantee);
                if (user == null)
                {
                    return false;
                }
                else
                {
                    grant.Grantee = new CanonicalUser();
                    grant.Grantee.ID = user.Id;
                    grant.Grantee.DisplayName = user.Name;
                    return true;
                }
            }
            else if (granteeType.Equals("uri"))
            {
                grant.Grantee = new Group();
                grant.Grantee.URI = grantee;
                return true;
            }

            return false;
        }

        #endregion
    }
}
