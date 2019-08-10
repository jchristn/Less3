using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

using SyslogLogging;
using S3ServerInterface;
using S3ServerInterface.S3Objects;

using Less3.Classes;

namespace Less3.Api.S3
{
    /// <summary>
    /// Bucket API callbacks.
    /// </summary>
    public class BucketHandler
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;
        private BucketManager _Buckets;
        private AuthManager _Auth; 

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param> 
        /// <param name="config">ConfigManager.</param>
        /// <param name="buckets">BucketManager.</param>
        /// <param name="auth">AuthManager.</param> 
        public BucketHandler(
            Settings settings,
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

        #region Public-Methods

        /// <summary>
        /// Bucket delete API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response Delete(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("BucketHandler Delete unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler Delete unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketDelete,
                req,
                user,
                cred,
                bucket,
                client,
                out authResult))
            {
                _Logging.Warn("BucketHandler Delete unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied); 
            }
              
            long count = 0;
            long bytes = 0;

            client.GetCounts(out count, out bytes);

            if (count > 0 || bytes > 0)
            {
                _Logging.Warn("BucketHandler Delete bucket " + bucket.Name + " is not empty");
                return new S3Response(req, ErrorCode.BucketNotEmpty);
            }

            _Logging.Log(LoggingModule.Severity.Info, "BucketHandler Delete deleting bucket " + req.Bucket);
            _Buckets.Remove(bucket, true);
            return new S3Response(req, 204, "application/xml", null, null); 
        }

        /// <summary>
        /// Bucket delete tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response DeleteTags(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("BucketHandler DeleteTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler DeleteTags unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketDeleteTags,
                req,
                user,
                cred,
                bucket,
                client,
                out authResult))
            {
                _Logging.Warn("BucketHandler DeleteTags unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }
              
            client.DeleteBucketTags(); 
            return new S3Response(req, 204, "application/xml", null, null); 
        }

        /// <summary>
        /// Bucket exists API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response Exists(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("BucketHandler Exists unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler Exists unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketExists,
                req,
                user,
                cred,
                bucket,
                client,
                out authResult))
            {
                _Logging.Warn("BucketHandler Exists unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            return new S3Response(req, 200, "text/plain", null, null);
        }

        /// <summary>
        /// Bucket read API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response Read(S3Request req)
        {
            string continuationToken = req.RetrieveHeaderValue("continuation-token");
            long startIndex = 0;
            if (!String.IsNullOrEmpty(continuationToken)) startIndex = ParseContinuationToken(continuationToken);

            string prefix = req.RetrieveHeaderValue("prefix");

            string maxKeys = req.RetrieveHeaderValue("max-keys");
            int maxResults = 1000;
            if (!String.IsNullOrEmpty(maxKeys)) maxResults = Convert.ToInt32(maxKeys);
            if (maxResults < 1 || maxResults > 1000) maxResults = 1000;

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("BucketHandler Read unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler Read unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketRead,
                req,
                user,
                cred,
                bucket,
                client,
                out authResult))
            {
                _Logging.Warn("BucketHandler Read unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            List<Obj> objs = new List<Obj>();
            client.Enumerate(prefix, startIndex, maxResults, out objs);

            long numObjects = 0;
            long numBytes = 0;
            client.GetCounts(out numObjects, out numBytes);

            long maxId = 0;
            if (objs.Count > 0)
            {
                objs = objs.OrderBy(p => p.Id).ToList();
                maxId = objs[objs.Count - 1].Id;
            }

            ListBucketResult resp = new ListBucketResult();
            resp.Contents = new List<Contents>();

            if (objs.Count > maxResults && objs.Count == maxResults)
            {
                resp.IsTruncated = true;
                resp.NextContinuationToken = BuildContinuationToken(maxId);
            }
            else
            {
                resp.IsTruncated = false;
            }

            resp.KeyCount = objs.Count;
            resp.MaxKeys = maxResults;
            resp.Name = req.Bucket;
            resp.Prefix = prefix;

            foreach (Obj curr in objs)
            {
                Contents c = new Contents();
                c.ETag = null;
                c.Key = curr.Key;
                c.LastModified = curr.LastUpdateUtc;
                c.Size = curr.ContentLength;
                c.StorageClass = "STANDARD";
                resp.Contents.Add(c);
            }

            return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(resp)));
        }

        /// <summary>
        /// Bucket read ACL API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ReadAcl(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("BucketHandler ReadAcl unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler ReadAcl unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketReadAcl,
                req,
                user,
                cred,
                bucket,
                client,
                out authResult))
            {
                _Logging.Warn("BucketHandler ReadAcl unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            AccessControlPolicy ret = new AccessControlPolicy();
            ret.Owner = new S3ServerInterface.S3Objects.Owner();
            ret.Owner.DisplayName = user.Name;
            ret.Owner.ID = user.GUID;

            ret.AccessControlList = new AccessControlList(); 
            ret.AccessControlList.Grant = new List<Grant>();

            List<BucketAcl> bucketAcls = new List<BucketAcl>();
            client.GetBucketAcl(out bucketAcls);

            foreach (BucketAcl curr in bucketAcls)
            { 
                if (!String.IsNullOrEmpty(curr.UserGUID))
                {
                    #region Individual-Permissions

                    User tempUser = null;
                    if (!_Config.GetUserByGuid(curr.UserGUID, out tempUser))
                    {
                        _Logging.Warn("BucketHandler ReadAcl unlinked ACL ID " + curr.Id + ", could not find user GUID " + curr.UserGUID);
                        continue;
                    }

                    if (curr.PermitRead)
                    {
                        Grant grant = new Grant(); 
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = tempUser.Name;
                        grant.Grantee.ID = curr.UserGUID;
                        grant.Permission = "READ"; 
                        ret.AccessControlList.Grant.Add(grant);
                    }

                    if (curr.PermitReadAcp)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = tempUser.Name;
                        grant.Grantee.ID = curr.UserGUID;
                        grant.Permission = "READ_ACP";
                        ret.AccessControlList.Grant.Add(grant);
                    }

                    if (curr.PermitWrite)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = tempUser.Name;
                        grant.Grantee.ID = curr.UserGUID;
                        grant.Permission = "WRITE";
                        ret.AccessControlList.Grant.Add(grant);
                    }

                    if (curr.PermitWriteAcp)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = tempUser.Name;
                        grant.Grantee.ID = curr.UserGUID;
                        grant.Permission = "WRITE_ACP";
                        ret.AccessControlList.Grant.Add(grant);
                    }

                    if (curr.FullControl)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = tempUser.Name;
                        grant.Grantee.ID = curr.UserGUID;
                        grant.Permission = "FULL_CONTROL";
                        ret.AccessControlList.Grant.Add(grant);
                    }

                    #endregion
                }
                else if (!String.IsNullOrEmpty(curr.UserGroup))
                {
                    #region Group-Permissions

                    if (curr.PermitRead)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = curr.UserGroup;
                        grant.Grantee.URI = curr.UserGroup;
                        grant.Permission = "READ";
                        ret.AccessControlList.Grant.Add(grant);
                    }

                    if (curr.PermitReadAcp)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = curr.UserGroup;
                        grant.Grantee.URI = curr.UserGroup;
                        grant.Permission = "READ_ACP";
                        ret.AccessControlList.Grant.Add(grant);
                    }

                    if (curr.PermitWrite)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = curr.UserGroup;
                        grant.Grantee.URI = curr.UserGroup;
                        grant.Permission = "WRITE";
                        ret.AccessControlList.Grant.Add(grant);
                    }

                    if (curr.PermitWriteAcp)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = curr.UserGroup;
                        grant.Grantee.URI = curr.UserGroup;
                        grant.Permission = "WRITE_ACP";
                        ret.AccessControlList.Grant.Add(grant);
                    }

                    if (curr.FullControl)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = curr.UserGroup;
                        grant.Grantee.URI = curr.UserGroup;
                        grant.Permission = "FULL_CONTROL";
                        ret.AccessControlList.Grant.Add(grant);
                    }

                    #endregion
                }
                else
                {
                    _Logging.Warn("BucketHandler ReadAcl incorrectly configured bucket ACL in ID " + curr.Id);
                }
            }
             
            return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(ret)));
        }

        /// <summary>
        /// Bucket read tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ReadTags(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("BucketHandler ReadTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler ReadTags unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketReadTags,
                req,
                user,
                cred,
                bucket,
                client,
                out authResult))
            {
                _Logging.Warn("BucketHandler ReadTags unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }
             
            Dictionary<string, string> storedTags = client.GetBucketTags(); 
            Tagging tags = new Tagging();
            tags.TagSet = new List<Tag>();

            if (storedTags != null && storedTags.Count > 0)
            {
                foreach (KeyValuePair<string, string> curr in storedTags)
                {
                    Tag currTag = new Tag();
                    currTag.Key = curr.Key;
                    currTag.Value = curr.Value;
                    tags.TagSet.Add(currTag);
                }
            }

            return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(tags)));   
        }

        /// <summary>
        /// Bucket read versions API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ReadVersions(S3Request req)
        {
            string prefix = req.RetrieveHeaderValue("prefix");

            string maxKeys = req.RetrieveHeaderValue("max-keys");
            int maxResults = 1000;
            if (!String.IsNullOrEmpty(maxKeys)) maxResults = Convert.ToInt32(maxKeys);
            if (maxResults < 1 || maxResults > 1000) maxResults = 1000;

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("BucketHandler ReadVersions unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler ReadVersions unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketReadVersions,
                req,
                user,
                cred,
                bucket,
                client,
                out authResult))
            {
                _Logging.Warn("BucketHandler ReadVersions unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }
             
            List<Obj> objs = new List<Obj>();
            client.Enumerate(prefix, 0, maxResults, out objs);

            long numObjects = 0;
            long numBytes = 0;
            client.GetCounts(out numObjects, out numBytes);

            string lastKey = null;
            long maxId = 0;
            if (objs.Count > 0)
            {
                objs = objs.OrderBy(p => p.Id).ToList();
                maxId = objs[objs.Count - 1].Id;
                lastKey = objs[objs.Count - 1].Key;
            }
                
            ListVersionsResult resp = new ListVersionsResult();
                
            if (objs.Count > maxResults && objs.Count == maxResults)
            {
                resp.IsTruncated = true;
            }
            else
            {
                resp.IsTruncated = false;
            }
            resp.KeyMarker = lastKey;
            resp.MaxKeys = maxResults;
            resp.Name = req.Bucket;
            resp.Prefix = prefix;

            foreach (Obj curr in objs)
            {
                if (curr.DeleteMarker == 1)
                {
                    DeleteMarker d = new DeleteMarker();
                    d.IsLatest = IsLatest(objs, curr.Key, curr.LastAccessUtc);
                    d.Key = curr.Key;
                    d.LastModified = curr.LastUpdateUtc;
                    d.Owner = new S3ServerInterface.S3Objects.Owner();
                    d.Owner.DisplayName = curr.Owner;
                    d.Owner.ID = curr.Owner;
                    d.VersionId = curr.Version.ToString();
                    resp.DeleteMarker.Add(d);
                }
                else
                {
                    S3ServerInterface.S3Objects.Version v = new S3ServerInterface.S3Objects.Version();
                    v.ETag = null;
                    v.IsLatest = IsLatest(objs, curr.Key, curr.LastAccessUtc);
                    v.Key = curr.Key;
                    v.LastModified = curr.LastUpdateUtc;
                    v.Owner = new S3ServerInterface.S3Objects.Owner();
                    v.Owner.DisplayName = curr.Owner;
                    v.Owner.ID = curr.Owner;
                    v.Size = curr.ContentLength;
                    v.StorageClass = "STANDARD";
                    resp.Version.Add(v);
                }
            }

            return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(resp))); 
        }

        /// <summary>
        /// Bucket read versioning API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ReadVersioning(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("BucketHandler ReadVersioning unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler ReadVersioning unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketReadVersioning,
                req,
                user,
                cred,
                bucket,
                client,
                out authResult))
            {
                _Logging.Warn("BucketHandler ReadVersioning unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }
             
            VersioningConfiguration ret = new VersioningConfiguration();
            ret.Status = "Off";
            ret.MfaDelete = "Disabled";
                 
            if (bucket.EnableVersioning)
            {
                ret.Status = "Enabled";
                ret.MfaDelete = "Disabled";
                return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(ret)));
            }
            else
            { 
                return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(ret)));
            } 
        }

        /// <summary>
        /// Bucket write API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response Write(S3Request req)
        {
            S3Bucket reqBody = null;
            if (req.DataStream != null)
            {
                try
                {
                    req.Data = Common.StreamToBytes(req.DataStream);
                    reqBody = Common.DeserializeXml<S3Bucket>(Encoding.UTF8.GetString(req.Data));
                }
                catch (Exception e)
                {
                    _Logging.LogException("BucketHandler", "Write", e);
                    return new S3Response(req, ErrorCode.InvalidRequest);
                }
            }

            if (IsInvalidBucketName(reqBody.BucketName))
            {
                _Logging.Warn("BucketHandler Write invalid bucket name: " + reqBody.BucketName);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketWrite,
                req,
                user,
                cred,
                out authResult))
            {
                _Logging.Warn("BucketHandler Write unable to authenticate or authorize request");
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            BucketConfiguration config = new BucketConfiguration(
                req.Bucket,
                user.GUID,
                _Settings.Storage.Directory + req.Bucket + "/" + req.Bucket + ".db",
                _Settings.Storage.Directory + req.Bucket + "/Objects/");

            if (!_Buckets.Add(config))
            {
                _Logging.Warn("BucketHandler Write unable to write bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.InternalError);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler Write unable to retrieve bucket client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.InternalError);
            }

            #region Permissions-in-Headers

            List<Grant> grants = GrantsFromHeaders(user, req.Headers);
            if (grants != null && grants.Count > 0)
            {
                foreach (Grant curr in grants)
                {
                    if (curr.Grantee != null)
                    {
                        BucketAcl bucketAcl = null;
                        User tempUser = null;
                        bool permitRead = false;
                        bool permitWrite = false;
                        bool permitReadAcp = false;
                        bool permitWriteAcp = false;
                        bool fullControl = false;

                        if (!String.IsNullOrEmpty(curr.Grantee.ID))
                        {
                            if (!_Config.GetUserByGuid(curr.Grantee.ID, out tempUser))
                            {
                                _Logging.Warn("BucketHandler Write unable to retrieve user " + curr.Grantee.ID + " to add ACL to bucket " + config.GUID);
                                continue;
                            }

                            if (String.IsNullOrEmpty(curr.Permission))
                            {
                                _Logging.Warn("BucketHandler no permissions specified for user " + curr.Grantee.ID + " in ACL for bucket " + config.GUID);
                                continue;
                            }

                            if (curr.Permission.Equals("READ")) permitRead = true;
                            else if (curr.Permission.Equals("WRITE")) permitWrite = true;
                            else if (curr.Permission.Equals("READ_ACP")) permitReadAcp = true;
                            else if (curr.Permission.Equals("WRITE_ACP")) permitWriteAcp = true;
                            else if (curr.Permission.Equals("FULL_CONTROL")) fullControl = true;

                            bucketAcl = BucketAcl.BucketUserAcl(curr.Grantee.ID, user.GUID, permitRead, permitWrite, permitReadAcp, permitWriteAcp, fullControl);
                            client.AddBucketAcl(bucketAcl);
                        }
                        else if (!String.IsNullOrEmpty(curr.Grantee.URI))
                        {
                            if (String.IsNullOrEmpty(curr.Permission))
                            {
                                _Logging.Warn("BucketHandler no permissions specified for user " + curr.Grantee.ID + " in ACL for bucket " + config.GUID);
                                continue;
                            }

                            if (curr.Permission.Equals("READ")) permitRead = true;
                            else if (curr.Permission.Equals("WRITE")) permitWrite = true;
                            else if (curr.Permission.Equals("READ_ACP")) permitReadAcp = true;
                            else if (curr.Permission.Equals("WRITE_ACP")) permitWriteAcp = true;
                            else if (curr.Permission.Equals("FULL_CONTROL")) fullControl = true;

                            bucketAcl = BucketAcl.BucketGroupAcl(curr.Grantee.URI, user.GUID, permitRead, permitWrite, permitReadAcp, permitWriteAcp, fullControl);
                            client.AddBucketAcl(bucketAcl);
                        }
                    }
                }
            }

            #endregion

            return new S3Response(req, 200, "text/plain", null, null);
        }

        /// <summary>
        /// Bucket write ACL API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response WriteAcl(S3Request req)
        {
            AccessControlPolicy reqBody = null;
            if (req.DataStream != null)
            {
                try
                {
                    req.Data = Common.StreamToBytes(req.DataStream); 
                    string xmlString = Encoding.UTF8.GetString(req.Data); 
                    reqBody = Common.DeserializeXml<AccessControlPolicy>(xmlString); 
                }
                catch (Exception e)
                {
                    _Logging.LogException("BucketHandler", "WriteAcl", e);
                    return new S3Response(req, ErrorCode.InvalidRequest);
                }
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("BucketHandler WriteAcl unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler WriteAcl unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketWriteAcl,
                req,
                user,
                cred,
                bucket,
                client,
                out authResult))
            {
                _Logging.Warn("BucketHandler WriteAcl unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            client.DeleteBucketAcl();

            List<Grant> headerGrants = GrantsFromHeaders(user, req.Headers);
            if (headerGrants != null && headerGrants.Count > 0)
            {
                if (reqBody.AccessControlList.Grant != null)
                {
                    foreach (Grant curr in headerGrants)
                    {
                        reqBody.AccessControlList.Grant.Add(curr);
                    }
                }
                else
                {
                    reqBody.AccessControlList.Grant = new List<Grant>(headerGrants);
                }
            }

            foreach (Grant curr in reqBody.AccessControlList.Grant)
            {
                BucketAcl acl = null;
                User tempUser = null;
                 
                if (curr.Grantee is CanonicalUser)
                {
                    #region User-ACL

                    if (!_Config.GetUserByGuid(curr.Grantee.ID, out tempUser))
                    {
                        _Logging.Warn("BucketHandler ReadAcl unable to find user GUID " + curr.Grantee.ID);
                        continue;
                    }

                    if (curr.Permission.Equals("READ"))
                        acl = BucketAcl.BucketUserAcl(curr.Grantee.ID, bucket.OwnerGUID, true, false, false, false, false);
                    else if (curr.Permission.Equals("WRITE"))
                        acl = BucketAcl.BucketUserAcl(curr.Grantee.ID, bucket.OwnerGUID, false, true, false, false, false);
                    else if (curr.Permission.Equals("READ_ACP"))
                        acl = BucketAcl.BucketUserAcl(curr.Grantee.ID, bucket.OwnerGUID, false, false, true, false, false);
                    else if (curr.Permission.Equals("WRITE_ACP"))
                        acl = BucketAcl.BucketUserAcl(curr.Grantee.ID, bucket.OwnerGUID, false, false, false, true, false);
                    else if (curr.Permission.Equals("FULL_CONTROL"))
                        acl = BucketAcl.BucketUserAcl(curr.Grantee.ID, bucket.OwnerGUID, false, false, false, false, true);

                    #endregion
                }
                else if (curr.Grantee is Group)
                {
                    #region Group-ACL
                     
                    if (curr.Permission.Equals("READ"))
                        acl = BucketAcl.BucketGroupAcl(curr.Grantee.URI, bucket.OwnerGUID, true, false, false, false, false);
                    else if (curr.Permission.Equals("WRITE"))
                        acl = BucketAcl.BucketGroupAcl(curr.Grantee.URI, bucket.OwnerGUID, false, true, false, false, false);
                    else if (curr.Permission.Equals("READ_ACP"))
                        acl = BucketAcl.BucketGroupAcl(curr.Grantee.URI, bucket.OwnerGUID, false, false, true, false, false);
                    else if (curr.Permission.Equals("WRITE_ACP"))
                        acl = BucketAcl.BucketGroupAcl(curr.Grantee.URI, bucket.OwnerGUID, false, false, false, true, false);
                    else if (curr.Permission.Equals("FULL_CONTROL"))
                        acl = BucketAcl.BucketGroupAcl(curr.Grantee.URI, bucket.OwnerGUID, false, false, false, false, true);

                    #endregion
                }

                if (acl != null)
                { 
                    client.AddBucketAcl(acl);
                }
            }

            return new S3Response(req, 200, "text/plain", null, null);
        }

        /// <summary>
        /// Bucket write tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response WriteTags(S3Request req)
        {
            Tagging reqBody = null;
            if (req.DataStream != null)
            {
                try
                {
                    req.Data = Common.StreamToBytes(req.DataStream);
                    reqBody = Common.DeserializeXml<Tagging>(Encoding.UTF8.GetString(req.Data));
                }
                catch (Exception e)
                {
                    _Logging.LogException("BucketHandler", "WriteTags", e);
                    return new S3Response(req, ErrorCode.InvalidRequest);
                }
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("BucketHandler WriteTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler WriteTags unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketWriteTags,
                req,
                user,
                cred,
                bucket,
                client,
                out authResult))
            {
                _Logging.Warn("BucketHandler WriteTags unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }
             
            client.DeleteBucketTags();

            Dictionary<string, string> tagSet = new Dictionary<string, string>();

            if (reqBody.TagSet != null && reqBody.TagSet.Count > 0)
            {
                foreach (Tag curr in reqBody.TagSet)
                {
                    tagSet.Add(curr.Key, curr.Value);
                }
            }

            client.AddBucketTags(tagSet);
            return new S3Response(req, 204, "text/plain", null, null);  
        }

        /// <summary>
        /// Bucket write versioning API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response WriteVersioning(S3Request req)
        {
            VersioningConfiguration reqBody = null;
            if (req.DataStream != null)
            {
                try
                {
                    req.Data = Common.StreamToBytes(req.DataStream); 
                    reqBody = Common.DeserializeXml<VersioningConfiguration>(Encoding.UTF8.GetString(req.Data));
                }
                catch (Exception e)
                {
                    _Logging.LogException("BucketHandler", "WriteVersioning", e);
                    return new S3Response(req, ErrorCode.InvalidRequest);
                }
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("BucketHandler WriteVersioning unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("BucketHandler WriteVersioning unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeBucketRequest(
                RequestType.BucketWriteVersioning,
                req,
                user,
                cred,
                bucket,
                client,
                out authResult))
            {
                _Logging.Warn("BucketHandler WriteVersioning unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }
             
            if (reqBody.Status.Equals("Enabled") && !bucket.EnableVersioning)
            {
                bucket.EnableVersioning = true;
                _Buckets.Remove(bucket, false);
                _Buckets.Add(bucket);
            }
            else if (!reqBody.Status.Equals("Enabled") && bucket.EnableVersioning)
            {
                bucket.EnableVersioning = false;
                _Buckets.Remove(bucket, false);
                _Buckets.Add(bucket);
            }

            return new S3Response(req, 200, "text/plain", null, null); 
        }

        #endregion

        #region Private-Methods
          
        private string AmazonTimestamp(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffz");
        }
          
        private string BuildContinuationToken(long lastId)
        {
            return Common.StringToBase64(lastId.ToString());
        }

        private long ParseContinuationToken(string base64)
        {
            return Convert.ToInt64(Common.Base64ToString(base64));
        }

        private bool IsLatest(List<Obj> objs, string key, DateTime lastAccessUtc)
        {
            bool laterObjExists = objs.Exists(o =>
                o.Key.Equals(key)
                && o.LastAccessUtc > lastAccessUtc);

            return !laterObjExists;
        }

        private List<Grant> GrantsFromHeaders(User user, Dictionary<string, string> headers)
        {
            List<Grant> ret = new List<Grant>();
            if (headers == null || headers.Count < 1) return ret;

            string headerVal = null;
            string[] grantees = null;
            Grant grant = null;

            if (headers.ContainsKey("x-amz-acl"))
            {
                headerVal = headers["x-amz-acl"];

                switch (headerVal)
                {
                    case "private":
                        grant = new Grant();
                        grant.Permission = "FULL_CONTROL";
                        grant.Grantee = new Grantee();
                        grant.Grantee.ID = user.GUID;
                        grant.Grantee.DisplayName = user.Name;
                        ret.Add(grant);
                        break;

                    case "public-read":
                        grant = new Grant();
                        grant.Permission = "READ";
                        grant.Grantee = new Grantee();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        grant.Grantee.DisplayName = "AllUsers";
                        ret.Add(grant);
                        break;

                    case "public-read-write":
                        grant = new Grant();
                        grant.Permission = "READ";
                        grant.Grantee = new Grantee();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        grant.Grantee.DisplayName = "AllUsers";
                        ret.Add(grant);

                        grant = new Grant();
                        grant.Permission = "WRITE";
                        grant.Grantee = new Grantee();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        grant.Grantee.DisplayName = "AllUsers";
                        ret.Add(grant);
                        break;

                    case "authenticated-read":
                        grant = new Grant();
                        grant.Permission = "READ";
                        grant.Grantee = new Grantee();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AuthenticatedUsers";
                        grant.Grantee.DisplayName = "AuthenticatedUsers";
                        ret.Add(grant);
                        break;
                }
            }

            if (headers.ContainsKey("x-amz-grant-read"))
            {
                headerVal = headers["x-amz-grant-read"];
                grantees = headerVal.Split(',');
                if (grantees.Length > 0)
                {
                    foreach (string curr in grantees)
                    {
                        grant = null;
                        if (!GrantFromString(curr, "READ", out grant)) continue;
                        ret.Add(grant);
                    }
                }
            }

            if (headers.ContainsKey("x-amz-grant-read"))
            {
                headerVal = headers["x-amz-grant-read"];
                grantees = headerVal.Split(',');
                if (grantees.Length > 0)
                {
                    foreach (string curr in grantees)
                    {
                        grant = null;
                        if (!GrantFromString(curr, "READ", out grant)) continue;
                        ret.Add(grant);
                    }
                }
            }

            if (headers.ContainsKey("x-amz-grant-write"))
            {
                headerVal = headers["x-amz-grant-write"];
                grantees = headerVal.Split(',');
                if (grantees.Length > 0)
                {
                    foreach (string curr in grantees)
                    {
                        grant = null;
                        if (!GrantFromString(curr, "WRITE", out grant)) continue;
                        ret.Add(grant);
                    }
                }
            }

            if (headers.ContainsKey("x-amz-grant-read-acp"))
            {
                headerVal = headers["x-amz-grant-read-acp"];
                grantees = headerVal.Split(',');
                if (grantees.Length > 0)
                {
                    foreach (string curr in grantees)
                    {
                        grant = null;
                        if (!GrantFromString(curr, "READ_ACP", out grant)) continue;
                        ret.Add(grant);
                    }
                }
            }

            if (headers.ContainsKey("x-amz-grant-write-acp"))
            {
                headerVal = headers["x-amz-grant-write-acp"];
                grantees = headerVal.Split(',');
                if (grantees.Length > 0)
                {
                    foreach (string curr in grantees)
                    {
                        grant = null;
                        if (!GrantFromString(curr, "WRITE_ACP", out grant)) continue;
                        ret.Add(grant);
                    }
                }
            }

            if (headers.ContainsKey("x-amz-grant-full-control"))
            {
                headerVal = headers["x-amz-grant-full-control"];
                grantees = headerVal.Split(',');
                if (grantees.Length > 0)
                {
                    foreach (string curr in grantees)
                    {
                        grant = null;
                        if (!GrantFromString(curr, "FULL_CONTROL", out grant)) continue;
                        ret.Add(grant);
                    }
                }
            }

            return ret;
        }

        private bool GrantFromString(string str, string permType, out Grant grant)
        {
            grant = null;
            if (String.IsNullOrEmpty(str)) return false;
            if (String.IsNullOrEmpty(permType)) return false;

            string[] parts = str.Split('=');
            if (parts.Length != 2) return false;
            string granteeType = parts[0];
            string grantee = parts[1];

            grant = new Grant();
            grant.Permission = permType;
            grant.Grantee = new Grantee();

            if (granteeType.Equals("emailAddress"))
            {
                User user = null;
                if (!_Config.GetUserByEmail(grantee, out user)) return false;
                grant.Grantee.ID = user.GUID;
                grant.Grantee.DisplayName = user.Name;
                return true;
            }
            else if (granteeType.Equals("id"))
            {
                User user = null;
                if (!_Config.GetUserByGuid(grantee, out user)) return false;
                grant.Grantee.ID = user.GUID;
                grant.Grantee.DisplayName = user.Name;
                return true;
            }
            else if (granteeType.Equals("uri"))
            {
                grant.Grantee.URI = grantee;
                return true;
            }

            return false;
        }

        private bool IsInvalidBucketName(string bucket)
        {
            List<string> invalidNames = new List<string>
            {
                "admin"
            };

            if (invalidNames.Contains(bucket.ToLower())) return true;
            return false;
        }

        #endregion
    }
}
