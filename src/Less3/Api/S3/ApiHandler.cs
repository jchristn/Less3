using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using S3ServerLibrary;
using S3ServerLibrary.S3Objects;

using SyslogLogging;

using Less3.Classes; 

namespace Less3.Api.S3
{
    /// <summary>
    /// API handler.
    /// </summary>
    internal class ApiHandler
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;
        private BucketManager _Buckets;
        private AuthManager _Auth; 

        private ServiceHandler _ServiceHandler;
        private BucketHandler _BucketHandler;
        private ObjectHandler _ObjectHandler;

        #endregion

        #region Constructors-and-Factories
         
        internal ApiHandler(
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

            _ServiceHandler = new ServiceHandler(_Settings, _Logging, _Config, _Buckets, _Auth);
            _BucketHandler = new BucketHandler(_Settings, _Logging, _Config, _Buckets, _Auth);
            _ObjectHandler = new ObjectHandler(_Settings, _Logging, _Config, _Buckets, _Auth);
        }

        #endregion

        #region Internal-Methods

        #region Service-APIs

        internal async Task<ListAllMyBucketsResult> ServiceListBuckets(S3Context ctx)
        {
            return await _ServiceHandler.ListBuckets(ctx);
        }

        internal async Task<string> ServiceExists(S3Context ctx)
        {
            return _Settings.Server.RegionString;
        }

        internal string FindMatchingBaseDomain(string hostname)
        {
            if (String.IsNullOrEmpty(hostname)) return null;
            if (String.IsNullOrEmpty(_Settings.Server.BaseDomain)) return null;

            if (hostname.Equals(_Settings.Server.BaseDomain)) return _Settings.Server.BaseDomain;

            string testDomain = "." + _Settings.Server.BaseDomain;
            if (hostname.EndsWith(testDomain)) return _Settings.Server.BaseDomain;

            throw new KeyNotFoundException("A base domain could not be found for hostname '" + hostname + "'.");
        }

        #endregion

        #region Bucket-APIs

        internal async Task BucketDelete(S3Context ctx)
        {
            await _BucketHandler.Delete(ctx); 
        }

        internal async Task BucketDeleteTagging(S3Context ctx)
        {
            await _BucketHandler.DeleteTags(ctx);
        }

        internal async Task<bool> BucketExists(S3Context ctx)
        {
            return await _BucketHandler.Exists(ctx);
        }

        internal async Task<LocationConstraint> BucketReadLocation(S3Context ctx)
        {
            return await _BucketHandler.ReadLocation(ctx);
        }

        internal async Task<ListBucketResult> BucketRead(S3Context ctx)
        {
            return await _BucketHandler.Read(ctx);
        }

        internal async Task<AccessControlPolicy> BucketReadAcl(S3Context ctx)
        {
            return await _BucketHandler.ReadAcl(ctx);
        }

        internal async Task<Tagging> BucketReadTagging(S3Context ctx)
        {
            return await _BucketHandler.ReadTags(ctx);
        }

        internal async Task<ListVersionsResult> BucketReadVersions(S3Context ctx)
        {
            return await _BucketHandler.ReadVersions(ctx);
        }

        internal async Task<VersioningConfiguration> BucketReadVersioning(S3Context ctx)
        {
            return await _BucketHandler.ReadVersioning(ctx);
        }

        internal async Task BucketWrite(S3Context ctx)
        {
            await _BucketHandler.Write(ctx);
        }

        internal async Task BucketWriteAcl(S3Context ctx, AccessControlPolicy acp)
        {
            await _BucketHandler.WriteAcl(ctx, acp);
        }

        internal async Task BucketWriteTagging(S3Context ctx, Tagging tagging)
        {
            await _BucketHandler.WriteTagging(ctx, tagging);
        }

        internal async Task BucketWriteVersioning(S3Context ctx, VersioningConfiguration versioning)
        {  
            await _BucketHandler.WriteVersioning(ctx, versioning);
        }

        #endregion

        #region Object-APIs

        internal async Task ObjectDelete(S3Context ctx)
        {
            await _ObjectHandler.Delete(ctx);
        }

        internal async Task<DeleteResult> ObjectDeleteMultiple(S3Context ctx, DeleteMultiple dm)
        {
            return await _ObjectHandler.DeleteMultiple(ctx, dm);
        }

        internal async Task ObjectDeleteTagging(S3Context ctx)
        {
            await _ObjectHandler.DeleteTags(ctx);
        }

        internal async Task<ObjectMetadata> ObjectExists(S3Context ctx)
        {
            return await _ObjectHandler.Exists(ctx);
        }

        internal async Task<S3Object> ObjectRead(S3Context ctx)
        {
            return await _ObjectHandler.Read(ctx);
        }

        internal async Task<AccessControlPolicy> ObjectReadAcl(S3Context ctx)
        {
            return await _ObjectHandler.ReadAcl(ctx);
        }

        internal async Task<S3Object> ObjectReadRange(S3Context ctx)
        {
            return await _ObjectHandler.ReadRange(ctx);
        }

        internal async Task<Tagging> ObjectReadTagging(S3Context ctx)
        {
            return await _ObjectHandler.ReadTags(ctx);
        }

        internal async Task ObjectWrite(S3Context ctx)
        {
            await _ObjectHandler.Write(ctx);
        }

        internal async Task ObjectWriteAcl(S3Context ctx, AccessControlPolicy acp)
        {
            await _ObjectHandler.WriteAcl(ctx, acp);
        }

        internal async Task ObjectWriteTagging(S3Context ctx, Tagging tagging)
        {
            await _ObjectHandler.WriteTagging(ctx, tagging);
        }

        #endregion

        #endregion

        #region Private-Methods

        private string AmazonTimestamp(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffz");
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
