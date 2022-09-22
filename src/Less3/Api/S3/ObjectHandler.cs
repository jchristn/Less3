using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SyslogLogging;
using S3ServerLibrary;
using S3ServerLibrary.S3Objects;
using WatsonWebserver;

using Less3.Classes;

namespace Less3.Api.S3
{
    /// <summary>
    /// Object APIs.
    /// </summary>
    public class ObjectHandler
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private ConfigManager _Config = null;
        private BucketManager _Buckets = null;
        private AuthManager _Auth = null;

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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal async Task Delete(S3Context ctx)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }
             
            long versionId = 1;
            if (!String.IsNullOrEmpty(ctx.Request.VersionId))
            {
                if (!Int64.TryParse(ctx.Request.VersionId, out versionId))
                {
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    throw new S3Exception(new Error(ErrorCode.NoSuchKey));
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }
             
            if (md.Obj.DeleteMarker)
            {
                ctx.Response.Headers.Add("X-Amz-Delete-Marker", "true");
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }

            md.BucketClient.DeleteObjectVersion(md.Obj.Key, versionId);
            md.BucketClient.DeleteObjectVersionAcl(md.Obj.Key, versionId);
            md.BucketClient.DeleteObjectVersionTags(md.Obj.Key, versionId);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal async Task<DeleteResult> DeleteMultiple(S3Context ctx, DeleteMultiple dm)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }
             
            DeleteResult deleteResult = new DeleteResult();

            if (dm.Objects.Count > 0)
            {
                foreach (S3ServerLibrary.S3Objects.Object curr in dm.Objects)
                {
                    long versionId = 1;
                    if (!String.IsNullOrEmpty(curr.VersionId)) versionId = Convert.ToInt64(curr.VersionId);

                    Obj obj = md.BucketClient.GetObjectVersionMetadata(curr.Key, versionId);
                    if (obj == null)
                    {
                        _Logging.Warn(header + "unable to find metadata for " + ctx.Request.Bucket + "/" + curr.Key);
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
                        deleteResult.Errors.Add(error);
                        continue;
                    }
                     
                    if (!md.BucketClient.DeleteObjectVersion(curr.Key, versionId))
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
                        deleteResult.Errors.Add(error);
                    }
                    else
                    {
                        Deleted deleted = new Deleted();
                        deleted.Key = curr.Key;
                        deleted.VersionId = versionId.ToString();
                        deleteResult.DeletedObjects.Add(deleted);
                    }
                }
            }

            return deleteResult;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal async Task DeleteTags(S3Context ctx)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }
             
            long versionId = 1;
            if (!String.IsNullOrEmpty(ctx.Request.VersionId))
            {
                if (!Int64.TryParse(ctx.Request.VersionId, out versionId))
                {
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    throw new S3Exception(new Error(ErrorCode.NoSuchKey));
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj.DeleteMarker)
            {
                ctx.Response.Headers.Add("X-Amz-Delete-Marker", "true");
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }
             
            md.BucketClient.DeleteObjectVersionTags(ctx.Request.Key, versionId);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal async Task<ObjectMetadata> Exists(S3Context ctx)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(ctx.Request.VersionId))
            {
                if (!Int64.TryParse(ctx.Request.VersionId, out versionId))
                {
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj == null)
            {
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }

            if (md.Obj.DeleteMarker)
            {
                ctx.Response.Headers.Add("X-Amz-Delete-Marker", "true");
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }

            return new ObjectMetadata(md.Obj.Key, md.Obj.LastUpdateUtc, md.Obj.Md5, md.Obj.ContentLength, new Owner(md.Obj.OwnerGUID, null));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal async Task<S3Object> Read(S3Context ctx)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(ctx.Request.VersionId))
            {
                if (!Int64.TryParse(ctx.Request.VersionId, out versionId))
                {
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    throw new S3Exception(new Error(ErrorCode.NoSuchKey));
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj.DeleteMarker)
            {
                ctx.Response.Headers.Add("X-Amz-Delete-Marker", "true");
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }

            bool isLatest = true;
            long latestVersion = md.BucketClient.GetObjectLatestVersion(md.Obj.Key);
            if (md.Obj.Version < latestVersion) isLatest = false;

            FileStream fs = new FileStream(GetObjectBlobFile(md.Bucket, md.Obj), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new S3Object(md.Obj.Key, md.Obj.Version.ToString(), isLatest, md.Obj.LastUpdateUtc, md.Obj.Etag, md.Obj.ContentLength, GetOwnerFromUserGuid(md.Obj.OwnerGUID), fs, md.Obj.ContentType);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal async Task<AccessControlPolicy> ReadAcl(S3Context ctx)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(ctx.Request.VersionId))
            {
                if (!Int64.TryParse(ctx.Request.VersionId, out versionId))
                {
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Debug(header + "no such key");
                    throw new S3Exception(new Error(ErrorCode.NoSuchKey));
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj.DeleteMarker)
            {
                ctx.Response.Headers.Add("X-Amz-Delete-Marker", "true");
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }

            User owner = _Config.GetUserByGuid(md.Obj.OwnerGUID);
            if (owner == null)
            {
                _Logging.Warn(header + "unable to find owner GUID " + md.Obj.OwnerGUID + " for object GUID " + md.Obj.GUID);
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            AccessControlPolicy ret = new AccessControlPolicy();
            ret.Owner = new S3ServerLibrary.S3Objects.Owner();
            ret.Owner.DisplayName = owner.Name;
            ret.Owner.ID = owner.GUID;

            ret.Acl = new AccessControlList();
            ret.Acl.Grants = new List<Grant>();
             
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
                        ret.Acl.Grants.Add(grant);
                    }

                    if (curr.PermitReadAcp)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = tempUser.Name;
                        grant.Grantee.ID = curr.UserGUID;
                        grant.Permission = "READ_ACP";
                        ret.Acl.Grants.Add(grant);
                    }

                    if (curr.PermitWrite)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = tempUser.Name;
                        grant.Grantee.ID = curr.UserGUID;
                        grant.Permission = "WRITE";
                        ret.Acl.Grants.Add(grant);
                    }

                    if (curr.PermitWriteAcp)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = tempUser.Name;
                        grant.Grantee.ID = curr.UserGUID;
                        grant.Permission = "WRITE_ACP";
                        ret.Acl.Grants.Add(grant);
                    }

                    if (curr.FullControl)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = tempUser.Name;
                        grant.Grantee.ID = curr.UserGUID;
                        grant.Permission = "FULL_CONTROL";
                        ret.Acl.Grants.Add(grant);
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
                        ret.Acl.Grants.Add(grant);
                    }

                    if (curr.PermitReadAcp)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = curr.UserGroup;
                        grant.Grantee.URI = curr.UserGroup;
                        grant.Permission = "READ_ACP";
                        ret.Acl.Grants.Add(grant);
                    }

                    if (curr.PermitWrite)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = curr.UserGroup;
                        grant.Grantee.URI = curr.UserGroup;
                        grant.Permission = "WRITE";
                        ret.Acl.Grants.Add(grant);
                    }

                    if (curr.PermitWriteAcp)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = curr.UserGroup;
                        grant.Grantee.URI = curr.UserGroup;
                        grant.Permission = "WRITE_ACP";
                        ret.Acl.Grants.Add(grant);
                    }

                    if (curr.FullControl)
                    {
                        Grant grant = new Grant();
                        grant.Grantee = new Grantee();
                        grant.Grantee.DisplayName = curr.UserGroup;
                        grant.Grantee.URI = curr.UserGroup;
                        grant.Permission = "FULL_CONTROL";
                        ret.Acl.Grants.Add(grant);
                    }

                    #endregion
                }
                else
                {
                    _Logging.Warn(header + "incorrectly configured object ACL in ID " + curr.Id);
                }
            }

            return ret;
        }

        internal async Task<S3Object> ReadRange(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(ctx.Request.VersionId))
            {
                if (!Int64.TryParse(ctx.Request.VersionId, out versionId))
                {
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                { 
                    _Logging.Warn(header + "no such key");
                    throw new S3Exception(new Error(ErrorCode.NoSuchKey));
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj.DeleteMarker)
            {
                ctx.Response.Headers.Add("X-Amz-Delete-Marker", "true");
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }

            if (ctx.Request.RangeStart == null)
            {
                _Logging.Debug(header + "null range start and end, shifting to full object read");
                return await Read(ctx);
            }

            bool isLatest = true;
            long latestVersion = md.BucketClient.GetObjectLatestVersion(md.Obj.Key);
            if (md.Obj.Version < latestVersion) isLatest = false;

            long readLen = 0;
            if (ctx.Request.RangeEnd != null)
            {
                // test that end is equal to or later than start
                // add one to RangeEnd because the number of bytes to read is inclusive of the byte specified in RangeEnd
                if (ctx.Request.RangeStart.Value >= (ctx.Request.RangeEnd.Value + 1))
                {
                    _Logging.Warn(header + "invalid range supplied, start " + ctx.Request.RangeStart.Value + " end " + ctx.Request.RangeEnd.Value);
                    throw new S3Exception(new Error(ErrorCode.InvalidRange));
                }

                // test that end is not beyond content length
                if (ctx.Request.RangeEnd.Value >= md.Obj.ContentLength)
                {
                    _Logging.Warn(header + "invalid range supplied, end " + ctx.Request.RangeEnd.Value + " is beyond content length " + md.Obj.ContentLength);
                    throw new S3Exception(new Error(ErrorCode.InvalidRange));
                }

                readLen = (ctx.Request.RangeEnd.Value + 1) - ctx.Request.RangeStart.Value;
            }
            else
            {
                // null RangeEnd, therefore should be object length - 1 (since it's inclusive of the byte specified in RangeEnd)
                // but first test if RangeStart is out of range
                if ((ctx.Request.RangeStart.Value + 1) >= md.Obj.ContentLength)
                {
                    _Logging.Warn(header + "invalid range supplied, start " + ctx.Request.RangeStart.Value + " is beyond content length " + md.Obj.ContentLength);
                    throw new S3Exception(new Error(ErrorCode.InvalidRange));
                }

                // set RangeEnd also!
                ctx.Request.RangeEnd = (md.Obj.ContentLength - 1);

                readLen = md.Obj.ContentLength - ctx.Request.RangeStart.Value;
            }

            _Logging.Info(
                header + 
                "range read " + 
                ctx.Request.RangeStart.Value + 
                " to " + ctx.Request.RangeEnd.Value + 
                " on " + md.Bucket.Name + "/" + md.Obj.Key + "/" + md.Obj.Version + 
                " len " + md.Obj.ContentLength);

            byte[] data = new byte[readLen];

            using (FileStream fs = new FileStream(GetObjectBlobFile(md.Bucket, md.Obj), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(ctx.Request.RangeStart.Value, SeekOrigin.Begin);

                int bytesRemaining = (int)readLen;
                int position = 0;
                int bufSize = 65536;

                while (bytesRemaining > 0)
                {
                    int read;

                    if (bytesRemaining >= bufSize)
                    {
                        read = fs.Read(data, position, bufSize);
                    }
                    else
                    {
                        read = fs.Read(data, position, bytesRemaining);
                    }

                    if (read > 0)
                    {
                        bytesRemaining -= read;
                        position += read;
                    }
                }

                return new S3Object(md.Obj.Key, md.Obj.Version.ToString(), isLatest, md.Obj.LastUpdateUtc, md.Obj.Etag, readLen, GetOwnerFromUserGuid(md.Obj.OwnerGUID), data, md.Obj.ContentType);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal async Task<Tagging> ReadTags(S3Context ctx)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(ctx.Request.VersionId))
            {
                if (!Int64.TryParse(ctx.Request.VersionId, out versionId))
                {
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    throw new S3Exception(new Error(ErrorCode.NoSuchKey));
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj.DeleteMarker)
            {
                ctx.Response.Headers.Add("X-Amz-Delete-Marker", "true");
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }
             
            Tagging tags = new Tagging();
            tags.Tags = new TagSet();
            tags.Tags.Tags = new List<Tag>();

            if (md.ObjectTags != null && md.ObjectTags.Count > 0)
            {
                foreach (ObjectTag curr in md.ObjectTags)
                {
                    Tag currTag = new Tag();
                    currTag.Key = curr.Key;
                    currTag.Value = curr.Value;
                    tags.Tags.Tags.Add(currTag);
                }
            }

            return tags;
        }

        internal async Task Write(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }

            Obj obj = md.BucketClient.GetObjectLatestMetadata(ctx.Request.Key);
            if (obj != null && !md.Bucket.EnableVersioning)
            {
                _Logging.Warn(header + "versioning disabled, prohibiting write to " + ctx.Request.Bucket + "/" + ctx.Request.Key);
                throw new S3Exception(new Error(ErrorCode.InvalidBucketState));
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
                    obj.AuthorGUID = ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port;
                    obj.OwnerGUID = ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port;
                }
                 
                obj.GUID = Guid.NewGuid().ToString();
                obj.Version = 1;
                obj.BlobFilename = obj.GUID;
                obj.ContentLength = ctx.Http.Request.ContentLength;
                obj.ContentType = ctx.Http.Request.ContentType;
                obj.CreatedUtc = ts;
                obj.DeleteMarker = false;
                obj.ExpirationUtc = null;
                obj.Key = ctx.Request.Key;
                obj.LastAccessUtc = ts;
                obj.LastUpdateUtc = ts;

                if (obj.ContentLength == 0 && obj.Key.EndsWith("/")) obj.IsFolder = true;
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
                    obj.AuthorGUID = ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port;
                    obj.OwnerGUID = ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port;
                }

                obj.GUID = Guid.NewGuid().ToString();
                obj.Version = obj.Version + 1;
                obj.BlobFilename = obj.GUID;
                obj.ContentLength = ctx.Http.Request.ContentLength;
                obj.ContentType = ctx.Http.Request.ContentType;
                obj.CreatedUtc = ts;
                obj.DeleteMarker = false;
                obj.ExpirationUtc = null;
                obj.Key = ctx.Request.Key;
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
                    if (ctx.Request.Chunked)
                    {
                        while (true)
                        {
                            Chunk chunk = await ctx.Request.ReadChunk();
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
                        if (ctx.Request.Data != null && ctx.Http.Request.ContentLength > 0)
                        {
                            long bytesRemaining = ctx.Http.Request.ContentLength;
                            byte[] buffer = new byte[65536];
                            int bytesRead = 0;

                            while (bytesRemaining > 0)
                            {
                                bytesRead = await ctx.Request.Data.ReadAsync(buffer, 0, buffer.Length);
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
                _Logging.Warn(header + "failure while writing " + ctx.Request.Bucket + "/" + ctx.Request.Key + " using tempfile " + tempFilename);
                _Logging.Exception(e, "ObjectHandler", "Write");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }
            finally
            {
                File.Delete(tempFilename);
            }

            if (!writeSuccess)
            {
                _Logging.Warn(header + "failed to write object " + ctx.Request.Bucket + "/" + ctx.Request.Key);
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            #endregion

            #region Permissions-in-Headers

            if (md.User != null)
            {
                List<Grant> grants = GrantsFromHeaders(md.User, ctx.Http.Request.Headers);
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
                                    _Logging.Warn(header + "unable to retrieve user " + curr.Grantee.ID + " to add ACL to object " + ctx.Request.Bucket + "/" + ctx.Request.Key + " version " + obj.Version);
                                    continue;
                                }

                                if (String.IsNullOrEmpty(curr.Permission))
                                {
                                    _Logging.Warn(header + "no permissions specified for user " + curr.Grantee.ID + " in ACL for object " + ctx.Request.Bucket + "/" + ctx.Request.Key);
                                    continue;
                                }

                                if (curr.Permission.Equals("READ")) permitRead = true;
                                else if (curr.Permission.Equals("WRITE")) permitWrite = true;
                                else if (curr.Permission.Equals("READ_ACP")) permitReadAcp = true;
                                else if (curr.Permission.Equals("WRITE_ACP")) permitWriteAcp = true;
                                else if (curr.Permission.Equals("FULL_CONTROL")) fullControl = true;

                                objectAcl = ObjectAcl.UserAcl(
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
                                    _Logging.Warn(header + "no permissions specified for user " + curr.Grantee.ID + " in ACL for object " + ctx.Request.Bucket + "/" + ctx.Request.Key + " version " + obj.Version);
                                    continue;
                                }

                                if (curr.Permission.Equals("READ")) permitRead = true;
                                else if (curr.Permission.Equals("WRITE")) permitWrite = true;
                                else if (curr.Permission.Equals("READ_ACP")) permitReadAcp = true;
                                else if (curr.Permission.Equals("WRITE_ACP")) permitWriteAcp = true;
                                else if (curr.Permission.Equals("FULL_CONTROL")) fullControl = true;

                                objectAcl = ObjectAcl.GroupAcl(
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
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal async Task WriteAcl(S3Context ctx, AccessControlPolicy acp)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }

            if (md.User == null || md.Credential == null)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            long versionId = 1;
            if (!String.IsNullOrEmpty(ctx.Request.VersionId))
            {
                if (!Int64.TryParse(ctx.Request.VersionId, out versionId))
                {
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    throw new S3Exception(new Error(ErrorCode.NoSuchKey));
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj.DeleteMarker)
            {
                ctx.Response.Headers.Add("X-Amz-Delete-Marker", "true");
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }

            md.BucketClient.DeleteObjectVersionAcl(ctx.Request.Key, versionId);

            List<Grant> headerGrants = GrantsFromHeaders(md.User, ctx.Http.Request.Headers);
            if (headerGrants != null && headerGrants.Count > 0)
            {
                if (acp.Acl.Grants != null)
                {
                    foreach (Grant curr in headerGrants)
                    {
                        acp.Acl.Grants.Add(curr);
                    }
                }
                else
                {
                    acp.Acl.Grants = new List<Grant>(headerGrants);
                }
            }

            foreach (Grant curr in acp.Acl.Grants)
            {
                ObjectAcl acl = null; 

                if (!String.IsNullOrEmpty(curr.Grantee.ID))
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
                        acl = ObjectAcl.UserAcl(
                            curr.Grantee.ID, 
                            md.Bucket.OwnerGUID, 
                            md.Bucket.GUID,
                            md.Obj.GUID, 
                            true, false, false, false, false);
                    }
                    else if (curr.Permission.Equals("WRITE"))
                    {
                        acl = ObjectAcl.UserAcl(
                            curr.Grantee.ID,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, true, false, false, false);
                    }
                    else if (curr.Permission.Equals("READ_ACP"))
                    {
                        acl = ObjectAcl.UserAcl(
                            curr.Grantee.ID,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, false, true, false, false);
                    }
                    else if (curr.Permission.Equals("WRITE_ACP"))
                    {
                        acl = ObjectAcl.UserAcl(
                            curr.Grantee.ID,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, false, false, true, false);
                    }
                    else if (curr.Permission.Equals("FULL_CONTROL"))
                    {
                        acl = ObjectAcl.UserAcl(
                            curr.Grantee.ID,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, false, false, false, true);
                    }

                    #endregion
                }
                else if (!String.IsNullOrEmpty(curr.Grantee.URI))
                {
                    #region Group-ACL

                    if (curr.Permission.Equals("READ"))
                    {
                        acl = ObjectAcl.GroupAcl(
                            curr.Grantee.URI,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            true, false, false, false, false);
                    }
                    else if (curr.Permission.Equals("WRITE"))
                    {
                        acl = ObjectAcl.GroupAcl(
                            curr.Grantee.URI,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, true, false, false, false);
                    }
                    else if (curr.Permission.Equals("READ_ACP"))
                    {
                        acl = ObjectAcl.GroupAcl(
                            curr.Grantee.URI,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, false, true, false, false);
                    }
                    else if (curr.Permission.Equals("WRITE_ACP"))
                    {
                        acl = ObjectAcl.GroupAcl(
                            curr.Grantee.URI,
                            md.Bucket.OwnerGUID,
                            md.Bucket.GUID,
                            md.Obj.GUID,
                            false, false, false, true, false);
                    }
                    else if (curr.Permission.Equals("FULL_CONTROL"))
                    {
                        acl = ObjectAcl.GroupAcl(
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
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal async Task WriteTagging(S3Context ctx, Tagging tagging)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                throw new S3Exception(new Error(ErrorCode.NoSuchBucket));
            }
             
            long versionId = 1;
            if (!String.IsNullOrEmpty(ctx.Request.VersionId))
            {
                if (!Int64.TryParse(ctx.Request.VersionId, out versionId))
                {
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj == null)
            {
                if (versionId == 1)
                {
                    _Logging.Warn(header + "no such key");
                    throw new S3Exception(new Error(ErrorCode.NoSuchKey));
                }
                else
                {
                    _Logging.Warn(header + "no such version");
                    throw new S3Exception(new Error(ErrorCode.NoSuchVersion));
                }
            }

            if (md.Obj.DeleteMarker)
            {
                ctx.Response.Headers.Add("X-Amz-Delete-Marker", "true");
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }

            md.BucketClient.DeleteObjectVersionTags(ctx.Request.Key, versionId);

            List<ObjectTag> tags = new List<ObjectTag>();
            if (tagging.Tags != null && tagging.Tags.Tags != null && tagging.Tags.Tags.Count > 0)
            {
                foreach (Tag curr in tagging.Tags.Tags)
                {
                    ObjectTag ot = new ObjectTag();
                    ot.BucketGUID = md.Bucket.GUID;
                    ot.ObjectGUID = md.Obj.GUID;
                    ot.Key = curr.Key;
                    ot.Value = curr.Value;
                    tags.Add(ot);
                }
            }

            md.BucketClient.AddObjectVersionTags(ctx.Request.Key, versionId, tags);
        }

        #endregion

        #region Private-Methods

        private Owner GetOwnerFromUserGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) return null;
            User user = _Config.GetUserByGuid(guid);
            if (user != null)
            {
                Owner owner = new Owner(guid, user.Name);
                return owner;
            }
            return null;
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
