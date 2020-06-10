using System;
using System.Collections.Generic;
using System.Text;

using S3ServerInterface;
using SyslogLogging;

namespace Less3.Classes
{
    /// <summary>
    /// Authentication manager.
    /// </summary>
    internal class AuthManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;
        private BucketManager _Buckets;

        #endregion

        #region Constructors-and-Factories

        internal AuthManager()
        {

        }

        internal AuthManager(
            Settings settings, 
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
            S3Request req,
            out User user,
            out Credential cred)
        {
            user = null;
            cred = null;
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (String.IsNullOrEmpty(req.AccessKey)) return false;

            user = _Config.GetUserByAccessKey(req.AccessKey);
            if (user == null)
            {
                return false;
            }

            cred = _Config.GetCredentialByAccessKey(req.AccessKey);
            if (cred == null)
            {
                return false;
            }

            return true;
        }

        internal RequestMetadata AuthenticateAndBuildMetadata(S3Request req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            RequestMetadata md = new RequestMetadata(); 
            md.Authentication = AuthenticationResult.NotAuthenticated;

            if (req == null) throw new ArgumentNullException(nameof(req));

            #region Credential-and-User

            if (String.IsNullOrEmpty(req.AccessKey))
            {
                md.Authentication = AuthenticationResult.NoMaterialSupplied;
            }
            else
            {
                Credential cred = _Config.GetCredentialByAccessKey(req.AccessKey);
                if (cred == null)
                {
                    md.Authentication = AuthenticationResult.AccessKeyNotFound; 
                }
                else
                {
                    md.Credential = cred;

                    User user = _Config.GetUserByAccessKey(req.AccessKey);
                    if (user == null)
                    {
                        md.Authentication = AuthenticationResult.UserNotFound;
                    }
                    else
                    {
                        md.User = user;
                        md.Authentication = AuthenticationResult.Authenticated;
                    }
                }
            }

            #endregion

            #region Bucket

            if (!String.IsNullOrEmpty(req.Bucket))
            {
                md.Bucket = _Buckets.Get(req.Bucket);

                if (md.Bucket != null)
                {
                    md.BucketClient = _Buckets.GetClient(req.Bucket);

                    if (md.BucketClient != null)
                    {
                        md.BucketAcls = md.BucketClient.GetBucketAcl();
                        md.BucketTags = md.BucketClient.GetBucketTags();
                    }
                }
            }

            #endregion

            #region Object

            if (md.BucketClient != null 
                && req.IsObjectRequest
                && !String.IsNullOrEmpty(req.Key))
            {
                md.Obj = md.BucketClient.GetObjectMetadata(req.Key);

                if (md.Obj != null)
                {
                    md.ObjectAcls = md.BucketClient.GetObjectAcl(md.Obj.GUID);
                    md.ObjectTags = md.BucketClient.GetObjectTags(md.Obj.GUID);
                }
            }

            #endregion
            
            return md;
        }

        internal RequestMetadata AuthorizeServiceRequest(S3Request req, RequestMetadata md)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (md == null) throw new ArgumentNullException(nameof(md));

            md.Authorization = AuthorizationResult.NotAuthorized;

            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.Method.ToString() + " " + req.RawUrl + "] AuthorizeServiceRequest "; 

            #region Check-for-Admin-API-Key

            if (req.Headers.ContainsKey(_Settings.Server.HeaderApiKey))
            {
                if (req.Headers[_Settings.Server.HeaderApiKey].Equals(_Settings.Server.AdminApiKey))
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
                md.Authorization = AuthorizationResult.PermitService;
            } 

            return md;
        }

        internal RequestMetadata AuthorizeBucketRequest(S3Request req, RequestMetadata md)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (md == null) throw new ArgumentNullException(nameof(md));

            md.Authorization = AuthorizationResult.NotAuthorized;

            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.Method.ToString() + " " + req.RawUrl + "] AuthorizeBucketRequest ";
            bool allowed = false;

            #region Check-for-Bucket-Write

            if (req.RequestType == S3RequestType.BucketWrite && md.Authentication == AuthenticationResult.Authenticated)
            {
                md.Authorization = AuthorizationResult.PermitBucketOwnership;
                return md;
            }

            #endregion

            #region Check-for-Admin-API-Key

            if (req.Headers.ContainsKey(_Settings.Server.HeaderApiKey))
            {
                if (req.Headers[_Settings.Server.HeaderApiKey].Equals(_Settings.Server.AdminApiKey))
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
                switch (req.RequestType)
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
                switch (req.RequestType)
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

            #region Check-for-Bucket-Owner

            if (md.Bucket != null)
            {
                if (md.Bucket.OwnerGUID.Equals(md.User.GUID))
                {
                    md.Authorization = AuthorizationResult.PermitBucketOwnership;
                    return md;
                }
            }

            #endregion

            #region Check-for-Bucket-AuthenticatedUsers-ACL

            if (md.BucketAcls != null && md.BucketAcls.Count > 0)
            {
                switch (req.RequestType)
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
                switch (req.RequestType)
                {
                    case S3RequestType.BucketExists:
                    case S3RequestType.BucketRead:
                    case S3RequestType.BucketReadVersioning:
                    case S3RequestType.BucketReadVersions: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
                            && (b.PermitRead || b.FullControl)); 
                        break;

                    case S3RequestType.BucketReadAcl: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
                            && (b.PermitReadAcp || b.FullControl)); 
                        break;

                    case S3RequestType.BucketDelete:
                    case S3RequestType.BucketDeleteTags:
                    case S3RequestType.BucketWrite:
                    case S3RequestType.BucketWriteTags:
                    case S3RequestType.BucketWriteVersioning: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
                            && (b.PermitWrite || b.FullControl)); 
                        break;

                    case S3RequestType.BucketWriteAcl: 
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
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

        internal RequestMetadata AuthorizeObjectRequest(S3Request req, RequestMetadata md)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (md == null) throw new ArgumentNullException(nameof(md));

            md.Authorization = AuthorizationResult.NotAuthorized;

            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.Method.ToString() + " " + req.RawUrl + "] AuthorizeObjectWriteRequest ";
            bool allowed = false;

            #region Get-Version-ID

            long versionId = 1;
            if (!String.IsNullOrEmpty(req.VersionId))
            {
                if (!Int64.TryParse(req.VersionId, out versionId))
                {

                }
            }

            #endregion

            #region Check-for-Admin-API-Key

            if (req.Headers.ContainsKey(_Settings.Server.HeaderApiKey))
            {
                if (req.Headers[_Settings.Server.HeaderApiKey].Equals(_Settings.Server.AdminApiKey))
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
                switch (req.RequestType)
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
                switch (req.RequestType)
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
                switch (req.RequestType)
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

            #region Check-for-Bucket-Owner

            if (md.Bucket != null)
            {
                if (md.Bucket.OwnerGUID.Equals(md.User.GUID))
                {
                    md.Authorization = AuthorizationResult.PermitBucketOwnership;
                    return md;
                }
            }

            #endregion

            #region Check-for-Object-Owner

            if (md.Obj != null)
            {
                if (md.Obj.OwnerGUID.Equals(md.User.GUID))
                {
                    md.Authorization = AuthorizationResult.PermitObjectOwnership;
                    return md;
                }
            }

            #endregion

            #region Check-for-Bucket-AuthenticatedUsers-ACL

            if (md.BucketAcls != null && md.BucketAcls.Count > 0)
            {
                switch (req.RequestType)
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
                switch (req.RequestType)
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
                switch (req.RequestType)
                {
                    case S3RequestType.ObjectExists:
                    case S3RequestType.ObjectRead:
                    case S3RequestType.ObjectReadLegalHold:
                    case S3RequestType.ObjectReadRange:
                    case S3RequestType.ObjectReadRetention:
                    case S3RequestType.ObjectReadTags:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
                            && (b.PermitRead || b.FullControl));
                        break;

                    case S3RequestType.ObjectReadAcl:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
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
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
                            && (b.PermitWrite || b.FullControl));
                        break;

                    case S3RequestType.ObjectWriteAcl:
                        allowed = md.BucketAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
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
                switch (req.RequestType)
                {
                    case S3RequestType.ObjectExists:
                    case S3RequestType.ObjectRead:
                    case S3RequestType.ObjectReadLegalHold:
                    case S3RequestType.ObjectReadRange:
                    case S3RequestType.ObjectReadRetention:
                    case S3RequestType.ObjectReadTags:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
                            && (b.PermitRead || b.FullControl));
                        break;

                    case S3RequestType.ObjectReadAcl:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
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
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
                            && (b.PermitWrite || b.FullControl));
                        break;

                    case S3RequestType.ObjectWriteAcl:
                        allowed = md.ObjectAcls.Exists(
                            b => !String.IsNullOrEmpty(b.UserGUID)
                            && b.UserGUID.Equals(md.User.GUID)
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

        #endregion
    }
}
