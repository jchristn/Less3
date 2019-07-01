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

using Less3.Classes;
using Less3.S3Responses;

namespace Less3.Api
{
    public class BucketHandler
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

        public BucketHandler(
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

        public S3Response Delete(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Delete unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Delete unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Delete unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            if (bucket.PermittedAccessKeys.Contains(req.AccessKey) ||
                bucket.Owner.Equals(user.Name))
            {
                long count = 0;
                long bytes = 0;

                client.GetCounts(out count, out bytes);

                if (count > 0 || bytes > 0)
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Delete bucket " + bucket.Name + " is not empty");
                    return new S3Response(req, S3ServerInterface.ErrorCode.BucketNotEmpty);
                }

                _Logging.Log(LoggingModule.Severity.Info, "BucketHandler Delete deleting bucket " + req.Bucket);
                _Buckets.Remove(bucket, true);
                return new S3Response(req, 204, "application/xml", null, null);
            }
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Delete unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
        }

        public S3Response DeleteTags(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler DeleteTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler DeleteTags unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            if (bucket.PermittedAccessKeys.Contains(req.AccessKey) ||
                bucket.Owner.Equals(user.Name))
            {
                if (File.Exists(GetTagsFile(bucket.Name)))
                    File.Delete(GetTagsFile(bucket.Name));

                return new S3Response(req, 204, "application/xml", null, null);
            }
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler DeleteTags unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
        }

        public S3Response Exists(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Exists unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            if (bucket.EnablePublicRead) return new S3Response(req, 200, "text/plain", null, null);
            if (bucket.PermittedAccessKeys.Contains(req.AccessKey)) return new S3Response(req, 200, "text/plain", null, null);

            User user = null;
            Credential cred = null;
            if (_Auth.Authenticate(req, out user, out cred))
            {
                if (_Auth.Authorize(RequestType.BucketExists, req, user, cred))
                {
                    if (bucket.Owner.Equals(user.Name)) return new S3Response(req, 200, "text/plain", null, null);
                }
            }

            _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Exists unauthorized attempt to access bucket " + req.Bucket);
            return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
        }

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
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Read unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Read unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Read unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            if (bucket.PermittedAccessKeys.Contains(req.AccessKey) ||
                bucket.Owner.Equals(user.Name))
            {
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
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Read unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
        }

        public S3Response ReadTags(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler ReadTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler ReadTags unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            if (bucket.PermittedAccessKeys.Contains(req.AccessKey) ||
                bucket.Owner.Equals(user.Name))
            { 
                if (File.Exists(GetTagsFile(bucket.Name)))
                {
                    byte[] fileData = Common.ReadBinaryFile(GetTagsFile(bucket.Name));
                    return new S3Response(req, 200, "application/xml", null, fileData);
                }
                else
                {
                    Tagging tags = new Tagging();
                    tags.TagSet = new List<Tag>();
                    File.WriteAllBytes(GetTagsFile(bucket.Name), Encoding.UTF8.GetBytes(Common.SerializeXml(tags)));
                    return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(tags))); 
                } 
            }
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler ReadTags unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
        }

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
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler ReadVersions unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            BucketClient client = null;
            if (!_Buckets.GetClient(req.Bucket, out client))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler ReadVersions unable to retrieve client for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler ReadVersions unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            if (bucket.PermittedAccessKeys.Contains(req.AccessKey) ||
                bucket.Owner.Equals(user.Name))
            {
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
                        d.Owner = new S3Responses.Owner();
                        d.Owner.DisplayName = curr.Owner;
                        d.Owner.ID = curr.Owner;
                        d.VersionId = curr.Version;
                        resp.DeleteMarker.Add(d);
                    }
                    else
                    {
                        S3Responses.Version v = new S3Responses.Version();
                        v.ETag = null;
                        v.IsLatest = IsLatest(objs, curr.Key, curr.LastAccessUtc);
                        v.Key = curr.Key;
                        v.LastModified = curr.LastUpdateUtc;
                        v.Owner = new S3Responses.Owner();
                        v.Owner.DisplayName = curr.Owner;
                        v.Owner.ID = curr.Owner;
                        v.Size = curr.ContentLength;
                        v.StorageClass = "STANDARD";
                        resp.Version.Add(v);
                    }
                }

                return new S3Response(req, 200, "application/xml", null, Encoding.UTF8.GetBytes(Common.SerializeXml(resp)));
            }
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler ReadVersions unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
        }

        public S3Response ReadVersioning(S3Request req)
        {
            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler ReadVersioning unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler ReadVersioning unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            if (bucket.PermittedAccessKeys.Contains(req.AccessKey) ||
                bucket.Owner.Equals(user.Name))
            {
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
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler ReadVersioning unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
        }

        public S3Response Write(S3Request req)
        {
            S3Bucket reqBody = null;
            if (req.Data != null)
            {
                try
                {
                    reqBody = Common.DeserializeXml<S3Bucket>(Encoding.UTF8.GetString(req.Data));
                }
                catch (Exception e)
                {
                    _Logging.LogException("BucketHandler", "Write", e);
                    return new S3Response(req, S3ServerInterface.ErrorCode.InvalidRequest); 
                }
            }

            BucketConfiguration test = null;
            if (_Buckets.Get(req.Bucket, out test))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Write unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Write unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            BucketConfiguration config = new BucketConfiguration(
                req.Bucket,
                user.Name,
                _Settings.Storage.Directory + req.Bucket + "/" + req.Bucket + ".db",
                _Settings.Storage.Directory + req.Bucket + "/Objects/",
                new List<string>());

            if (!_Buckets.Add(config))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler Write unable to write bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.InternalError);
            }

            return new S3Response(req, 200, "text/plain", null, null);
        }
         
        public S3Response WriteTags(S3Request req)
        {
            Tagging reqBody = null;
            if (req.Data != null)
            {
                try
                {
                    reqBody = Common.DeserializeXml<Tagging>(Encoding.UTF8.GetString(req.Data));
                }
                catch (Exception e)
                {
                    _Logging.LogException("BucketHandler", "WriteTags", e);
                    return new S3Response(req, S3ServerInterface.ErrorCode.InvalidRequest);
                }
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler WriteTags unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler WriteTags unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }

            if (bucket.PermittedAccessKeys.Contains(req.AccessKey) ||
                bucket.Owner.Equals(user.Name))
            {
                if (File.Exists(GetTagsFile(bucket.Name))) File.Delete(GetTagsFile(bucket.Name));
                File.WriteAllBytes(GetTagsFile(bucket.Name), Encoding.UTF8.GetBytes(Common.SerializeXml(reqBody)));
                return new S3Response(req, 204, "text/plain", null, null); 
            }
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler WriteTags unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
        }

        public S3Response WriteVersioning(S3Request req)
        {
            VersioningConfiguration reqBody = null;
            if (req.Data != null)
            {
                try
                {
                    reqBody = Common.DeserializeXml<VersioningConfiguration>(Encoding.UTF8.GetString(req.Data));
                }
                catch (Exception e)
                {
                    _Logging.LogException("BucketHandler", "WriteVersioning", e);
                    return new S3Response(req, S3ServerInterface.ErrorCode.InvalidRequest);
                }
            }

            BucketConfiguration bucket = null;
            if (!_Buckets.Get(req.Bucket, out bucket))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler WriteVersioning unable to retrieve bucket configuration for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.NoSuchBucket);
            }

            User user = null;
            Credential cred = null;
            if (!_Auth.Authenticate(req, out user, out cred))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler WriteVersioning unable to authenticate request for bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
             
            if (bucket.PermittedAccessKeys.Contains(req.AccessKey) ||
                bucket.Owner.Equals(user.Name))
            {
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
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "BucketHandler WriteVersioning unauthorized access attempt to bucket " + req.Bucket);
                return new S3Response(req, S3ServerInterface.ErrorCode.AccessDenied); 
            }
        }

        #endregion

        #region Private-Methods

        private string AmazonTimestamp(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffz");
        }
         
        private string GetTagsFile(string name)
        {
            return _Settings.Storage.Directory + name + "/Tags.xml";
        }

        private string BuildContinuationToken(long lastId)
        {
            return Common.StringToBase64(lastId.ToString());
        }

        private long ParseContinuationToken(string base64)
        {
            return Convert.ToInt64(Common.Base64ToString(base64));
        }

        public bool IsLatest(List<Obj> objs, string key, DateTime lastAccessUtc)
        {
            bool laterObjExists = objs.Exists(o =>
                o.Key.Equals(key)
                && o.LastAccessUtc > lastAccessUtc);

            return !laterObjExists;
        }

        #endregion
    }
}
