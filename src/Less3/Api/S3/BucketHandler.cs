namespace Less3.Api.S3
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using System.Text;
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

        private const int _MaxListKeys = 1000;
        private const string _ContinuationTokenPrefix = "less3:";

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
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            // Amazon S3 refuses to delete a bucket until every object version and delete marker is gone.
            if (md.BucketClient.HasAnyVersions())
            {
                _Logging.Warn(header + "bucket " + md.Bucket.Name + " is not empty");
                throw new S3Exception(new Error(ErrorCode.BucketNotEmpty));
            }

            _Logging.Info(header + "deleting bucket " + ctx.Request.Bucket);
            _Buckets.Remove(md.Bucket, true);
        }

        internal async Task DeleteTags(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            md.BucketClient.DeleteBucketTags();
        }

        internal async Task<bool> Exists(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);

            if (md.Bucket == null || md.BucketClient == null)
            {
                _Logging.Warn(header + "no such bucket");
                return false;
            }

            ctx.Response.Headers.Add("x-amz-bucket-region", md.Bucket.RegionString);
            return true;
        }

        /// <summary>
        /// ListObjects (v1) and ListObjectsV2. S3Server shapes the response for the requested list type
        /// (Marker for v1; KeyCount, ContinuationToken and StartAfter for v2; Owner only with fetch-owner
        /// for v2) and applies encoding-type=url, so values are returned unencoded.
        /// </summary>
        internal async Task<ListBucketResult> ListObjects(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            bool v2 = String.Equals(ctx.Request.RetrieveQueryValue("list-type"), "2", StringComparison.Ordinal);
            string prefix = ctx.Request.Prefix;
            string delimiter = ctx.Request.Delimiter;
            int maxKeys = Math.Min(ctx.Request.MaxKeys, _MaxListKeys);

            string marker = null;
            string continuationToken = null;
            string startAfter = null;

            if (v2)
            {
                continuationToken = ctx.Request.ContinuationToken;
                startAfter = ctx.Request.StartAfter;

                // A continuation token takes precedence over start-after.
                if (continuationToken != null) marker = DecodeContinuationToken(continuationToken, header);
                else if (!String.IsNullOrEmpty(startAfter)) marker = startAfter;
            }
            else
            {
                marker = ctx.Request.Marker;
                if (marker == "") marker = null;
            }

            ObjectListing listing = md.BucketClient.ListObjects(prefix, delimiter, marker, maxKeys);
            Dictionary<string, S3ServerLibrary.S3Objects.Owner> ownerCache = new Dictionary<string, S3ServerLibrary.S3Objects.Owner>();

            ListBucketResult result = new ListBucketResult();
            result.Name = md.Bucket.Name;
            result.BucketRegion = md.Bucket.RegionString;
            result.Prefix = prefix;
            result.MaxKeys = maxKeys;
            result.Delimiter = String.IsNullOrEmpty(delimiter) ? null : delimiter;
            result.IsTruncated = listing.IsTruncated;

            if (v2)
            {
                result.ContinuationToken = continuationToken;
                result.StartAfter = String.IsNullOrEmpty(startAfter) ? null : startAfter;
                result.KeyCount = listing.Objects.Count + listing.CommonPrefixes.Count;
                if (listing.IsTruncated) result.NextContinuationToken = EncodeContinuationToken(listing.NextMarker);
            }
            else
            {
                result.Marker = marker ?? "";
                if (listing.IsTruncated && !String.IsNullOrEmpty(delimiter)) result.NextMarker = listing.NextMarker;
            }

            foreach (Obj obj in listing.Objects)
            {
                result.Contents.Add(new ObjectMetadata(
                    obj.Key,
                    UtcTimestamp.AsUtc(obj.LastUpdateUtc),
                    obj.Etag ?? obj.Md5,
                    obj.ContentLength,
                    OwnerFor(obj.OwnerId, ownerCache)));
            }

            foreach (string commonPrefix in listing.CommonPrefixes)
            {
                result.CommonPrefixes.Add(new CommonPrefixes(commonPrefix));
            }

            return result;
        }

        /// <summary>
        /// ListObjectVersions, with versions and delete markers interleaved in key order, newest version
        /// first. S3Server shapes delete markers and applies encoding-type=url, so values are returned unencoded.
        /// </summary>
        internal async Task<ListVersionsResult> ListObjectVersions(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            string prefix = ctx.Request.Prefix;
            string delimiter = ctx.Request.Delimiter;
            int maxKeys = Math.Min(ctx.Request.MaxKeys, _MaxListKeys);
            string keyMarker = ctx.Request.KeyMarker;
            string versionIdMarker = ctx.Request.VersionIdMarker;
            if (keyMarker == "") keyMarker = null;
            if (versionIdMarker == "") versionIdMarker = null;

            long? afterVersion = null;
            if (versionIdMarker != null)
            {
                // A version-id-marker is only meaningful together with a key-marker.
                if (keyMarker == null || !ObjectVersionReference.TryParse(versionIdMarker, out ObjectVersionReference markerReference))
                {
                    _Logging.Warn(header + "invalid version-id-marker " + versionIdMarker);
                    throw new S3Exception(new Error(ErrorCode.InvalidArgument));
                }

                Obj markerObj = md.BucketClient.ResolveObject(keyMarker, markerReference);
                if (markerObj != null) afterVersion = markerObj.Version;
                else if (!markerReference.IsNullVersion) afterVersion = markerReference.Version;
            }

            ObjectListing listing = md.BucketClient.ListObjectVersions(prefix, delimiter, keyMarker, afterVersion, maxKeys);
            Dictionary<string, S3ServerLibrary.S3Objects.Owner> ownerCache = new Dictionary<string, S3ServerLibrary.S3Objects.Owner>();

            ListVersionsResult result = new ListVersionsResult();
            result.Name = md.Bucket.Name;
            result.BucketRegion = md.Bucket.RegionString;
            result.Prefix = prefix;
            result.KeyMarker = keyMarker ?? "";
            result.VersionIdMarker = versionIdMarker ?? "";
            result.MaxKeys = maxKeys;
            result.Delimiter = String.IsNullOrEmpty(delimiter) ? null : delimiter;
            result.IsTruncated = listing.IsTruncated;

            if (listing.IsTruncated)
            {
                result.NextKeyMarker = listing.NextMarker;
                result.NextVersionIdMarker = listing.NextVersionIdMarker;
            }

            foreach (Obj obj in listing.Objects)
            {
                string versionId = md.BucketClient.VersionIdString(obj);
                bool isLatest = listing.LatestObjectIds.Contains(obj.Id);
                DateTime lastModified = UtcTimestamp.AsUtc(obj.LastUpdateUtc);
                S3ServerLibrary.S3Objects.Owner owner = OwnerFor(obj.OwnerId, ownerCache);

                if (obj.DeleteMarker)
                    result.Entries.Add(new DeleteMarker(obj.Key, versionId, isLatest, lastModified, owner));
                else
                    result.Entries.Add(new ObjectVersion(obj.Key, versionId, isLatest, lastModified, obj.Etag ?? obj.Md5, obj.ContentLength, owner));
            }

            foreach (string commonPrefix in listing.CommonPrefixes)
            {
                result.CommonPrefixes.Add(new CommonPrefixes(commonPrefix));
            }

            return result;
        }

        internal async Task<LocationConstraint> ReadLocation(S3Context ctx)
        {
            return new LocationConstraint(_Settings.RegionString);
        }

        internal async Task<AccessControlPolicy> ReadAcl(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            User owner = _Config.GetUserById(md.Bucket.OwnerId);
            if (owner == null)
            {
                _Logging.Warn(header + "unable to find owner Id " + md.Bucket.OwnerId + " for bucket Id " + md.Bucket.Id);
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            return AclConverter.BucketAclsToPolicy(md.BucketAcls, owner, _Config, _Logging, header);
        }

        internal async Task<Tagging> ReadTags(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            Tagging tags = new Tagging();
            tags.Tags = new TagSet();
            tags.Tags.Tags = new List<Tag>();

            foreach (BucketTag curr in md.BucketTags ?? new List<BucketTag>())
            {
                tags.Tags.Tags.Add(new Tag { Key = curr.Key, Value = curr.Value });
            }

            return tags;
        }

        internal async Task<VersioningConfiguration> ReadVersioning(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            VersioningConfiguration vc = new VersioningConfiguration();
            vc.IncludeMfaDelete = false;

            if (md.Bucket.EnableVersioning)
            {
                vc.IncludeStatus = true;
                vc.Status = VersioningStatusEnum.Enabled;
            }
            else if (md.Bucket.VersioningSuspended)
            {
                vc.IncludeStatus = true;
                vc.Status = VersioningStatusEnum.Suspended;
            }
            else
            {
                // A bucket that has never had versioning enabled reports no status.
                vc.IncludeStatus = false;
            }

            return vc;
        }

        internal async Task Write(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthentication(md, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);

            if (md.Bucket != null || md.BucketClient != null)
            {
                _Logging.Warn(header + "bucket already exists");
                bool ownedByRequester = md.Bucket != null && String.Equals(md.Bucket.OwnerId, md.User.Id, StringComparison.Ordinal);
                throw new S3Exception(new Error(ownedByRequester ? ErrorCode.BucketAlreadyOwnedByYou : ErrorCode.BucketAlreadyExists));
            }

            if (BucketNameValidator.IsInvalid(ctx.Request.Bucket))
            {
                _Logging.Warn(header + "invalid bucket name: " + ctx.Request.Bucket);
                throw new S3Exception(new Error(ErrorCode.InvalidBucketName));
            }

            Classes.Bucket bucket = new Classes.Bucket(
                ctx.Request.Bucket,
                md.User.Id,
                _Settings.Storage.StorageType,
                _Settings.Storage.DiskDirectory + ctx.Request.Bucket + "/Objects/",
                _Settings.RegionString);
            bucket.TenantId = md.TenantId;

            // Validate ACL headers before creating anything.
            List<BucketAcl> acls = AclConverter.PolicyToBucketAcls(null, ctx.Http.Request.Headers, md.User, bucket.Id, md.User.Id, _Config, _Logging, header);

            if (!_Buckets.Add(bucket))
            {
                _Logging.Warn(header + "unable to write bucket " + ctx.Request.Bucket);
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            BucketClient client = _Buckets.GetClient(md.TenantId, ctx.Request.Bucket);
            if (client == null)
            {
                _Logging.Warn(header + "unable to retrieve bucket client for bucket " + ctx.Request.Bucket);
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            ctx.Response.Headers.Add("Location", "/" + ctx.Request.Bucket);
            if (acls.Count > 0) client.SetBucketAcls(acls);
        }

        internal async Task WriteAcl(S3Context ctx, AccessControlPolicy acp)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
            RequestValidator.ValidateAuthentication(md, _Logging, header);

            List<BucketAcl> acls = AclConverter.PolicyToBucketAcls(
                acp,
                ctx.Http.Request.Headers,
                md.User,
                md.Bucket.Id,
                md.Bucket.OwnerId,
                _Config,
                _Logging,
                header);

            md.BucketClient.SetBucketAcls(acls);
        }

        internal async Task WriteTagging(S3Context ctx, Tagging tagging)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            if (S3TagValidator.IsInvalid(tagging))
            {
                _Logging.Warn(header + "invalid bucket tag set");
                throw new S3Exception(new Error(ErrorCode.InvalidRequest));
            }

            List<BucketTag> tags = new List<BucketTag>();
            if (tagging.Tags != null && tagging.Tags.Tags != null)
            {
                foreach (Tag curr in tagging.Tags.Tags)
                {
                    BucketTag tag = new BucketTag();
                    tag.TenantId = md.Bucket.TenantId;
                    tag.BucketId = md.Bucket.Id;
                    tag.Key = curr.Key;
                    tag.Value = curr.Value;
                    tags.Add(tag);
                }
            }

            md.BucketClient.AddBucketTags(tags);
        }

        internal async Task WriteVersioning(S3Context ctx, VersioningConfiguration vc)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            if (vc == null || (vc.Status != VersioningStatusEnum.Enabled && vc.Status != VersioningStatusEnum.Suspended))
            {
                _Logging.Warn(header + "versioning status must be Enabled or Suspended");
                throw new S3Exception(new Error(ErrorCode.MalformedXML));
            }

            Classes.Bucket bucket = _Config.GetBucketById(md.Bucket.TenantId, md.Bucket.Id);
            if (bucket == null) throw new S3Exception(new Error(ErrorCode.NoSuchBucket));

            if (vc.Status == VersioningStatusEnum.Enabled)
            {
                bucket.EnableVersioning = true;
                bucket.VersioningSuspended = false;
            }
            else if (bucket.EnableVersioning || bucket.VersioningSuspended)
            {
                // Suspending keeps every existing version; new writes replace the null version.
                bucket.EnableVersioning = false;
                bucket.VersioningSuspended = true;
            }
            else
            {
                // Suspending versioning on a bucket that never had it enabled changes nothing; Amazon S3
                // then reports the bucket as Suspended.
                bucket.VersioningSuspended = true;
            }

            if (!_Buckets.Update(bucket))
            {
                _Logging.Warn(header + "unable to update versioning on bucket " + bucket.Name);
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            _Logging.Info(header + "versioning on bucket " + bucket.Name + " set to " + vc.Status.ToString());
        }

        internal async Task<ListMultipartUploadsResult> ReadMultipartUploads(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            // S3Server validates max-uploads and applies encoding-type=url to the response.
            string prefix = ctx.Request.Prefix ?? "";
            string delimiter = String.IsNullOrEmpty(ctx.Request.Delimiter) ? null : ctx.Request.Delimiter;
            string keyMarker = ctx.Request.KeyMarker;
            string uploadIdMarker = ctx.Request.UploadIdMarker;
            int maxUploads = Math.Min(ctx.Request.MaxUploads, _MaxListKeys);

            List<Less3.Classes.Upload> uploads = (_Config.GetUploadsByBucketId(md.Bucket.TenantId, md.Bucket.Id) ?? new List<Less3.Classes.Upload>())
                .Where(u => u.ExpirationUtc > DateTime.UtcNow)
                .Where(u => u.Key.StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(u => u.Key, StringComparer.Ordinal)
                .ThenBy(u => u.CreatedUtc)
                .ThenBy(u => u.Id, StringComparer.Ordinal)
                .ToList();

            if (!String.IsNullOrEmpty(keyMarker))
            {
                // With an upload-id-marker, uploads for the marker key after that upload are included;
                // without one, every upload for the marker key is skipped.
                int markerIndex = String.IsNullOrEmpty(uploadIdMarker)
                    ? -1
                    : uploads.FindIndex(u => String.Equals(u.Key, keyMarker, StringComparison.Ordinal) && String.Equals(u.Id, uploadIdMarker, StringComparison.Ordinal));

                uploads = uploads
                    .Where((u, index) =>
                        String.CompareOrdinal(u.Key, keyMarker) > 0
                        || (markerIndex >= 0 && String.Equals(u.Key, keyMarker, StringComparison.Ordinal) && index > markerIndex))
                    .ToList();
            }

            ListMultipartUploadsResult result = new ListMultipartUploadsResult();
            result.Bucket = ctx.Request.Bucket;
            result.Prefix = prefix;
            result.Delimiter = delimiter;
            result.KeyMarker = keyMarker;
            result.UploadIdMarker = uploadIdMarker;
            result.MaxUploads = maxUploads;
            result.Uploads = new List<S3ServerLibrary.S3Objects.Upload>();
            result.CommonPrefixes = new List<CommonPrefixes>();

            int count = 0;
            string lastPrefix = null;
            S3ServerLibrary.S3Objects.Upload lastUpload = null;
            string lastKey = null;
            Dictionary<string, S3ServerLibrary.S3Objects.Owner> ownerCache = new Dictionary<string, S3ServerLibrary.S3Objects.Owner>();

            foreach (Less3.Classes.Upload upload in uploads)
            {
                string commonPrefix = null;
                if (!String.IsNullOrEmpty(delimiter))
                {
                    int index = upload.Key.IndexOf(delimiter, prefix.Length, StringComparison.Ordinal);
                    if (index >= 0) commonPrefix = upload.Key.Substring(0, index + delimiter.Length);
                }

                if (commonPrefix != null && String.Equals(commonPrefix, lastPrefix, StringComparison.Ordinal)) continue;

                if (count >= maxUploads)
                {
                    result.IsTruncated = true;
                    break;
                }

                if (commonPrefix != null)
                {
                    result.CommonPrefixes.Add(new CommonPrefixes(commonPrefix));
                    lastPrefix = commonPrefix;
                    lastKey = commonPrefix;
                    lastUpload = null;
                    count++;
                    continue;
                }

                S3ServerLibrary.S3Objects.Upload u = new S3ServerLibrary.S3Objects.Upload();
                u.UploadId = upload.Id;
                u.Key = upload.Key;
                u.Initiated = upload.CreatedUtc;
                u.StorageClass = StorageClassEnum.STANDARD;
                u.Initiator = OwnerFor(upload.AuthorId, ownerCache);
                u.Owner = OwnerFor(upload.OwnerId, ownerCache);
                result.Uploads.Add(u);

                lastKey = upload.Key;
                lastUpload = u;
                count++;
            }

            if (result.IsTruncated)
            {
                result.NextKeyMarker = lastKey;
                result.NextUploadIdMarker = lastUpload != null ? lastUpload.UploadId : null;
            }

            _Logging.Debug(header + "returning " + result.Uploads.Count + " multipart uploads for bucket " + ctx.Request.Bucket);

            return result;
        }

        #endregion

        #region Private-Methods

        private static string Header(S3Context ctx)
        {
            return "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";
        }

        private static string EncodeContinuationToken(string marker)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(_ContinuationTokenPrefix + marker));
        }

        private string DecodeContinuationToken(string token, string header)
        {
            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                if (decoded.StartsWith(_ContinuationTokenPrefix, StringComparison.Ordinal))
                    return decoded.Substring(_ContinuationTokenPrefix.Length);
            }
            catch (FormatException)
            {
            }

            _Logging.Warn(header + "invalid continuation token");
            throw new S3Exception(new Error(ErrorCode.InvalidArgument));
        }

        private S3ServerLibrary.S3Objects.Owner OwnerFor(string id, Dictionary<string, S3ServerLibrary.S3Objects.Owner> cache)
        {
            if (String.IsNullOrEmpty(id)) return null;
            if (cache.TryGetValue(id, out S3ServerLibrary.S3Objects.Owner cached)) return cached;

            User user = _Config.GetUserById(id);
            S3ServerLibrary.S3Objects.Owner owner = new S3ServerLibrary.S3Objects.Owner();
            owner.ID = id;
            owner.DisplayName = user != null ? user.Name : id;
            cache[id] = owner;
            return owner;
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
