using System;
using System.Collections.Generic;
using System.Text;

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
    public class ApiHandler
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

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param> 
        /// <param name="config">ConfigManager.</param>
        /// <param name="buckets">BucketManager.</param>
        /// <param name="auth">AuthManager.</param> 
        public ApiHandler(
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

        #region Public-Methods

        #region Service-Callbacks

        /// <summary>
        /// Service list buckets API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ServiceListBuckets(S3Request req)
        {
            return _ServiceHandler.ListBuckets(req);
        }

        #endregion

        #region Bucket-Callbacks

        /// <summary>
        /// Bucket delete API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketDelete(S3Request req)
        {
            return _BucketHandler.Delete(req);
        }

        /// <summary>
        /// Bucket delete tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketDeleteTags(S3Request req)
        {
            return _BucketHandler.DeleteTags(req);
        }

        /// <summary>
        /// Bucket exists API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketExists(S3Request req)
        {
            return _BucketHandler.Exists(req);
        }

        /// <summary>
        /// Bucket read API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketRead(S3Request req)
        {
            return _BucketHandler.Read(req);
        }

        /// <summary>
        /// Bucket read ACL API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketReadAcl(S3Request req)
        {
            return _BucketHandler.ReadAcl(req);
        }

        /// <summary>
        /// Bucket read tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketReadTags(S3Request req)
        {
            return _BucketHandler.ReadTags(req);
        }

        /// <summary>
        /// Bucket read versions API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketReadVersions(S3Request req)
        {
            return _BucketHandler.ReadVersions(req);
        }

        /// <summary>
        /// Bucket read versioning API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketReadVersioning(S3Request req)
        {
            return _BucketHandler.ReadVersioning(req);
        }

        /// <summary>
        /// Bucket write API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketWrite(S3Request req)
        {
            return _BucketHandler.Write(req);
        }

        /// <summary>
        /// Bucket write ACL API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketWriteAcl(S3Request req)
        {
            return _BucketHandler.WriteAcl(req);
        }

        /// <summary>
        /// Bucket write tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketWriteTags(S3Request req)
        {
            return _BucketHandler.WriteTags(req);
        }

        /// <summary>
        /// Bucket write versioning API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response BucketWriteVersioning(S3Request req)
        {  
            return _BucketHandler.WriteVersioning(req);
        }

        #endregion

        #region Object-Callbacks

        /// <summary>
        /// Object delete API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ObjectDelete(S3Request req)
        {
            return _ObjectHandler.Delete(req);
        }

        /// <summary>
        /// Object delete multiple API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ObjectDeleteMultiple(S3Request req)
        {
            return _ObjectHandler.DeleteMultiple(req);
        }

        /// <summary>
        /// Object delete tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ObjectDeleteTags(S3Request req)
        {
            return _ObjectHandler.DeleteTags(req);
        }

        /// <summary>
        /// Object exists API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ObjectExists(S3Request req)
        {
            return _ObjectHandler.Exists(req);
        }

        /// <summary>
        /// Object read API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ObjectRead(S3Request req)
        {
            return _ObjectHandler.Read(req);
        }

        /// <summary>
        /// Object read ACL API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ObjectReadAcl(S3Request req)
        {
            return _ObjectHandler.ReadAcl(req);
        }

        /// <summary>
        /// Object read range API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ObjectReadRange(S3Request req)
        {
            return _ObjectHandler.ReadRange(req);
        }

        /// <summary>
        /// Object read tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ObjectReadTags(S3Request req)
        {
            return _ObjectHandler.ReadTags(req);
        }

        /// <summary>
        /// Object write API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ObjectWrite(S3Request req)
        {
            return _ObjectHandler.Write(req);
        }

        /// <summary>
        /// Object write ACL API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ObjectWriteAcl(S3Request req)
        {
            return _ObjectHandler.WriteAcl(req);
        }

        /// <summary>
        /// Object write tags API callback.
        /// </summary>
        /// <param name="req">S3Request.</param>
        /// <returns>S3Response.</returns>
        public S3Response ObjectWriteTags(S3Request req)
        {
            return _ObjectHandler.WriteTags(req);
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
