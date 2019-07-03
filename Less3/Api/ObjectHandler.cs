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

using Less3.Classes;
using Less3.S3Responses;

namespace Less3.Api
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
        private CredentialManager _Credentials;
        private BucketManager _Buckets;
        private AuthManager _Auth;
        private UserManager _Users;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param>
        /// <param name="credentials">CredentialManager.</param>
        /// <param name="buckets">BucketManager.</param>
        /// <param name="auth">AuthManager.</param>
        /// <param name="users">UserManager.</param>
        public ObjectHandler(
            Settings settings,
            LoggingModule logging,
            CredentialManager credentials,
            BucketManager buckets,
            AuthManager auth,
            UserManager users)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));
            if (buckets == null) throw new ArgumentNullException(nameof(buckets));
            if (auth == null) throw new ArgumentNullException(nameof(auth));
            if (users == null) throw new ArgumentNullException(nameof(users));

            _Settings = settings;
            _Logging = logging;
            _Credentials = credentials;
            _Buckets = buckets;
            _Auth = auth;
            _Users = users;
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
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Delete unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Delete unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            if (!bucket.Owner.Equals(user.Name)
                && !bucket.PermittedAccessKeys.Contains(req.AccessKey))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Delete unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
             
            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Delete unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.InternalError);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null;
            if (!client.GetMetadata(req.Key, versionId, out obj))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Delete unable to find metadata for " + req.Bucket + "/" + req.Key);
                if (versionId == 1) return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchKey);
                else return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchVersion);
            }

            if (obj.DeleteMarker == 1)
            {
                respHeaders.Add("x-amz-delete-marker", "true");
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchKey);
            }

            client.Delete(obj.Key, versionId);
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
                    return new S3Response(req, S3ServerInterface.ErrorCode.InvalidRequest);
                }
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler DeleteMultiple unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }
             
            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler DeleteMultiple unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            if (!bucket.PermittedAccessKeys.Contains(req.AccessKey)
                && !bucket.Owner.Equals(user.Name))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler DeleteMultiple unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler DeleteMultiple unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            DeleteResult resp = new DeleteResult();

            if (reqBody.Object.Count > 0)
            {
                foreach (S3Responses.Object curr in reqBody.Object)
                { 
                    long versionId = 1;  
                    if (!String.IsNullOrEmpty(curr.VersionId)) versionId = Convert.ToInt64(curr.VersionId);
                    if (!client.Delete(curr.Key, versionId))
                    {
                        S3Responses.Error error = null; 
                        if (versionId > 1) error = new S3Responses.Error(S3Responses.ErrorCode.NoSuchVersion);
                        else error = new S3Responses.Error(S3Responses.ErrorCode.NoSuchKey);
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
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler DeleteTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler DeleteTags unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            if (!bucket.PermittedAccessKeys.Contains(req.AccessKey)
                && !bucket.Owner.Equals(user.Name))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler DeleteTags unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler DeleteTags unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null;
            if (!client.GetMetadata(req.Key, versionId, out obj))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler DeleteTags unable to find metadata for " + req.Bucket + "/" + req.Key);
                if (versionId == 1) return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchKey);
                else return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchVersion);
            }

            if (File.Exists(GetTagsFile(bucket, obj))) File.Delete(GetTagsFile(bucket, obj));
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
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Exists unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Exists unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                if (!bucket.EnablePublicRead)
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Exists unauthenticated request to non-public bucket " + req.Bucket);
                    return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
                }
            }
            else if (!bucket.EnablePublicRead)
            {
                if (!bucket.Owner.Equals(user.Name)
                    || !bucket.PermittedAccessKeys.Contains(req.AccessKey))
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Exists unauthorized attempt to access bucket " + req.Bucket);
                    return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
                }
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            if (client.Exists(req.Key, versionId))
            {
                Obj obj = null;
                if (!client.GetMetadata(req.Key, versionId, out obj))
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Exists unable to find metadata for " + req.Bucket + "/" + req.Key);
                    if (versionId == 1) return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchKey);
                    else return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchVersion);
                }

                return new S3Response(req, 200, "text/plain", null, obj.ContentLength);
            }
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Exists unable to find object " + req.Bucket + "/" + req.Key);
                if (versionId == 1) return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchKey);
                else return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchVersion);
            }
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
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Read unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!bucket.EnablePublicRead)
            {
                if (!_Auth.Authenticate(req, out user, out cred))
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Read unable to authenticate request for bucket " + req.Bucket);
                    return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
                }

                if (!bucket.PermittedAccessKeys.Contains(req.AccessKey)
                    && !bucket.Owner.Equals(user.Name))
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Read unauthorized access attempt to bucket " + req.Bucket);
                    return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
                }
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Read unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null; 
            if (!client.GetMetadata(req.Key, versionId, out obj))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Read unable to find metadata for " + req.Bucket + "/" + req.Key);
                if (versionId == 1) return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchKey);
                else return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchVersion);
            }

            if (obj.DeleteMarker == 1)
            { 
                respHeaders.Add("x-amz-delete-marker", "true");
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchKey);
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
            long readLen = endPosition - startPosition;
            if (readLen < 1) return new S3Response(req, S3ServerInterface.ErrorCode.InvalidRange);

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler ReadRange unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!bucket.EnablePublicRead)
            {
                if (!_Auth.Authenticate(req, out user, out cred))
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler ReadRange unable to authenticate request for bucket " + req.Bucket);
                    return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
                }

                if (!bucket.PermittedAccessKeys.Contains(req.AccessKey)
                    && !bucket.Owner.Equals(user.Name))
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler ReadRange unauthorized access attempt to bucket " + req.Bucket);
                    return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
                }
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler ReadRange unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            string versionIdStr = req.RetrieveHeaderValue("versionId");
            long versionId = 1;
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            Obj obj = null; 
            if (!client.GetMetadata(req.Key, versionId, out obj))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler ReadRange unable to find metadata for " + req.Bucket + "/" + req.Key);
                if (versionId == 1) return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchKey);
                else return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchVersion); 
            }

            if (obj.DeleteMarker == 1)
            {
                respHeaders.Add("x-amz-delete-marker", "true");
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchKey);
            } 

            if (endPosition > obj.ContentLength)
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler ReadRange out of range " + req.Bucket + "/" + req.Key);
                return new S3Response(req, S3ServerInterface.ErrorCode.InvalidRange);
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
            long versionId = 1;
            string versionIdStr = req.RetrieveHeaderValue("versionId");
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler ReadTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler ReadTags unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler ReadTags unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            Obj obj = null;
            if (!client.GetMetadata(req.Key, versionId, out obj))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler ReadTags unable to find metadata for " + req.Bucket + "/" + req.Key + " version " + versionId);
                if (versionId == 1) return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchKey);
                else return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchVersion); 
            }

            if (bucket.PermittedAccessKeys.Contains(req.AccessKey) ||
                bucket.Owner.Equals(user.Name))
            {
                if (File.Exists(GetTagsFile(bucket, obj)))
                {
                    byte[] fileData = Common.ReadBinaryFile(GetTagsFile(bucket, obj));
                    return new S3Response(req, 200, "application/xml", null, fileData);
                }
                else
                {
                    Tagging tags = new Tagging();
                    tags.TagSet = new List<Tag>();
                    File.WriteAllBytes(GetTagsFile(bucket, obj), Encoding.UTF8.GetBytes(Common.SerializeXml(tags)));
                    return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(tags)));
                }
            }
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler ReadTags unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
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
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Write unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }
             
            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Write unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
             
            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Write unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }
             
            Obj obj = null;
            if (client.GetMetadata(req.Key, out obj))
            { 
                if (!bucket.EnableVersioning)
                { 
                    _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Write metadata already exists for " + req.Bucket + "/" + req.Key);
                    return new S3Response(req, S3ServerInterface.ErrorCode.InvalidBucketState);
                }
            }
             
            if (bucket.PermittedAccessKeys.Contains(req.AccessKey) ||
                bucket.Owner.Equals(user.Name))
            { 
                if (obj == null)
                { 
                    // new object 
                    DateTime ts = DateTime.Now.ToUniversalTime(); 
                    obj = new Obj();
                    obj.Author = user.Name;
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
                    obj.Md5 = Common.Md5(req.Data);
                    obj.Owner = user.Name;

                    if (!client.Add(obj, req.DataStream))
                    { 
                        _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Write unable to write database entry for " + req.Bucket + "/" + req.Key);
                        return new S3Response(req, S3ServerInterface.ErrorCode.InternalError);
                    }
                     
                    return new S3Response(req, 200, "text/plain", null, null);
                }
                else
                { 
                    // new version 
                    DateTime ts = DateTime.Now.ToUniversalTime(); 
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
                    obj.Md5 = Common.Md5(req.Data);
                    obj.Owner = user.Name;

                    if (!client.Add(obj, req.DataStream))
                    { 
                        _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Write unable to write database entry for " + req.Bucket + "/" + req.Key);
                        return new S3Response(req, S3ServerInterface.ErrorCode.InternalError);
                    }
                     
                    return new S3Response(req, 200, "text/plain", null, null);
                } 
            }
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler Write unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
        }

        /// <summary>
        /// Object write tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response WriteTags(S3Request req)
        {
            long versionId = 1;
            string versionIdStr = req.RetrieveHeaderValue("versionId");
            if (!String.IsNullOrEmpty(versionIdStr)) versionId = Convert.ToInt64(versionIdStr);

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
                    return new S3Response(req, S3ServerInterface.ErrorCode.InvalidRequest);
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
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler WriteTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler WriteTags unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler WriteTags unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            Obj obj = null;
            if (!client.GetMetadata(req.Key, versionId, out obj))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler WriteTags unable to find metadata for " + req.Bucket + "/" + req.Key + " version " + versionId);
                if (versionId == 1) return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchKey);
                else return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchVersion);
            }

            if (bucket.PermittedAccessKeys.Contains(req.AccessKey) ||
                bucket.Owner.Equals(user.Name))
            { 
                if (File.Exists(GetTagsFile(bucket, obj))) File.Delete(GetTagsFile(bucket, obj)); 
                File.WriteAllBytes(GetTagsFile(bucket, obj), Encoding.UTF8.GetBytes(Common.SerializeXml(reqBody)));
                return new S3Response(req, 204, "text/plain", null, null); 
            }
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "ObjectHandler WriteTags unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
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

        private string GetTagsFile(BucketConfiguration bucket, Obj obj)
        {
            return bucket.ObjectsDirectory + obj.BlobFilename + ".Tags.xml";
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
            start = Convert.ToInt64(vals[0]);
            end = Convert.ToInt64(vals[1]); 
        }
         
        #endregion
    }
}
