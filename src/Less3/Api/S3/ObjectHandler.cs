namespace Less3.Api.S3
{
    using Azure;
    using Less3.Classes;
    using Less3.Helpers;
    using Less3.Settings;
    using S3ServerLibrary;
    using S3ServerLibrary.S3Objects;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;

    /// <summary>
    /// Object APIs.
    /// </summary>
    public class ObjectHandler
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

        internal ObjectHandler(
            SettingsBase settings,
            LoggingModule logging,
            ConfigManager config,
            BucketManager buckets,
            AuthManager auth)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Config = config ?? throw new ArgumentNullException(nameof(config));
            _Buckets = buckets ?? throw new ArgumentNullException(nameof(buckets));
            _Auth = auth ?? throw new ArgumentNullException(nameof(auth));
        }

        #endregion

        #region Internal-Methods

        internal async Task Delete(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            long versionId = RequestValidator.ParseVersionId(ctx);
            RequestValidator.ValidateObjectExists(md.Obj, versionId, _Logging, header);
            RequestValidator.CheckDeleteMarker(md.Obj, ctx);

            md.BucketClient.DeleteObjectVersion(md.Obj.Key, versionId);
            md.BucketClient.DeleteObjectVersionAcl(md.Obj.Key, versionId);
            md.BucketClient.DeleteObjectVersionTags(md.Obj.Key, versionId);

            if (md.Bucket.EnableVersioning)
                ctx.Response.Headers.Add("x-amz-version-id", md.Obj.Version.ToString());
        }

        internal async Task<DeleteResult> DeleteMultiple(S3Context ctx, DeleteMultiple dm)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

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

        internal async Task DeleteTags(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            long versionId = RequestValidator.ParseVersionId(ctx);
            RequestValidator.ValidateObjectExists(md.Obj, versionId, _Logging, header);
            RequestValidator.CheckDeleteMarker(md.Obj, ctx);

            md.BucketClient.DeleteObjectVersionTags(ctx.Request.Key, versionId);

            if (md.Bucket.EnableVersioning)
                ctx.Response.Headers.Add("x-amz-version-id", md.Obj.Version.ToString());
        }

        internal async Task<ObjectMetadata> Exists(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            long versionId = RequestValidator.ParseVersionId(ctx);

            if (md.Obj == null)
            {
                throw new S3Exception(new Error(ErrorCode.NoSuchKey));
            }

            RequestValidator.CheckDeleteMarker(md.Obj, ctx);

            ObjectMetadata metadata = new ObjectMetadata(md.Obj.Key, md.Obj.LastUpdateUtc, md.Obj.Md5, md.Obj.ContentLength, new Owner(md.Obj.OwnerGUID, null));
            metadata.ContentType = md.Obj.ContentType;

            if (md.Bucket.EnableVersioning)
                ctx.Response.Headers.Add("x-amz-version-id", md.Obj.Version.ToString());

            if (md.Obj.Metadata != null)
            {
                Dictionary<string, string> userMeta = SerializationHelper.DeserializeJson<Dictionary<string, string>>(md.Obj.Metadata);
                if (userMeta != null)
                {
                    foreach (KeyValuePair<string, string> kvp in userMeta)
                    {
                        ctx.Response.Headers.Add("x-amz-meta-" + kvp.Key, kvp.Value);
                    }
                }
            }

            return metadata;
        }

        internal async Task<S3Object> Read(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            long versionId = RequestValidator.ParseVersionId(ctx);
            RequestValidator.ValidateObjectExists(md.Obj, versionId, _Logging, header);
            RequestValidator.CheckDeleteMarker(md.Obj, ctx);

            bool isLatest = true;
            long latestVersion = md.BucketClient.GetObjectLatestVersion(md.Obj.Key);
            if (md.Obj.Version < latestVersion) isLatest = false;

            if (md.Bucket.EnableVersioning)
                ctx.Response.Headers.Add("x-amz-version-id", md.Obj.Version.ToString());

            if (md.Obj.Metadata != null)
            {
                Dictionary<string, string> userMeta = SerializationHelper.DeserializeJson<Dictionary<string, string>>(md.Obj.Metadata);
                if (userMeta != null)
                {
                    foreach (KeyValuePair<string, string> kvp in userMeta)
                    {
                        ctx.Response.Headers.Add("x-amz-meta-" + kvp.Key, kvp.Value);
                    }
                }
            }

            FileStream fs = new FileStream(GetObjectBlobFile(md.Bucket, md.Obj), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new S3Object(md.Obj.Key, md.Obj.Version.ToString(), isLatest, md.Obj.LastUpdateUtc, md.Obj.Etag, md.Obj.ContentLength, GetOwnerFromUserGuid(md.Obj.OwnerGUID), fs, md.Obj.ContentType);
        }

        internal async Task<AccessControlPolicy> ReadAcl(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            long versionId = RequestValidator.ParseVersionId(ctx);
            RequestValidator.ValidateObjectExists(md.Obj, versionId, _Logging, header);
            RequestValidator.CheckDeleteMarker(md.Obj, ctx);

            User owner = _Config.GetUserByGuid(md.Obj.OwnerGUID);
            if (owner == null)
            {
                _Logging.Warn(header + "unable to find owner GUID " + md.Obj.OwnerGUID + " for object GUID " + md.Obj.GUID);
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }

            if (md.Bucket.EnableVersioning)
                ctx.Response.Headers.Add("x-amz-version-id", md.Obj.Version.ToString());

            return AclConverter.ObjectAclsToPolicy(md.ObjectAcls, owner, _Config, _Logging, header);
        }

        internal async Task<S3Object> ReadRange(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            long versionId = RequestValidator.ParseVersionId(ctx);
            RequestValidator.ValidateObjectExists(md.Obj, versionId, _Logging, header);
            RequestValidator.CheckDeleteMarker(md.Obj, ctx);

            if (ctx.Request.RangeStart == null)
            {
                _Logging.Debug(header + "null range start, shifting to full object read");
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

                ctx.Response.Headers.Add("Content-Range", "bytes " + ctx.Request.RangeStart.Value + "-" + ctx.Request.RangeEnd.Value + "/" + md.Obj.ContentLength);
                ctx.Response.Headers.Add("Accept-Ranges", "bytes");
                ctx.Response.Headers.Add("ETag", "\"" + md.Obj.Etag + "\"");

                if (md.Bucket.EnableVersioning)
                    ctx.Response.Headers.Add("x-amz-version-id", md.Obj.Version.ToString());

                return new S3Object(md.Obj.Key, md.Obj.Version.ToString(), isLatest, md.Obj.LastUpdateUtc, md.Obj.Etag, readLen, GetOwnerFromUserGuid(md.Obj.OwnerGUID), data, md.Obj.ContentType);
            }
        }

        internal async Task<Tagging> ReadTags(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            long versionId = RequestValidator.ParseVersionId(ctx);
            RequestValidator.ValidateObjectExists(md.Obj, versionId, _Logging, header);
            RequestValidator.CheckDeleteMarker(md.Obj, ctx);

            if (md.Bucket.EnableVersioning)
                ctx.Response.Headers.Add("x-amz-version-id", md.Obj.Version.ToString());

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

                obj.Etag = null;
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

                            if (chunk.IsFinal) break;
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
                                    await fs.WriteAsync(buffer, 0, bytesRead);
                                }
                            }

                            totalLength = obj.ContentLength;
                        }
                    }
                }

                Dictionary<string, string> userMetadata = ExtractMetadataFromHeaders(ctx.Http.Request.Headers);
                if (userMetadata != null && userMetadata.Count > 0)
                {
                    obj.Metadata = SerializationHelper.SerializeJson(userMetadata, false);
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

            ctx.Response.Headers.Add("ETag", "\"" + obj.Etag + "\"");

            if (md.Bucket.EnableVersioning)
                ctx.Response.Headers.Add("x-amz-version-id", obj.Version.ToString());

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

                                if (curr.Permission == PermissionEnum.Read) permitRead = true;
                                else if (curr.Permission == PermissionEnum.Write) permitWrite = true;
                                else if (curr.Permission == PermissionEnum.ReadAcp) permitReadAcp = true;
                                else if (curr.Permission == PermissionEnum.WriteAcp) permitWriteAcp = true;
                                else if (curr.Permission == PermissionEnum.FullControl) fullControl = true;

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
                                if (curr.Permission == PermissionEnum.Read) permitRead = true;
                                else if (curr.Permission == PermissionEnum.Write) permitWrite = true;
                                else if (curr.Permission == PermissionEnum.ReadAcp) permitReadAcp = true;
                                else if (curr.Permission == PermissionEnum.WriteAcp) permitWriteAcp = true;
                                else if (curr.Permission == PermissionEnum.FullControl) fullControl = true;

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

        internal async Task WriteAcl(S3Context ctx, AccessControlPolicy acp)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
            RequestValidator.ValidateAuthentication(md, _Logging, header);

            long versionId = RequestValidator.ParseVersionId(ctx);
            RequestValidator.ValidateObjectExists(md.Obj, versionId, _Logging, header);
            RequestValidator.CheckDeleteMarker(md.Obj, ctx);

            md.BucketClient.DeleteObjectVersionAcl(ctx.Request.Key, versionId);

            List<ObjectAcl> acls = AclConverter.PolicyToObjectAcls(
                acp,
                ctx.Http.Request.Headers,
                md.User,
                md.Bucket.GUID,
                md.Obj.GUID,
                md.Bucket.OwnerGUID,
                _Config,
                _Logging,
                header);

            foreach (ObjectAcl acl in acls)
            {
                md.BucketClient.AddObjectAcl(acl);
            }

            if (md.Bucket.EnableVersioning)
                ctx.Response.Headers.Add("x-amz-version-id", md.Obj.Version.ToString());
        }

        internal async Task WriteTagging(S3Context ctx, Tagging tagging)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            long versionId = RequestValidator.ParseVersionId(ctx);
            RequestValidator.ValidateObjectExists(md.Obj, versionId, _Logging, header);
            RequestValidator.CheckDeleteMarker(md.Obj, ctx);

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

            if (md.Bucket.EnableVersioning)
                ctx.Response.Headers.Add("x-amz-version-id", md.Obj.Version.ToString());
        }

        internal async Task<InitiateMultipartUploadResult> CreateMultipartUpload(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);

            DateTime ts = DateTime.UtcNow;

            Less3.Classes.Upload upload = new Less3.Classes.Upload();
            upload.GUID = Guid.NewGuid().ToString();
            upload.BucketGUID = md.Bucket.GUID;
            upload.Key = ctx.Request.Key;
            upload.CreatedUtc = ts;
            upload.LastAccessUtc = ts;
            upload.ExpirationUtc = ts.AddSeconds(60 * 60 * 24 * 7);

            if (md.User != null)
            {
                upload.OwnerGUID = md.User.GUID;
                upload.AuthorGUID = md.User.GUID;
            }
            else
            {
                upload.OwnerGUID = ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port;
                upload.AuthorGUID = ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port;
            }

            if (ctx.Http.Request.Headers != null)
            {
                if (ctx.Http.Request.Headers.AllKeys.Contains("content-type"))
                {
                    upload.ContentType = ctx.Http.Request.Headers["content-type"];
                }

                Dictionary<string, string> metadata = ExtractMetadataFromHeaders(ctx.Http.Request.Headers);
                if (metadata != null && metadata.Count > 0)
                {
                    upload.Metadata = SerializationHelper.SerializeJson(metadata, false);
                }
            }

            _Config.AddUpload(upload);

            _Logging.Info(header + "initiated multipart upload " + upload.GUID + " for key " + ctx.Request.Bucket + "/" + ctx.Request.Key);

            InitiateMultipartUploadResult result = new InitiateMultipartUploadResult();
            result.Bucket = ctx.Request.Bucket;
            result.Key = ctx.Request.Key;
            result.UploadId = upload.GUID;

            return result;
        }

        internal async Task<CompleteMultipartUploadResult> CompleteMultipartUpload(S3Context ctx, CompleteMultipartUpload upload)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
            RequestValidator.ValidateUploadId(ctx, _Logging, header);

            Less3.Classes.Upload uploadRecord = _Config.GetUploadByGuid(ctx.Request.UploadId);
            RequestValidator.ValidateUpload(uploadRecord, ctx.Request.UploadId, _Logging, header);

            List<UploadPart> parts = _Config.GetUploadPartsByUploadGuid(ctx.Request.UploadId);
            if (parts == null || parts.Count == 0)
            {
                _Logging.Warn(header + "no parts found for upload " + ctx.Request.UploadId);
                throw new S3Exception(new Error(ErrorCode.InvalidPart));
            }

            parts = parts.OrderBy(p => p.PartNumber).ToList();

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].PartNumber != (i + 1))
                {
                    _Logging.Warn(header + "missing part number " + (i + 1) + " for upload " + ctx.Request.UploadId);
                    throw new S3Exception(new Error(ErrorCode.InvalidPart));
                }
            }

            Obj existingObj = md.BucketClient.GetObjectLatestMetadata(ctx.Request.Key);
            if (existingObj != null && !md.Bucket.EnableVersioning)
            {
                _Logging.Warn(header + "versioning disabled, prohibiting write to " + ctx.Request.Bucket + "/" + ctx.Request.Key);
                throw new S3Exception(new Error(ErrorCode.InvalidBucketState));
            }

            DateTime ts = DateTime.UtcNow;

            Obj obj = new Obj();
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
            obj.BlobFilename = obj.GUID;
            obj.Key = ctx.Request.Key;
            obj.ContentType = uploadRecord.ContentType;
            obj.CreatedUtc = ts;
            obj.DeleteMarker = false;
            obj.ExpirationUtc = null;
            obj.LastAccessUtc = ts;
            obj.LastUpdateUtc = ts;

            if (existingObj == null)
            {
                obj.Version = 1;
            }
            else
            {
                obj.Version = existingObj.Version + 1;
            }

            string tempFilename = _Settings.Storage.TempDirectory + Guid.NewGuid().ToString();
            long totalLength = 0;

            try
            {
                using (FileStream outStream = new FileStream(tempFilename, FileMode.Create, FileAccess.Write))
                {
                    foreach (UploadPart part in parts)
                    {
                        string partFile = GetPartFilePath(md.Bucket.GUID, ctx.Request.UploadId, part.PartNumber);
                        if (!File.Exists(partFile))
                        {
                            _Logging.Warn(header + "part file " + partFile + " not found for part " + part.PartNumber);
                            throw new S3Exception(new Error(ErrorCode.InvalidPart));
                        }

                        using (FileStream inStream = new FileStream(partFile, FileMode.Open, FileAccess.Read))
                        {
                            byte[] buffer = new byte[65536];
                            int bytesRead = 0;

                            while ((bytesRead = await inStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await outStream.WriteAsync(buffer, 0, bytesRead);
                                totalLength += bytesRead;
                            }
                        }
                    }
                }

                obj.ContentLength = totalLength;

                using (FileStream fs = new FileStream(tempFilename, FileMode.Open, FileAccess.Read))
                {
                    bool success = md.BucketClient.AddObject(obj, fs);
                    if (!success)
                    {
                        _Logging.Warn(header + "failed to add object " + ctx.Request.Bucket + "/" + ctx.Request.Key);
                        throw new S3Exception(new Error(ErrorCode.InternalError));
                    }
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
                throw new S3Exception(new Error(ErrorCode.InternalError));
            }
            finally
            {
                if (File.Exists(tempFilename))
                {
                    File.Delete(tempFilename);
                }
            }

            foreach (UploadPart part in parts)
            {
                string partFile = GetPartFilePath(md.Bucket.GUID, ctx.Request.UploadId, part.PartNumber);
                if (File.Exists(partFile))
                {
                    File.Delete(partFile);
                }
            }

            _Config.DeleteUploadParts(ctx.Request.UploadId);
            _Config.DeleteUpload(ctx.Request.UploadId);

            _Logging.Info(header + "completed multipart upload " + ctx.Request.UploadId + " for key " + ctx.Request.Bucket + "/" + ctx.Request.Key);

            string multipartEtag = ComputeMultipartEtag(parts);

            CompleteMultipartUploadResult result = new CompleteMultipartUploadResult();
            result.Location = "http://" + ctx.Http.Request.Headers["Host"] + "/" + ctx.Request.Bucket + "/" + ctx.Request.Key;
            result.Bucket = ctx.Request.Bucket;
            result.Key = ctx.Request.Key;
            result.ETag = multipartEtag;

            ctx.Response.Headers.Add("ETag", "\"" + multipartEtag + "\"");

            if (md.Bucket.EnableVersioning)
                ctx.Response.Headers.Add("x-amz-version-id", obj.Version.ToString());

            return result;
        }

        internal async Task UploadPart(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
            RequestValidator.ValidateUploadId(ctx, _Logging, header);
            RequestValidator.ValidatePartNumber(ctx.Request.PartNumber, _Logging, header);

            Less3.Classes.Upload uploadRecord = _Config.GetUploadByGuid(ctx.Request.UploadId);
            RequestValidator.ValidateUpload(uploadRecord, ctx.Request.UploadId, _Logging, header);

            string partFile = GetPartFilePath(md.Bucket.GUID, ctx.Request.UploadId, ctx.Request.PartNumber);
            long partLength = 0;

            try
            {
                using (FileStream fs = new FileStream(partFile, FileMode.Create, FileAccess.Write))
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
                                await fs.WriteAsync(buffer, 0, bytesRead);
                                partLength += bytesRead;
                            }
                        }
                    }
                }

                byte[] partData = File.ReadAllBytes(partFile);
                HashResult hashes = HashHelper.ComputeHashes(partData);

                UploadPart part = new UploadPart();
                part.GUID = Guid.NewGuid().ToString();
                part.BucketGUID = md.Bucket.GUID;
                part.UploadGUID = ctx.Request.UploadId;
                part.PartNumber = ctx.Request.PartNumber;
                part.PartLength = (int)partLength;
                part.MD5Hash = hashes.MD5;
                part.Sha1Hash = hashes.SHA1;
                part.Sha256Hash = hashes.SHA256;
                part.CreatedUtc = DateTime.UtcNow;
                part.LastAccessUtc = DateTime.UtcNow;

                if (md.User != null)
                {
                    part.OwnerGUID = md.User.GUID;
                }
                else
                {
                    part.OwnerGUID = Guid.NewGuid().ToString();
                }

                _Config.AddUploadPart(part);

                ctx.Response.Headers.Add("ETag", "\"" + hashes.MD5 + "\"");

                _Logging.Info(header + "uploaded part " + ctx.Request.PartNumber + " for upload " + ctx.Request.UploadId);
            }
            catch (Exception e)
            {
                _Logging.Warn(header + "failure while uploading part " + ctx.Request.PartNumber + " for upload " + ctx.Request.UploadId);
                _Logging.Exception(e, "ObjectHandler", "UploadPart");

                if (File.Exists(partFile))
                {
                    File.Delete(partFile);
                }

                throw new S3Exception(new Error(ErrorCode.InternalError));
            }
        }

        internal async Task AbortMultipartUpload(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
            RequestValidator.ValidateUploadId(ctx, _Logging, header);

            Less3.Classes.Upload uploadRecord = _Config.GetUploadByGuid(ctx.Request.UploadId);
            RequestValidator.ValidateUpload(uploadRecord, ctx.Request.UploadId, _Logging, header);

            List<UploadPart> parts = _Config.GetUploadPartsByUploadGuid(ctx.Request.UploadId);
            if (parts != null && parts.Count > 0)
            {
                foreach (UploadPart part in parts)
                {
                    string partFile = GetPartFilePath(md.Bucket.GUID, ctx.Request.UploadId, part.PartNumber);
                    if (File.Exists(partFile))
                    {
                        File.Delete(partFile);
                    }
                }
            }

            _Config.DeleteUploadParts(ctx.Request.UploadId);
            _Config.DeleteUpload(ctx.Request.UploadId);

            _Logging.Info(header + "aborted multipart upload " + ctx.Request.UploadId);
        }

        internal async Task<ListPartsResult> ReadParts(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = RequestValidator.ValidateAndGetMetadata(ctx, _Logging, header);
            RequestValidator.ValidateAuthorization(md, _Logging, header);
            RequestValidator.ValidateBucketExists(md, _Logging, header);
            RequestValidator.ValidateUploadId(ctx, _Logging, header);

            Less3.Classes.Upload uploadRecord = _Config.GetUploadByGuid(ctx.Request.UploadId);
            RequestValidator.ValidateUpload(uploadRecord, ctx.Request.UploadId, _Logging, header);

            List<UploadPart> parts = _Config.GetUploadPartsByUploadGuid(ctx.Request.UploadId);

            ListPartsResult result = new ListPartsResult();
            result.Bucket = ctx.Request.Bucket;
            result.Key = ctx.Request.Key;
            result.UploadId = ctx.Request.UploadId;
            result.Initiator = new Owner();
            result.Initiator.ID = uploadRecord.OwnerGUID;
            result.Owner = new Owner();
            result.Owner.ID = uploadRecord.OwnerGUID;

            User owner = _Config.GetUserByGuid(uploadRecord.OwnerGUID);
            if (owner != null)
            {
                result.Owner.DisplayName = owner.Name;
                result.Initiator.DisplayName = owner.Name;
            }

            result.StorageClass = S3ServerLibrary.S3Objects.StorageClassEnum.STANDARD;
            result.Parts = new List<Part>();

            if (parts != null && parts.Count > 0)
            {
                parts = parts.OrderBy(p => p.PartNumber).ToList();

                foreach (UploadPart uploadPart in parts)
                {
                    Part part = new Part();
                    part.PartNumber = uploadPart.PartNumber;
                    part.LastModified = uploadPart.LastAccessUtc;
                    part.ETag = "\"" + uploadPart.MD5Hash + "\"";
                    part.Size = uploadPart.PartLength;
                    result.Parts.Add(part);
                }
            }

            result.IsTruncated = false;

            if (result.Parts.Count > 0)
            {
                result.NextPartNumberMarker = result.Parts[result.Parts.Count - 1].PartNumber;
            }

            _Logging.Debug(header + "listed " + result.Parts.Count + " parts for upload " + ctx.Request.UploadId);

            return result;
        }

        #endregion

        #region Private-Methods

        private string GetPartFilePath(string bucketGuid, string uploadGuid, int partNumber)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            if (String.IsNullOrEmpty(uploadGuid)) throw new ArgumentNullException(nameof(uploadGuid));
            if (partNumber < 1) throw new ArgumentOutOfRangeException(nameof(partNumber));

            string tempDir = _Settings.Storage.TempDirectory;
            if (!tempDir.EndsWith("/") && !tempDir.EndsWith("\\"))
            {
                tempDir += "/";
            }

            return tempDir + bucketGuid + "-upload-" + uploadGuid + "-part-" + partNumber;
        }

        private Dictionary<string, string> ExtractMetadataFromHeaders(NameValueCollection headers)
        {
            if (headers == null || headers.Count == 0) return null;

            Dictionary<string, string> metadata = new Dictionary<string, string>();

            foreach (string key in headers.AllKeys)
            {
                if (key != null && key.ToLower().StartsWith("x-amz-meta-"))
                {
                    string metaKey = key.Substring("x-amz-meta-".Length);
                    metadata[metaKey] = headers[key];
                }
            }

            if (metadata.Count == 0) return null;

            return metadata;
        }

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

        private string ComputeMultipartEtag(List<UploadPart> parts)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                List<byte> allMd5Bytes = new List<byte>();
                foreach (UploadPart part in parts)
                {
                    if (!String.IsNullOrEmpty(part.MD5Hash))
                    {
                        byte[] partMd5 = HexStringToBytes(part.MD5Hash);
                        allMd5Bytes.AddRange(partMd5);
                    }
                }
                byte[] combinedHash = md5.ComputeHash(allMd5Bytes.ToArray());
                string hex = BitConverter.ToString(combinedHash).Replace("-", "").ToLowerInvariant();
                return hex + "-" + parts.Count;
            }
        }

        private byte[] HexStringToBytes(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
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
                        grant.Grantee = new CanonicalUser();
                        grant.Grantee.ID = user.GUID;
                        grant.Grantee.DisplayName = user.Name;
                        ret.Add(grant);
                        break;

                    case "public-read":
                        grant = new Grant();
                        grant.Permission = PermissionEnum.FullControl;
                        grant.Grantee = new CanonicalUser();
                        grant.Grantee.ID = user.GUID;
                        grant.Grantee.DisplayName = user.Name;
                        ret.Add(grant);

                        grant = new Grant();
                        grant.Permission = PermissionEnum.Read;
                        grant.Grantee = new Group();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        ret.Add(grant);
                        break;

                    case "public-read-write":
                        grant = new Grant();
                        grant.Permission = PermissionEnum.FullControl;
                        grant.Grantee = new CanonicalUser();
                        grant.Grantee.ID = user.GUID;
                        grant.Grantee.DisplayName = user.Name;
                        ret.Add(grant);

                        grant = new Grant();
                        grant.Permission = PermissionEnum.Read;
                        grant.Grantee = new Group();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        ret.Add(grant);

                        grant = new Grant();
                        grant.Permission = PermissionEnum.Write;
                        grant.Grantee = new Group();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AllUsers";
                        ret.Add(grant);
                        break;

                    case "authenticated-read":
                        grant = new Grant();
                        grant.Permission = PermissionEnum.FullControl;
                        grant.Grantee = new CanonicalUser();
                        grant.Grantee.ID = user.GUID;
                        grant.Grantee.DisplayName = user.Name;
                        ret.Add(grant);

                        grant = new Grant();
                        grant.Permission = PermissionEnum.Read;
                        grant.Grantee = new Group();
                        grant.Grantee.URI = "http://acs.amazonaws.com/groups/global/AuthenticatedUsers";
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

            if (granteeType.Equals("emailAddress"))
            {
                User user = _Config.GetUserByEmail(grantee);
                if (user == null)
                {
                    return false;
                }
                else
                {
                    grant.Grantee = new CanonicalUser();
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
                    grant.Grantee = new CanonicalUser();
                    grant.Grantee.ID = user.GUID;
                    grant.Grantee.DisplayName = user.Name;
                    return true;
                }
            }
            else if (granteeType.Equals("uri"))
            {
                grant.Grantee = new Group();
                grant.Grantee.URI = grantee;
                return true;
            }

            return false;
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
