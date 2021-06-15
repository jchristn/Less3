using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

using S3ServerInterface;

using SyslogLogging;

using Less3.Classes; 

namespace Less3.Api.S3
{
    /// <summary>
    /// API handler.
    /// </summary>
    internal class ApiHandler
    {
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

        #region Service-Callbacks

        internal async Task ServiceListBuckets(S3Context ctx)
        {
            await _ServiceHandler.ListBuckets(ctx);
        }

        #endregion

        #region Bucket-Callbacks

        internal async Task BucketDelete(S3Context ctx)
        {
            await _BucketHandler.Delete(ctx); 
        }

        internal async Task BucketDeleteTags(S3Context ctx)
        {
            await _BucketHandler.DeleteTags(ctx);
        }

        internal async Task BucketExists(S3Context ctx)
        {
            await _BucketHandler.Exists(ctx);
        }

        internal async Task BucketReadLocation(S3Context ctx)
        {
            await _BucketHandler.ReadLocation(ctx);
        }

        internal async Task BucketRead(S3Context ctx)
        {
            await _BucketHandler.Read(ctx);
        }

        internal async Task BucketReadAcl(S3Context ctx)
        {
            await _BucketHandler.ReadAcl(ctx);
        }

        internal async Task BucketReadTags(S3Context ctx)
        {
            await _BucketHandler.ReadTags(ctx);
        }

        internal async Task BucketReadVersions(S3Context ctx)
        {
            await _BucketHandler.ReadVersions(ctx);
        }

        internal async Task BucketReadVersioning(S3Context ctx)
        {
            await _BucketHandler.ReadVersioning(ctx);
        }

        internal async Task BucketWrite(S3Context ctx)
        {
            await _BucketHandler.Write(ctx);
        }

        internal async Task BucketWriteAcl(S3Context ctx)
        {
            await _BucketHandler.WriteAcl(ctx);
        }

        internal async Task BucketWriteTags(S3Context ctx)
        {
            await _BucketHandler.WriteTags(ctx);
        }

        internal async Task BucketWriteVersioning(S3Context ctx)
        {  
            await _BucketHandler.WriteVersioning(ctx);
        }

        #endregion

        #region Object-Callbacks

        internal async Task ObjectDelete(S3Context ctx)
        {
            await _ObjectHandler.Delete(ctx);
        }

        internal async Task ObjectDeleteMultiple(S3Context ctx)
        {
            await _ObjectHandler.DeleteMultiple(ctx);
        }

        internal async Task ObjectDeleteTags(S3Context ctx)
        {
            await _ObjectHandler.DeleteTags(ctx);
        }

        internal async Task ObjectExists(S3Context ctx)
        {
            await _ObjectHandler.Exists(ctx);
        }

        internal async Task ObjectRead(S3Context ctx)
        {
            await _ObjectHandler.Read(ctx);
        }

        internal async Task ObjectReadAcl(S3Context ctx)
        {
            await _ObjectHandler.ReadAcl(ctx);
        }

        internal async Task ObjectReadRange(S3Context ctx)
        {
            await _ObjectHandler.ReadRange(ctx);
        }

        internal async Task ObjectReadTags(S3Context ctx)
        {
            await _ObjectHandler.ReadTags(ctx);
        }

        internal async Task ObjectWrite(S3Context ctx)
        {
            await _ObjectHandler.Write(ctx);
        }

        internal async Task ObjectWriteAcl(S3Context ctx)
        {
            await _ObjectHandler.WriteAcl(ctx);
        }

        internal async Task ObjectWriteTags(S3Context ctx)
        {
            await _ObjectHandler.WriteTags(ctx);
        }

        #endregion

        #endregion

        #region Private-Methods

        private string AmazonTimestamp(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffz");
        }

        #endregion
    }
}
