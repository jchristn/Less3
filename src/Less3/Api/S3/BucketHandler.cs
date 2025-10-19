namespace Less3.Api.S3
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using SyslogLogging;
    using S3ServerLibrary;
    using S3ServerLibrary.S3Objects;

    using Less3.Classes;
    using Less3.Helpers;
    using Less3.Settings;

    /// <summary>
    /// Bucket APIs.
    /// </summary>
    internal class BucketHandler
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        #endregion

        #region Private-Members

        private SettingsBase _Settings = null;
        private LoggingModule _Logging = null;
        private ConfigManager _Config = null;
        private BucketManager _Buckets = null;
        private AuthManager _Auth = null;

        #endregion

        #region Constructors-and-Factories

        internal BucketHandler(
            SettingsBase settings,
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

        internal async Task Delete(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await ctx.Response.Send(ErrorCode.InternalError);
                return;
            }
             
            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await ctx.Response.Send(ErrorCode.AccessDenied);
                return;
            }
             
            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await ctx.Response.Send(ErrorCode.NoSuchBucket);
                return;
            }

            BucketStatistics stats = md.BucketClient.GetFullStatistics();
            if (stats.Objects > 0 || stats.Bytes > 0)
            {
                _Logging.Warn(header + "bucket " + md.Bucket.Name + " is not empty");
                await ctx.Response.Send(ErrorCode.BucketNotEmpty);
                return;
            }

            _Logging.Info(header + "deleting bucket " + ctx.Request.Bucket);
            _Buckets.Remove(md.Bucket, true);

            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "application/xml";
            await ctx.Response.Send();
            return;
        }

        internal async Task DeleteTags(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await ctx.Response.Send(ErrorCode.InternalError);
                return;
            }

            if (md.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized");
                await ctx.Response.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                await ctx.Response.Send(ErrorCode.NoSuchBucket);
                return;
            }

            md.BucketClient.DeleteBucketTags();

            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "application/xml";
            await ctx.Response.Send();
            return;
        }

        internal async Task<bool> Exists(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                return false;
            }

            return true;
        }

        internal async Task<ListBucketResult> Read(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
             
            int startIndex = 0;
            if (!String.IsNullOrEmpty(ctx.Request.ContinuationToken))
            {
                startIndex = ParseContinuationToken(ctx.Request.ContinuationToken);
            }

            if (!String.IsNullOrEmpty(ctx.Request.Marker))
            {
                Obj marker = md.BucketClient.GetObjectLatestMetadata(ctx.Request.Marker);
                if (marker != null) startIndex = (marker.Id + 1);
            }
              
            List<Obj> objects = new List<Obj>();
            List<string> prefixes = new List<string>();
            int nextStartIndex = startIndex;
            bool isTruncated = false;
            md.BucketClient.Enumerate(ctx.Request.Delimiter, ctx.Request.Prefix, startIndex, (int)ctx.Request.MaxKeys, out objects, out prefixes, out nextStartIndex, out isTruncated);
             
            ListBucketResult listBucketResult = new ListBucketResult();
            listBucketResult.Contents = new List<ObjectMetadata>();

            listBucketResult.Prefix = ctx.Request.Prefix;
            listBucketResult.Delimiter = ctx.Request.Delimiter;
            listBucketResult.KeyCount = objects.Count;
            listBucketResult.MaxKeys = ctx.Request.MaxKeys;
            listBucketResult.Name = ctx.Request.Bucket;
            listBucketResult.BucketRegion = md.Bucket.RegionString;
            listBucketResult.Marker = ctx.Request.Marker;
            listBucketResult.Prefix = ctx.Request.Prefix; 
            listBucketResult.CommonPrefixes.Prefixes = prefixes;
            listBucketResult.IsTruncated = false;

            if (isTruncated)
            {
                listBucketResult.IsTruncated = true;
                listBucketResult.NextContinuationToken = BuildContinuationToken(nextStartIndex); 
            }

            Dictionary<string, S3ServerLibrary.S3Objects.Owner> ownerCache = new Dictionary<string, S3ServerLibrary.S3Objects.Owner>();

            foreach (Obj curr in objects)
            {
                ObjectMetadata c = new ObjectMetadata();
                c.ETag = "\"" + curr.Md5 + "\"";
                c.Key = curr.Key;
                c.LastModified = curr.LastUpdateUtc;
                c.Size = curr.ContentLength;
                c.ContentType = curr.ContentType;
                c.StorageClass = StorageClassEnum.STANDARD;
                
                c.Owner = new S3ServerLibrary.S3Objects.Owner();
                if (ownerCache.ContainsKey(curr.OwnerGUID))
                {
                    c.Owner = ownerCache[curr.OwnerGUID];
                }
                else
                {
                    User u = _Config.GetUserByGuid(curr.OwnerGUID);
                    c.Owner.DisplayName = u.Name;
                    c.Owner.ID = u.GUID;
                    ownerCache.Add(u.GUID, c.Owner);
                }

                listBucketResult.Contents.Add(c);
            }

            return listBucketResult;
        }

        internal async Task<LocationConstraint> ReadLocation(S3Context ctx)
        {
            return new LocationConstraint(_Settings.RegionString);
        }

        internal async Task<AccessControlPolicy> ReadAcl(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            User owner = _Config.GetUserByGuid(md.Bucket.OwnerGUID);
            if (owner == null)
            {
                _Logging.Warn(header + "unable to find owner GUID " + md.Bucket.OwnerGUID + " for bucket GUID " + md.Bucket.GUID);
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            return AclConverter.BucketAclsToPolicy(md.BucketAcls, owner, _Config, _Logging, header);
        }

        internal async Task<Tagging> ReadTags(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
             
            Tagging tags = new Tagging();
            tags.Tags = new TagSet();
            tags.Tags.Tags = new List<Tag>();

            if (md.BucketTags != null && md.BucketTags.Count > 0)
            {
                foreach (BucketTag curr in md.BucketTags)
                {
                    Tag currTag = new Tag();
                    currTag.Key = curr.Key;
                    currTag.Value = curr.Value;
                    tags.Tags.Tags.Add(currTag);
                }
            }

            return tags;
        }

        internal async Task<ListVersionsResult> ReadVersions(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
             
            int startIndex = 0;
            if (!String.IsNullOrEmpty(ctx.Request.ContinuationToken))
            {
                startIndex = ParseContinuationToken(ctx.Request.ContinuationToken);
            }

            if (!String.IsNullOrEmpty(ctx.Request.Marker))
            {
                Obj marker = md.BucketClient.GetObjectLatestMetadata(ctx.Request.Marker);
                if (marker != null) startIndex = (marker.Id + 1);
            }
              
            List<Obj> objects = new List<Obj>();
            List<string> prefixes = new List<string>();
            int nextStartIndex = startIndex;
            bool isTruncated = false;
            md.BucketClient.Enumerate(
                ctx.Request.Delimiter, 
                ctx.Request.Prefix, 
                startIndex, 
                (int)ctx.Request.MaxKeys, 
                out objects, 
                out prefixes, 
                out nextStartIndex, 
                out isTruncated);
             
            string lastKey = null; 
            if (objects.Count > 0)
            {
                objects = objects.OrderBy(p => p.Id).ToList(); 
                lastKey = objects[objects.Count - 1].Key; 
            }
             
            ListVersionsResult lvr = new ListVersionsResult();
            lvr.IsTruncated = isTruncated;
            lvr.KeyMarker = lastKey;
            lvr.MaxKeys = ctx.Request.MaxKeys;
            lvr.Name = ctx.Request.Bucket;
            lvr.BucketRegion = md.Bucket.RegionString;
            lvr.Prefix = ctx.Request.Prefix;

            Dictionary<string, S3ServerLibrary.S3Objects.Owner> ownerCache = new Dictionary<string, S3ServerLibrary.S3Objects.Owner>();

            foreach (Obj curr in objects)
            {
                if (curr.DeleteMarker)
                {
                    DeleteMarker d = new DeleteMarker();
                    d.IsLatest = IsLatest(objects, curr.Key, curr.LastAccessUtc);
                    d.Key = curr.Key;
                    d.LastModified = curr.LastUpdateUtc;
                    d.VersionId = curr.Version.ToString();

                    d.Owner = new S3ServerLibrary.S3Objects.Owner();
                    if (ownerCache.ContainsKey(curr.OwnerGUID))
                    {
                        d.Owner = ownerCache[curr.OwnerGUID];
                    }
                    else
                    {
                        User u = _Config.GetUserByGuid(curr.OwnerGUID);
                        d.Owner.DisplayName = u.Name;
                        d.Owner.ID = u.GUID;
                        ownerCache.Add(u.GUID, d.Owner);
                    }

                    lvr.DeleteMarkers.Add(d);
                }
                else
                {
                    S3ServerLibrary.S3Objects.ObjectVersion v = new S3ServerLibrary.S3Objects.ObjectVersion();
                    v.ETag = null;
                    v.IsLatest = IsLatest(objects, curr.Key, curr.LastAccessUtc);
                    v.Key = curr.Key;
                    v.ETag = "\"" + curr.Md5 + "\"";
                    v.LastModified = curr.LastUpdateUtc
                    v.VersionId = curr.Version.ToString();
                    v.Size = curr.ContentLength;
                    v.StorageClass = StorageClassEnum.STANDARD;

                    v.Owner = new S3ServerLibrary.S3Objects.Owner();
                    if (ownerCache.ContainsKey(curr.OwnerGUID))
                    {
                        v.Owner = ownerCache[curr.OwnerGUID];
                    }
                    else
                    {
                        User u = _Config.GetUserByGuid(curr.OwnerGUID);
                        v.Owner.DisplayName = u.Name;
                        v.Owner.ID = u.GUID;
                        ownerCache.Add(u.GUID, v.Owner);
                    }

                    lvr.Versions.Add(v);
                }
            }

            return lvr;
        }

        internal async Task<VersioningConfiguration> ReadVersioning(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            VersioningConfiguration vc = new VersioningConfiguration();
            vc.Status = VersioningStatusEnum.Suspended;
            vc.MfaDelete = MfaDeleteStatusEnum.Disabled;
                 
            if (md.Bucket.EnableVersioning) vc.Status = VersioningStatusEnum.Enabled;

            return vc;
        }

        internal async Task Write(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthentication(md, _Logging, header);

            if (md.Bucket != null || md.BucketClient != null)
            {
                _Logging.Warn(header + "bucket already exists");
                throw new S3Exception(new Error(ErrorCode.BucketAlreadyExists));
            }
               
            if (IsInvalidBucketName(ctx.Request.Bucket))
            {
                _Logging.Warn(header + "invalid bucket name: " + ctx.Request.Bucket);
                throw new S3Exception(new Error(ErrorCode.InvalidRequest));
            }
             
            Classes.Bucket bucket = new Classes.Bucket(
                ctx.Request.Bucket,
                md.User.GUID, 
                _Settings.Storage.StorageType, 
                _Settings.Storage.DiskDirectory + ctx.Request.Bucket + "/Objects/", 
                _Settings.RegionString);
             
            if (!_Buckets.Add(bucket))
            {
                _Logging.Warn(header + "unable to write bucket " + ctx.Request.Bucket);
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            BucketClient client = _Buckets.GetClient(ctx.Request.Bucket);
            if (client == null)
            {
                _Logging.Warn(header + "unable to retrieve bucket client for bucket " + ctx.Request.Bucket);
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            #region Permissions-in-Headers

            List<Grant> grants = GrantsFromHeaders(md.User, ctx.Http.Request.Headers);
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
                            tempUser = _Config.GetUserByGuid(curr.Grantee.ID);
                            if (tempUser == null) 
                            {
                                _Logging.Warn(header + "unable to retrieve user " + curr.Grantee.ID + " to add ACL to bucket " + bucket.GUID);
                                continue;
                            }

                            if (curr.Permission == PermissionEnum.Read) permitRead = true;
                            else if (curr.Permission == PermissionEnum.Write) permitWrite = true;
                            else if (curr.Permission == PermissionEnum.ReadAcp) permitReadAcp = true;
                            else if (curr.Permission == PermissionEnum.WriteAcp) permitWriteAcp = true;
                            else if (curr.Permission == PermissionEnum.FullControl) fullControl = true;

                            bucketAcl = BucketAcl.UserAcl(
                                curr.Grantee.ID, 
                                md.User.GUID, 
                                bucket.GUID,
                                permitRead, 
                                permitWrite, 
                                permitReadAcp, 
                                permitWriteAcp, 
                                fullControl);

                            client.AddBucketAcl(bucketAcl);
                        }
                        else if (!String.IsNullOrEmpty(curr.Grantee.URI))
                        {
                            if (curr.Permission == PermissionEnum.Read) permitRead = true;
                            else if (curr.Permission == PermissionEnum.Write) permitWrite = true;
                            else if (curr.Permission == PermissionEnum.ReadAcp) permitReadAcp = true;
                            else if (curr.Permission == PermissionEnum.WriteAcp) permitWriteAcp = true;
                            else if (curr.Permission == PermissionEnum.FullControl) fullControl = true;

                            bucketAcl = BucketAcl.GroupAcl(
                                curr.Grantee.URI, 
                                md.User.GUID, 
                                bucket.GUID,
                                permitRead, 
                                permitWrite, 
                                permitReadAcp, 
                                permitWriteAcp, 
                                fullControl);

                            client.AddBucketAcl(bucketAcl);
                        }
                    }
                }
            }

            #endregion
        }

        internal async Task WriteAcl(S3Context ctx, AccessControlPolicy acp)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            md.BucketClient.DeleteBucketAcl();

            List<BucketAcl> acls = AclConverter.PolicyToBucketAcls(
                acp,
                ctx.Http.Request.Headers,
                md.User,
                md.Bucket.GUID,
                md.Bucket.OwnerGUID,
                _Config,
                _Logging,
                header);

            foreach (BucketAcl acl in acls)
            {
                md.BucketClient.AddBucketAcl(acl);
            }
        }

        internal async Task WriteTagging(S3Context ctx, Tagging tagging)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            md.BucketClient.DeleteBucketTags();

            List<BucketTag> tags = new List<BucketTag>(); 
            if (tagging.Tags != null && tagging.Tags.Tags != null && tagging.Tags.Tags.Count > 0)
            {
                foreach (Tag curr in tagging.Tags.Tags)
                {
                    BucketTag tag = new BucketTag();
                    tag.BucketGUID = md.Bucket.GUID;
                    tag.Key = curr.Key;
                    tag.Value = curr.Value;
                    tags.Add(tag);
                }
            }

            md.BucketClient.AddBucketTags(tags);
        }

        internal async Task WriteVersioning(S3Context ctx, VersioningConfiguration vc)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            if (vc.Status == VersioningStatusEnum.Enabled && !md.Bucket.EnableVersioning)
            {
                md.Bucket.EnableVersioning = true;
                _Buckets.Remove(md.Bucket, false);
                _Buckets.Add(md.Bucket);
            }
            else if (vc.Status != VersioningStatusEnum.Enabled && md.Bucket.EnableVersioning)
            {
                md.Bucket.EnableVersioning = false;
                _Buckets.Remove(md.Bucket, false);
                _Buckets.Add(md.Bucket);
            }
        }

        internal async Task<ListMultipartUploadsResult> ReadMultipartUploads(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            List<Less3.Classes.Upload> uploads = _Config.GetUploadsByBucketGuid(md.Bucket.GUID);
            if (uploads == null)
            {
                uploads = new List<Less3.Classes.Upload>();
            }

            uploads = uploads.Where(u => u.ExpirationUtc > DateTime.UtcNow).ToList();

            if (!String.IsNullOrEmpty(ctx.Request.Prefix))
            {
                uploads = uploads.Where(u => u.Key.StartsWith(ctx.Request.Prefix)).ToList();
            }

            uploads = uploads.OrderBy(u => u.Key).ThenBy(u => u.CreatedUtc).ToList();

            int maxUploads = 1000;
            bool isTruncated = false;
            if (uploads.Count > maxUploads)
            {
                isTruncated = true;
                uploads = uploads.Take(maxUploads).ToList();
            }

            ListMultipartUploadsResult result = new ListMultipartUploadsResult();
            result.Bucket = ctx.Request.Bucket;
            result.Prefix = ctx.Request.Prefix;
            result.Delimiter = ctx.Request.Delimiter;
            result.MaxUploads = maxUploads;
            result.IsTruncated = isTruncated;
            result.Uploads = new List<S3ServerLibrary.S3Objects.Upload>();

            Dictionary<string, S3ServerLibrary.S3Objects.Owner> ownerCache = new Dictionary<string, S3ServerLibrary.S3Objects.Owner>();

            foreach (Less3.Classes.Upload upload in uploads)
            {
                S3ServerLibrary.S3Objects.Upload u = new S3ServerLibrary.S3Objects.Upload();
                u.UploadId = upload.GUID;
                u.Key = upload.Key;
                u.Initiated = upload.CreatedUtc;
                u.StorageClass = StorageClassEnum.STANDARD;

                if (ownerCache.ContainsKey(upload.AuthorGUID))
                {
                    u.Initiator = ownerCache[upload.AuthorGUID];
                }
                else
                {
                    User author = _Config.GetUserByGuid(upload.AuthorGUID);
                    if (author != null)
                    {
                        S3ServerLibrary.S3Objects.Owner initiatorOwner = new S3ServerLibrary.S3Objects.Owner();
                        initiatorOwner.ID = author.GUID;
                        initiatorOwner.DisplayName = author.Name;
                        u.Initiator = initiatorOwner;
                        ownerCache.Add(author.GUID, initiatorOwner);
                    }
                    else
                    {
                        S3ServerLibrary.S3Objects.Owner initiatorOwner = new S3ServerLibrary.S3Objects.Owner();
                        initiatorOwner.ID = upload.AuthorGUID;
                        initiatorOwner.DisplayName = upload.AuthorGUID;
                        u.Initiator = initiatorOwner;
                    }
                }

                if (ownerCache.ContainsKey(upload.OwnerGUID))
                {
                    u.Owner = ownerCache[upload.OwnerGUID];
                }
                else
                {
                    User owner = _Config.GetUserByGuid(upload.OwnerGUID);
                    if (owner != null)
                    {
                        S3ServerLibrary.S3Objects.Owner ownerObj = new S3ServerLibrary.S3Objects.Owner();
                        ownerObj.ID = owner.GUID;
                        ownerObj.DisplayName = owner.Name;
                        u.Owner = ownerObj;
                        ownerCache.Add(owner.GUID, ownerObj);
                    }
                    else
                    {
                        S3ServerLibrary.S3Objects.Owner ownerObj = new S3ServerLibrary.S3Objects.Owner();
                        ownerObj.ID = upload.OwnerGUID;
                        ownerObj.DisplayName = upload.OwnerGUID;
                        u.Owner = ownerObj;
                    }
                }

                result.Uploads.Add(u);
            }

            if (isTruncated && result.Uploads.Count > 0)
            {
                S3ServerLibrary.S3Objects.Upload last = result.Uploads[result.Uploads.Count - 1];
                result.NextKeyMarker = last.Key;
                result.NextUploadIdMarker = last.UploadId;
            }

            _Logging.Debug(header + "returning " + result.Uploads.Count + " multipart uploads for bucket " + ctx.Request.Bucket);

            return result;
        }

        #endregion

        #region Private-Methods

        private string BuildContinuationToken(long lastId)
        {
            return Common.StringToBase64(lastId.ToString());
        }

        private int ParseContinuationToken(string base64)
        {
            return Convert.ToInt32(Common.Base64ToString(base64));
        }

        private bool IsLatest(List<Obj> objs, string key, DateTime lastAccessUtc)
        {
            bool laterObjExists = objs.Exists(o =>
                o.Key.Equals(key)
                && o.LastAccessUtc > lastAccessUtc);

            return !laterObjExists;
        }

        private List<Grant> GrantsFromHeaders(User user, NameValueCollection headers)
        {
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
                        grant.Grantee = new Grantee();
                        grant.Grantee.ID = user.GUID;
                        grant.Grantee.DisplayName = user.Name;
                        ret.Add(grant);
                        break;

                    case "public-read":
                        grant = new Grant();
                        grant.Permission = PermissionEnum.Read;
                        grant.Grantee = new Grantee();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        grant.Grantee.DisplayName = "AllUsers";
                        ret.Add(grant);
                        break;

                    case "public-read-write":
                        grant = new Grant();
                        grant.Permission = PermissionEnum.Read;
                        grant.Grantee = new Grantee();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        grant.Grantee.DisplayName = "AllUsers";
                        ret.Add(grant);

                        grant = new Grant();
                        grant.Permission = PermissionEnum.Write;
                        grant.Grantee = new Grantee();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        grant.Grantee.DisplayName = "AllUsers";
                        ret.Add(grant);
                        break;

                    case "authenticated-read":
                        grant = new Grant();
                        grant.Permission = PermissionEnum.Read;
                        grant.Grantee = new Grantee();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AuthenticatedUsers";
                        grant.Grantee.DisplayName = "AuthenticatedUsers";
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
                        if (!GrantFromString(curr, PermissionEnum.Read, out grant)) continue;
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
                        if (!GrantFromString(curr, PermissionEnum.Write, out grant)) continue;
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
                        if (!GrantFromString(curr, PermissionEnum.ReadAcp, out grant)) continue;
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
                        if (!GrantFromString(curr, PermissionEnum.WriteAcp, out grant)) continue;
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
                        if (!GrantFromString(curr, PermissionEnum.FullControl, out grant)) continue;
                        ret.Add(grant);
                    }
                }
            }

            return ret;
        }

        private bool GrantFromString(string str, PermissionEnum permType, out Grant grant)
        {
            grant = null;
            if (String.IsNullOrEmpty(str)) return false;

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
                grant.Grantee.ID = user.GUID;
                grant.Grantee.DisplayName = user.Name;
                return true;
            }
            else if (granteeType.Equals("id"))
            {
                User user = _Config.GetUserByGuid(grantee);
                if (user == null)
                {
                    return false;
                }
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

        private bool IsInvalidBucketName(string name)
        {
            List<string> invalidNames = new List<string>
            {
                "admin"
            };

            if (invalidNames.Contains(name.ToLower())) return true;
            return false;
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
