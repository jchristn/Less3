namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Less3.Settings;
    using S3ServerLibrary;
    using SyslogLogging;

    /// <summary>
    /// Authentication manager.
    /// </summary>
    internal class AuthManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SettingsBase _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;
        private BucketManager _Buckets;

        #endregion

        #region Constructors-and-Factories

        internal AuthManager()
        {

        }

        internal AuthManager(
            SettingsBase settings, 
            LoggingModule logging, 
            ConfigManager config, 
            BucketManager buckets)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (buckets == null) throw new ArgumentNullException(nameof(buckets));

            _Settings = settings;
            _Logging = logging;
            _Config = config;
            _Buckets = buckets;
        }

        #endregion

        #region Internal-Methods

        internal bool Authenticate(
            S3Context ctx,
            out User user,
            out Credential cred)
        {
            user = null;
            cred = null;
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (String.IsNullOrEmpty(ctx.Request.AccessKey)) return false;

            cred = _Config.GetCredentialByAccessKey(ctx.Request.AccessKey);
            if (cred == null || !cred.Active)
            {
                return false;
            }

            Tenant tenant = _Config.GetTenantById(cred.TenantId);
            if (tenant == null || !tenant.Active)
            {
                return false;
            }

            user = _Config.GetUserById(cred.TenantId, cred.UserId);
            if (user == null || !user.Active)
            {
                return false;
            }

            if (!String.Equals(cred.TenantId, user.TenantId, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        internal bool TryAuthenticateBearerToken(
            string authorizationHeader,
            string sourceIp,
            out RequestContext requestContext,
            out string reason)
        {
            requestContext = null;
            reason = null;

            string rawToken = ExtractBearerToken(authorizationHeader);
            if (String.IsNullOrWhiteSpace(rawToken))
            {
                reason = "Session token is required.";
                return false;
            }

            AuthSession session = _Config.GetAuthSessionByTokenHash(HashToken(rawToken));
            if (session == null)
            {
                reason = "Session was not found.";
                return false;
            }

            if (!session.Active || session.RevokedUtc.HasValue)
            {
                reason = "Session has been revoked.";
                return false;
            }

            if (session.ExpirationUtc <= DateTime.UtcNow)
            {
                reason = "Session has expired.";
                return false;
            }

            Tenant tenant = _Config.GetTenantById(session.TenantId);
            if (tenant == null || !tenant.Active)
            {
                reason = "Tenant is not active.";
                return false;
            }

            RequestContext ctx = new RequestContext();
            ctx.IsAuthenticated = true;
            ctx.TenantId = session.TenantId;
            ctx.SessionId = session.Id;

            if (String.Equals(session.PrincipalType, "Credential", StringComparison.OrdinalIgnoreCase))
            {
                Credential credential = _Config.GetCredentialById(session.TenantId, session.PrincipalId);
                if (credential == null || !credential.Active)
                {
                    reason = "Credential is not active.";
                    return false;
                }

                User user = _Config.GetUserById(session.TenantId, credential.UserId);
                if (user == null || !user.Active)
                {
                    reason = "User is not active.";
                    return false;
                }

                ctx.CredentialId = credential.Id;
                ctx.UserId = user.Id;
                ctx.PrincipalName = credential.AccessKey;
                ctx.IsAdmin = user.IsAdmin;
                ctx.IsTenantAdmin = user.IsTenantAdmin;
            }
            else
            {
                User user = _Config.GetUserById(session.TenantId, session.PrincipalId);
                if (user == null || !user.Active)
                {
                    reason = "User is not active.";
                    return false;
                }

                ctx.UserId = user.Id;
                ctx.PrincipalName = !String.IsNullOrEmpty(user.Email) ? user.Email : user.Name;
                ctx.IsAdmin = user.IsAdmin;
                ctx.IsTenantAdmin = user.IsTenantAdmin;
            }

            requestContext = ctx;
            return true;
        }

        internal bool Authorize(
            RequestContext requestContext,
            string resourceType,
            string operation,
            string resourceId,
            out string reason)
        {
            reason = null;

            if (requestContext == null || !requestContext.IsAuthenticated)
            {
                reason = "Request is not authenticated.";
                return false;
            }

            if (String.IsNullOrWhiteSpace(requestContext.TenantId))
            {
                reason = "Tenant could not be resolved.";
                return false;
            }

            bool hasDecision = TryEvaluateRbac(
                requestContext.TenantId,
                "User",
                requestContext.UserId,
                resourceType,
                operation,
                resourceId,
                out bool permitted,
                out reason);

            if (!hasDecision && !String.IsNullOrEmpty(requestContext.CredentialId))
            {
                hasDecision = TryEvaluateRbac(
                    requestContext.TenantId,
                    "Credential",
                    requestContext.CredentialId,
                    resourceType,
                    operation,
                    resourceId,
                    out permitted,
                    out reason);
            }

            if (!hasDecision && requestContext.IsAdmin)
            {
                permitted = true;
                reason = "Principal is a global administrator.";
                hasDecision = true;
            }

            if (!hasDecision && requestContext.IsTenantAdmin)
            {
                permitted = true;
                reason = "Principal is a tenant administrator.";
                hasDecision = true;
            }

            if (!hasDecision)
            {
                reason = "No matching RBAC permission.";
                permitted = false;
            }

            RecordAuthorizationAudit(requestContext, resourceType, resourceId, operation, permitted, reason);
            return permitted;
        }

        internal RequestMetadata AuthenticateAndBuildMetadata(S3Context ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            RequestMetadata md = new RequestMetadata(); 
            md.Authentication = AuthenticationResult.NotAuthenticated;
             
            #region Credential-and-User

            if (String.IsNullOrEmpty(ctx.Request.AccessKey))
            {
                md.Authentication = AuthenticationResult.NoMaterialSupplied;
            }
            else
            {
                Credential cred = _Config.GetCredentialByAccessKey(ctx.Request.AccessKey);
                if (cred == null)
                {
                    md.Authentication = AuthenticationResult.AccessKeyNotFound; 
                }
                else
                {
                    md.Credential = cred;
                    md.TenantId = cred.TenantId;

                    if (!cred.Active)
                    {
                        md.Authentication = AuthenticationResult.CredentialInactive;
                        RecordCredentialAuthenticationFailure(cred);
                        return md;
                    }

                    Tenant tenant = _Config.GetTenantById(cred.TenantId);
                    if (tenant == null || !tenant.Active)
                    {
                        md.Authentication = AuthenticationResult.TenantInactive;
                        RecordCredentialAuthenticationFailure(cred);
                        return md;
                    }

                    User user = _Config.GetUserById(cred.TenantId, cred.UserId);
                    if (user == null)
                    {
                        md.Authentication = AuthenticationResult.UserNotFound;
                        RecordCredentialAuthenticationFailure(cred);
                    }
                    else if (!user.Active)
                    {
                        md.Authentication = AuthenticationResult.UserInactive;
                        RecordCredentialAuthenticationFailure(cred);
                    }
                    else if (!String.Equals(cred.TenantId, user.TenantId, StringComparison.Ordinal))
                    {
                        md.Authentication = AuthenticationResult.TenantMismatch;
                        RecordCredentialAuthenticationFailure(cred);
                    }
                    else
                    {
                        md.User = user;
                        md.Authentication = AuthenticationResult.Authenticated;
                        RecordCredentialAuthenticationSuccess(cred);
                    }
                }
            }

            #endregion

            #region Bucket

            if (!String.IsNullOrEmpty(ctx.Request.Bucket))
            {
                string tenantId = String.IsNullOrEmpty(md.TenantId) ? "default" : md.TenantId;
                md.Bucket = _Buckets.GetByName(tenantId, ctx.Request.Bucket);

                if (md.Bucket != null)
                {
                    md.BucketClient = _Buckets.GetClient(tenantId, ctx.Request.Bucket);

                    if (md.BucketClient != null)
                    {
                        md.BucketAcls = md.BucketClient.GetBucketAcl();
                        md.BucketTags = md.BucketClient.GetBucketTags();
                    }
                }
                else
                {
                    if (md.Authentication == AuthenticationResult.Authenticated)
                    {
                        md.Authorization = AuthorizationResult.PermitBucketOwnership;
                    }
                }
            }

            #endregion

            #region Object

            if (md.BucketClient != null 
                && ctx.Request.IsObjectRequest
                && !String.IsNullOrEmpty(ctx.Request.Key))
            {
                if (String.IsNullOrEmpty(ctx.Request.VersionId))
                {
                    md.Obj = md.BucketClient.GetObjectLatestMetadata(ctx.Request.Key);
                }
                else
                {
                    long versionId = 1;
                    if (!String.IsNullOrEmpty(ctx.Request.VersionId))
                    {
                        Int64.TryParse(ctx.Request.VersionId, out versionId);
                    }
                    md.Obj = md.BucketClient.GetObjectVersionMetadata(ctx.Request.Key, versionId);
                }
                                
                if (md.Obj != null)
                {
                    md.ObjectAcls = md.BucketClient.GetObjectAcl(md.Obj.Id);
                    md.ObjectTags = md.BucketClient.GetObjectTags(md.Obj.Id);
                }
                else
                {
                    if (md.Authentication == AuthenticationResult.Authenticated)
                    {
                        md.Authorization = AuthorizationResult.PermitObjectOwnership;
                    }
                }
            }

            #endregion
            
            return md;
        }

        internal RequestMetadata AuthorizeServiceRequest(S3Context ctx, RequestMetadata md)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (md == null) throw new ArgumentNullException(nameof(md));

            md.Authorization = AuthorizationResult.NotAuthorized;

            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Http.Request.Method.ToString() + " " + ctx.Http.Request.Url.RawWithoutQuery + "] AuthorizeServiceRequest "; 

            #region Check-for-Admin-API-Key

            if (ctx.Http.Request.Headers.AllKeys.Contains(_Settings.HeaderApiKey))
            {
                if (ctx.Http.Request.Headers[_Settings.HeaderApiKey].Equals(_Settings.AdminApiKey))
                {
                    if (_Settings.Debug.Authentication)
                    {
                        _Logging.Info(header + "admin API key in use");
                    }

                    md.Authorization = AuthorizationResult.AdminAuthorized;
                    return md;
                }
            }

            #endregion

            if (md.User != null && md.Authentication == AuthenticationResult.Authenticated)
            {
                if (TryAuthorizeRbac(md, "Storage", "Read", null, out bool permitted, out string reason))
                {
                    md.Authorization = permitted ? AuthorizationResult.PermitRbac : AuthorizationResult.NotAuthorized;
                    return md;
                }

                if (_Settings.Debug.Authentication)
                {
                    _Logging.Debug(header + reason);
                }
            }

            return md;
        }

        internal RequestMetadata AuthorizeBucketRequest(S3Context ctx, RequestMetadata md)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (md == null) throw new ArgumentNullException(nameof(md));

            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Http.Request.Method.ToString() + " " + ctx.Http.Request.Url.RawWithoutQuery + "] AuthorizeBucketRequest ";
            bool allowed = false;
            md.Authorization = AuthorizationResult.NotAuthorized;

            #region Check-for-Bucket-Write

            if (ctx.Request.RequestType == S3RequestType.BucketWrite && md.Authentication == AuthenticationResult.Authenticated)
            {
                if (TryAuthorizeRbac(md, "Bucket", "Create", null, out bool permitted, out string reason))
                {
                    md.Authorization = permitted ? AuthorizationResult.PermitRbac : AuthorizationResult.NotAuthorized;
                    return md;
                }

                if (_Settings.Debug.Authentication)
                {
                    _Logging.Debug(header + reason);
                }

                md.Authorization = AuthorizationResult.NotAuthorized;
                return md;
            }

            #endregion

            #region Check-for-Admin-API-Key

            if (ctx.Http.Request.Headers.AllKeys.Contains(_Settings.HeaderApiKey))
            {
                if (ctx.Http.Request.Headers[_Settings.HeaderApiKey].Equals(_Settings.AdminApiKey))
                {
                    if (_Settings.Debug.Authentication)
                    {
                        _Logging.Info(header + "admin API key in use");
                    }

                    md.Authorization = AuthorizationResult.AdminAuthorized;
                    return md;
                }
            }

            #endregion

            #region Check-for-Bucket-Global-Config

            if (md.Bucket != null)
            {
                switch (ctx.Request.RequestType)
                {
                    case S3RequestType.BucketExists:
                    case S3RequestType.BucketRead:
                    case S3RequestType.BucketReadVersioning:
                    case S3RequestType.BucketReadVersions:
                        if (md.Bucket.EnablePublicRead)
                        {
                            md.Authorization = AuthorizationResult.PermitBucketGlobalConfig;
                            return md;
                        }
                        break;

                    case S3RequestType.BucketDeleteTags:
                    case S3RequestType.BucketWriteTags:
                    case S3RequestType.BucketWriteVersioning:
                        if (md.Bucket.EnablePublicWrite)
                        {
                            md.Authorization = AuthorizationResult.PermitBucketGlobalConfig;
                            return md;
                        }
                        break;
                }
            }


            #endregion

            #region Check-for-Bucket-AllUsers-ACL

            if (md.BucketAcls != null && md.BucketAcls.Count > 0)
            {
                switch (ctx.Request.RequestType)
                {
                    case S3RequestType.BucketExists:
                    case S3RequestType.BucketRead:
                    case S3RequestType.BucketReadVersioning:
                    case S3RequestType.BucketReadVersions: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitRead || b.FullControl)); 
                        break;

                    case S3RequestType.BucketReadAcl: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitReadAcp || b.FullControl)); 
                        break;

                    case S3RequestType.BucketDelete:
                    case S3RequestType.BucketDeleteTags:
                    case S3RequestType.BucketWrite:
                    case S3RequestType.BucketWriteTags:
                    case S3RequestType.BucketWriteVersioning: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitWrite || b.FullControl)); 
                        break;

                    case S3RequestType.BucketWriteAcl: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitWriteAcp || b.FullControl)); 
                        break;
                }

                if (allowed)
                {
                    md.Authorization = AuthorizationResult.PermitBucketAllUsersAcl;
                    return md;
                }
            }

            #endregion

            #region Check-for-Auth-Material

            if (md.User == null || md.Credential == null)
            {
                md.Authorization = AuthorizationResult.NotAuthorized;
                return md;
            }

            #endregion

            #region Check-for-RBAC

            if (TryAuthorizeRbac(
                md,
                "Bucket",
                OperationForBucketRequest(ctx.Request.RequestType),
                md.Bucket != null ? md.Bucket.Id : null,
                out bool rbacPermitted,
                out string rbacReason))
            {
                md.Authorization = rbacPermitted ? AuthorizationResult.PermitRbac : AuthorizationResult.NotAuthorized;
                return md;
            }

            if (_Settings.Debug.Authentication)
            {
                _Logging.Debug(header + rbacReason);
            }

            #endregion

            #region Check-for-Bucket-Owner

            if (md.Bucket != null)
            {
                if (md.Bucket.OwnerId.Equals(md.User.Id))
                {
                    md.Authorization = AuthorizationResult.PermitBucketOwnership;
                    return md;
                }
            }

            #endregion

            #region Check-for-Bucket-AuthenticatedUsers-ACL

            if (md.BucketAcls != null && md.BucketAcls.Count > 0)
            {
                switch (ctx.Request.RequestType)
                {
                    case S3RequestType.BucketExists:
                    case S3RequestType.BucketRead:
                    case S3RequestType.BucketReadVersioning:
                    case S3RequestType.BucketReadVersions: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitRead || b.FullControl)); 
                        break;

                    case S3RequestType.BucketReadAcl: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitReadAcp || b.FullControl)); 
                        break;

                    case S3RequestType.BucketDelete:
                    case S3RequestType.BucketDeleteTags:
                    case S3RequestType.BucketWrite:
                    case S3RequestType.BucketWriteTags:
                    case S3RequestType.BucketWriteVersioning: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitWrite || b.FullControl)); 
                        break;

                    case S3RequestType.BucketWriteAcl: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitWriteAcp || b.FullControl)); 
                        break;
                }

                if (allowed)
                {
                    md.Authorization = AuthorizationResult.PermitBucketAuthUserAcl;
                    return md;
                }
            }

            #endregion

            #region Check-for-Bucket-User-ACL

            if (md.BucketAcls != null && md.BucketAcls.Count > 0)
            {
                switch (ctx.Request.RequestType)
                {
                    case S3RequestType.BucketExists:
                    case S3RequestType.BucketRead:
                    case S3RequestType.BucketReadVersioning:
                    case S3RequestType.BucketReadVersions: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitRead || b.FullControl)); 
                        break;

                    case S3RequestType.BucketReadAcl: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitReadAcp || b.FullControl)); 
                        break;

                    case S3RequestType.BucketDelete:
                    case S3RequestType.BucketDeleteTags:
                    case S3RequestType.BucketWrite:
                    case S3RequestType.BucketWriteTags:
                    case S3RequestType.BucketWriteVersioning: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitWrite || b.FullControl)); 
                        break;

                    case S3RequestType.BucketWriteAcl: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitWriteAcp || b.FullControl)); 
                        break;
                }

                if (allowed)
                {
                    md.Authorization = AuthorizationResult.PermitBucketUserAcl;
                    return md;
                }
            }

            #endregion

            return md;
        }

        internal RequestMetadata AuthorizeObjectRequest(S3Context ctx, RequestMetadata md)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (md == null) throw new ArgumentNullException(nameof(md));

            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Http.Request.Method.ToString() + " " + ctx.Http.Request.Url.RawWithoutQuery + "] AuthorizeObjectWriteRequest ";
            bool allowed = false;
            md.Authorization = AuthorizationResult.NotAuthorized;

            #region Get-Version-ID

            long versionId = 1;
            if (!String.IsNullOrEmpty(ctx.Request.VersionId))
            {
                if (!Int64.TryParse(ctx.Request.VersionId, out versionId))
                {

                }
            }

            #endregion

            #region Check-for-Admin-API-Key

            if (ctx.Http.Request.Headers.AllKeys.Contains(_Settings.HeaderApiKey))
            {
                if (ctx.Http.Request.Headers[_Settings.HeaderApiKey].Equals(_Settings.AdminApiKey))
                {
                    if (_Settings.Debug.Authentication)
                    {
                        _Logging.Info(header + "admin API key in use");
                    }

                    md.Authorization = AuthorizationResult.AdminAuthorized;
                    return md;
                }
            }

            #endregion

            #region Check-for-Bucket-Global-Config

            if (md.Bucket != null)
            {
                switch (ctx.Request.RequestType)
                {
                    case S3RequestType.ObjectExists:
                    case S3RequestType.ObjectRead:
                    case S3RequestType.ObjectReadLegalHold:
                    case S3RequestType.ObjectReadRange:
                    case S3RequestType.ObjectReadRetention:
                    case S3RequestType.ObjectReadTags:
                        if (md.Bucket.EnablePublicRead) allowed = true;
                        break;

                    case S3RequestType.ObjectDelete:
                    case S3RequestType.ObjectDeleteMultiple:
                    case S3RequestType.ObjectDeleteTags:
                    case S3RequestType.ObjectWrite:
                    case S3RequestType.ObjectWriteLegalHold:
                    case S3RequestType.ObjectWriteRetention:
                    case S3RequestType.ObjectWriteTags:
                        if (md.Bucket.EnablePublicWrite) allowed = true;
                        break;
                }
            }

            if (allowed)
            {
                md.Authorization = AuthorizationResult.PermitBucketGlobalConfig;
                return md;
            }

            #endregion

            #region Check-for-Bucket-AllUsers-ACL

            if (md.BucketAcls != null && md.BucketAcls.Count > 0)
            {
                switch (ctx.Request.RequestType)
                {
                    case S3RequestType.ObjectExists:
                    case S3RequestType.ObjectRead:
                    case S3RequestType.ObjectReadLegalHold:
                    case S3RequestType.ObjectReadRange:
                    case S3RequestType.ObjectReadRetention:
                    case S3RequestType.ObjectReadTags:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitRead || b.FullControl));
                        break;

                    case S3RequestType.ObjectReadAcl:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitReadAcp || b.FullControl));
                        break;

                    case S3RequestType.ObjectDelete:
                    case S3RequestType.ObjectDeleteMultiple:
                    case S3RequestType.ObjectDeleteTags:
                    case S3RequestType.ObjectWrite:
                    case S3RequestType.ObjectWriteLegalHold:
                    case S3RequestType.ObjectWriteRetention:
                    case S3RequestType.ObjectWriteTags:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitWrite || b.FullControl));
                        break;

                    case S3RequestType.ObjectWriteAcl:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitWriteAcp || b.FullControl));
                        break;
                }

                if (allowed)
                {
                    md.Authorization = AuthorizationResult.PermitBucketAllUsersAcl;
                    return md;
                }
            }

            #endregion

            #region Check-for-Object-AllUsers-ACL

            if (md.ObjectAcls != null && md.ObjectAcls.Count > 0)
            {
                switch (ctx.Request.RequestType)
                {
                    case S3RequestType.ObjectExists:
                    case S3RequestType.ObjectRead:
                    case S3RequestType.ObjectReadLegalHold:
                    case S3RequestType.ObjectReadRange:
                    case S3RequestType.ObjectReadRetention:
                    case S3RequestType.ObjectReadTags:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitRead || b.FullControl));
                        break;

                    case S3RequestType.ObjectReadAcl:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitReadAcp || b.FullControl));
                        break;

                    case S3RequestType.ObjectDelete:
                    case S3RequestType.ObjectDeleteMultiple:
                    case S3RequestType.ObjectDeleteTags:
                    // case S3RequestType.ObjectWrite:
                    case S3RequestType.ObjectWriteLegalHold:
                    case S3RequestType.ObjectWriteRetention:
                    case S3RequestType.ObjectWriteTags:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitWrite || b.FullControl));
                        break;

                    case S3RequestType.ObjectWriteAcl:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AllUsers")
                            && (b.PermitWriteAcp || b.FullControl));
                        break;
                }

                if (allowed)
                {
                    md.Authorization = AuthorizationResult.PermitObjectAllUsersAcl;
                    return md;
                }
            }

            #endregion

            #region Check-for-Auth-Material

            if (md.User == null || md.Credential == null)
            {
                md.Authorization = AuthorizationResult.NotAuthorized;
                return md;
            }

            #endregion

            #region Check-for-RBAC

            if (TryAuthorizeRbac(
                md,
                "Object",
                OperationForObjectRequest(ctx.Request.RequestType),
                md.Obj != null ? md.Obj.Id : null,
                out bool rbacPermitted,
                out string rbacReason))
            {
                md.Authorization = rbacPermitted ? AuthorizationResult.PermitRbac : AuthorizationResult.NotAuthorized;
                return md;
            }

            if (_Settings.Debug.Authentication)
            {
                _Logging.Debug(header + rbacReason);
            }

            #endregion

            #region Check-for-Bucket-Owner

            if (md.Bucket != null)
            {
                if (md.Bucket.OwnerId.Equals(md.User.Id))
                {
                    md.Authorization = AuthorizationResult.PermitBucketOwnership;
                    return md;
                }
            }

            #endregion

            #region Check-for-Object-Owner

            if (md.Obj != null)
            {
                if (md.Obj.OwnerId.Equals(md.User.Id))
                {
                    md.Authorization = AuthorizationResult.PermitObjectOwnership;
                    return md;
                }
            }

            #endregion

            #region Check-for-Bucket-AuthenticatedUsers-ACL

            if (md.BucketAcls != null && md.BucketAcls.Count > 0)
            {
                switch (ctx.Request.RequestType)
                {
                    case S3RequestType.ObjectExists:
                    case S3RequestType.ObjectRead:
                    case S3RequestType.ObjectReadLegalHold:
                    case S3RequestType.ObjectReadRange:
                    case S3RequestType.ObjectReadRetention:
                    case S3RequestType.ObjectReadTags:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitRead || b.FullControl));
                        break;

                    case S3RequestType.ObjectReadAcl:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitReadAcp || b.FullControl));
                        break;

                    case S3RequestType.ObjectDelete:
                    case S3RequestType.ObjectDeleteMultiple:
                    case S3RequestType.ObjectDeleteTags:
                    case S3RequestType.ObjectWrite:
                    case S3RequestType.ObjectWriteLegalHold:
                    case S3RequestType.ObjectWriteRetention:
                    case S3RequestType.ObjectWriteTags:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitWrite || b.FullControl));
                        break;

                    case S3RequestType.ObjectWriteAcl:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitWriteAcp || b.FullControl));
                        break;
                }

                if (allowed)
                {
                    md.Authorization = AuthorizationResult.PermitBucketAuthUserAcl;
                    return md;
                }
            }

            #endregion

            #region Check-for-Object-AuthenticatedUsers-ACL

            if (md.ObjectAcls != null && md.ObjectAcls.Count > 0)
            {
                switch (ctx.Request.RequestType)
                {
                    case S3RequestType.ObjectExists:
                    case S3RequestType.ObjectRead:
                    case S3RequestType.ObjectReadLegalHold:
                    case S3RequestType.ObjectReadRange:
                    case S3RequestType.ObjectReadRetention:
                    case S3RequestType.ObjectReadTags:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitRead || b.FullControl));
                        break;

                    case S3RequestType.ObjectReadAcl:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitReadAcp || b.FullControl));
                        break;

                    case S3RequestType.ObjectDelete:
                    case S3RequestType.ObjectDeleteMultiple:
                    case S3RequestType.ObjectDeleteTags:
                    // case S3RequestType.ObjectWrite:
                    case S3RequestType.ObjectWriteLegalHold:
                    case S3RequestType.ObjectWriteRetention:
                    case S3RequestType.ObjectWriteTags:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitWrite || b.FullControl));
                        break;

                    case S3RequestType.ObjectWriteAcl:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGroup)
                            && b.UserGroup.Contains("AuthenticatedUsers")
                            && (b.PermitWriteAcp || b.FullControl));
                        break;
                }

                if (allowed)
                {
                    md.Authorization = AuthorizationResult.PermitObjectAuthUserAcl;
                    return md;
                }
            }

            #endregion

            #region Check-for-Bucket-User-ACL

            if (md.BucketAcls != null && md.BucketAcls.Count > 0)
            {
                switch (ctx.Request.RequestType)
                {
                    case S3RequestType.ObjectExists:
                    case S3RequestType.ObjectRead:
                    case S3RequestType.ObjectReadLegalHold:
                    case S3RequestType.ObjectReadRange:
                    case S3RequestType.ObjectReadRetention:
                    case S3RequestType.ObjectReadTags:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitRead || b.FullControl));
                        break;

                    case S3RequestType.ObjectReadAcl:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitReadAcp || b.FullControl));
                        break;

                    case S3RequestType.ObjectDelete:
                    case S3RequestType.ObjectDeleteMultiple:
                    case S3RequestType.ObjectDeleteTags:
                    case S3RequestType.ObjectWrite:
                    case S3RequestType.ObjectWriteLegalHold:
                    case S3RequestType.ObjectWriteRetention:
                    case S3RequestType.ObjectWriteTags:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitWrite || b.FullControl));
                        break;

                    case S3RequestType.ObjectWriteAcl:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitWriteAcp || b.FullControl));
                        break;
                }

                if (allowed)
                {
                    md.Authorization = AuthorizationResult.PermitBucketUserAcl;
                    return md;
                }
            }

            #endregion

            #region Check-for-Object-User-ACL

            if (md.ObjectAcls != null && md.ObjectAcls.Count > 0)
            {
                switch (ctx.Request.RequestType)
                {
                    case S3RequestType.ObjectExists:
                    case S3RequestType.ObjectRead:
                    case S3RequestType.ObjectReadLegalHold:
                    case S3RequestType.ObjectReadRange:
                    case S3RequestType.ObjectReadRetention:
                    case S3RequestType.ObjectReadTags:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitRead || b.FullControl));
                        break;

                    case S3RequestType.ObjectReadAcl:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitReadAcp || b.FullControl));
                        break;

                    case S3RequestType.ObjectDelete:
                    case S3RequestType.ObjectDeleteMultiple:
                    case S3RequestType.ObjectDeleteTags:
                    // case S3RequestType.ObjectWrite:
                    case S3RequestType.ObjectWriteLegalHold:
                    case S3RequestType.ObjectWriteRetention:
                    case S3RequestType.ObjectWriteTags:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitWrite || b.FullControl));
                        break;

                    case S3RequestType.ObjectWriteAcl:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserId)
                            && b.UserId.Equals(md.User.Id)
                            && (b.PermitWriteAcp || b.FullControl));
                        break;
                }

                if (allowed)
                {
                    md.Authorization = AuthorizationResult.PermitObjectUserAcl;
                    return md;
                }
            }

            #endregion

            return md;
        }
          
        #endregion

        #region Private-Methods

        private bool TryAuthorizeRbac(
            RequestMetadata md,
            string resourceType,
            string operation,
            string resourceId,
            out bool permitted,
            out string reason)
        {
            permitted = false;
            reason = null;

            if (md == null
                || md.Authentication != AuthenticationResult.Authenticated
                || md.User == null
                || md.Credential == null
                || String.IsNullOrEmpty(md.TenantId))
            {
                reason = "RBAC skipped because authentication material is incomplete.";
                return false;
            }

            bool userDecision = TryEvaluateRbac(
                md.TenantId,
                "User",
                md.User.Id,
                resourceType,
                operation,
                resourceId,
                out permitted,
                out reason);

            if (userDecision)
            {
                RecordAuthorizationAudit(md, resourceType, resourceId, operation, permitted, reason);
                return true;
            }

            bool credentialDecision = TryEvaluateRbac(
                md.TenantId,
                "Credential",
                md.Credential.Id,
                resourceType,
                operation,
                resourceId,
                out permitted,
                out reason);

            if (credentialDecision)
            {
                RecordAuthorizationAudit(md, resourceType, resourceId, operation, permitted, reason);
                return true;
            }

            if (md.User.IsAdmin)
            {
                permitted = true;
                reason = "Principal is a global administrator.";
                RecordAuthorizationAudit(md, resourceType, resourceId, operation, permitted, reason);
                return true;
            }

            if (md.User.IsTenantAdmin)
            {
                permitted = true;
                reason = "Principal is a tenant administrator.";
                RecordAuthorizationAudit(md, resourceType, resourceId, operation, permitted, reason);
                return true;
            }

            reason = "No matching RBAC permission.";
            return false;
        }

        private bool TryEvaluateRbac(
            string tenantId,
            string principalType,
            string principalId,
            string resourceType,
            string operation,
            string resourceId,
            out bool permitted,
            out string reason)
        {
            permitted = false;
            reason = null;

            if (String.IsNullOrWhiteSpace(tenantId)
                || String.IsNullOrWhiteSpace(principalType)
                || String.IsNullOrWhiteSpace(principalId))
            {
                reason = "Principal is incomplete.";
                return false;
            }

            List<RoleAssignment> assignments = _Config.EnumerateRoleAssignments(new Less3.Requests.EnumerationQuery
            {
                TenantId = tenantId,
                Limit = 1000,
                Filters = new Dictionary<string, string>
                {
                    { "principalType", principalType },
                    { "principalId", principalId }
                }
            }).Items;

            if (assignments == null || assignments.Count < 1)
            {
                reason = "Principal has no matching role assignments.";
                return false;
            }

            List<Permission> permissions = _Config.EnumeratePermissions(new Less3.Requests.EnumerationQuery
            {
                TenantId = tenantId,
                Limit = 1000
            }).Items;

            Permission permitMatch = null;
            string permitRoleId = null;

            foreach (RoleAssignment assignment in assignments.Where(a => a.Active))
            {
                if (!AssignmentScopeMatches(tenantId, assignment, resourceType, resourceId)) continue;

                Role role = _Config.GetRoleById(tenantId, assignment.RoleId);
                if (role == null || !role.Active) continue;

                IEnumerable<Permission> rolePermissions = permissions
                    .Where(p => p.Active && String.Equals(p.RoleId, role.Id, StringComparison.Ordinal));

                foreach (Permission permission in rolePermissions)
                {
                    if (!PermissionResourceMatches(permission.ResourceType, resourceType)) continue;
                    if (!PermissionOperationMatches(permission.Operation, operation)) continue;

                    if (!permission.Permit)
                    {
                        permitted = false;
                        reason = "RBAC deny from role " + role.Id + " permission " + permission.Id + ".";
                        return true;
                    }

                    if (permitMatch == null)
                    {
                        permitMatch = permission;
                        permitRoleId = role.Id;
                    }
                }
            }

            if (permitMatch != null)
            {
                permitted = true;
                reason = "RBAC permit from role " + permitRoleId + " permission " + permitMatch.Id + ".";
                return true;
            }

            reason = "No matching RBAC permission.";
            return false;
        }

        private static bool AssignmentScopeMatches(
            string tenantId,
            RoleAssignment assignment,
            string resourceType,
            string resourceId)
        {
            if (assignment == null) return false;
            if (String.IsNullOrEmpty(assignment.ResourceType)) return true;

            if (String.Equals(assignment.ResourceType, "Tenant", StringComparison.OrdinalIgnoreCase)
                && (String.IsNullOrEmpty(assignment.ResourceId) || String.Equals(assignment.ResourceId, tenantId, StringComparison.Ordinal)))
            {
                return true;
            }

            if (!PermissionResourceMatches(assignment.ResourceType, resourceType)) return false;
            if (String.IsNullOrEmpty(assignment.ResourceId)) return true;
            if (String.IsNullOrEmpty(resourceId)) return false;
            return String.Equals(assignment.ResourceId, resourceId, StringComparison.Ordinal);
        }

        private static bool PermissionResourceMatches(string permissionResourceType, string requestedResourceType)
        {
            if (String.IsNullOrEmpty(permissionResourceType)) return false;
            if (String.Equals(permissionResourceType, "All", StringComparison.OrdinalIgnoreCase)) return true;
            if (String.Equals(permissionResourceType, requestedResourceType, StringComparison.OrdinalIgnoreCase)) return true;

            if (String.Equals(permissionResourceType, "Storage", StringComparison.OrdinalIgnoreCase))
            {
                return String.Equals(requestedResourceType, "Bucket", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(requestedResourceType, "Object", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(requestedResourceType, "BucketTag", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(requestedResourceType, "ObjectTag", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(requestedResourceType, "BucketAcl", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(requestedResourceType, "ObjectAcl", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(requestedResourceType, "Storage", StringComparison.OrdinalIgnoreCase);
            }

            if (String.Equals(permissionResourceType, "Security", StringComparison.OrdinalIgnoreCase))
            {
                return IsSecurityResource(requestedResourceType);
            }

            if (String.Equals(permissionResourceType, "Audit", StringComparison.OrdinalIgnoreCase))
            {
                return String.Equals(requestedResourceType, "AuthorizationAudit", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(requestedResourceType, "RequestHistory", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static bool PermissionOperationMatches(string permissionOperation, string requestedOperation)
        {
            if (String.IsNullOrEmpty(permissionOperation)) return false;
            if (String.Equals(permissionOperation, "All", StringComparison.OrdinalIgnoreCase)) return true;
            if (String.Equals(permissionOperation, "Admin", StringComparison.OrdinalIgnoreCase)) return true;
            if (String.Equals(permissionOperation, requestedOperation, StringComparison.OrdinalIgnoreCase)) return true;

            if (String.Equals(permissionOperation, "Read", StringComparison.OrdinalIgnoreCase))
            {
                return String.Equals(requestedOperation, "Exists", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(requestedOperation, "Enumerate", StringComparison.OrdinalIgnoreCase);
            }

            if (String.Equals(permissionOperation, "Write", StringComparison.OrdinalIgnoreCase))
            {
                return String.Equals(requestedOperation, "Create", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(requestedOperation, "Update", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(requestedOperation, "Delete", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(requestedOperation, "Write", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static bool IsSecurityResource(string requestedResourceType)
        {
            return String.Equals(requestedResourceType, "User", StringComparison.OrdinalIgnoreCase)
                || String.Equals(requestedResourceType, "Credential", StringComparison.OrdinalIgnoreCase)
                || String.Equals(requestedResourceType, "Role", StringComparison.OrdinalIgnoreCase)
                || String.Equals(requestedResourceType, "Permission", StringComparison.OrdinalIgnoreCase)
                || String.Equals(requestedResourceType, "RoleAssignment", StringComparison.OrdinalIgnoreCase)
                || String.Equals(requestedResourceType, "AuthSession", StringComparison.OrdinalIgnoreCase)
                || String.Equals(requestedResourceType, "AuthorizationAudit", StringComparison.OrdinalIgnoreCase);
        }

        private void RecordAuthorizationAudit(
            RequestMetadata md,
            string resourceType,
            string resourceId,
            string operation,
            bool permitted,
            string reason)
        {
            if (md == null) return;

            RequestContext context = new RequestContext();
            context.TenantId = md.TenantId;
            if (md.User != null) context.UserId = md.User.Id;
            if (md.Credential != null) context.CredentialId = md.Credential.Id;

            RecordAuthorizationAudit(context, resourceType, resourceId, operation, permitted, reason);
        }

        private void RecordAuthorizationAudit(
            RequestContext context,
            string resourceType,
            string resourceId,
            string operation,
            bool permitted,
            string reason)
        {
            if (context == null || String.IsNullOrEmpty(context.TenantId)) return;

            try
            {
                AuthorizationAudit audit = new AuthorizationAudit();
                audit.TenantId = context.TenantId;
                audit.UserId = context.UserId;
                audit.CredentialId = context.CredentialId;
                audit.ResourceType = resourceType;
                audit.ResourceId = resourceId;
                audit.Operation = operation;
                audit.Permitted = permitted;
                audit.Reason = reason;
                _Config.AddAuthorizationAudit(audit);
            }
            catch (Exception e)
            {
                _Logging.Debug("Failed to persist authorization audit: " + e.Message);
            }
        }

        private void RecordCredentialAuthenticationSuccess(Credential credential)
        {
            if (credential == null) return;

            try
            {
                credential.LastUsedUtc = DateTime.UtcNow;
                _Config.UpdateCredential(credential);
            }
            catch (Exception e)
            {
                _Logging.Debug("Failed to update credential last-used timestamp: " + e.Message);
            }
        }

        private void RecordCredentialAuthenticationFailure(Credential credential)
        {
            if (credential == null) return;

            try
            {
                credential.LastFailedUtc = DateTime.UtcNow;
                _Config.UpdateCredential(credential);
            }
            catch (Exception e)
            {
                _Logging.Debug("Failed to update credential last-failed timestamp: " + e.Message);
            }
        }

        private static string OperationForBucketRequest(S3RequestType requestType)
        {
            switch (requestType)
            {
                case S3RequestType.BucketExists:
                    return "Exists";
                case S3RequestType.BucketRead:
                case S3RequestType.BucketReadAcl:
                case S3RequestType.BucketReadLocation:
                case S3RequestType.BucketReadLogging:
                case S3RequestType.BucketReadTags:
                case S3RequestType.BucketReadVersioning:
                case S3RequestType.BucketReadVersions:
                case S3RequestType.BucketReadWebsite:
                case S3RequestType.BucketReadMultipartUploads:
                    return "Read";
                case S3RequestType.BucketDelete:
                case S3RequestType.BucketDeleteTags:
                case S3RequestType.BucketDeleteWebsite:
                    return "Delete";
                default:
                    return "Write";
            }
        }

        private static string OperationForObjectRequest(S3RequestType requestType)
        {
            switch (requestType)
            {
                case S3RequestType.ObjectExists:
                    return "Exists";
                case S3RequestType.ObjectRead:
                case S3RequestType.ObjectReadAcl:
                case S3RequestType.ObjectReadLegalHold:
                case S3RequestType.ObjectReadRange:
                case S3RequestType.ObjectReadRetention:
                case S3RequestType.ObjectReadTags:
                case S3RequestType.ObjectReadParts:
                    return "Read";
                case S3RequestType.ObjectDelete:
                case S3RequestType.ObjectDeleteMultiple:
                case S3RequestType.ObjectDeleteTags:
                case S3RequestType.ObjectAbortMultipartUpload:
                    return "Delete";
                case S3RequestType.ObjectWrite:
                case S3RequestType.ObjectCreateMultipartUpload:
                    return "Create";
                default:
                    return "Write";
            }
        }

        private static string ExtractBearerToken(string authorizationHeader)
        {
            if (String.IsNullOrWhiteSpace(authorizationHeader)) return null;

            string value = authorizationHeader.Trim();
            if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return value.Substring("Bearer ".Length).Trim();
            }

            return value;
        }

        private static string HashToken(string token)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        #endregion
    }
}
