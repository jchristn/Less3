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

        internal async Task Exists(S3Context ctx)
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

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
        }

        internal async Task Read(S3Context ctx)
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
             
            int startIndex = 0;
            if (!String.IsNullOrEmpty(ctx.Request.ContinuationToken))
            {
                startIndex = ParseContinuationToken(ctx.Request.ContinuationToken);
            }

            if (!String.IsNullOrEmpty(ctx.Request.Marker))
            {
                Obj marker = md.BucketClient.GetObjectMetadata(ctx.Request.Marker);
                if (marker != null) startIndex = (marker.Id + 1);
            }
              
            List<Obj> objects = new List<Obj>();
            List<string> prefixes = new List<string>();
            int nextStartIndex = startIndex;
            bool isTruncated = false;
            md.BucketClient.Enumerate(ctx.Request.Delimiter, ctx.Request.Prefix, startIndex, (int)ctx.Request.MaxKeys, out objects, out prefixes, out nextStartIndex, out isTruncated);
             
            ListBucketResult listBucketResult = new ListBucketResult();
            listBucketResult.Contents = new List<Contents>();

            listBucketResult.Prefix = ctx.Request.Prefix;
            listBucketResult.Delimiter = ctx.Request.Delimiter;
            listBucketResult.KeyCount = objects.Count;
            listBucketResult.MaxKeys = ctx.Request.MaxKeys;
            listBucketResult.Name = ctx.Request.Bucket;
            listBucketResult.Marker = ctx.Request.Marker;
            listBucketResult.Prefix = ctx.Request.Prefix; 
            listBucketResult.CommonPrefixes.Prefix = prefixes;
            listBucketResult.IsTruncated = false;

            if (isTruncated)
            {
                listBucketResult.IsTruncated = true;
                listBucketResult.NextContinuationToken = BuildContinuationToken(nextStartIndex); 
            }

            Dictionary<string, S3ServerInterface.S3Objects.Owner> ownerCache = new Dictionary<string, S3ServerInterface.S3Objects.Owner>();

            foreach (Obj curr in objects)
            {
                Contents c = new Contents();
                c.ETag = "\"" + curr.Md5 + "\"";
                c.Key = curr.Key;
                c.LastModified = curr.LastUpdateUtc;
                c.Size = curr.ContentLength;
                c.StorageClass = "STANDARD";
                
                c.Owner = new S3ServerInterface.S3Objects.Owner();
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

            await ApiHelper.SendSerializedResponse<ListBucketResult>(ctx, listBucketResult);
            return;
        }

        internal async Task ReadLocation(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + "] ";
            ctx.Response.Chunked = false;

            LocationConstraint loc = new LocationConstraint();
            loc.Text = _Settings.Server.RegionString;

            await ApiHelper.SendSerializedResponse<LocationConstraint>(ctx, loc);
            return;
        }

        internal async Task ReadAcl(S3Context ctx)
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

            User owner = _Config.GetUserByGuid(md.Bucket.OwnerGUID);
            if (owner == null)
            {
                _Logging.Warn(header + "unable to find owner GUID " + md.Bucket.OwnerGUID + " for bucket GUID " + md.Bucket.GUID);
                await ctx.Response.Send(ErrorCode.InternalError);
                return;
            }

            AccessControlPolicy ret = new AccessControlPolicy();
            ret.Owner = new S3ServerInterface.S3Objects.Owner();
            ret.Owner.DisplayName = owner.Name;
            ret.Owner.ID = owner.GUID;

            ret.AccessControlList = new AccessControlList(); 
            ret.AccessControlList.Grant = new List<Grant>();

            foreach (BucketAcl curr in md.BucketAcls)
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
                    _Logging.Warn(header + "incorrectly configured bucket ACL ID " + curr.Id + " (not user or group)");
                }
            }

            await ApiHelper.SendSerializedResponse<AccessControlPolicy>(ctx, ret);
            return; 
        }

        internal async Task ReadTags(S3Context ctx)
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
             
            Tagging tags = new Tagging();
            tags.TagSet = new List<Tag>();

            if (md.BucketTags != null && md.BucketTags.Count > 0)
            {
                foreach (BucketTag curr in md.BucketTags)
                {
                    Tag currTag = new Tag();
                    currTag.Key = curr.Key;
                    currTag.Value = curr.Value;
                    tags.TagSet.Add(currTag);
                }
            }

            await ApiHelper.SendSerializedResponse<Tagging>(ctx, tags);
            return; 
        }

        internal async Task ReadVersions(S3Context ctx)
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
             
            int startIndex = 0;
            if (!String.IsNullOrEmpty(ctx.Request.ContinuationToken))
            {
                startIndex = ParseContinuationToken(ctx.Request.ContinuationToken);
            }

            if (!String.IsNullOrEmpty(ctx.Request.Marker))
            {
                Obj marker = md.BucketClient.GetObjectMetadata(ctx.Request.Marker);
                if (marker != null) startIndex = (marker.Id + 1);
            }
              
            List<Obj> objects = new List<Obj>();
            List<string> prefixes = new List<string>();
            int nextStartIndex = startIndex;
            bool isTruncated = false;
            md.BucketClient.Enumerate(ctx.Request.Delimiter, ctx.Request.Prefix, startIndex, (int)ctx.Request.MaxKeys, out objects, out prefixes, out nextStartIndex, out isTruncated);
             
            string lastKey = null; 
            if (objects.Count > 0)
            {
                objects = objects.OrderBy(p => p.Id).ToList(); 
                lastKey = objects[objects.Count - 1].Key; 
            }
             
            ListVersionsResult listVersionsResult = new ListVersionsResult();
            listVersionsResult.IsTruncated = isTruncated;
            listVersionsResult.KeyMarker = lastKey;
            listVersionsResult.MaxKeys = ctx.Request.MaxKeys;
            listVersionsResult.Name = ctx.Request.Bucket;
            listVersionsResult.Prefix = ctx.Request.Prefix;

            Dictionary<string, S3ServerInterface.S3Objects.Owner> ownerCache = new Dictionary<string, S3ServerInterface.S3Objects.Owner>();

            foreach (Obj curr in objects)
            {
                if (curr.DeleteMarker)
                {
                    DeleteMarker d = new DeleteMarker();
                    d.IsLatest = IsLatest(objects, curr.Key, curr.LastAccessUtc);
                    d.Key = curr.Key;
                    d.LastModified = curr.LastUpdateUtc;
                    d.VersionId = curr.Version.ToString();

                    d.Owner = new S3ServerInterface.S3Objects.Owner();
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

                    listVersionsResult.DeleteMarker.Add(d);
                }
                else
                {
                    S3ServerInterface.S3Objects.Version v = new S3ServerInterface.S3Objects.Version();
                    v.ETag = null;
                    v.IsLatest = IsLatest(objects, curr.Key, curr.LastAccessUtc);
                    v.Key = curr.Key;
                    v.ETag = "\"" + curr.Md5 + "\"";
                    v.LastModified = curr.LastUpdateUtc;
                    v.Size = curr.ContentLength;
                    v.StorageClass = "STANDARD";

                    v.Owner = new S3ServerInterface.S3Objects.Owner();
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

                    listVersionsResult.Version.Add(v);
                }
            }

            await ApiHelper.SendSerializedResponse<ListVersionsResult>(ctx, listVersionsResult);
            return; 
        }

        internal async Task ReadVersioning(S3Context ctx)
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

            VersioningConfiguration ret = new VersioningConfiguration();
            ret.Status = "Off";
            ret.MfaDelete = "Disabled";
                 
            if (md.Bucket.EnableVersioning)
            {
                ret.Status = "Enabled";
                ret.MfaDelete = "Disabled";
            }

            await ApiHelper.SendSerializedResponse<VersioningConfiguration>(ctx, ret);
            return; 
        }

        internal async Task Write(S3Context ctx)
        {
            string header = "[" + ctx.Http.Request.Source.IpAddress + ":" + ctx.Http.Request.Source.Port + " " + ctx.Request.RequestType.ToString() + "] ";

            RequestMetadata md = ApiHelper.GetRequestMetadata(ctx);
            if (md == null)
            {
                _Logging.Warn(header + "unable to retrieve metadata");
                await ctx.Response.Send(ErrorCode.InternalError);
                return;
            }

            if (md.User == null || md.Credential == null)
            {
                _Logging.Warn(header + "not authorized");
                await ctx.Response.Send(ErrorCode.AccessDenied);
                return;
            }

            if (md.Bucket != null || md.BucketClient != null)
            {
                _Logging.Warn(header + "bucket already exists");
                await ctx.Response.Send(ErrorCode.BucketAlreadyExists);
                return;
            }
               
            if (IsInvalidBucketName(ctx.Request.Bucket))
            {
                _Logging.Warn(header + "invalid bucket name: " + ctx.Request.Bucket);
                await ctx.Response.Send(ErrorCode.InvalidRequest);
                return;
            }
             
            Classes.Bucket bucket = new Classes.Bucket(
                Guid.NewGuid().ToString(),
                ctx.Request.Bucket,
                md.User.GUID, 
                _Settings.Storage.StorageType, 
                _Settings.Storage.DiskDirectory + ctx.Request.Bucket + "/Objects/");
             
            if (!_Buckets.Add(bucket))
            {
                _Logging.Warn(header + "unable to write bucket " + ctx.Request.Bucket);
                await ctx.Response.Send(ErrorCode.InternalError);
                return;
            }

            BucketClient client = _Buckets.GetClient(ctx.Request.Bucket);
            if (client == null)
            {
                _Logging.Warn(header + "unable to retrieve bucket client for bucket " + ctx.Request.Bucket);
                await ctx.Response.Send(ErrorCode.InternalError);
                return;
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

                            if (String.IsNullOrEmpty(curr.Permission))
                            {
                                _Logging.Warn(header + "no permissions specified for user " + curr.Grantee.ID + " in ACL for bucket " + bucket.GUID);
                                continue;
                            }

                            if (curr.Permission.Equals("READ")) permitRead = true;
                            else if (curr.Permission.Equals("WRITE")) permitWrite = true;
                            else if (curr.Permission.Equals("READ_ACP")) permitReadAcp = true;
                            else if (curr.Permission.Equals("WRITE_ACP")) permitWriteAcp = true;
                            else if (curr.Permission.Equals("FULL_CONTROL")) fullControl = true;

                            bucketAcl = BucketAcl.BucketUserAcl(
                                curr.Grantee.ID, 
                                md.User.GUID, 
                                permitRead, 
                                permitWrite, 
                                permitReadAcp, 
                                permitWriteAcp, 
                                fullControl);

                            client.AddBucketAcl(bucketAcl);
                        }
                        else if (!String.IsNullOrEmpty(curr.Grantee.URI))
                        {
                            if (String.IsNullOrEmpty(curr.Permission))
                            {
                                _Logging.Warn(header + "no permissions specified for user " + curr.Grantee.ID + " in ACL for bucket " + bucket.GUID);
                                continue;
                            }

                            if (curr.Permission.Equals("READ")) permitRead = true;
                            else if (curr.Permission.Equals("WRITE")) permitWrite = true;
                            else if (curr.Permission.Equals("READ_ACP")) permitReadAcp = true;
                            else if (curr.Permission.Equals("WRITE_ACP")) permitWriteAcp = true;
                            else if (curr.Permission.Equals("FULL_CONTROL")) fullControl = true;

                            bucketAcl = BucketAcl.BucketGroupAcl(
                                curr.Grantee.URI, 
                                md.User.GUID, 
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

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
        }

        internal async Task WriteAcl(S3Context ctx)
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

            ctx.Response.Chunked = false;

            byte[] data = null;
            AccessControlPolicy reqBody = null;

            if (ctx.Request.Data != null)
            {
                try
                {
                    data = Common.StreamToBytes(ctx.Request.Data); 
                    string xmlString = Encoding.UTF8.GetString(data); 
                    reqBody = Common.DeserializeXml<AccessControlPolicy>(xmlString); 
                }
                catch (Exception e)
                {
                    _Logging.Exception(e, header + "BucketHandler", "WriteAcl");
                    await ctx.Response.Send(ErrorCode.InvalidRequest);
                    return;
                }
            }
             
            md.BucketClient.DeleteBucketAcl();

            List<Grant> headerGrants = GrantsFromHeaders(md.User, ctx.Http.Request.Headers);
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

                    tempUser = _Config.GetUserByGuid(curr.Grantee.ID);
                    if (tempUser == null)
                    {
                        _Logging.Warn(header + "unable to find user GUID " + curr.Grantee.ID);
                        continue;
                    }

                    if (curr.Permission.Equals("READ"))
                        acl = BucketAcl.BucketUserAcl(curr.Grantee.ID, md.Bucket.OwnerGUID, true, false, false, false, false);
                    else if (curr.Permission.Equals("WRITE"))
                        acl = BucketAcl.BucketUserAcl(curr.Grantee.ID, md.Bucket.OwnerGUID, false, true, false, false, false);
                    else if (curr.Permission.Equals("READ_ACP"))
                        acl = BucketAcl.BucketUserAcl(curr.Grantee.ID, md.Bucket.OwnerGUID, false, false, true, false, false);
                    else if (curr.Permission.Equals("WRITE_ACP"))
                        acl = BucketAcl.BucketUserAcl(curr.Grantee.ID, md.Bucket.OwnerGUID, false, false, false, true, false);
                    else if (curr.Permission.Equals("FULL_CONTROL"))
                        acl = BucketAcl.BucketUserAcl(curr.Grantee.ID, md.Bucket.OwnerGUID, false, false, false, false, true);

                    #endregion
                }
                else if (curr.Grantee is Group)
                {
                    #region Group-ACL
                     
                    if (curr.Permission.Equals("READ"))
                        acl = BucketAcl.BucketGroupAcl(curr.Grantee.URI, md.Bucket.OwnerGUID, true, false, false, false, false);
                    else if (curr.Permission.Equals("WRITE"))
                        acl = BucketAcl.BucketGroupAcl(curr.Grantee.URI, md.Bucket.OwnerGUID, false, true, false, false, false);
                    else if (curr.Permission.Equals("READ_ACP"))
                        acl = BucketAcl.BucketGroupAcl(curr.Grantee.URI, md.Bucket.OwnerGUID, false, false, true, false, false);
                    else if (curr.Permission.Equals("WRITE_ACP"))
                        acl = BucketAcl.BucketGroupAcl(curr.Grantee.URI, md.Bucket.OwnerGUID, false, false, false, true, false);
                    else if (curr.Permission.Equals("FULL_CONTROL"))
                        acl = BucketAcl.BucketGroupAcl(curr.Grantee.URI, md.Bucket.OwnerGUID, false, false, false, false, true);

                    #endregion
                }

                if (acl != null)
                { 
                    md.BucketClient.AddBucketAcl(acl);
                }
            }

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
        }

        internal async Task WriteTags(S3Context ctx)
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

            ctx.Response.Chunked = false;

            byte[] data = null;
            Tagging reqBody = null;

            if (ctx.Request.Data!= null)
            {
                try
                {
                    data = Common.StreamToBytes(ctx.Request.Data);
                    reqBody = Common.DeserializeXml<Tagging>(Encoding.UTF8.GetString(data));
                }
                catch (Exception e)
                {
                    _Logging.Exception(e, header + "BucketHandler", "WriteTags");
                    await ctx.Response.Send(ErrorCode.InvalidRequest);
                    return;
                }
            }
             
            md.BucketClient.DeleteBucketTags();

            List<BucketTag> tags = new List<BucketTag>(); 
            if (reqBody.TagSet != null && reqBody.TagSet.Count > 0)
            {
                foreach (Tag curr in reqBody.TagSet)
                {
                    BucketTag tag = new BucketTag();
                    tag.BucketGUID = md.Bucket.GUID;
                    tag.Key = curr.Key;
                    tag.Value = curr.Value;
                    tags.Add(tag);
                }
            }

            md.BucketClient.AddBucketTags(tags);

            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
        }

        internal async Task WriteVersioning(S3Context ctx)
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

            ctx.Response.Chunked = false;

            byte[] data = null;
            VersioningConfiguration reqBody = null;

            if (ctx.Request.Data != null)
            {
                try
                {
                    data = Common.StreamToBytes(ctx.Request.Data); 
                    reqBody = Common.DeserializeXml<VersioningConfiguration>(Encoding.UTF8.GetString(data));
                }
                catch (Exception e)
                {
                    _Logging.Exception(e, header + "BucketHandler", "WriteVersioning");
                    await ctx.Response.Send(ErrorCode.InvalidRequest);
                    return;
                }
            }
             
            if (reqBody.Status.Equals("Enabled") && !md.Bucket.EnableVersioning)
            {
                md.Bucket.EnableVersioning = true;
                _Buckets.Remove(md.Bucket, false);
                _Buckets.Add(md.Bucket);
            }
            else if (!reqBody.Status.Equals("Enabled") && md.Bucket.EnableVersioning)
            {
                md.Bucket.EnableVersioning = false;
                _Buckets.Remove(md.Bucket, false);
                _Buckets.Add(md.Bucket);
            }

            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.Send();
            return;
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
    }
}
