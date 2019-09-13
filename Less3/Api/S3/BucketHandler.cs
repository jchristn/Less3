using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
    internal class BucketHandler
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

        internal BucketHandler(
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

        #region Internal-Methods

        internal async Task Delete(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn(header + "BucketHandler Delete unable to retrieve configuration for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler Delete unable to retrieve client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
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
                _Logging.Warn(header + "BucketHandler Delete unable to authenticate or authorize request for bucket " + req.Bucket);
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }
              
            long count = 0;
            long bytes = 0;

            client.GetCounts(out count, out bytes);

            if (count > 0 || bytes > 0)
            {
                _Logging.Warn(header + "BucketHandler Delete bucket " + bucket.Name + " is not empty");
                await resp.Send(ErrorCode.BucketNotEmpty);
                return;
            }

            _Logging.Info(header + "BucketHandler Delete deleting bucket " + req.Bucket);
            _Buckets.Remove(bucket, true);

            resp.StatusCode = 204;
            resp.ContentType = "application/xml";
            await resp.Send();
            return;
        }

        internal async Task DeleteTags(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn(header + "BucketHandler DeleteTags unable to retrieve configuration for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler DeleteTags unable to retrieve client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
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
                _Logging.Warn(header + "BucketHandler DeleteTags unable to authenticate or authorize request for bucket " + req.Bucket);
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }
              
            client.DeleteBucketTags();

            resp.StatusCode = 204;
            resp.ContentType = "application/xml";
            await resp.Send();
            return;
        }

        internal async Task Exists(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn(header + "BucketHandler Exists unable to retrieve configuration for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler Exists unable to retrieve client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
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
                _Logging.Warn(header + "BucketHandler Exists unable to authenticate or authorize request for bucket " + req.Bucket);
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            resp.StatusCode = 200;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
        }

        internal async Task Read(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            string continuationToken = req.RetrieveHeaderValue("continuation-token");
            long startIndex = 0;
            if (!String.IsNullOrEmpty(continuationToken)) startIndex = ParseContinuationToken(continuationToken);
            if (req.MaxKeys < 1 || req.MaxKeys > 1000) req.MaxKeys = 1000;

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn(header + "BucketHandler Read unable to retrieve configuration for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler Read unable to retrieve client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
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
                _Logging.Warn(header + "BucketHandler Read unable to authenticate or authorize request for bucket " + req.Bucket);
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            List<Obj> objs = new List<Obj>();
            client.Enumerate(req.Prefix, startIndex, (int)req.MaxKeys, out objs);

            long numObjects = 0;
            long numBytes = 0;
            client.GetCounts(out numObjects, out numBytes);

            long maxId = 0;
            if (objs.Count > 0)
            {
                objs = objs.OrderBy(p => p.Id).ToList();
                maxId = objs[objs.Count - 1].Id;
            }

            ListBucketResult listBucketResult = new ListBucketResult();
            listBucketResult.Contents = new List<Contents>();

            if (objs.Count > req.MaxKeys && objs.Count == req.MaxKeys)
            {
                listBucketResult.IsTruncated = true;
                listBucketResult.NextContinuationToken = BuildContinuationToken(maxId);
            }
            else
            {
                listBucketResult.IsTruncated = false;
            }

            listBucketResult.KeyCount = objs.Count;
            listBucketResult.MaxKeys = req.MaxKeys;
            listBucketResult.Name = req.Bucket;
            listBucketResult.Prefix = req.Prefix;

            foreach (Obj curr in objs)
            {
                Contents c = new Contents();
                c.ETag = "\"" + curr.Md5 + "\"";
                c.Key = curr.Key;
                c.LastModified = curr.LastUpdateUtc;
                c.Size = curr.ContentLength;
                c.StorageClass = "STANDARD";
                listBucketResult.Contents.Add(c);
            }

            resp.StatusCode = 200;
            resp.ContentType = "application/xml";
            await resp.Send(Common.SerializeXml<ListBucketResult>(listBucketResult, false));
            return;
        }

        internal async Task ReadLocation(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            LocationConstraint loc = new LocationConstraint();
            loc.Text = _Settings.Server.RegionString;

            resp.StatusCode = 200;
            resp.ContentType = "application/xml";
            await resp.Send(Common.SerializeXml<LocationConstraint>(loc, false));

            return;
        }

        internal async Task ReadAcl(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn(header + "BucketHandler ReadAcl unable to retrieve configuration for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler ReadAcl unable to retrieve client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
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
                _Logging.Warn(header + "BucketHandler ReadAcl unable to authenticate or authorize request for bucket " + req.Bucket);
                await resp.Send(ErrorCode.AccessDenied);
                return;
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
                        _Logging.Warn(header + "BucketHandler ReadAcl unlinked ACL ID " + curr.Id + ", could not find user GUID " + curr.UserGUID);
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
                    _Logging.Warn(header + "BucketHandler ReadAcl incorrectly configured bucket ACL in ID " + curr.Id);
                }
            }

            resp.StatusCode = 200;
            resp.ContentType = "application/xml";
            await resp.Send(Common.SerializeXml<AccessControlPolicy>(ret, false));
            return;
        }

        internal async Task ReadTags(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn(header + "BucketHandler ReadTags unable to retrieve configuration for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler ReadTags unable to retrieve client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
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
                _Logging.Warn(header + "BucketHandler ReadTags unable to authenticate or authorize request for bucket " + req.Bucket);
                await resp.Send(ErrorCode.AccessDenied);
                return;
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

            resp.StatusCode = 200;
            resp.ContentType = "application/xml";
            await resp.Send(Common.SerializeXml<Tagging>(tags, false));
            return;
        }

        internal async Task ReadVersions(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";
            if (req.MaxKeys < 1 || req.MaxKeys > 1000) req.MaxKeys = 1000;
             
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn(header + "BucketHandler ReadVersions unable to retrieve configuration for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler ReadVersions unable to retrieve client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
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
                _Logging.Warn(header + "BucketHandler ReadVersions unable to authenticate or authorize request for bucket " + req.Bucket);
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }
             
            List<Obj> objs = new List<Obj>();
            client.Enumerate(req.Prefix, 0, (int)req.MaxKeys, out objs);

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
                
            ListVersionsResult listVersionsResult = new ListVersionsResult();
                
            if (objs.Count > req.MaxKeys && objs.Count == req.MaxKeys)
            {
                listVersionsResult.IsTruncated = true;
            }
            else
            {
                listVersionsResult.IsTruncated = false;
            }

            listVersionsResult.KeyMarker = lastKey;
            listVersionsResult.MaxKeys = req.MaxKeys;
            listVersionsResult.Name = req.Bucket;
            listVersionsResult.Prefix = req.Prefix;

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
                    listVersionsResult.DeleteMarker.Add(d);
                }
                else
                {
                    S3ServerInterface.S3Objects.Version v = new S3ServerInterface.S3Objects.Version();
                    v.ETag = null;
                    v.IsLatest = IsLatest(objs, curr.Key, curr.LastAccessUtc);
                    v.Key = curr.Key;
                    v.ETag = "\"" + curr.Md5 + "\"";
                    v.LastModified = curr.LastUpdateUtc;
                    v.Owner = new S3ServerInterface.S3Objects.Owner();
                    v.Owner.DisplayName = curr.Owner;
                    v.Owner.ID = curr.Owner;
                    v.Size = curr.ContentLength;
                    v.StorageClass = "STANDARD";
                    listVersionsResult.Version.Add(v);
                }
            }

            resp.StatusCode = 200;
            resp.ContentType = "application/xml";
            await resp.Send(Common.SerializeXml<ListVersionsResult>(listVersionsResult, false));
            return;
        }

        internal async Task ReadVersioning(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn(header + "BucketHandler ReadVersioning unable to retrieve configuration for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler ReadVersioning unable to retrieve client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
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
                _Logging.Warn(header + "BucketHandler ReadVersioning unable to authenticate or authorize request for bucket " + req.Bucket);
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }
             
            VersioningConfiguration ret = new VersioningConfiguration();
            ret.Status = "Off";
            ret.MfaDelete = "Disabled";
                 
            if (bucket.EnableVersioning)
            {
                ret.Status = "Enabled";
                ret.MfaDelete = "Disabled";
            }

            resp.StatusCode = 200;
            resp.ContentType = "application/json";
            await resp.Send(Common.SerializeXml<VersioningConfiguration>(ret, false));
            return;
        }

        internal async Task Write(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            byte[] data = null;
            S3Bucket reqBody = null;

            if (req.Data != null)
            {
                try
                {
                    data = Common.StreamToBytes(req.Data);
                    reqBody = Common.DeserializeXml<S3Bucket>(Encoding.UTF8.GetString(data));
                }
                catch (Exception e)
                {
                    _Logging.Exception(header + " BucketHandler", "Write", e);
                    await resp.Send(ErrorCode.InvalidRequest);
                    return;
                }
            }

            if (IsInvalidBucketName(reqBody.BucketName))
            {
                _Logging.Warn(header + "BucketHandler Write invalid bucket name: " + reqBody.BucketName);
                await resp.Send(ErrorCode.InvalidRequest);
                return;
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
                _Logging.Warn(header + "BucketHandler Write unable to authenticate or authorize request");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            BucketConfiguration config = new BucketConfiguration(
                req.Bucket,
                user.GUID,
                _Settings.Storage.Directory + req.Bucket + "/" + req.Bucket + ".db",
                _Settings.Storage.Directory + req.Bucket + "/Objects/");

            if (!_Buckets.Add(config))
            {
                _Logging.Warn(header + "BucketHandler Write unable to write bucket " + req.Bucket);
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler Write unable to retrieve bucket client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.InternalError);
                return;
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
                                _Logging.Warn(header + "BucketHandler Write unable to retrieve user " + curr.Grantee.ID + " to add ACL to bucket " + config.GUID);
                                continue;
                            }

                            if (String.IsNullOrEmpty(curr.Permission))
                            {
                                _Logging.Warn(header + "BucketHandler no permissions specified for user " + curr.Grantee.ID + " in ACL for bucket " + config.GUID);
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
                                _Logging.Warn(header + "BucketHandler no permissions specified for user " + curr.Grantee.ID + " in ACL for bucket " + config.GUID);
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

            resp.StatusCode = 200;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
        }

        internal async Task WriteAcl(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            byte[] data = null;
            AccessControlPolicy reqBody = null;

            if (req.Data!= null)
            {
                try
                {
                    data = Common.StreamToBytes(req.Data); 
                    string xmlString = Encoding.UTF8.GetString(data); 
                    reqBody = Common.DeserializeXml<AccessControlPolicy>(xmlString); 
                }
                catch (Exception e)
                {
                    _Logging.Exception(header + "BucketHandler", "WriteAcl", e);
                    await resp.Send(ErrorCode.InvalidRequest);
                    return;
                }
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn(header + "BucketHandler WriteAcl unable to retrieve configuration for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler WriteAcl unable to retrieve client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
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
                _Logging.Warn(header + "BucketHandler WriteAcl unable to authenticate or authorize request for bucket " + req.Bucket);
                await resp.Send(ErrorCode.AccessDenied);
                return;
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
                        _Logging.Warn(header + "BucketHandler ReadAcl unable to find user GUID " + curr.Grantee.ID);
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

            resp.StatusCode = 200;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
        }

        internal async Task WriteTags(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            byte[] data = null;
            Tagging reqBody = null;

            if (req.Data!= null)
            {
                try
                {
                    data = Common.StreamToBytes(req.Data);
                    reqBody = Common.DeserializeXml<Tagging>(Encoding.UTF8.GetString(data));
                }
                catch (Exception e)
                {
                    _Logging.Exception(header + "BucketHandler", "WriteTags", e);
                    await resp.Send(ErrorCode.InvalidRequest);
                    return;
                }
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn(header + "BucketHandler WriteTags unable to retrieve configuration for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler WriteTags unable to retrieve client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
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
                _Logging.Warn(header + "BucketHandler WriteTags unable to authenticate or authorize request for bucket " + req.Bucket);
                await resp.Send(ErrorCode.AccessDenied);
                return;
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

            resp.StatusCode = 204;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
        }

        internal async Task WriteVersioning(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + "] ";

            byte[] data = null;
            VersioningConfiguration reqBody = null;

            if (req.Data != null)
            {
                try
                {
                    data = Common.StreamToBytes(req.Data); 
                    reqBody = Common.DeserializeXml<VersioningConfiguration>(Encoding.UTF8.GetString(data));
                }
                catch (Exception e)
                {
                    _Logging.Exception(header + "BucketHandler", "WriteVersioning", e);
                    await resp.Send(ErrorCode.InvalidRequest);
                    return;
                }
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn(header + "BucketHandler WriteVersioning unable to retrieve configuration for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn(header + "BucketHandler WriteVersioning unable to retrieve client for bucket " + req.Bucket);
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
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
                _Logging.Warn(header + "BucketHandler WriteVersioning unable to authenticate or authorize request for bucket " + req.Bucket);
                await resp.Send(ErrorCode.AccessDenied);
                return;
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

            resp.StatusCode = 200;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
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
