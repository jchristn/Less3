using System;
using System.Collections.Generic;
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
using WatsonWebserver;

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

        internal ObjectHandler(
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
            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(req);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }
             
            long versionId = 1;
            if (!String.IsNullOrEmpty(req.VersionId))
            {
                if (!Int64.TryParse(req.VersionId, out versionId))
                {
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    await resp.Send(ErrorCode.NoSuchKey);
                    return;
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }
             
            if (md.Obj.DeleteMarker)
            {
                resp.Headers.Add("X-Amz-Delete-Marker", "true");
                await resp.Send(ErrorCode.NoSuchKey);
                return;
            }

            md.BucketClient.DeleteObject(md.Obj.Key, versionId);

            resp.StatusCode = 204;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
        }

        internal async Task DeleteMultiple(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(req);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }
             
            byte[] data = null;
            DeleteMultiple reqBody = null;

            if (req.Data != null)
            {
                try
                {
                    data = Common.StreamToBytes(req.Data);
                    reqBody = Common.DeserializeXml<DeleteMultiple>(Encoding.UTF8.GetString(data));
                }
                catch (Exception e)
                {
                    _Logging.Exception("ObjectHandler", "DeleteMultiple", e);
                    await resp.Send(ErrorCode.InvalidRequest);
                    return;
                }
            }
             
            DeleteResult deleteResult = new DeleteResult();

            if (reqBody.Object.Count > 0)
            {
                foreach (S3ServerInterface.S3Objects.Object curr in reqBody.Object)
                {
                    long versionId = 1;
                    if (!String.IsNullOrEmpty(curr.VersionId)) versionId = Convert.ToInt64(curr.VersionId);

                    Obj obj = md.BucketClient.GetObjectMetadata(curr.Key, versionId);
                    if (obj == null)
                    {
                        _Logging.Warn(header + "unable to find metadata for " + req.Bucket + "/" + curr.Key);
                        Error error = null;

                        if (versionId > 1)
                        {
                            error = new Error(ErrorCode.NoSuchVersion);
                        }
                        else
                        {
                            error = new Error(ErrorCode.NoSuchKey);
                        }

                        error.Key = curr.Key;
                        error.VersionId = versionId.ToString();
                        deleteResult.Error.Add(error);
                        continue;
                    }
                     
                    if (!md.BucketClient.DeleteObject(curr.Key, versionId))
                    {
                        Error error = null;
                        if (versionId > 1)
                        {
                            error = new Error(ErrorCode.NoSuchVersion);
                        }
                        else
                        {
                            error = new Error(ErrorCode.NoSuchKey);
                        }

                        error.Key = curr.Key;
                        error.VersionId = versionId.ToString();
                        deleteResult.Error.Add(error);
                    }
                    else
                    {
                        Deleted deleted = new Deleted();
                        deleted.Key = curr.Key;
                        deleted.VersionId = versionId.ToString();
                        deleteResult.Deleted.Add(deleted);
                    }
                }
            }

            resp.StatusCode = 200;
            resp.ContentType = "application/xml";
            await resp.Send(Common.SerializeXml<DeleteResult>(deleteResult, false));
            return;
        }

        internal async Task DeleteTags(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(req);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }
             
            long versionId = 1;
            if (!String.IsNullOrEmpty(req.VersionId))
            {
                if (!Int64.TryParse(req.VersionId, out versionId))
                {
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    await resp.Send(ErrorCode.NoSuchKey);
                    return;
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj.DeleteMarker)
            {
                resp.Headers.Add("X-Amz-Delete-Marker", "true");
                await resp.Send(ErrorCode.NoSuchKey);
                return;
            }
             
            md.BucketClient.DeleteObjectTags(req.Key, versionId);

            resp.StatusCode = 204;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
        }

        internal async Task Exists(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(req);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(req.VersionId))
            {
                if (!Int64.TryParse(req.VersionId, out versionId))
                {
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    await resp.Send(ErrorCode.NoSuchKey);
                    return;
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj.DeleteMarker)
            {
                resp.Headers.Add("X-Amz-Delete-Marker", "true");
                await resp.Send(ErrorCode.NoSuchKey);
                return;
            }

            resp.StatusCode = 200;
            resp.ContentType = "text/plain";
            resp.ContentLength = md.Obj.ContentLength;
            await resp.Send();
            return;
        }

        internal async Task Read(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(req);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(req.VersionId))
            {
                if (!Int64.TryParse(req.VersionId, out versionId))
                {
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    await resp.Send(ErrorCode.NoSuchKey);
                    return;
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj.DeleteMarker)
            {
                resp.Headers.Add("X-Amz-Delete-Marker", "true");
                await resp.Send(ErrorCode.NoSuchKey);
                return;
            }

            resp.StatusCode = 200;
            resp.ContentType = md.Obj.ContentType;

            if (req.Chunked)
            {
                resp.Chunked = true;
            }

            using (FileStream fs = new FileStream(GetObjectBlobFile(md.Bucket, md.Obj), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (resp.Chunked)
                {
                    long bytesRemaining = md.Obj.ContentLength; 
                    byte[] buffer = new byte[65536];
                    int bytesRead = 0;

                    while (bytesRemaining > 0)
                    {
                        bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            bytesRemaining -= bytesRead;
                            
                            if (buffer.Length != bytesRead)
                            {
                                byte[] tempBuffer = new byte[bytesRead];
                                Buffer.BlockCopy(buffer, 0, tempBuffer, 0, bytesRead);
                                buffer = new byte[bytesRead];
                                Buffer.BlockCopy(tempBuffer, 0, buffer, 0, bytesRead);
                            }

                            if (bytesRemaining > 0)
                            { 
                                await resp.SendChunk(buffer);
                            }
                            else
                            { 
                                await resp.SendFinalChunk(buffer);
                            }
                        }
                    }
                     
                    return;
                }
                else
                {
                    resp.StatusCode = 200;
                    resp.ContentType = md.Obj.ContentType;
                    resp.ContentLength = md.Obj.ContentLength;
                    await resp.Send(resp.ContentLength, fs);
                    return;
                } 
            }
        }

        internal async Task ReadAcl(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(req);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(req.VersionId))
            {
                if (!Int64.TryParse(req.VersionId, out versionId))
                {
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    await resp.Send(ErrorCode.NoSuchKey);
                    return;
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj.DeleteMarker)
            {
                resp.Headers.Add("X-Amz-Delete-Marker", "true");
                await resp.Send(ErrorCode.NoSuchKey);
                return;
            }

            User owner = _Config.GetUserByGuid(md.Obj.OwnerGUID);
            if (owner == null)
            {
                _Logging.Warn(header + "unable to find owner GUID " + md.Obj.OwnerGUID + " for object GUID " + md.Obj.GUID);
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            AccessControlPolicy ret = new AccessControlPolicy();
            ret.Owner = new S3ServerInterface.S3Objects.Owner();
            ret.Owner.DisplayName = owner.Name;
            ret.Owner.ID = owner.GUID;

            ret.AccessControlList = new AccessControlList();
            ret.AccessControlList.Grant = new List<Grant>();
             
            foreach (ObjectAcl curr in md.ObjectAcls)
            {
                if (!String.IsNullOrEmpty(curr.UserGUID))
                {
                    #region Individual-Permissions

                    User tempUser = _Config.GetUserByGuid(curr.UserGUID);
                    if (tempUser == null)
                    {
                        _Logging.Warn(header + "unlinked ACL ID " + curr.Id + ", could not find user GUID " + curr.UserGUID);
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
                    _Logging.Warn(header + "incorrectly configured object ACL in ID " + curr.Id);
                }
            }
             
            await ApiHelper.SendSerializedResponse<AccessControlPolicy>(req, resp, ret);
            return; 
        }

        internal async Task ReadRange(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(req);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(req.VersionId))
            {
                if (!Int64.TryParse(req.VersionId, out versionId))
                {
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    await resp.Send(ErrorCode.NoSuchKey);
                    return;
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj.DeleteMarker)
            {
                resp.Headers.Add("X-Amz-Delete-Marker", "true");
                await resp.Send(ErrorCode.NoSuchKey);
                return;
            }

            if (req.RangeStart == null || req.RangeEnd == null)
            {
                await Read(req, resp);
                return;
            }

            long endPosition = (long)req.RangeEnd;
            long startPosition = (long)req.RangeStart;
            long readLen = endPosition - startPosition;

            if (endPosition > 0)
            {
                if (readLen < 1)
                {
                    _Logging.Warn(header + "invalid range supplied, start " + startPosition + " end " + endPosition);
                    await resp.Send(ErrorCode.InvalidRange);
                    return;
                }
            }
            else
            {
                endPosition = md.Obj.ContentLength;
            }

            if (endPosition > md.Obj.ContentLength)
            {
                _Logging.Warn(header + "out of range " + req.Bucket + "/" + req.Key + " version " + versionId);
                await resp.Send(ErrorCode.InvalidRange);
                return;
            }

            resp.StatusCode = 200;
            resp.ContentType = md.Obj.ContentType;
            if (req.Chunked) resp.Chunked = true;

            using (FileStream fs = new FileStream(GetObjectBlobFile(md.Bucket, md.Obj), FileMode.Open))
            {
                fs.Seek(startPosition, SeekOrigin.Begin);
                
                long bytesRemaining = readLen;
                byte[] buffer = new byte[65536];
                int bytesRead = 0;

                if (resp.Chunked)
                {
                    while (bytesRemaining > 0)
                    {
                        if (bytesRemaining < buffer.Length) buffer = new byte[bytesRemaining];
                        bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            bytesRemaining -= bytesRead;

                            if (bytesRead == buffer.Length)
                            {
                                await resp.SendChunk(buffer);
                            }
                            else
                            {
                                byte[] tempBuffer = new byte[bytesRead];
                                Buffer.BlockCopy(buffer, 0, tempBuffer, 0, bytesRead);
                                await resp.SendChunk(tempBuffer);
                            }
                        }
                    }

                    await resp.SendFinalChunk(null);
                    return;
                }
                else
                {
                    byte[] respData = new byte[readLen];
                    await fs.ReadAsync (respData, 0, respData.Length);
                    resp.ContentLength = respData.Length;
                    await resp.Send(respData);
                    return;
                }
            }
        }

        internal async Task ReadTags(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(req);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(req.VersionId))
            {
                if (!Int64.TryParse(req.VersionId, out versionId))
                {
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    await resp.Send(ErrorCode.NoSuchKey);
                    return;
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj.DeleteMarker)
            {
                resp.Headers.Add("X-Amz-Delete-Marker", "true");
                await resp.Send(ErrorCode.NoSuchKey);
                return;
            }
             
            Tagging tags = new Tagging();
            tags.TagSet = new List<Tag>();

            if (md.ObjectTags != null && md.ObjectTags.Count > 0)
            {
                foreach (ObjectTag curr in md.ObjectTags)
                {
                    Tag currTag = new Tag();
                    currTag.Key = curr.Key;
                    currTag.Value = curr.Value;
                    tags.TagSet.Add(currTag);
                }
            }

            await ApiHelper.SendSerializedResponse<Tagging>(req, resp, tags);
            return; 
        }

        internal async Task Write(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(req);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            Obj obj = md.BucketClient.GetObjectMetadata(req.Key);
            if (obj != null)
            {
                if (!md.Bucket.EnableVersioning)
                {
                    _Logging.Warn(header + "metadata already exists for " + req.Bucket + "/" + req.Key);
                    await resp.Send(ErrorCode.InvalidBucketState);
                    return;
                }
            }

            #region Populate-Metadata

            DateTime ts = DateTime.Now.ToUniversalTime();

            if (obj == null)
            {
                // new object 
                obj = new Obj();

                if (md.User != null)
                {
                    obj.AuthorGUID = md.User.GUID;
                    obj.OwnerGUID = md.User.GUID;
                }
                else
                {
                    obj.AuthorGUID = req.SourceIp + ":" + req.SourcePort;
                    obj.OwnerGUID = req.SourceIp + ":" + req.SourcePort;
                }
                 
                obj.GUID = Guid.NewGuid().ToString();
                obj.Version = 1;
                obj.BlobFilename = obj.GUID;
                obj.ContentLength = req.ContentLength;
                obj.ContentType = req.ContentType;
                obj.CreatedUtc = ts;
                obj.DeleteMarker = false;
                obj.ExpirationUtc = null;
                obj.Key = req.Key;
                obj.LastAccessUtc = ts;
                obj.LastUpdateUtc = ts; 
            }
            else
            {
                // new version  
                if (md.User != null)
                {
                    obj.AuthorGUID = md.User.GUID;
                    obj.OwnerGUID = md.User.GUID;
                }
                else
                {
                    obj.AuthorGUID = req.SourceIp + ":" + req.SourcePort;
                    obj.OwnerGUID = req.SourceIp + ":" + req.SourcePort;
                }

                obj.GUID = Guid.NewGuid().ToString();
                obj.Version = obj.Version + 1;
                obj.BlobFilename = obj.GUID;
                obj.ContentLength = req.ContentLength;
                obj.ContentType = req.ContentType;
                obj.CreatedUtc = ts;
                obj.DeleteMarker = false;
                obj.ExpirationUtc = null;
                obj.Key = req.Key;
                obj.LastAccessUtc = ts;
                obj.LastUpdateUtc = ts; 
            }

            #endregion 

            #region Write-Data-to-Temp-and-to-Bucket
             
            string tempFilename = _Settings.Storage.TempDirectory + Guid.NewGuid().ToString();
            long totalLength = 0;
            bool writeSuccess = false;

            try
            {
                using (FileStream fs = new FileStream(tempFilename, FileMode.Create))
                {
                    if (req.Chunked)
                    {
                        while (true)
                        {
                            Chunk chunk = await req.ReadChunk();
                            if (chunk == null) break;

                            if (chunk.Data != null && chunk.Data.Length > 0)
                            {
                                await fs.WriteAsync(chunk.Data, 0, chunk.Data.Length);
                                totalLength += chunk.Data.Length;
                            }

                            if (chunk.IsFinalChunk) break;
                        }
                    }
                    else
                    {
                        if (req.Data != null && req.ContentLength > 0)
                        {
                            long bytesRemaining = req.ContentLength;
                            byte[] buffer = new byte[65536];
                            int bytesRead = 0;

                            while (bytesRemaining > 0)
                            {
                                bytesRead = await req.Data.ReadAsync(buffer, 0, buffer.Length);
                                if (bytesRead > 0)
                                {
                                    bytesRemaining -= bytesRead;
                                    if (bytesRead == buffer.Length)
                                    {
                                        await fs.WriteAsync(buffer, 0, buffer.Length);
                                    }
                                    else
                                    {
                                        byte[] tempBuffer = new byte[bytesRead];
                                        Buffer.BlockCopy(buffer, 0, tempBuffer, 0, bytesRead);
                                        await fs.WriteAsync(tempBuffer, 0, tempBuffer.Length);
                                    }
                                }
                            }

                            totalLength = obj.ContentLength;
                        }
                    }
                }

                using (FileStream fs = new FileStream(tempFilename, FileMode.Open, FileAccess.Read))
                {
                    obj.ContentLength = totalLength;
                    writeSuccess = md.BucketClient.AddObject(obj, fs);
                } 
            }
            catch (Exception e)
            {
                _Logging.Warn(header + "failure while writing " + req.Bucket + "/" + req.Key + " using tempfile " + tempFilename);
                _Logging.Exception("ObjectHandler", "Write", e);
                await resp.Send(ErrorCode.InternalError);
                return;
            }
            finally
            {
                File.Delete(tempFilename);
            }

            if (!writeSuccess)
            {
                _Logging.Warn(header + "failed to write object " + req.Bucket + "/" + req.Key);
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            #endregion

            #region Permissions-in-Headers

            if (md.User != null)
            {
                List<Grant> grants = GrantsFromHeaders(md.User, req.Headers);
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
                                tempUser = _Config.GetUserByGuid(curr.Grantee.ID);
                                if (tempUser == null)
                                {
                                    _Logging.Warn(header + "unable to retrieve user " + curr.Grantee.ID + " to add ACL to object " + req.Bucket + "/" + req.Key + " version " + obj.Version);
                                    continue;
                                }

                                if (String.IsNullOrEmpty(curr.Permission))
                                {
                                    _Logging.Warn(header + "no permissions specified for user " + curr.Grantee.ID + " in ACL for object " + req.Bucket + "/" + req.Key);
                                    continue;
                                }

                                if (curr.Permission.Equals("READ")) permitRead = true;
                                else if (curr.Permission.Equals("WRITE")) permitWrite = true;
                                else if (curr.Permission.Equals("READ_ACP")) permitReadAcp = true;
                                else if (curr.Permission.Equals("WRITE_ACP")) permitWriteAcp = true;
                                else if (curr.Permission.Equals("FULL_CONTROL")) fullControl = true;

                                objectAcl = ObjectAcl.ObjectUserAcl(
                                    curr.Grantee.ID,
                                    md.User.GUID,
                                    md.Bucket.GUID,
                                    obj.GUID,
                                    permitRead,
                                    permitWrite,
                                    permitReadAcp,
                                    permitWriteAcp,
                                    fullControl);

                                md.BucketClient.AddObjectAcl(objectAcl);
                            }
                            else if (!String.IsNullOrEmpty(curr.Grantee.URI))
                            {
                                if (String.IsNullOrEmpty(curr.Permission))
                                {
                                    _Logging.Warn(header + "no permissions specified for user " + curr.Grantee.ID + " in ACL for object " + req.Bucket + "/" + req.Key + " version " + obj.Version);
                                    continue;
                                }

                                if (curr.Permission.Equals("READ")) permitRead = true;
                                else if (curr.Permission.Equals("WRITE")) permitWrite = true;
                                else if (curr.Permission.Equals("READ_ACP")) permitReadAcp = true;
                                else if (curr.Permission.Equals("WRITE_ACP")) permitWriteAcp = true;
                                else if (curr.Permission.Equals("FULL_CONTROL")) fullControl = true;

                                objectAcl = ObjectAcl.ObjectGroupAcl(
                                    curr.Grantee.URI,
                                    md.User.GUID,
                                    md.Bucket.GUID,
                                    obj.GUID,
                                    permitRead,
                                    permitWrite,
                                    permitReadAcp,
                                    permitWriteAcp,
                                    fullControl);

                                md.BucketClient.AddObjectAcl(objectAcl);
                            }
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
            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(req);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }

            if (md.User == null || md.Credential == null)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(req.VersionId))
            {
                if (!Int64.TryParse(req.VersionId, out versionId))
                {
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    await resp.Send(ErrorCode.NoSuchKey);
                    return;
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj.DeleteMarker)
            {
                resp.Headers.Add("X-Amz-Delete-Marker", "true");
                await resp.Send(ErrorCode.NoSuchKey);
                return;
            }

            byte[] data = null;
            AccessControlPolicy reqBody = null;

            if (req.Data != null)
            {
                try
                {
                    data = Common.StreamToBytes(req.Data);
                    string xmlString = Encoding.UTF8.GetString(data);
                    reqBody = Common.DeserializeXml<AccessControlPolicy>(xmlString);
                }
                catch (Exception e)
                {
                    _Logging.Exception("ObjectHandler", "WriteAcl", e);
                    await resp.Send(ErrorCode.InvalidRequest);
                    return;
                }
            }
             
            md.BucketClient.DeleteObjectAcl(req.Key, versionId);

            List<Grant> headerGrants = GrantsFromHeaders(md.User, req.Headers);
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

                if (curr.Grantee is CanonicalUser)
                {
                    #region User-ACL

                    User tempUser = _Config.GetUserByGuid(curr.Grantee.ID);
                    if (tempUser == null)
                    {
                        _Logging.Warn(header + "unable to find user GUID " + curr.Grantee.ID);
                        continue;
                    }

                    if (curr.Permission.Equals("READ"))
                    {
                        acl = ObjectAcl.ObjectUserAcl(
                            curr.Grantee.ID, 
                            md.Bucket.OwnerGUID, 
                            md.Bucket.GUID,
                            md.Obj.GUID, 
                            true, false, false, false, false);
                    }
                    else if (curr.Permission.Equals("WRITE"))
                    {
                        acl = ObjectAcl.ObjectUserAcl(
                            curr.Grantee.ID,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, true, false, false, false);
                    }
                    else if (curr.Permission.Equals("READ_ACP"))
                    {
                        acl = ObjectAcl.ObjectUserAcl(
                            curr.Grantee.ID,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, false, true, false, false);
                    }
                    else if (curr.Permission.Equals("WRITE_ACP"))
                    {
                        acl = ObjectAcl.ObjectUserAcl(
                            curr.Grantee.ID,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, false, false, true, false);
                    }
                    else if (curr.Permission.Equals("FULL_CONTROL"))
                    {
                        acl = ObjectAcl.ObjectUserAcl(
                            curr.Grantee.ID,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, false, false, false, true);
                    }

                    #endregion
                }
                else if (curr.Grantee is Group)
                {
                    #region Group-ACL

                    if (curr.Permission.Equals("READ"))
                    {
                        acl = ObjectAcl.ObjectGroupAcl(
                            curr.Grantee.URI,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            true, false, false, false, false);
                    }
                    else if (curr.Permission.Equals("WRITE"))
                    {
                        acl = ObjectAcl.ObjectGroupAcl(
                            curr.Grantee.URI,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, true, false, false, false);
                    }
                    else if (curr.Permission.Equals("READ_ACP"))
                    {
                        acl = ObjectAcl.ObjectGroupAcl(
                            curr.Grantee.URI,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, false, true, false, false);
                    }
                    else if (curr.Permission.Equals("WRITE_ACP"))
                    {
                        acl = ObjectAcl.ObjectGroupAcl(
                            curr.Grantee.URI,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, false, false, true, false);
                    }
                    else if (curr.Permission.Equals("FULL_CONTROL"))
                    {
                        acl = ObjectAcl.ObjectGroupAcl(
                            curr.Grantee.URI,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, false, false, false, true);
                    }

                    #endregion
                }

                if (acl != null)
                { 
                    md.BucketClient.AddObjectAcl(acl);
                }
            }

            resp.StatusCode = 200;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
        }

        internal async Task WriteTags(S3Request req, S3Response resp)
        {
            string header = "[" + req.SourceIp + ":" + req.SourcePort + " " + req.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(req);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await resp.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await resp.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await resp.Send(ErrorCode.NoSuchBucket);
                return;
            }
             
            long versionId = 1;
            if (!String.IsNullOrEmpty(req.VersionId))
            {
                if (!Int64.TryParse(req.VersionId, out versionId))
                {
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    await resp.Send(ErrorCode.NoSuchKey);
                    return;
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    await resp.Send(ErrorCode.NoSuchVersion);
                    return;
                }
            }

            if (md.Obj.DeleteMarker)
            {
                resp.Headers.Add("X-Amz-Delete-Marker", "true");
                await resp.Send(ErrorCode.NoSuchKey);
                return;
            }

            byte[] data = null;
            Tagging reqBody = null;

            if (req.Data != null)
            {
                try
                {
                    data = Common.StreamToBytes(req.Data);
                    reqBody = Common.DeserializeXml<Tagging>(Encoding.UTF8.GetString(data));
                }
                catch (Exception e)
                {
                    _Logging.Exception("ObjectHandler", "WriteTags", e);
                    await resp.Send(ErrorCode.InvalidRequest);
                    return;
                }
            }
            else
            {
                reqBody = new Tagging();
                reqBody.TagSet = new List<Tag>();
            }
             
            md.BucketClient.DeleteObjectTags(req.Key, versionId);

            List<ObjectTag> tags = new List<ObjectTag>();
            if (reqBody.TagSet != null && reqBody.TagSet.Count > 0)
            {
                foreach (Tag curr in reqBody.TagSet)
                {
                    ObjectTag ot = new ObjectTag();
                    ot.BucketGUID = md.Bucket.GUID;
                    ot.ObjectGUID = md.Obj.GUID;
                    ot.Key = curr.Key;
                    ot.Value = curr.Value;
                    tags.Add(ot);
                }
            }

            md.BucketClient.AddObjectTags(req.Key, versionId, tags);

            resp.StatusCode = 204;
            resp.ContentType = "text/plain";
            await resp.Send();
            return;
        }

        #endregion

        #region Private-Methods

        private string aAmazonTimestamp(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffz");
        }

        private string GetObjectBlobFile(Classes.Bucket bucket, Obj obj)
        {
            return bucket.DiskDirectory + obj.BlobFilename;
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
                User user = _Config.GetUserByEmail(grantee);
                if (user == null)
                {
                    return false;
                }
                else
                {
                    grant.Grantee.ID = user.GUID;
                    grant.Grantee.DisplayName = user.Name;
                    return true;
                }
            }
            else if (granteeType.Equals("id"))
            {
                User user = _Config.GetUserByGuid(grantee);
                if (user == null)
                {
                    return false;
                }
                else
                {
                    grant.Grantee.ID = user.GUID;
                    grant.Grantee.DisplayName = user.Name;
                    return true;
                }
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
