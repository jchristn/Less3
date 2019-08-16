using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
 
using SyslogLogging;

namespace Less3.Classes
{
    /// <summary>
    /// Bucket manager.
    /// </summary>
    public class BucketManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;

        private readonly object _BucketsLock = new object();
        private List<BucketClient> _Buckets = new List<BucketClient>();
         
        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param>
        public BucketManager(Settings settings, LoggingModule logging, ConfigManager config)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (config == null) throw new ArgumentNullException(nameof(config));

            _Settings = settings;
            _Logging = logging;
            _Config = config;

            InitializeBuckets();
        }

        #endregion

        #region Public-Methods
         
        /// <summary>
        /// Add a bucket.
        /// </summary>
        /// <param name="bucket">BucketConfiguration.</param>
        /// <returns>True if successful.</returns>
        public bool Add(BucketConfiguration bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            bool success = _Config.AddBucket(bucket);
            if (success)
            {
                BucketClient client = new BucketClient(_Settings, _Logging, bucket);

                lock (_BucketsLock)
                {
                    _Buckets.Add(client);
                }

                InitializeBucket(bucket);
            }

            return success; 
        }

        /// <summary>
        /// Remove a bucket.
        /// </summary>
        /// <param name="bucket">BucketConfiguration.</param>
        /// <param name="destroy">True to destroy the bucket data.</param>
        /// <returns>True if successful.</returns>
        public bool Remove(BucketConfiguration bucket, bool destroy)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            bool removed = false;

            if (_Config.BucketExists(bucket.Name))
            {
                BucketClient client = null;
                if (GetClient(bucket.Name, out client))
                {
                    lock (_BucketsLock)
                    {
                        List<BucketClient> clients = _Buckets.Where(b => !b.Name.Equals(bucket.Name)).ToList();
                        _Buckets = new List<BucketClient>(clients);
                        client.Dispose();
                        client = null;
                    }
                }

                removed = true;

                _Config.DeleteBucket(bucket.GUID);
            }

            if (removed)
            {
                if (destroy)
                {
                    if (!Destroy(bucket))
                        _Logging.Warn("BucketManager Remove issues encountered removing data for bucket " + bucket.Name + ", cleanup required");
                    else
                        _Logging.Info("BucketManager Remove removed bucket with name " + bucket.Name + " with owner " + bucket.OwnerGUID);
                }
                else
                {
                    _Logging.Info("BucketManager Remove removed bucket with name " + bucket.Name + " with owner " + bucket.OwnerGUID);
                }

                return true;
            }
            else
            {
                _Logging.Warn("BucketManager Remove bucket with name " + bucket.Name + " not found");
                return false;
            }
        }

        /// <summary>
        /// Retrieve a bucket's configuration.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="bucket">BucketConfiguration.</param>
        /// <returns>True if successful.</returns>
        public bool Get(string bucketName, out BucketConfiguration bucket)
        {
            bucket = null;
            if (String.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

            if (!_Config.GetBucketByName(bucketName, out bucket))
            {
                _Logging.Warn("BucketManager Get unable to find bucket with name " + bucketName);
                return false;
            }
            else
            {
                return true;
            }
        }
         
        /// <summary>
        /// Retrieve a bucket's client.
        /// </summary>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="client">BucketClient.</param>
        /// <returns>True if successful.</returns>
        public bool GetClient(string bucketName, out BucketClient client)
        {
            client = null;
            if (String.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

            lock (_BucketsLock)
            {
                bool exists = _Buckets.Exists(b => b.Name.Equals(bucketName));
                if (!exists) return false;
                client = _Buckets.First(b => b.Name.Equals(bucketName));
                return true;
            }
        }

        /// <summary>
        /// Get all buckets associated with a user.
        /// </summary>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="buckets">List of BucketConfiguration.</param> 
        public void GetUserBuckets(string userGuid, out List<BucketConfiguration> buckets)
        {
            buckets = null;
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));

            _Config.GetBucketsByUser(userGuid, out buckets);
        }

        #endregion

        #region Private-Methods

        private void InitializeBuckets()
        {
            List<BucketConfiguration> buckets = null;
            _Config.GetBuckets(out buckets);

            if (buckets == null || buckets.Count < 1)
                throw new Exception("No buckets configured.");

            foreach (BucketConfiguration curr in buckets)
            {
                InitializeBucket(curr);
            }
        }

        private void InitializeBucket(BucketConfiguration bucket)
        {
            lock (_BucketsLock)
            {
                BucketClient client = new BucketClient(_Settings, _Logging, bucket);
                _Buckets.Add(client);
            }
        }

        private bool Destroy(BucketConfiguration bucket)
        { 
            #region Delete-Database

            bool databaseDelete = false;
            try
            {
                if (File.Exists(bucket.DatabaseFilename))
                    File.Delete(bucket.DatabaseFilename);
                databaseDelete = true;
            }
            catch (Exception)
            {
                _Logging.Warn("Destroy bucket " + bucket.Name + " failed");
            }

            #endregion

            #region Delete-Object-Files

            bool objectFilesDelete = false;
            try
            {
                if (Directory.Exists(bucket.ObjectsDirectory))
                    ClearDirectory(bucket.ObjectsDirectory);
                objectFilesDelete = true;
            }
            catch (Exception)
            {

            }

            #endregion

            #region Remove-Objects-Directory

            bool objectsDirectoryDelete = false;
            try
            {
                if (Directory.Exists(bucket.ObjectsDirectory))
                    Directory.Delete(bucket.ObjectsDirectory);
                objectsDirectoryDelete = true;
            }
            catch (Exception)
            {

            }

            #endregion

            #region Delete-Root-Files

            bool rootFilesDelete = false;
            try
            {
                if (Directory.Exists(_Settings.Storage.Directory + bucket.Name))
                    ClearDirectory(_Settings.Storage.Directory + bucket.Name);
                rootFilesDelete = true;
            }
            catch (Exception)
            {

            }

            #endregion

            #region Remove-Root-Directory

            bool rootDirectoryDelete = false;
            try
            {
                if (Directory.Exists(_Settings.Storage.Directory + bucket.Name))
                    Directory.Delete(_Settings.Storage.Directory + bucket.Name);
                rootDirectoryDelete = true;
            }
            catch (Exception)
            {

            }

            #endregion

            _Logging.Info("Destroy bucket " + bucket.Name + ": " +
                "db files [" + databaseDelete + "] " +
                "obj files [" + objectFilesDelete + "] " +
                "obj dir [" + objectsDirectoryDelete + "] " +
                "root files [" + rootFilesDelete + "] " +
                "root dir [" + rootDirectoryDelete + "]");

            return databaseDelete && objectFilesDelete && objectsDirectoryDelete && rootFilesDelete && rootDirectoryDelete;
        }

        private void ClearDirectory(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            foreach (FileInfo fi in dir.EnumerateFiles())
            {
                fi.Delete();
            }
        }

        #endregion
    }
}
