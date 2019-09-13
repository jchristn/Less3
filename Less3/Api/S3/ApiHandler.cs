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

        internal async Task ServiceListBuckets(S3Request req, S3Response resp)
        {
            await _ServiceHandler.ListBuckets(req, resp);
        }

        #endregion

        #region Bucket-Callbacks

        internal async Task BucketDelete(S3Request req, S3Response resp)
        {
            await _BucketHandler.Delete(req, resp); 
        }

        internal async Task BucketDeleteTags(S3Request req, S3Response resp)
        {
            await _BucketHandler.DeleteTags(req, resp);
        }

        internal async Task BucketExists(S3Request req, S3Response resp)
        {
            await _BucketHandler.Exists(req, resp);
        }

        internal async Task BucketReadLocation(S3Request req, S3Response resp)
        {
            await _BucketHandler.ReadLocation(req, resp);
        }

        internal async Task BucketRead(S3Request req, S3Response resp)
        {
            await _BucketHandler.Read(req, resp);
        }

        internal async Task BucketReadAcl(S3Request req, S3Response resp)
        {
            await _BucketHandler.ReadAcl(req, resp);
        }

        internal async Task BucketReadTags(S3Request req, S3Response resp)
        {
            await _BucketHandler.ReadTags(req, resp);
        }

        internal async Task BucketReadVersions(S3Request req, S3Response resp)
        {
            await _BucketHandler.ReadVersions(req, resp);
        }

        internal async Task BucketReadVersioning(S3Request req, S3Response resp)
        {
            await _BucketHandler.ReadVersioning(req, resp);
        }

        internal async Task BucketWrite(S3Request req, S3Response resp)
        {
            await _BucketHandler.Write(req, resp);
        }

        internal async Task BucketWriteAcl(S3Request req, S3Response resp)
        {
            await _BucketHandler.WriteAcl(req, resp);
        }

        internal async Task BucketWriteTags(S3Request req, S3Response resp)
        {
            await _BucketHandler.WriteTags(req, resp);
        }

        internal async Task BucketWriteVersioning(S3Request req, S3Response resp)
        {  
            await _BucketHandler.WriteVersioning(req, resp);
        }

        #endregion

        #region Object-Callbacks

        internal async Task ObjectDelete(S3Request req, S3Response resp)
        {
            await _ObjectHandler.Delete(req, resp);
        }

        internal async Task ObjectDeleteMultiple(S3Request req, S3Response resp)
        {
            await _ObjectHandler.DeleteMultiple(req, resp);
        }

        internal async Task ObjectDeleteTags(S3Request req, S3Response resp)
        {
            await _ObjectHandler.DeleteTags(req, resp);
        }

        internal async Task ObjectExists(S3Request req, S3Response resp)
        {
            await _ObjectHandler.Exists(req, resp);
        }

        internal async Task ObjectRead(S3Request req, S3Response resp)
        {
            await _ObjectHandler.Read(req, resp);
        }

        internal async Task ObjectReadAcl(S3Request req, S3Response resp)
        {
            await _ObjectHandler.ReadAcl(req, resp);
        }

        internal async Task ObjectReadRange(S3Request req, S3Response resp)
        {
            await _ObjectHandler.ReadRange(req, resp);
        }

        internal async Task ObjectReadTags(S3Request req, S3Response resp)
        {
            await _ObjectHandler.ReadTags(req, resp);
        }

        internal async Task ObjectWrite(S3Request req, S3Response resp)
        {
            await _ObjectHandler.Write(req, resp);
        }

        internal async Task ObjectWriteAcl(S3Request req, S3Response resp)
        {
            await _ObjectHandler.WriteAcl(req, resp);
        }

        internal async Task ObjectWriteTags(S3Request req, S3Response resp)
        {
            await _ObjectHandler.WriteTags(req, resp);
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
