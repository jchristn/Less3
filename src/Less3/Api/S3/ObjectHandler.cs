namespace Less3.Api.S3
{
    using Less3.Classes;
    using Less3.Helpers;
    using Less3.Locking;
    using Less3.Settings;
    using Less3.Storage;
    using Less3.Telemetry;
    using S3ServerLibrary;
    using S3ServerLibrary.S3Objects;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using WatsonWebserver.Core;

    /// <summary>
    /// Object APIs.
    /// </summary>
    public class ObjectHandler
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        /// <summary>
        /// Maximum number of keys Amazon S3 accepts in one DeleteObjects request.
        /// </summary>
        public const int MaxDeleteObjectsKeys = 1000;

        #endregion

        #region Private-Members

        private SettingsBase _Settings = null;
        private LoggingModule _Logging = null;
        private ConfigManager _Config = null;
        private BucketManager _Buckets = null;
        private AuthManager _Auth = null;
        private ILockManager _LockManager = null;

        // System metadata headers stored with the object and returned on GET/HEAD. They are kept in the
        // object's metadata JSON under a ':' prefix, which can never collide with a user metadata key
        // because ':' is not a valid HTTP header name character.
        private static readonly string[] _SystemMetadataHeaders = new string[]
        {
            "cache-control",
            "content-disposition",
            "content-encoding",
            "content-language",
            "expires"
        };

        private const string _SystemMetadataPrefix = ":";

        #endregion

        #region Constructors-and-Factories

        internal ObjectHandler(
            SettingsBase settings,
            LoggingModule logging,
            ConfigManager config,
            BucketManager buckets,
            AuthManager auth,
            ILockManager lockManager)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Config = config ?? throw new ArgumentNullException(nameof(config));
            _Buckets = buckets ?? throw new ArgumentNullException(nameof(buckets));
            _Auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _LockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
        }

        #endregion

        #region Internal-Methods

        internal async Task Delete(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            ObjectVersionReference reference = ParseVersionReference(ctx, header);
            ObjectDeleteResult result = md.BucketClient.DeleteObject(ctx.Request.Key, reference, RequesterId(md, ctx));

            if (md.BucketClient.VersioningConfigured && !String.IsNullOrEmpty(result.VersionId))
                ctx.Response.Headers.Add("x-amz-version-id", result.VersionId);

            if (result.DeleteMarker)
                ctx.Response.Headers.Add(Constants.Headers.DeleteMarker, "true");
        }

        /// <summary>
        /// Delete multiple objects. Each key is authorized as an individual DeleteObject, and each key's
        /// outcome is reported independently; one key failing never prevents the others from being processed.
        /// A key or version that does not exist is reported as deleted, as Amazon S3 does.
        /// </summary>
        internal async Task<DeleteResult> DeleteMultiple(S3Context ctx, DeleteMultiple dm)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            if (dm == null || dm.Objects == null || dm.Objects.Count < 1 || dm.Objects.Count > MaxDeleteObjectsKeys)
            {
                _Logging.Warn(header + "DeleteObjects request must name between 1 and " + MaxDeleteObjectsKeys + " keys");
                throw new S3Exception(new Error(ErrorCode.MalformedXML));
            }

            Activity activity = Less3Telemetry.StartObjectOperation("DeleteObjects");
            Stopwatch sw = Stopwatch.StartNew();
            DeleteResult deleteResult = new DeleteResult();
            string requesterId = RequesterId(md, ctx);

            try
            {
                foreach (S3ServerLibrary.S3Objects.Object curr in dm.Objects)
                {
                    string key = curr != null ? curr.Key : null;
                    string versionId = curr != null && !String.IsNullOrEmpty(curr.VersionId) ? curr.VersionId : null;

                    if (String.IsNullOrEmpty(key))
                    {
                        deleteResult.Errors.Add(KeyError(ErrorCode.UserKeyMustBeSpecified, key, versionId));
                        continue;
                    }

                    if (!ObjectVersionReference.TryParse(versionId, out ObjectVersionReference reference))
                    {
                        deleteResult.Errors.Add(KeyError(ErrorCode.NoSuchVersion, key, versionId));
                        continue;
                    }

                    try
                    {
                        Obj target = md.BucketClient.ResolveObject(key, reference);
                        RequestMetadata keyMd = _Auth.ForObject(md, md.Bucket, md.BucketClient, target);
                        keyMd = _Auth.AuthorizeObjectRequest(ctx, keyMd, S3RequestType.ObjectDelete);
                        if (keyMd.Authorization == AuthorizationResult.NotAuthorized)
                        {
                            _Logging.Warn(header + "not authorized to delete " + md.Bucket.Name + "/" + key);
                            deleteResult.Errors.Add(KeyError(ErrorCode.AccessDenied, key, versionId));
                            continue;
                        }

                        ObjectDeleteResult result = md.BucketClient.DeleteObject(key, reference, requesterId);
                        if (dm.Quiet) continue;

                        Deleted deleted = new Deleted();
                        deleted.Key = key;
                        deleted.DeleteMarker = null;

                        if (!reference.IsLatest)
                        {
                            deleted.VersionId = versionId;
                            if (result.Found && result.DeleteMarker)
                            {
                                deleted.DeleteMarker = true;
                                deleted.DeleteMarkerVersionId = result.VersionId;
                            }
                        }
                        else if (result.CreatedDeleteMarker)
                        {
                            deleted.DeleteMarker = true;
                            deleted.DeleteMarkerVersionId = result.VersionId;
                        }

                        deleteResult.DeletedObjects.Add(deleted);
                    }
                    catch (S3Exception s3e)
                    {
                        deleteResult.Errors.Add(KeyError(s3e.Error.Code, key, versionId));
                    }
                    catch (Exception e)
                    {
                        // A lost lock or storage failure affects only this key; keep processing the rest.
                        _Logging.Warn(header + "failed to delete " + md.Bucket.Name + "/" + key + ": " + e.Message);
                        deleteResult.Errors.Add(KeyError(ErrorCode.InternalError, key, versionId));
                    }
                }

                return deleteResult;
            }
            finally
            {
                activity?.SetTag("less3.delete_objects.keys", dm.Objects.Count);
                activity?.SetTag("less3.delete_objects.errors", deleteResult.Errors.Count);
                Less3Telemetry.ObjectStage(activity, "DeleteObjects", "total", sw.Elapsed.TotalMilliseconds);
                activity?.Dispose();
            }
        }

        internal async Task DeleteTags(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            ObjectVersionReference reference = ParseVersionReference(ctx, header);
            Obj obj = RequireLiveObject(md, ctx, reference, header);

            if (!md.BucketClient.SetObjectTags(obj, null)) throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            AddVersionHeader(md, obj, ctx);
        }

        internal async Task<ObjectMetadata> Exists(S3Context ctx)
        {
            string header = Header(ctx);

            Activity activity = Less3Telemetry.StartObjectOperation("HeadObject");
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
                RequestValidator.ValidateAuthorization(md, _Logging, header);
                RequestValidator.ValidateBucketExists(md, _Logging, header);

                ObjectVersionReference reference = ParseVersionReference(ctx, header);
                Obj obj = RequireLiveObject(md, ctx, reference, header);
                Less3Telemetry.ObjectStage(activity, "HeadObject", "metadata_read", sw.Elapsed.TotalMilliseconds);

                CheckConditionalRequest(ctx, obj);

                AddVersionHeader(md, obj, ctx);
                AddObjectMetadataHeaders(obj, ctx, header);

                ObjectMetadata metadata = new ObjectMetadata(obj.Key, obj.LastUpdateUtc, EtagOf(obj), obj.ContentLength, new Owner(obj.OwnerId, null));
                metadata.ContentType = ResponseContentType(ctx, obj);
                return metadata;
            }
            finally
            {
                Less3Telemetry.ObjectStage(activity, "HeadObject", "total", sw.Elapsed.TotalMilliseconds);
                activity?.Dispose();
            }
        }

        internal async Task<S3Object> Read(S3Context ctx)
        {
            string header = Header(ctx);

            Activity activity = Less3Telemetry.StartObjectOperation("GetObject");
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
                RequestValidator.ValidateAuthorization(md, _Logging, header);
                RequestValidator.ValidateBucketExists(md, _Logging, header);

                ObjectVersionReference reference = ParseVersionReference(ctx, header);
                Obj obj = RequireLiveObject(md, ctx, reference, header);
                Less3Telemetry.ObjectStage(activity, "GetObject", "metadata_read", sw.Elapsed.TotalMilliseconds);

                CheckConditionalRequest(ctx, obj);

                ObjectReadResult read = OpenForRead(ctx, md, obj, reference, null);
                Less3Telemetry.ObjectStage(activity, "GetObject", "storage_open", sw.Elapsed.TotalMilliseconds);

                Obj servedObj = read.Object;
                AddVersionHeader(md, servedObj, ctx);
                AddObjectMetadataHeaders(servedObj, ctx, header);

                return new S3Object(servedObj.Key, md.BucketClient.VersionIdString(servedObj), IsLatest(md, servedObj), servedObj.LastUpdateUtc, EtagOf(servedObj), servedObj.ContentLength, GetOwnerFromUserId(servedObj.OwnerId), read.Data, ResponseContentType(ctx, servedObj));
            }
            finally
            {
                Less3Telemetry.ObjectStage(activity, "GetObject", "total", sw.Elapsed.TotalMilliseconds);
                activity?.Dispose();
            }
        }

        internal async Task<AccessControlPolicy> ReadAcl(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            ObjectVersionReference reference = ParseVersionReference(ctx, header);
            Obj obj = RequireLiveObject(md, ctx, reference, header);

            User owner = _Config.GetUserById(obj.OwnerId);
            if (owner == null)
            {
                _Logging.Warn(header + "unable to find owner Id " + obj.OwnerId + " for object Id " + obj.Id);
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            AddVersionHeader(md, obj, ctx);
            return AclConverter.ObjectAclsToPolicy(md.BucketClient.GetObjectAcl(obj.Id), owner, _Config, _Logging, header);
        }

        internal async Task<S3Object> ReadRange(S3Context ctx)
        {
            string header = Header(ctx);

            Activity activity = Less3Telemetry.StartObjectOperation("GetObjectRange");
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
                RequestValidator.ValidateAuthorization(md, _Logging, header);
                RequestValidator.ValidateBucketExists(md, _Logging, header);

                ObjectVersionReference reference = ParseVersionReference(ctx, header);
                Obj obj = RequireLiveObject(md, ctx, reference, header);
                Less3Telemetry.ObjectStage(activity, "GetObjectRange", "metadata_read", sw.Elapsed.TotalMilliseconds);

                CheckConditionalRequest(ctx, obj);

                long? requestedStart = ctx.Request.RangeStart;
                long? requestedEnd = ctx.Request.RangeEnd;
                long? suffixLength = ctx.Request.RangeSuffixLength;

                // An unsatisfiable range is rejected here, before a stream (and the object's read lock) is
                // opened, rather than left for S3Server's own range check after the callback returns.
                ObjectReadResult read = OpenForRead(ctx, md, obj, reference, served => SelectRange(served.ContentLength, requestedStart, requestedEnd, suffixLength, header));
                Less3Telemetry.ObjectStage(activity, "GetObjectRange", "storage_open", sw.Elapsed.TotalMilliseconds);

                Obj servedObj = read.Object;
                AddVersionHeader(md, servedObj, ctx);
                AddObjectMetadataHeaders(servedObj, ctx, header);

                // S3Server emits Content-Range for the bytes returned (start-end/total) using S3Object.TotalSize,
                // and answers a suffix range on an empty object with 200 and an empty body.
                S3Object s3obj = new S3Object(servedObj.Key, md.BucketClient.VersionIdString(servedObj), IsLatest(md, servedObj), servedObj.LastUpdateUtc, EtagOf(servedObj), read.Length, GetOwnerFromUserId(servedObj.OwnerId), read.Data, ResponseContentType(ctx, servedObj));
                s3obj.TotalSize = servedObj.ContentLength;
                return s3obj;
            }
            finally
            {
                Less3Telemetry.ObjectStage(activity, "GetObjectRange", "total", sw.Elapsed.TotalMilliseconds);
                activity?.Dispose();
            }
        }

        internal async Task<Tagging> ReadTags(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            ObjectVersionReference reference = ParseVersionReference(ctx, header);
            Obj obj = RequireLiveObject(md, ctx, reference, header);

            AddVersionHeader(md, obj, ctx);

            Tagging tags = new Tagging();
            tags.Tags = new TagSet();
            tags.Tags.Tags = new List<Tag>();

            List<ObjectTag> objectTags = md.BucketClient.GetObjectTags(obj.Id);
            if (objectTags != null)
            {
                foreach (ObjectTag curr in objectTags)
                {
                    tags.Tags.Tags.Add(new Tag { Key = curr.Key, Value = curr.Value });
                }
            }

            return tags;
        }

        internal async Task Write(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            DateTime ts = DateTime.UtcNow;

            Obj obj = new Obj();
            obj.Id = Less3.Helpers.IdGenerator.GenerateObjectId();
            obj.TenantId = md.Bucket.TenantId;
            obj.AuthorId = RequesterId(md, ctx);
            obj.OwnerId = RequesterId(md, ctx);
            obj.BlobFilename = obj.Id;
            obj.Key = ctx.Request.Key;
            obj.ContentType = RequestContentType(ctx);
            obj.CreatedUtc = ts;
            obj.LastAccessUtc = ts;
            obj.LastUpdateUtc = ts;
            obj.Metadata = SerializeMetadata(ExtractMetadataFromHeaders(ctx.Http.Request.Headers));

            // Validate ACL and tagging headers before storing anything, so a bad grant or tag set is
            // rejected without leaving an object behind.
            List<ObjectAcl> acls = AclsFromHeaders(md, ctx, obj.Id, header);
            List<ObjectTag> tags = TagsFromHeader(ctx, header);

            string tempFilename = _Settings.Storage.TempDirectory + Less3.Helpers.IdGenerator.GenerateObjectId();

            try
            {
                obj.ContentLength = await ReceiveBodyToFile(ctx, tempFilename, header).ConfigureAwait(false);
                if (obj.ContentLength == 0 && obj.Key.EndsWith("/")) obj.IsFolder = true;

                using (FileStream fs = new FileStream(tempFilename, FileMode.Open, FileAccess.Read))
                {
                    md.BucketClient.AddObject(obj, fs, WritePreconditions(ctx), acls, tags);
                }
            }
            catch (ObjectPreconditionException ope)
            {
                _Logging.Warn(header + ope.Message);
                throw new S3Exception(new Error(ope.ObjectMissing ? ErrorCode.NoSuchKey : ErrorCode.PreconditionFailed));
            }
            catch (S3Exception)
            {
                throw;
            }
            catch (Exception e)
            {
                _Logging.Warn(header + "failure while writing " + ctx.Request.Bucket + "/" + ctx.Request.Key + " using tempfile " + tempFilename);
                _Logging.Exception(e, "ObjectHandler", "Write");
                throw new S3Exception(new Error(ErrorCode.InternalError), e);
            }
            finally
            {
                if (File.Exists(tempFilename)) File.Delete(tempFilename);
            }


            ctx.Response.Headers.Add("ETag", "\"" + obj.Etag + "\"");
            AddVersionHeader(md, obj, ctx);
        }

        internal async Task WriteAcl(S3Context ctx, AccessControlPolicy acp)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
            RequestValidator.ValidateAuthentication(md, _Logging, header);

            ObjectVersionReference reference = ParseVersionReference(ctx, header);
            Obj obj = RequireLiveObject(md, ctx, reference, header);

            List<ObjectAcl> acls = AclConverter.PolicyToObjectAcls(
                acp,
                ctx.Http.Request.Headers,
                md.User,
                md.Bucket.Id,
                obj.Id,
                md.Bucket.OwnerId,
                _Config,
                _Logging,
                header);

            if (!md.BucketClient.SetObjectAcls(obj, acls)) throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            AddVersionHeader(md, obj, ctx);
        }

        internal async Task WriteTagging(S3Context ctx, Tagging tagging)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            if (S3TagValidator.IsInvalid(tagging))
            {
                _Logging.Warn(header + "invalid object tag set");
                throw new S3Exception(new Error(ErrorCode.InvalidRequest));
            }

            ObjectVersionReference reference = ParseVersionReference(ctx, header);
            Obj obj = RequireLiveObject(md, ctx, reference, header);

            List<ObjectTag> tags = new List<ObjectTag>();
            if (tagging.Tags != null && tagging.Tags.Tags != null)
            {
                foreach (Tag curr in tagging.Tags.Tags)
                {
                    tags.Add(new ObjectTag { Key = curr.Key, Value = curr.Value });
                }
            }

            if (!md.BucketClient.SetObjectTags(obj, tags)) throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            AddVersionHeader(md, obj, ctx);
        }

        internal async Task<InitiateMultipartUploadResult> CreateMultipartUpload(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            DateTime ts = DateTime.UtcNow;

            Less3.Classes.Upload upload = new Less3.Classes.Upload();
            upload.TenantId = md.Bucket.TenantId;
            upload.Id = Less3.Helpers.IdGenerator.GenerateUploadId();
            upload.BucketId = md.Bucket.Id;
            upload.Key = ctx.Request.Key;
            upload.CreatedUtc = ts;
            upload.LastAccessUtc = ts;
            upload.ExpirationUtc = ts.AddSeconds(60 * 60 * 24 * 7);
            upload.OwnerId = RequesterId(md, ctx);
            upload.AuthorId = RequesterId(md, ctx);
            upload.ContentType = RequestContentType(ctx);
            upload.Metadata = SerializeMetadata(ExtractMetadataFromHeaders(ctx.Http.Request.Headers));

            _Config.AddUpload(upload);

            _Logging.Info(header + "initiated multipart upload " + upload.Id + " for key " + ctx.Request.Bucket + "/" + ctx.Request.Key);

            InitiateMultipartUploadResult result = new InitiateMultipartUploadResult();
            result.Bucket = ctx.Request.Bucket;
            result.Key = ctx.Request.Key;
            result.UploadId = upload.Id;

            return result;
        }

        internal async Task<CompleteMultipartUploadResult> CompleteMultipartUpload(S3Context ctx, CompleteMultipartUpload upload)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
            RequestValidator.ValidateUploadId(ctx, _Logging, header);

            Less3.Classes.Upload uploadRecord = _Config.GetUploadById(md.Bucket.TenantId, ctx.Request.UploadId);
            RequestValidator.ValidateUpload(uploadRecord, ctx.Request.UploadId, _Logging, header);

            // Serialize completion and abort of this upload across all nodes. Any node can assemble
            // the object from parts on shared storage, but only one at a time; a duplicate complete
            // re-reads the (now absent) upload under the lock and fails cleanly, yielding one object.
            LockHandle uploadLock = _LockManager.AcquireAsync(LockKeys.Upload(ctx.Request.UploadId), LockMode.Write, new AcquireOptions(_Settings.Cluster.Lock.AcquireTimeoutMs), CancellationToken.None).GetAwaiter().GetResult();
            try
            {
                uploadRecord = _Config.GetUploadById(md.Bucket.TenantId, ctx.Request.UploadId);
                RequestValidator.ValidateUpload(uploadRecord, ctx.Request.UploadId, _Logging, header);

                if (!String.Equals(uploadRecord.Key, ctx.Request.Key, StringComparison.Ordinal))
                {
                    _Logging.Warn(header + "upload " + ctx.Request.UploadId + " belongs to a different key");
                    throw new S3Exception(new Error(ErrorCode.NoSuchUpload));
                }

                if (upload == null || upload.Parts == null || upload.Parts.Count < 1)
                {
                    _Logging.Warn(header + "complete multipart upload request did not include any parts");
                    throw new S3Exception(new Error(ErrorCode.MalformedXML));
                }

                List<UploadPart> storedParts = _Config.GetUploadPartsByUploadId(md.Bucket.TenantId, ctx.Request.UploadId);
                if (storedParts == null || storedParts.Count == 0)
                {
                    _Logging.Warn(header + "no parts found for upload " + ctx.Request.UploadId);
                    throw new S3Exception(new Error(ErrorCode.InvalidPart));
                }

                List<UploadPart> availableParts = GetLatestUploadPartsByNumber(storedParts);
                Dictionary<int, UploadPart> availablePartsByNumber = availableParts.ToDictionary(p => p.PartNumber);
                List<UploadPart> selectedParts = new List<UploadPart>();
                int previousPartNumber = 0;

                foreach (Part requestedPart in upload.Parts)
                {
                    if (requestedPart == null)
                    {
                        _Logging.Warn(header + "complete multipart upload request contained a null part element");
                        throw new S3Exception(new Error(ErrorCode.InvalidPart));
                    }

                    if (requestedPart.PartNumber <= previousPartNumber)
                    {
                        _Logging.Warn(header + "parts were not supplied in ascending order for upload " + ctx.Request.UploadId);
                        throw new S3Exception(new Error(ErrorCode.InvalidPartOrder));
                    }

                    if (!availablePartsByNumber.TryGetValue(requestedPart.PartNumber, out UploadPart storedPart))
                    {
                        _Logging.Warn(header + "requested part number " + requestedPart.PartNumber + " not found for upload " + ctx.Request.UploadId);
                        throw new S3Exception(new Error(ErrorCode.InvalidPart));
                    }

                    if (!String.Equals(NormalizeEtag(requestedPart.ETag), NormalizeEtag(storedPart.MD5Hash), StringComparison.OrdinalIgnoreCase))
                    {
                        _Logging.Warn(header + "etag mismatch for requested part number " + requestedPart.PartNumber + " in upload " + ctx.Request.UploadId);
                        throw new S3Exception(new Error(ErrorCode.InvalidPart));
                    }

                    selectedParts.Add(storedPart);
                    previousPartNumber = requestedPart.PartNumber;
                }

                DateTime ts = DateTime.UtcNow;

                Obj obj = new Obj();
                obj.TenantId = md.Bucket.TenantId;
                obj.AuthorId = RequesterId(md, ctx);
                obj.OwnerId = RequesterId(md, ctx);
                obj.Id = Less3.Helpers.IdGenerator.GenerateObjectId();
                obj.BlobFilename = obj.Id;
                obj.Key = ctx.Request.Key;
                obj.ContentType = uploadRecord.ContentType;
                obj.Metadata = uploadRecord.Metadata;
                obj.CreatedUtc = ts;
                obj.LastAccessUtc = ts;
                obj.LastUpdateUtc = ts;

                string tempFilename = _Settings.Storage.TempDirectory + Less3.Helpers.IdGenerator.GenerateObjectId();
                long totalLength = 0;

                string multipartEtag = ComputeMultipartEtag(selectedParts);
                obj.Etag = multipartEtag;

                try
                {
                    using (FileStream outStream = new FileStream(tempFilename, FileMode.Create, FileAccess.Write))
                    {
                        foreach (UploadPart part in selectedParts)
                        {
                            string partFile = GetPartFilePath(md.Bucket.Id, ctx.Request.UploadId, part.PartNumber);
                            if (!File.Exists(partFile))
                            {
                                _Logging.Warn(header + "part file " + partFile + " not found for part " + part.PartNumber);
                                throw new S3Exception(new Error(ErrorCode.InvalidPart));
                            }

                            using (FileStream inStream = new FileStream(partFile, FileMode.Open, FileAccess.Read))
                            {
                                byte[] buffer = new byte[65536];
                                int bytesRead = 0;

                                while ((bytesRead = await inStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                                {
                                    await outStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                                    totalLength += bytesRead;
                                }
                            }
                        }
                    }

                    obj.ContentLength = totalLength;

                    using (FileStream fs = new FileStream(tempFilename, FileMode.Open, FileAccess.Read))
                    {
                        md.BucketClient.AddObject(obj, fs);
                    }
                }
                catch (S3Exception)
                {
                    throw;
                }
                catch (Exception e)
                {
                    _Logging.Warn(header + "failure while completing multipart upload " + ctx.Request.UploadId);
                    _Logging.Exception(e, "ObjectHandler", "CompleteMultipartUpload");
                    throw new S3Exception(new Error(ErrorCode.InternalError), e);
                }
                finally
                {
                    if (File.Exists(tempFilename)) File.Delete(tempFilename);
                }

                foreach (UploadPart part in availableParts)
                {
                    string partFile = GetPartFilePath(md.Bucket.Id, ctx.Request.UploadId, part.PartNumber);
                    if (File.Exists(partFile)) File.Delete(partFile);
                }

                _Config.DeleteUploadParts(md.Bucket.TenantId, ctx.Request.UploadId);
                _Config.DeleteUpload(md.Bucket.TenantId, ctx.Request.UploadId);

                _Logging.Info(header + "completed multipart upload " + ctx.Request.UploadId + " for key " + ctx.Request.Bucket + "/" + ctx.Request.Key);
                Less3Telemetry.MultipartCompleted();

                CompleteMultipartUploadResult result = new CompleteMultipartUploadResult();
                result.Location = RequestScheme(ctx) + "://" + ctx.Http.Request.Headers["Host"] + "/" + ctx.Request.Bucket + "/" + ctx.Request.Key;
                result.Bucket = ctx.Request.Bucket;
                result.Key = ctx.Request.Key;
                result.ETag = multipartEtag;

                ctx.Response.Headers.Add("ETag", "\"" + multipartEtag + "\"");
                AddVersionHeader(md, obj, ctx);

                return result;
            }
            finally
            {
                _LockManager.ReleaseAsync(uploadLock, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        internal async Task UploadPart(S3Context ctx)
        {
            await StorePart(ctx, false).ConfigureAwait(false);
        }

        internal async Task<CopyPartResult> UploadPartCopy(S3Context ctx)
        {
            return await StorePart(ctx, true).ConfigureAwait(false);
        }

        private async Task<CopyPartResult> StorePart(S3Context ctx, bool isCopy)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
            RequestValidator.ValidateUploadId(ctx, _Logging, header);
            RequestValidator.ValidatePartNumber(ctx.Request.PartNumber, _Logging, header);

            Less3.Classes.Upload uploadRecord = _Config.GetUploadById(md.Bucket.TenantId, ctx.Request.UploadId);
            RequestValidator.ValidateUpload(uploadRecord, ctx.Request.UploadId, _Logging, header);

            string partFile = GetPartFilePath(md.Bucket.Id, ctx.Request.UploadId, ctx.Request.PartNumber);
            string tempPartFile = partFile + ".tmp-" + Less3.Helpers.IdGenerator.GenerateUploadPartId();
            Obj copySource = null;
            BucketClient copySourceClient = null;

            try
            {
                long partLength;
                if (isCopy)
                {
                    copySource = ResolveCopySource(ctx, md, header, out copySourceClient, out bool _);
                    long start = 0;
                    long length = copySource.ContentLength;
                    string range = GetHeader(ctx, "x-amz-copy-source-range");
                    if (!String.IsNullOrEmpty(range)) ParseCopySourceRange(range, copySource.ContentLength, out start, out length, header);

                    CheckCopySourceConditions(ctx, copySource, header);
                    await CopyObjectToFile(ctx, copySourceClient, copySource, false, new ObjectByteRange(start, length), tempPartFile, header).ConfigureAwait(false);
                    partLength = length;
                }
                else
                {
                    partLength = await ReceiveBodyToFile(ctx, tempPartFile, header).ConfigureAwait(false);
                }

                if (partLength > Int32.MaxValue)
                {
                    _Logging.Warn(header + "part " + ctx.Request.PartNumber + " exceeded the supported persisted size");
                    throw new S3Exception(new Error(ErrorCode.EntityTooLarge));
                }

                HashResult hashes = HashFile(tempPartFile);

                // Replace atomically so a concurrent CompleteMultipartUpload never finds the part missing.
                File.Move(tempPartFile, partFile, true);
                _Config.DeleteUploadPart(md.Bucket.TenantId, ctx.Request.UploadId, ctx.Request.PartNumber);

                UploadPart part = new UploadPart();
                part.TenantId = md.Bucket.TenantId;
                part.Id = Less3.Helpers.IdGenerator.GenerateUploadPartId();
                part.BucketId = md.Bucket.Id;
                part.UploadId = ctx.Request.UploadId;
                part.PartNumber = ctx.Request.PartNumber;
                part.PartLength = (int)partLength;
                part.MD5Hash = hashes.MD5;
                part.Sha1Hash = hashes.SHA1;
                part.Sha256Hash = hashes.SHA256;
                part.CreatedUtc = DateTime.UtcNow;
                part.LastAccessUtc = DateTime.UtcNow;
                part.OwnerId = md.User != null ? md.User.Id : RequesterId(md, ctx);

                _Config.AddUploadPart(part);
                Less3Telemetry.PartUploaded();

                ctx.Response.Headers.Add("ETag", "\"" + hashes.MD5 + "\"");
                _Logging.Info(header + (isCopy ? "copied" : "uploaded") + " part " + ctx.Request.PartNumber + " for upload " + ctx.Request.UploadId);

                if (!isCopy) return null;

                if (copySourceClient.VersioningConfigured)
                    ctx.Response.Headers.Add("x-amz-copy-source-version-id", copySourceClient.VersionIdString(copySource));

                return new CopyPartResult(hashes.MD5, UtcTimestamp.AsUtc(part.CreatedUtc));
            }
            catch (S3Exception)
            {
                if (File.Exists(tempPartFile)) File.Delete(tempPartFile);
                throw;
            }
            catch (Exception e)
            {
                _Logging.Warn(header + "failure while uploading part " + ctx.Request.PartNumber + " for upload " + ctx.Request.UploadId);
                _Logging.Exception(e, "ObjectHandler", "UploadPart");
                if (File.Exists(tempPartFile)) File.Delete(tempPartFile);
                throw new S3Exception(new Error(ErrorCode.InternalError), e);
            }
        }

        internal async Task AbortMultipartUpload(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
            RequestValidator.ValidateUploadId(ctx, _Logging, header);

            Less3.Classes.Upload uploadRecord = _Config.GetUploadById(md.Bucket.TenantId, ctx.Request.UploadId);
            RequestValidator.ValidateUpload(uploadRecord, ctx.Request.UploadId, _Logging, header);

            LockHandle uploadLock = _LockManager.AcquireAsync(LockKeys.Upload(ctx.Request.UploadId), LockMode.Write, new AcquireOptions(_Settings.Cluster.Lock.AcquireTimeoutMs), CancellationToken.None).GetAwaiter().GetResult();
            try
            {
                List<UploadPart> parts = GetLatestUploadPartsByNumber(_Config.GetUploadPartsByUploadId(md.Bucket.TenantId, ctx.Request.UploadId));
                foreach (UploadPart part in parts)
                {
                    string partFile = GetPartFilePath(md.Bucket.Id, ctx.Request.UploadId, part.PartNumber);
                    if (File.Exists(partFile)) File.Delete(partFile);
                }

                _Config.DeleteUploadParts(md.Bucket.TenantId, ctx.Request.UploadId);
                _Config.DeleteUpload(md.Bucket.TenantId, ctx.Request.UploadId);

                _Logging.Info(header + "aborted multipart upload " + ctx.Request.UploadId);
                Less3Telemetry.MultipartAborted();
            }
            finally
            {
                _LockManager.ReleaseAsync(uploadLock, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        internal async Task<ListPartsResult> ReadParts(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
            RequestValidator.ValidateUploadId(ctx, _Logging, header);

            Less3.Classes.Upload uploadRecord = _Config.GetUploadById(md.Bucket.TenantId, ctx.Request.UploadId);
            RequestValidator.ValidateUpload(uploadRecord, ctx.Request.UploadId, _Logging, header);

            int maxParts = ParseNonNegativeQueryInt(ctx, "max-parts", 1000, header);
            if (maxParts > 1000) maxParts = 1000;
            int partNumberMarker = ParseNonNegativeQueryInt(ctx, "part-number-marker", 0, header);

            List<UploadPart> parts = GetLatestUploadPartsByNumber(_Config.GetUploadPartsByUploadId(md.Bucket.TenantId, ctx.Request.UploadId))
                .Where(p => p.PartNumber > partNumberMarker)
                .ToList();

            ListPartsResult result = new ListPartsResult();
            result.Bucket = ctx.Request.Bucket;
            result.Key = ctx.Request.Key;
            result.UploadId = ctx.Request.UploadId;
            result.Initiator = new Owner();
            result.Initiator.ID = uploadRecord.OwnerId;
            result.Owner = new Owner();
            result.Owner.ID = uploadRecord.OwnerId;
            result.MaxParts = maxParts;
            result.PartNumberMarker = partNumberMarker;

            User owner = _Config.GetUserById(uploadRecord.OwnerId);
            if (owner != null)
            {
                result.Owner.DisplayName = owner.Name;
                result.Initiator.DisplayName = owner.Name;
            }

            result.StorageClass = S3ServerLibrary.S3Objects.StorageClassEnum.STANDARD;
            result.Parts = new List<Part>();

            foreach (UploadPart uploadPart in parts.Take(maxParts))
            {
                Part part = new Part();
                part.PartNumber = uploadPart.PartNumber;
                part.LastModified = uploadPart.LastAccessUtc;
                part.ETag = "\"" + uploadPart.MD5Hash + "\"";
                part.Size = uploadPart.PartLength;
                result.Parts.Add(part);
            }

            result.IsTruncated = parts.Count > maxParts;
            if (result.IsTruncated && result.Parts.Count > 0)
            {
                result.NextPartNumberMarker = result.Parts[result.Parts.Count - 1].PartNumber;
            }

            _Logging.Debug(header + "listed " + result.Parts.Count + " parts for upload " + ctx.Request.UploadId);

            return result;
        }

        #endregion

        #region Private-Methods

        private static string Header(S3Context ctx)
        {
            return "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";
        }

        private static string GetHeader(S3Context ctx, string name)
        {
            if (ctx.Http.Request.Headers == null) return null;
            return ctx.Http.Request.Headers[name];
        }

        private static string RequesterId(RequestMetadata md, S3Context ctx)
        {
            if (md != null && md.User != null) return md.User.Id;
            return ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port;
        }

        private static string RequestScheme(S3Context ctx)
        {
            string forwarded = GetHeader(ctx, "x-forwarded-proto");
            if (!String.IsNullOrEmpty(forwarded)) return forwarded.Split(',')[0].Trim();
            return ctx.Http.Request.Url.Full.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
        }

        private ObjectVersionReference ParseVersionReference(S3Context ctx, string header)
        {
            if (!ObjectVersionReference.TryParse(ctx.Request.VersionId, out ObjectVersionReference reference))
            {
                _Logging.Warn(header + "invalid version ID " + ctx.Request.VersionId);
                throw new S3Exception(new Error(ErrorCode.InvalidArgument));
            }

            return reference;
        }

        private Obj RequireLiveObject(RequestMetadata md, S3Context ctx, ObjectVersionReference reference, string header)
        {
            Obj obj = md.Obj;

            if (obj == null)
            {
                _Logging.Debug(header + "no such " + (reference.IsLatest ? "key " : "version " + reference.ToString() + " of ") + ctx.Request.Key);
                throw new S3Exception(new Error(reference.IsLatest ? ErrorCode.NoSuchKey : ErrorCode.NoSuchVersion));
            }

            if (obj.DeleteMarker)
            {
                ctx.Response.Headers.Add(Constants.Headers.DeleteMarker, "true");
                if (md.BucketClient.VersioningConfigured) ctx.Response.Headers.Add("x-amz-version-id", md.BucketClient.VersionIdString(obj));

                // Amazon S3 answers a request for the current version with 404 when it is a delete marker,
                // and a request that names a delete marker's version ID with 405.
                if (reference.IsLatest) throw new S3Exception(new Error(ErrorCode.NoSuchKey));
                throw new S3Exception(new Error(ErrorCode.MethodNotAllowed));
            }

            return obj;
        }

        private static void AddVersionHeader(RequestMetadata md, Obj obj, S3Context ctx)
        {
            if (md.BucketClient.VersioningConfigured)
                ctx.Response.Headers.Add("x-amz-version-id", md.BucketClient.VersionIdString(obj));
        }

        private static bool IsLatest(RequestMetadata md, Obj obj)
        {
            return md.BucketClient.GetObjectLatestVersion(obj.Key) == obj.Version;
        }

        private static string EtagOf(Obj obj)
        {
            return obj.Etag ?? obj.Md5;
        }

        private static Error KeyError(ErrorCode code, string key, string versionId)
        {
            Error error = new Error(code);
            error.Key = key;
            error.VersionId = versionId;
            return error;
        }

        private void CheckConditionalRequest(S3Context ctx, Obj obj)
        {
            ConditionalRequestOutcome outcome = ConditionalRequestEvaluator.Evaluate(ctx.Http.Request.Headers, "", EtagOf(obj), obj.LastUpdateUtc);
            if (outcome == ConditionalRequestOutcome.PreconditionFailed)
                throw new S3Exception(new Error(ErrorCode.PreconditionFailed));

            if (outcome == ConditionalRequestOutcome.NotModified)
            {
                // S3Server sends NotModified as 304 with headers only, keeping the headers added here.
                ctx.Response.Headers.Add("ETag", "\"" + EtagOf(obj) + "\"");
                ctx.Response.Headers.Add("Last-Modified", UtcTimestamp.AsUtc(obj.LastUpdateUtc).ToString("r", CultureInfo.InvariantCulture));
                throw new S3Exception(new Error(ErrorCode.NotModified));
            }
        }

        private ObjectByteRange SelectRange(long length, long? start, long? end, long? suffixLength, string header)
        {
            if (suffixLength != null)
            {
                // Amazon S3 answers a suffix range on an empty object with 200 and an empty body.
                if (length == 0) return new ObjectByteRange(0, 0);
                if (suffixLength.Value < 1) throw RangeNotSatisfiable("bytes=-" + suffixLength.Value, length, header);

                long first = Math.Max(0, length - suffixLength.Value);
                return new ObjectByteRange(first, length - first);
            }

            long from = start ?? 0;
            long to = end ?? (length - 1);

            // A first byte at or beyond the end of the object cannot be satisfied (416).
            if (from >= length || to < from) throw RangeNotSatisfiable("bytes=" + from + "-" + (end?.ToString() ?? ""), length, header);

            // A last byte beyond the end is clamped to the last byte of the object, as in Amazon S3.
            if (to >= length) to = length - 1;
            return new ObjectByteRange(from, to - from + 1);
        }

        private S3Exception RangeNotSatisfiable(string range, long length, string header)
        {
            _Logging.Warn(header + "unsatisfiable range " + range + " on object of length " + length);
            Error error = new Error(ErrorCode.InvalidRange);
            error.ActualObjectSize = length;
            return new S3Exception(error);
        }

        private ObjectReadResult OpenForRead(S3Context ctx, RequestMetadata md, Obj obj, ObjectVersionReference reference, Func<Obj, ObjectByteRange> selectRange)
        {
            // Conditions were evaluated against the row resolved before the read lock. If a different row
            // is served (the key was overwritten in between), re-check If-Match / If-Unmodified-Since against
            // it, so a resumed download never splices bytes of a different version. A NotModified outcome for
            // the new row just serves it in full, which is always correct.
            Func<Obj, ObjectByteRange> guarded = served =>
            {
                if (!String.Equals(served.Id, obj.Id, StringComparison.Ordinal)
                    && ConditionalRequestEvaluator.Evaluate(ctx.Http.Request.Headers, "", EtagOf(served), served.LastUpdateUtc) == ConditionalRequestOutcome.PreconditionFailed)
                {
                    throw new S3Exception(new Error(ErrorCode.PreconditionFailed));
                }

                return selectRange != null ? selectRange(served) : new ObjectByteRange(0, served.ContentLength);
            };

            ObjectReadResult read = md.BucketClient.OpenObject(obj, reference.IsLatest, guarded);
            if (read == null) throw new S3Exception(new Error(reference.IsLatest ? ErrorCode.NoSuchKey : ErrorCode.NoSuchVersion));
            return read;
        }

        private static string RequestContentType(S3Context ctx)
        {
            string contentType = ctx.Http.Request.ContentType;
            if (String.IsNullOrWhiteSpace(contentType)) return "binary/octet-stream";
            return contentType;
        }

        private static string ResponseContentType(S3Context ctx, Obj obj)
        {
            string overrideValue = ctx.Request.RetrieveQueryValue("response-content-type");
            if (!String.IsNullOrEmpty(overrideValue)) return overrideValue;
            if (String.IsNullOrEmpty(obj.ContentType)) return "binary/octet-stream";
            return obj.ContentType;
        }

        private ObjectWritePreconditions WritePreconditions(S3Context ctx)
        {
            ObjectWritePreconditions preconditions = new ObjectWritePreconditions();
            preconditions.IfMatch = GetHeader(ctx, "if-match");
            preconditions.IfNoneMatch = GetHeader(ctx, "if-none-match");

            if (!String.IsNullOrEmpty(preconditions.IfNoneMatch) && preconditions.IfNoneMatch.Trim() != "*")
            {
                // Amazon S3 only supports If-None-Match: * on writes.
                throw new S3Exception(new Error(ErrorCode.NotImplemented));
            }

            return preconditions.Any ? preconditions : null;
        }

        private async Task<long> ReceiveBodyToFile(S3Context ctx, string filename, string header)
        {
            long expected = ExpectedContentLength(ctx);
            long total = 0;
            string contentMd5 = GetHeader(ctx, "content-md5");

            using (IncrementalHash md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                if (ctx.Request.Chunked)
                {
                    while (true)
                    {
                        Chunk chunk = await ctx.Request.ReadChunk().ConfigureAwait(false);
                        if (chunk == null) break;

                        if (chunk.Data != null && chunk.Data.Length > 0)
                        {
                            await fs.WriteAsync(chunk.Data, 0, chunk.Data.Length).ConfigureAwait(false);
                            md5.AppendData(chunk.Data, 0, chunk.Data.Length);
                            total += chunk.Data.Length;
                        }

                        if (chunk.IsFinal) break;
                    }
                }
                else if (ctx.Http.Request.ContentLength > 0)
                {
                    if (BodyMayBeBuffered(ctx))
                    {
                        // Signature validation hashes a signed payload, which reads the body into memory;
                        // use that copy rather than the (already consumed) stream.
                        byte[] bodyBytes = ctx.Request.DataAsBytes;
                        if (bodyBytes != null && bodyBytes.Length > 0)
                        {
                            await fs.WriteAsync(bodyBytes, 0, bodyBytes.Length).ConfigureAwait(false);
                            md5.AppendData(bodyBytes, 0, bodyBytes.Length);
                            total = bodyBytes.Length;
                        }
                    }
                    else
                    {
                        // Stream the body to disk so objects are not limited by memory or array size.
                        Stream body = ctx.Http.Request.Data;
                        byte[] buffer = new byte[65536];
                        long remaining = ctx.Http.Request.ContentLength;

                        while (remaining > 0)
                        {
                            int read = await body.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, remaining)).ConfigureAwait(false);
                            if (read <= 0) break;
                            await fs.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                            md5.AppendData(buffer, 0, read);
                            total += read;
                            remaining -= read;
                        }
                    }
                }

                if (expected >= 0 && total != expected)
                {
                    _Logging.Warn(header + "request body length " + total + " does not match declared length " + expected);
                    throw new S3Exception(new Error(ErrorCode.IncompleteBody));
                }

                if (!String.IsNullOrEmpty(contentMd5))
                {
                    byte[] supplied;
                    try
                    {
                        supplied = Convert.FromBase64String(contentMd5.Trim());
                    }
                    catch (FormatException)
                    {
                        supplied = null;
                    }

                    if (supplied == null || supplied.Length != 16)
                    {
                        _Logging.Warn(header + "invalid Content-MD5 header");
                        throw new S3Exception(new Error(ErrorCode.InvalidDigest));
                    }

                    byte[] computed = md5.GetHashAndReset();
                    if (!CryptographicOperations.FixedTimeEquals(supplied, computed))
                    {
                        _Logging.Warn(header + "Content-MD5 does not match the request body");
                        throw new S3Exception(new Error(ErrorCode.BadDigest));
                    }
                }
            }

            return total;
        }

        private static long ExpectedContentLength(S3Context ctx)
        {
            string decoded = GetHeader(ctx, "x-amz-decoded-content-length");
            if (!String.IsNullOrEmpty(decoded) && Int64.TryParse(decoded, NumberStyles.None, CultureInfo.InvariantCulture, out long decodedLength)) return decodedLength;
            if (ctx.Request.Chunked) return -1;
            return ctx.Http.Request.ContentLength;
        }

        private bool BodyMayBeBuffered(S3Context ctx)
        {
            if (!_Settings.ValidateSignatures) return false;
            string contentSha256 = GetHeader(ctx, "x-amz-content-sha256");
            if (String.Equals(contentSha256, "UNSIGNED-PAYLOAD", StringComparison.Ordinal)) return false;
            return true;
        }

        /// <summary>
        /// CopyObject (PUT with x-amz-copy-source). The caller must be allowed to write the destination and
        /// read the source.
        /// </summary>
        internal async Task<CopyObjectResult> Copy(S3Context ctx)
        {
            string header = Header(ctx);

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            Obj source = ResolveCopySource(ctx, md, header, out BucketClient sourceClient, out bool sourceIsLatest);

            // Fail fast on the row resolved so far; the conditions are checked again on the row actually copied.
            CheckCopySourceConditions(ctx, source, header);

            string metadataDirective = (GetHeader(ctx, "x-amz-metadata-directive") ?? "COPY").Trim().ToUpperInvariant();
            string taggingDirective = (GetHeader(ctx, "x-amz-tagging-directive") ?? "COPY").Trim().ToUpperInvariant();
            if ((metadataDirective != "COPY" && metadataDirective != "REPLACE") || (taggingDirective != "COPY" && taggingDirective != "REPLACE"))
                throw new S3Exception(new Error(ErrorCode.InvalidArgument));

            bool sameObject = sourceClient.Id == md.BucketClient.Id && String.Equals(source.Key, ctx.Request.Key, StringComparison.Ordinal);
            if (sameObject && metadataDirective == "COPY" && !md.BucketClient.VersioningConfigured)
            {
                // Amazon S3 rejects copying an object onto itself without changing anything.
                _Logging.Warn(header + "copy of an object onto itself without changing metadata");
                throw new S3Exception(new Error(ErrorCode.InvalidRequest));
            }

            DateTime ts = DateTime.UtcNow;
            Obj obj = new Obj();
            obj.Id = Less3.Helpers.IdGenerator.GenerateObjectId();
            obj.TenantId = md.Bucket.TenantId;
            obj.AuthorId = RequesterId(md, ctx);
            obj.OwnerId = RequesterId(md, ctx);
            obj.BlobFilename = obj.Id;
            obj.Key = ctx.Request.Key;
            obj.CreatedUtc = ts;
            obj.LastAccessUtc = ts;
            obj.LastUpdateUtc = ts;

            if (metadataDirective == "REPLACE")
            {
                obj.ContentType = RequestContentType(ctx);
                obj.Metadata = SerializeMetadata(ExtractMetadataFromHeaders(ctx.Http.Request.Headers));
            }

            List<ObjectAcl> acls = AclsFromHeaders(md, ctx, obj.Id, header);
            List<ObjectTag> tags = taggingDirective == "REPLACE" ? (TagsFromHeader(ctx, header) ?? new List<ObjectTag>()) : null;

            // Stage the source content in a temporary file under the source's read lock, then release
            // it before taking the destination's write lock. Holding both would deadlock when the
            // source and destination are the same key.
            string tempFilename = _Settings.Storage.TempDirectory + Less3.Helpers.IdGenerator.GenerateObjectId();
            try
            {
                Obj copied = await CopyObjectToFile(ctx, sourceClient, source, sourceIsLatest, null, tempFilename, header).ConfigureAwait(false);
                obj.ContentLength = copied.ContentLength;
                if (obj.ContentLength == 0 && obj.Key.EndsWith("/")) obj.IsFolder = true;

                // With the COPY directives, metadata and tags come from the exact row whose bytes were copied.
                if (metadataDirective == "COPY")
                {
                    obj.ContentType = copied.ContentType;
                    obj.Metadata = copied.Metadata;
                }

                if (tags == null)
                {
                    tags = sourceClient.GetObjectTags(copied.Id)
                        .Select(t => new ObjectTag { Key = t.Key, Value = t.Value })
                        .ToList();
                }

                using (FileStream fs = new FileStream(tempFilename, FileMode.Open, FileAccess.Read))
                {
                    md.BucketClient.AddObject(obj, fs, WritePreconditions(ctx), acls, tags);
                }
            }
            catch (ObjectPreconditionException ope)
            {
                _Logging.Warn(header + ope.Message);
                throw new S3Exception(new Error(ope.ObjectMissing ? ErrorCode.NoSuchKey : ErrorCode.PreconditionFailed));
            }
            finally
            {
                if (File.Exists(tempFilename)) File.Delete(tempFilename);
            }


            _Logging.Info(header + "copied " + sourceClient.Name + "/" + source.Key + " to " + md.Bucket.Name + "/" + obj.Key);

            AddVersionHeader(md, obj, ctx);
            if (sourceClient.VersioningConfigured) ctx.Response.Headers.Add("x-amz-copy-source-version-id", sourceClient.VersionIdString(source));

            return new CopyObjectResult(obj.Etag, UtcTimestamp.AsUtc(obj.LastUpdateUtc));
        }

        private Obj ResolveCopySource(S3Context ctx, RequestMetadata md, string header, out BucketClient sourceClient, out bool isLatestReference)
        {
            sourceClient = null;
            isLatestReference = true;
            string copySource = GetHeader(ctx, "x-amz-copy-source");

            if (!TryParseCopySource(copySource, out string sourceBucketName, out string sourceKey, out string sourceVersionId))
            {
                _Logging.Warn(header + "invalid x-amz-copy-source " + copySource);
                throw new S3Exception(new Error(ErrorCode.InvalidArgument));
            }

            if (!ObjectVersionReference.TryParse(sourceVersionId, out ObjectVersionReference reference))
            {
                _Logging.Warn(header + "invalid copy source version ID " + sourceVersionId);
                throw new S3Exception(new Error(ErrorCode.InvalidArgument));
            }

            isLatestReference = reference.IsLatest;
            string tenantId = md.Bucket.TenantId;
            Classes.Bucket sourceBucket = _Buckets.GetByName(tenantId, sourceBucketName);
            sourceClient = sourceBucket != null ? _Buckets.GetClient(tenantId, sourceBucketName) : null;
            if (sourceBucket == null || sourceClient == null) throw new S3Exception(new Error(ErrorCode.NoSuchBucket));

            Obj source = sourceClient.ResolveObject(sourceKey, reference);

            // The caller must be allowed to read the source, exactly as a GetObject on it would require.
            RequestMetadata sourceMd = _Auth.ForObject(md, sourceBucket, sourceClient, source);
            sourceMd = _Auth.AuthorizeObjectRequest(ctx, sourceMd, S3RequestType.ObjectRead);
            if (sourceMd.Authorization == AuthorizationResult.NotAuthorized)
            {
                _Logging.Warn(header + "not authorized to read copy source " + sourceBucketName + "/" + sourceKey);
                throw new S3Exception(new Error(ErrorCode.AccessDenied));
            }

            if (source == null) throw new S3Exception(new Error(reference.IsLatest ? ErrorCode.NoSuchKey : ErrorCode.NoSuchVersion));

            if (source.DeleteMarker)
            {
                // A delete marker has no content to copy.
                throw new S3Exception(new Error(reference.IsLatest ? ErrorCode.NoSuchKey : ErrorCode.InvalidRequest));
            }

            return source;
        }

        private static bool TryParseCopySource(string value, out string bucket, out string key, out string versionId)
        {
            bucket = null;
            key = null;
            versionId = null;
            if (String.IsNullOrWhiteSpace(value)) return false;

            string source = value.Trim();
            int queryIndex = source.IndexOf('?');
            if (queryIndex >= 0)
            {
                string query = source.Substring(queryIndex + 1);
                source = source.Substring(0, queryIndex);

                foreach (string pair in query.Split('&'))
                {
                    if (pair.StartsWith("versionId=", StringComparison.Ordinal))
                        versionId = Uri.UnescapeDataString(pair.Substring("versionId=".Length));
                }
            }

            source = Uri.UnescapeDataString(source).TrimStart('/');
            int slash = source.IndexOf('/');
            if (slash < 1 || slash == source.Length - 1) return false;

            bucket = source.Substring(0, slash);
            key = source.Substring(slash + 1);
            return true;
        }

        private void ParseCopySourceRange(string range, long objectLength, out long start, out long length, string header)
        {
            start = 0;
            length = 0;

            string value = range.Trim();
            if (value.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase)) value = value.Substring(6);
            string[] parts = value.Split('-');

            if (parts.Length != 2
                || !Int64.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out long first)
                || !Int64.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out long last)
                || last < first
                || last >= objectLength)
            {
                _Logging.Warn(header + "invalid x-amz-copy-source-range " + range + " for source of length " + objectLength);
                throw new S3Exception(new Error(ErrorCode.InvalidArgument));
            }

            start = first;
            length = last - first + 1;
        }

        private async Task<Obj> CopyObjectToFile(S3Context ctx, BucketClient sourceClient, Obj source, bool followLatest, ObjectByteRange range, string filename, string header)
        {
            // The source is re-resolved under its read lock; with followLatest the copy takes whatever is
            // the latest version at that moment, and the returned row describes exactly what was copied. The
            // x-amz-copy-source-if-* conditions are evaluated against that row, under the same lock.
            ObjectReadResult read = sourceClient.OpenObject(source, followLatest, served =>
            {
                CheckCopySourceConditions(ctx, served, header);
                return range ?? new ObjectByteRange(0, served.ContentLength);
            });
            if (read == null) throw new S3Exception(new Error(followLatest ? ErrorCode.NoSuchKey : ErrorCode.NoSuchVersion));

            using (read.Data)
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                await read.Data.CopyToAsync(fs, 65536).ConfigureAwait(false);
            }

            return read.Object;
        }

        private void CheckCopySourceConditions(S3Context ctx, Obj source, string header)
        {
            ConditionalRequestOutcome outcome = ConditionalRequestEvaluator.Evaluate(ctx.Http.Request.Headers, "x-amz-copy-source-", EtagOf(source), source.LastUpdateUtc);
            if (outcome != ConditionalRequestOutcome.Proceed)
            {
                // CopyObject and UploadPartCopy report every failed copy-source condition as 412.
                _Logging.Warn(header + "copy source precondition failed");
                throw new S3Exception(new Error(ErrorCode.PreconditionFailed));
            }
        }

        private List<ObjectAcl> AclsFromHeaders(RequestMetadata md, S3Context ctx, string objectId, string header)
        {
            // Anonymous writers cannot grant permissions.
            if (md.User == null) return new List<ObjectAcl>();

            return AclConverter.PolicyToObjectAcls(
                null,
                ctx.Http.Request.Headers,
                md.User,
                md.Bucket.Id,
                objectId,
                md.Bucket.OwnerId,
                _Config,
                _Logging,
                header);
        }

        private List<ObjectTag> TagsFromHeader(S3Context ctx, string header)
        {
            string tagging = GetHeader(ctx, "x-amz-tagging");
            if (tagging == null) return null;

            List<ObjectTag> tags = new List<ObjectTag>();
            List<Tag> validation = new List<Tag>();

            foreach (string pair in tagging.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int equals = pair.IndexOf('=');
                string key = Uri.UnescapeDataString((equals >= 0 ? pair.Substring(0, equals) : pair).Replace('+', ' '));
                string value = equals >= 0 ? Uri.UnescapeDataString(pair.Substring(equals + 1).Replace('+', ' ')) : "";
                if (String.IsNullOrEmpty(key))
                {
                    _Logging.Warn(header + "x-amz-tagging header contains a tag with an empty key");
                    throw new S3Exception(new Error(ErrorCode.InvalidArgument));
                }

                tags.Add(new ObjectTag { Key = key, Value = value });
                validation.Add(new Tag { Key = key, Value = value });
            }

            if (S3TagValidator.IsInvalid(validation))
            {
                _Logging.Warn(header + "invalid x-amz-tagging header");
                throw new S3Exception(new Error(ErrorCode.InvalidArgument));
            }

            return tags;
        }

        private string GetPartFilePath(string bucketId, string uploadId, int partNumber)
        {
            return MultipartPaths.PartFilePath(_Settings.Storage, bucketId, uploadId, partNumber);
        }

        private void AddObjectMetadataHeaders(Obj obj, S3Context ctx, string header)
        {
            Dictionary<string, string> metadata = DeserializeMetadata(obj.Metadata, obj.Id, header);

            foreach (string systemHeader in _SystemMetadataHeaders)
            {
                string value = null;
                string overrideValue = ctx.Request.RetrieveQueryValue("response-" + systemHeader);
                if (!String.IsNullOrEmpty(overrideValue)) value = overrideValue;
                else if (metadata != null && metadata.TryGetValue(_SystemMetadataPrefix + systemHeader, out string stored)) value = stored;

                if (!String.IsNullOrEmpty(value)) ctx.Response.Headers.Add(systemHeader, value);
            }

            if (metadata == null) return;

            foreach (KeyValuePair<string, string> kvp in metadata)
            {
                if (kvp.Key.StartsWith(_SystemMetadataPrefix, StringComparison.Ordinal)) continue;
                ctx.Response.Headers.Add("x-amz-meta-" + kvp.Key, kvp.Value);
            }
        }

        private Dictionary<string, string> DeserializeMetadata(string json, string objectId, string header)
        {
            if (String.IsNullOrWhiteSpace(json)) return null;

            try
            {
                return SerializationHelper.DeserializeJson<Dictionary<string, string>>(json);
            }
            catch (System.Text.Json.JsonException)
            {
                _Logging.Warn(header + "ignoring invalid object metadata for object Id " + objectId);
                return null;
            }
        }

        private static string SerializeMetadata(Dictionary<string, string> metadata)
        {
            if (metadata == null || metadata.Count < 1) return null;
            return SerializationHelper.SerializeJson(metadata, false);
        }

        private static Dictionary<string, string> ExtractMetadataFromHeaders(NameValueCollection headers)
        {
            if (headers == null || headers.Count == 0) return null;

            Dictionary<string, string> metadata = new Dictionary<string, string>();

            foreach (string key in headers.AllKeys)
            {
                if (key == null) continue;
                string lower = key.ToLowerInvariant();

                if (lower.StartsWith("x-amz-meta-"))
                {
                    // Amazon S3 stores user metadata keys in lowercase.
                    metadata[lower.Substring("x-amz-meta-".Length)] = headers[key];
                }
                else if (_SystemMetadataHeaders.Contains(lower))
                {
                    string value = headers[key];

                    // aws-chunked describes how the request body was framed, not the object; Amazon S3
                    // removes it from the stored Content-Encoding.
                    if (lower == "content-encoding")
                    {
                        value = String.Join(",", value.Split(',')
                            .Select(v => v.Trim())
                            .Where(v => v.Length > 0 && !String.Equals(v, "aws-chunked", StringComparison.OrdinalIgnoreCase)));
                        if (value.Length == 0) continue;
                    }

                    metadata[_SystemMetadataPrefix + lower] = value;
                }
            }

            if (metadata.Count == 0) return null;
            return metadata;
        }

        private Owner GetOwnerFromUserId(string id)
        {
            if (String.IsNullOrEmpty(id)) return null;
            User user = _Config.GetUserById(id);
            if (user != null) return new Owner(id, user.Name);
            return null;
        }

        private int ParseNonNegativeQueryInt(S3Context ctx, string name, int defaultValue, string header)
        {
            string value = ctx.Request.RetrieveQueryValue(name);
            if (String.IsNullOrEmpty(value)) return defaultValue;

            if (!Int32.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
            {
                _Logging.Warn(header + "invalid " + name + " value " + value);
                throw new S3Exception(new Error(ErrorCode.InvalidArgument));
            }

            return parsed;
        }

        private List<UploadPart> GetLatestUploadPartsByNumber(List<UploadPart> parts)
        {
            if (parts == null || parts.Count < 1) return new List<UploadPart>();

            List<UploadPart> latestParts = parts
                .GroupBy(p => p.PartNumber)
                .Select(g => g
                    .OrderByDescending(p => p.CreatedUtc)
                    .ThenByDescending(p => p.Id)
                    .First())
                .OrderBy(p => p.PartNumber)
                .ToList();

            if (latestParts.Count != parts.Count)
            {
                _Logging.Warn("ObjectHandler detected duplicate stored multipart rows and selected the newest row per part number");
            }

            return latestParts;
        }

        private static HashResult HashFile(string filename)
        {
            HashResult hashes = new HashResult();

            using (IncrementalHash md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
            using (IncrementalHash sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1))
            using (IncrementalHash sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[65536];
                int read;
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    md5.AppendData(buffer, 0, read);
                    sha1.AppendData(buffer, 0, read);
                    sha256.AppendData(buffer, 0, read);
                }

                hashes.MD5 = ToHexString(md5.GetHashAndReset());
                hashes.SHA1 = ToHexString(sha1.GetHashAndReset());
                hashes.SHA256 = ToHexString(sha256.GetHashAndReset());
            }

            return hashes;
        }

        private string ComputeMultipartEtag(List<UploadPart> parts)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                List<byte> allMd5Bytes = new List<byte>();
                foreach (UploadPart part in parts)
                {
                    if (!String.IsNullOrEmpty(part.MD5Hash))
                    {
                        allMd5Bytes.AddRange(Convert.FromHexString(part.MD5Hash));
                    }
                }

                byte[] combinedHash = md5.ComputeHash(allMd5Bytes.ToArray());
                return ToHexString(combinedHash) + "-" + parts.Count;
            }
        }

        private static string NormalizeEtag(string etag)
        {
            if (String.IsNullOrWhiteSpace(etag)) return null;
            return etag.Trim().Trim('"');
        }

        private static string ToHexString(byte[] data)
        {
            if (data == null || data.Length < 1) return String.Empty;
            return Convert.ToHexString(data).ToLowerInvariant();
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
