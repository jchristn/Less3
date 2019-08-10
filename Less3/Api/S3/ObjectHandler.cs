using System;
using System.Collections.Generic;
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
    /// Object API callbacks.
    /// </summary>
    public class ObjectHandler
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
        public ObjectHandler(
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
        /// Delete object API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response Delete(S3Request req)
        {
            Dictionary<string, string> respHeaders = new Dictionary<string, string>();

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("ObjectHandler Delete unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("ObjectHandler Delete unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null;
            if (!client.GetObjectMetadata(req.Key, versionId, out obj))
            {
                _Logging.Warn("ObjectHandler Delete unable to find metadata for " + req.Bucket + "/" + req.Key + " version " + versionId);
                if (versionId == 1) return new S3Response(req, ErrorCode.NoSuchKey);
                else return new S3Response(req, ErrorCode.NoSuchVersion);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeObjectRequest(
                RequestType.ObjectDelete,
                req,
                user,
                cred,
                bucket,
                client,
                obj,
                out authResult))
            {
                _Logging.Warn("ObjectHandler Delete unable to authenticate or authorize request for bucket " + req.Bucket + "/" + req.Key + " version " + versionId);
                return new S3Response(req, ErrorCode.AccessDenied);
            }
             
            if (obj.DeleteMarker == 1)
            {
                respHeaders.Add("x-amz-delete-marker", "true");
                return new S3Response(req, ErrorCode.NoSuchKey);
            }

            client.DeleteObject(obj.Key, versionId);
            return new S3Response(req, 204, "text/plain", null, null);
        }

        /// <summary>
        /// Delete multiple objects API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response DeleteMultiple(S3Request req)
        {
            DeleteMultiple reqBody = null;
            if (req.DataStream != null)
            {
                try
                {
                    req.Data = Common.StreamToBytes(req.DataStream); 
                    reqBody = Common.DeserializeXml<DeleteMultiple>(Encoding.UTF8.GetString(req.Data));
                }
                catch (Exception e)
                {
                    _Logging.LogException("ObjectHandler", "DeleteMultiple", e);
                    return new S3Response(req, ErrorCode.InvalidRequest);
                }
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("ObjectHandler DeleteMultiple unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("ObjectHandler DeleteMultiple unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            DeleteResult resp = new DeleteResult();

            if (reqBody.Object.Count > 0)
            {   
                foreach (S3ServerInterface.S3Objects.Object curr in reqBody.Object)
                { 
                    long versionId = 1;  
                    if (!String.IsNullOrEmpty(curr.VersionId)) versionId = Convert.ToInt64(curr.VersionId);

                    Obj obj = null;
                    if (!client.GetObjectMetadata(curr.Key, versionId, out obj))
                    {
                        _Logging.Warn("ObjectHandler DeleteMultiple unable to find metadata for " + req.Bucket + "/" + curr.Key);
                        Error error = null;
                        
                        if (versionId > 1) error = new Error(ErrorCode.NoSuchVersion);
                        else error = new Error(ErrorCode.NoSuchKey);
                        error.Key = curr.Key;
                        error.VersionId = versionId.ToString();
                        resp.Error.Add(error);
                        continue;
                    }

                    AuthResult authResult = AuthResult.Denied; 
                    _Auth.Authenticate(req, out user, out cred);
                    if (!_Auth.AuthorizeObjectRequest(
                        RequestType.ObjectDeleteMultiple,
                        req,
                        user,
                        cred,
                        bucket,
                        client,
                        obj,
                        out authResult))
                    { 
                        _Logging.Warn("ObjectHandler DeleteMultiple unable to authenticate or authorize request for " + req.Bucket + "/" + curr.Key);
                        Error error = new Error(ErrorCode.AccessDenied);
                        error.Key = curr.Key;
                        error.VersionId = versionId.ToString();
                        resp.Error.Add(error);
                        continue;
                    }

                    if (!client.DeleteObject(curr.Key, versionId))
                    {
                        Error error = null; 
                        if (versionId > 1) error = new Error(ErrorCode.NoSuchVersion);
                        else error = new Error(ErrorCode.NoSuchKey);
                        error.Key = curr.Key;
                        error.VersionId = versionId.ToString(); 
                        resp.Error.Add(error);
                    }
                    else
                    {
                        Deleted deleted = new Deleted();
                        deleted.Key = curr.Key;
                        deleted.VersionId = versionId.ToString();
                        resp.Deleted.Add(deleted); 
                    }
                }
            }

            return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(resp)));
        }

        /// <summary>
        /// Delete object tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response DeleteTags(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("ObjectHandler DeleteTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("ObjectHandler DeleteTags unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null;
            if (!client.GetObjectMetadata(req.Key, versionId, out obj))
            {
                _Logging.Warn("ObjectHandler DeleteTags unable to find metadata for " + req.Bucket + "/" + req.Key + " version " + versionId);
                if (versionId == 1) return new S3Response(req, ErrorCode.NoSuchKey);
                else return new S3Response(req, ErrorCode.NoSuchVersion);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeObjectRequest(
                RequestType.ObjectDeleteTags,
                req,
                user,
                cred,
                bucket,
                client,
                obj,
                out authResult))
            {
                _Logging.Warn("ObjectHandler DeleteTags unable to authenticate or authorize request for bucket " + req.Bucket + "/" + req.Key + " version " + versionId);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            client.DeleteObjectTags(req.Key, versionId);
            return new S3Response(req, 204, "text/plain", null, null);
        }

        /// <summary>
        /// Object exists API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response Exists(S3Request req)
        { 
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("ObjectHandler Exists unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("ObjectHandler Exists unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null;
            if (!client.GetObjectMetadata(req.Key, versionId, out obj))
            {
                _Logging.Warn("ObjectHandler Exists unable to find metadata for " + req.Bucket + "/" + req.Key + " version " + versionId);
                if (versionId == 1) return new S3Response(req, ErrorCode.NoSuchKey);
                else return new S3Response(req, ErrorCode.NoSuchVersion);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeObjectRequest(
                RequestType.ObjectExists,
                req,
                user,
                cred,
                bucket,
                client,
                obj,
                out authResult))
            {
                _Logging.Warn("ObjectHandler Exists unable to authenticate or authorize request for bucket " + req.Bucket + "/" + req.Key + " version " + versionId);
                return new S3Response(req, ErrorCode.AccessDenied);
            }
             
            return new S3Response(req, 200, "text/plain", null, obj.ContentLength); 
        }

        /// <summary>
        /// Object read API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response Read(S3Request req)
        { 
            Dictionary<string, string> respHeaders = new Dictionary<string, string>();

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("ObjectHandler Read unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("ObjectHandler Read unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null;
            if (!client.GetObjectMetadata(req.Key, versionId, out obj))
            {
                _Logging.Warn("ObjectHandler Read unable to find metadata for " + req.Bucket + "/" + req.Key + " version " + versionId);
                if (versionId == 1) return new S3Response(req, ErrorCode.NoSuchKey);
                else return new S3Response(req, ErrorCode.NoSuchVersion);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeObjectRequest(
                RequestType.ObjectRead,
                req,
                user,
                cred,
                bucket,
                client,
                obj,
                out authResult))
            {
                _Logging.Warn("ObjectHandler Read unable to authenticate or authorize request for bucket " + req.Bucket + "/" + req.Key + " version " + versionId);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            if (obj.DeleteMarker == 1)
            { 
                respHeaders.Add("x-amz-delete-marker", "true");
                return new S3Response(req, ErrorCode.NoSuchKey);
            }

            using (FileStream fs = new FileStream(GetObjectBlobFile(bucket, obj), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                MemoryStream ms = new MemoryStream();

                long bytesRemaining = obj.ContentLength;
                byte[] buffer = new byte[65536];
                int bytesRead = 0;

                while (bytesRemaining > 0)
                {
                    bytesRead = fs.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        ms.Write(buffer, 0, bytesRead);
                        bytesRemaining -= bytesRead;
                    }
                }

                ms.Seek(0, SeekOrigin.Begin);
                return new S3Response(req, 200, obj.ContentType, null, obj.ContentLength, ms); 
            }
        }

        /// <summary>
        /// Object read ACL API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ReadAcl(S3Request req)
        { 
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("ObjectHandler ReadAcl unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("ObjectHandler ReadAcl unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null;
            if (!client.GetObjectMetadata(req.Key, versionId, out obj))
            {
                _Logging.Debug("ObjectHandler ReadAcl unable to find metadata for " + req.Bucket + "/" + req.Key + " version " + versionId);
                if (versionId == 1) return new S3Response(req, ErrorCode.NoSuchKey);
                else return new S3Response(req, ErrorCode.NoSuchVersion);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeObjectRequest(
                RequestType.ObjectReadAcl,
                req,
                user,
                cred,
                bucket,
                client,
                obj,
                out authResult))
            {
                _Logging.Warn("ObjectHandler ReadAcl unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            AccessControlPolicy ret = new AccessControlPolicy();
            ret.Owner = new S3ServerInterface.S3Objects.Owner();
            ret.Owner.DisplayName = user.Name;
            ret.Owner.ID = user.GUID;

            ret.AccessControlList = new AccessControlList(); 
            ret.AccessControlList.Grant = new List<Grant>();

            List<ObjectAcl> objectAcls = new List<ObjectAcl>();
            client.GetObjectAcl(req.Key, versionId, out objectAcls);

            foreach (ObjectAcl curr in objectAcls)
            { 
                if (!String.IsNullOrEmpty(curr.UserGUID))
                {
                    #region Individual-Permissions

                    User tempUser = null;
                    if (!_Config.GetUserByGuid(curr.UserGUID, out tempUser))
                    {
                        _Logging.Warn("ObjectHandler ReadAcl unlinked ACL ID " + curr.Id + ", could not find user GUID " + curr.UserGUID);
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
                    _Logging.Warn("ObjectHandler ReadAcl incorrectly configured object ACL in ID " + curr.Id);
                }
            }
             
            return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(ret)));
        }

        /// <summary>
        /// Object read range API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ReadRange(S3Request req)
        { 
            Dictionary<string, string> respHeaders = new Dictionary<string, string>();

            string rangeHeader = req.RetrieveHeaderValue("Range");
            if (String.IsNullOrEmpty(rangeHeader)) return Read(req);

            long startPosition = 0;
            long endPosition = 0;
            ParseRangeHeader(rangeHeader, out startPosition, out endPosition);

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("ObjectHandler ReadRange unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("ObjectHandler ReadRange unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null;
            if (!client.GetObjectMetadata(req.Key, versionId, out obj))
            {
                _Logging.Warn("ObjectHandler ReadRange unable to find metadata for " + req.Bucket + "/" + req.Key + " version " + versionId);
                if (versionId == 1) return new S3Response(req, ErrorCode.NoSuchKey);
                else return new S3Response(req, ErrorCode.NoSuchVersion);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeObjectRequest(
                RequestType.ObjectReadRange,
                req,
                user,
                cred,
                bucket,
                client,
                obj,
                out authResult))
            {
                _Logging.Warn("ObjectHandler ReadRange unable to authenticate or authorize request for bucket " + req.Bucket + "/" + req.Key + " version " + versionId);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            if (obj.DeleteMarker == 1)
            {
                respHeaders.Add("x-amz-delete-marker", "true");
                return new S3Response(req, ErrorCode.NoSuchKey);
            }
             
            long readLen = endPosition - startPosition;
            if (endPosition > 0)
            {
                if (readLen < 1)
                {
                    _Logging.Warn("ObjectHandler ReadRange invalid range supplied, start " + startPosition + " end " + endPosition);
                    return new S3Response(req, ErrorCode.InvalidRange);
                }
            }
            else
            {
                endPosition = obj.ContentLength;
            }

            if (endPosition > obj.ContentLength)
            {
                _Logging.Warn("ObjectHandler ReadRange out of range " + req.Bucket + "/" + req.Key + " version " + versionId);
                return new S3Response(req, ErrorCode.InvalidRange);
            }

            byte[] respData = new byte[readLen];
            using (FileStream fs = new FileStream(GetObjectBlobFile(bucket, obj), FileMode.Open))
            {
                fs.Seek(startPosition, SeekOrigin.Begin);
                fs.Read(respData, 0, respData.Length);
            }

            return new S3Response(req, 200, obj.ContentType, null, respData);
        }

        /// <summary>
        /// Object read tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ReadTags(S3Request req)
        { 
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("ObjectHandler ReadTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("ObjectHandler ReadTags unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null;
            if (!client.GetObjectMetadata(req.Key, versionId, out obj))
            {
                _Logging.Warn("ObjectHandler ReadTags unable to find metadata for " + req.Bucket + "/" + req.Key + " version " + versionId);
                if (versionId == 1) return new S3Response(req, ErrorCode.NoSuchKey);
                else return new S3Response(req, ErrorCode.NoSuchVersion);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeObjectRequest(
                RequestType.ObjectReadTags,
                req,
                user,
                cred,
                bucket,
                client,
                obj,
                out authResult))
            {
                _Logging.Warn("ObjectHandler ReadTags unable to authenticate or authorize request for bucket " + req.Bucket + "/" + req.Key + " version " + versionId);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            Dictionary<string, string> storedTags = client.GetObjectTags(req.Key, versionId);
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
        /// Object write API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response Write(S3Request req)
        { 
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("ObjectHandler Write unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("ObjectHandler Write unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeObjectRequest(
                RequestType.ObjectWrite,
                req,
                user,
                cred,
                bucket,
                client, 
                out authResult))
            {
                _Logging.Warn("ObjectHandler Write unable to authenticate or authorize request for bucket " + req.Bucket + "/" + req.Key);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            Obj obj = null;
            if (client.GetObjectMetadata(req.Key, out obj))
            { 
                if (!bucket.EnableVersioning)
                { 
                    _Logging.Warn("ObjectHandler Write metadata already exists for " + req.Bucket + "/" + req.Key);
                    return new S3Response(req, ErrorCode.InvalidBucketState);
                }
            }
            
            if (obj == null)
            { 
                // new object 
                DateTime ts = DateTime.Now.ToUniversalTime(); 
                obj = new Obj();
                if (user != null && !String.IsNullOrEmpty(user.Name)) obj.Author = user.Name;
                obj.BlobFilename = Guid.NewGuid().ToString();
                obj.ContentLength = req.ContentLength;
                obj.ContentType = req.ContentType;
                obj.CreatedUtc = ts;
                obj.Version = 1;
                obj.DeleteMarker = 0;
                obj.ExpirationUtc = null;
                obj.Key = req.Key;
                obj.LastAccessUtc = ts;
                obj.LastUpdateUtc = ts; 

                if (user != null && !String.IsNullOrEmpty(user.Name)) obj.Owner = user.Name;

                if (!client.AddObject(obj, req.DataStream))
                { 
                    _Logging.Warn("ObjectHandler Write unable to write database entry for " + req.Bucket + "/" + req.Key);
                    return new S3Response(req, ErrorCode.InternalError);
                } 
            }
            else
            { 
                // new version 
                DateTime ts = DateTime.Now.ToUniversalTime();
                if (user != null && !String.IsNullOrEmpty(user.Name)) obj.Author = user.Name;
                obj.Version = obj.Version + 1;
                obj.BlobFilename = Guid.NewGuid().ToString();  
                obj.ContentLength = req.ContentLength;
                obj.ContentType = req.ContentType;
                obj.CreatedUtc = ts; 
                obj.DeleteMarker = 0;
                obj.ExpirationUtc = null;
                obj.Key = req.Key;
                obj.LastAccessUtc = ts;
                obj.LastUpdateUtc = ts; 

                if (user != null && !String.IsNullOrEmpty(user.Name)) obj.Owner = user.Name;

                if (!client.AddObject(obj, req.DataStream))
                { 
                    _Logging.Warn("ObjectHandler Write unable to write database entry for " + req.Bucket + "/" + req.Key);
                    return new S3Response(req, ErrorCode.InternalError);
                } 
            }

            #region Permissions-in-Headers

            List<Grant> grants = GrantsFromHeaders(user, req.Headers);
            if (grants != null && grants.Count > 0)
            {
                foreach (Grant curr in grants)
                {
                    if (curr.Grantee != null)
                    {
                        ObjectAcl objectAcl = null;
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
                                _Logging.Warn("ObjectHandler Write unable to retrieve user " + curr.Grantee.ID + " to add ACL to object " + req.Bucket + "/" + req.Key + " version " + obj.Version);
                                continue;
                            }

                            if (String.IsNullOrEmpty(curr.Permission))
                            {
                                _Logging.Warn("ObjectHandler no permissions specified for user " + curr.Grantee.ID + " in ACL for object " + req.Bucket + "/" + req.Key);
                                continue;
                            }

                            if (curr.Permission.Equals("READ")) permitRead = true;
                            else if (curr.Permission.Equals("WRITE")) permitWrite = true;
                            else if (curr.Permission.Equals("READ_ACP")) permitReadAcp = true;
                            else if (curr.Permission.Equals("WRITE_ACP")) permitWriteAcp = true;
                            else if (curr.Permission.Equals("FULL_CONTROL")) fullControl = true;

                            objectAcl = ObjectAcl.ObjectUserAcl(curr.Grantee.ID, user.GUID, req.Key, obj.Version, permitRead, permitWrite, permitReadAcp, permitWriteAcp, fullControl);
                            client.AddObjectAcl(objectAcl);
                        }
                        else if (!String.IsNullOrEmpty(curr.Grantee.URI))
                        {
                            if (String.IsNullOrEmpty(curr.Permission))
                            {
                                _Logging.Warn("ObjectHandler no permissions specified for user " + curr.Grantee.ID + " in ACL for object " + req.Bucket + "/" + req.Key + " version " + obj.Version);
                                continue;
                            }

                            if (curr.Permission.Equals("READ")) permitRead = true;
                            else if (curr.Permission.Equals("WRITE")) permitWrite = true;
                            else if (curr.Permission.Equals("READ_ACP")) permitReadAcp = true;
                            else if (curr.Permission.Equals("WRITE_ACP")) permitWriteAcp = true;
                            else if (curr.Permission.Equals("FULL_CONTROL")) fullControl = true;

                            objectAcl = ObjectAcl.ObjectGroupAcl(curr.Grantee.URI, user.GUID, req.Key, obj.Version, permitRead, permitWrite, permitReadAcp, permitWriteAcp, fullControl);
                            client.AddObjectAcl(objectAcl);
                        }
                    }
                }
            }

            #endregion

            return new S3Response(req, 200, "text/plain", null, null);
        }

        /// <summary>
        /// Object write ACL API callback.
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
                    _Logging.LogException("ObjectHandler", "WriteAcl", e); 
                    return new S3Response(req, ErrorCode.InvalidRequest);
                }
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("ObjectHandler WriteAcl unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("ObjectHandler WriteAcl unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null;
            if (!client.GetObjectMetadata(req.Key, versionId, out obj))
            {
                _Logging.Warn("ObjectHandler WriteAcl unable to find metadata for " + req.Bucket + "/" + req.Key + " version " + versionId);
                if (versionId == 1) return new S3Response(req, ErrorCode.NoSuchKey);
                else return new S3Response(req, ErrorCode.NoSuchVersion);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeObjectRequest(
                RequestType.ObjectWriteAcl,
                req,
                user,
                cred,
                bucket,
                client,
                obj,
                out authResult))
            {
                _Logging.Warn("ObjectHandler WriteAcl unable to authenticate or authorize request for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.AccessDenied);
            }

            client.DeleteObjectAcl(req.Key, versionId);

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
                ObjectAcl acl = null;
                User tempUser = null;
 
                if (curr.Grantee is CanonicalUser)
                {
                    #region User-ACL

                    if (!_Config.GetUserByGuid(curr.Grantee.ID, out tempUser))
                    {
                        _Logging.Warn("ObjectHandler WriteAcl unable to find user GUID " + curr.Grantee.ID);
                        continue;
                    }

                    if (curr.Permission.Equals("READ"))
                        acl = ObjectAcl.ObjectUserAcl(curr.Grantee.ID, bucket.OwnerGUID, req.Key, versionId, true, false, false, false, false);
                    else if (curr.Permission.Equals("WRITE"))
                        acl = ObjectAcl.ObjectUserAcl(curr.Grantee.ID, bucket.OwnerGUID, req.Key, versionId, false, true, false, false, false);
                    else if (curr.Permission.Equals("READ_ACP"))
                        acl = ObjectAcl.ObjectUserAcl(curr.Grantee.ID, bucket.OwnerGUID, req.Key, versionId, false, false, true, false, false);
                    else if (curr.Permission.Equals("WRITE_ACP"))
                        acl = ObjectAcl.ObjectUserAcl(curr.Grantee.ID, bucket.OwnerGUID, req.Key, versionId, false, false, false, true, false);
                    else if (curr.Permission.Equals("FULL_CONTROL"))
                        acl = ObjectAcl.ObjectUserAcl(curr.Grantee.ID, bucket.OwnerGUID, req.Key, versionId, false, false, false, false, true);

                    #endregion
                }
                else if (curr.Grantee is Group)
                {
                    #region Group-ACL

                    if (curr.Permission.Equals("READ"))
                        acl = ObjectAcl.ObjectGroupAcl(curr.Grantee.URI, bucket.OwnerGUID, req.Key, versionId, true, false, false, false, false);
                    else if (curr.Permission.Equals("WRITE"))
                        acl = ObjectAcl.ObjectGroupAcl(curr.Grantee.URI, bucket.OwnerGUID, req.Key, versionId, false, true, false, false, false);
                    else if (curr.Permission.Equals("READ_ACP"))
                        acl = ObjectAcl.ObjectGroupAcl(curr.Grantee.URI, bucket.OwnerGUID, req.Key, versionId, false, false, true, false, false);
                    else if (curr.Permission.Equals("WRITE_ACP"))
                        acl = ObjectAcl.ObjectGroupAcl(curr.Grantee.URI, bucket.OwnerGUID, req.Key, versionId, false, false, false, true, false);
                    else if (curr.Permission.Equals("FULL_CONTROL"))
                        acl = ObjectAcl.ObjectGroupAcl(curr.Grantee.URI, bucket.OwnerGUID, req.Key, versionId, false, false, false, false, true);

                    #endregion
                } 

                if (acl != null)
                {
                    acl.ObjectKey = req.Key; 
                    client.AddObjectAcl(acl);
                }
            }

            return new S3Response(req, 200, "text/plain", null, null);
        }

        /// <summary>
        /// Object write tags API callback.
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
                    _Logging.LogException("ObjectHandler", "WriteTags", e);
                    return new S3Response(req, ErrorCode.InvalidRequest);
                }
            }
            else
            {
                reqBody = new Tagging();
                reqBody.TagSet = new List<Tag>();
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Warn("ObjectHandler WriteTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Warn("ObjectHandler WriteTags unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null;
            if (!client.GetObjectMetadata(req.Key, versionId, out obj))
            {
                _Logging.Warn("ObjectHandler WriteTags unable to find metadata for " + req.Bucket + "/" + req.Key + " version " + versionId);
                if (versionId == 1) return new S3Response(req, ErrorCode.NoSuchKey);
                else return new S3Response(req, ErrorCode.NoSuchVersion);
            }

            User user = null;
            Credential cred = null;
            AuthResult authResult = AuthResult.Denied;
            _Auth.Authenticate(req, out user, out cred);
            if (!_Auth.AuthorizeObjectRequest(
                RequestType.ObjectWriteTags,
                req,
                user,
                cred,
                bucket,
                client,
                obj,
                out authResult))
            {
                _Logging.Warn("ObjectHandler WriteTags unable to authenticate or authorize request for bucket " + req.Bucket + "/" + req.Key + " version " + versionId);
                return new S3Response(req, ErrorCode.AccessDenied);
            }
             
            client.DeleteObjectTags(req.Key, versionId);

            Dictionary<string, string> tags = new Dictionary<string, string>();
            if (reqBody.TagSet != null && reqBody.TagSet.Count > 0)
            {
                foreach (Tag curr in reqBody.TagSet)
                {
                    tags.Add(curr.Key, curr.Value);
                }
            }

            client.AddObjectTags(req.Key, versionId, tags);
            return new S3Response(req, 204, "text/plain", null, null);  
        }

        #endregion

        #region Private-Methods

        private string AmazonTimestamp(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffz");
        }

        private string GetObjectBlobFile(BucketConfiguration bucket, Obj obj)
        {
            return bucket.ObjectsDirectory + obj.BlobFilename;
        }
         
        private string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

        private string TimestampUtc(DateTime? ts)
        {
            if (ts == null) return null;
            return Convert.ToDateTime(ts).ToUniversalTime().ToString(TimestampFormat);
        }

        private string TimestampUtc(DateTime ts)
        {
            return ts.ToUniversalTime().ToString(TimestampFormat);
        }

        private void ParseRangeHeader(string header, out long start, out long end)
        {
            if (String.IsNullOrEmpty(header)) throw new ArgumentNullException(nameof(header));
            header = header.ToLower();
            if (header.StartsWith("bytes=")) header = header.Substring(6);
            string[] vals = header.Split('-');
            if (vals.Length != 2) throw new ArgumentException("Invalid range header: " + header);

            start = 0;
            end = 0;

            if (!String.IsNullOrEmpty(vals[0])) start = Convert.ToInt64(vals[0]);
            if (!String.IsNullOrEmpty(vals[1])) end = Convert.ToInt64(vals[1]); 
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

        #endregion
    }
}
