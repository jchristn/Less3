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

        private readonly object _BucketsLock = new object();
        private List<BucketClient> _Buckets = new List<BucketClient>();

        private readonly object _BucketConfigLock = new object();
        private List<BucketConfiguration> _BucketConfigs = new List<BucketConfiguration>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param>
        public BucketManager(Settings settings, LoggingModule logging)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _Settings = settings;
            _Logging = logging;

            Load();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Load buckets from the filesystem.
        /// </summary>
        public void Load()
        {
            lock (_BucketConfigLock)
            {
                _BucketConfigs = Common.DeserializeJson<List<BucketConfiguration>>(Common.ReadTextFile(_Settings.Files.Buckets));
            }

            InitializeBuckets();
        }

        /// <summary>
        /// Save buckets to the filesystem.
        /// </summary>
        public void Save()
        {
            lock (_BucketConfigLock)
            {
                Common.WriteFile(_Settings.Files.Buckets, Encoding.UTF8.GetBytes(Common.SerializeJson(_BucketConfigs, true)));
            }
        }
         
        /// <summary>
        /// Add a bucket.
        /// </summary>
        /// <param name="bucket">BucketConfiguration.</param>
        /// <returns>True if successful.</returns>
        public bool Add(BucketConfiguration bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            bool added = false;

            lock (_BucketConfigLock)
            {
                bool exists = _BucketConfigs.Exists(c => c.Name.Equals(bucket.Name));
                if (!exists)
                {
                    _BucketConfigs.Add(bucket);
                    BucketClient client = new BucketClient(_Settings, _Logging, bucket);

                    lock (_BucketsLock)
                    {
                        _Buckets.Add(client);
                    }

                    added = true;
                }
                else
                {
                    return false;
                }
            }
            
            if (added)
            {
                Save();
                InitializeBucket(bucket);
                _Logging.Log(LoggingModule.Severity.Info, "BucketManager Add added bucket with name " + bucket.Name + " with owner " + bucket.Owner);
                return true;
            }

            _Logging.Log(LoggingModule.Severity.Warn, "BucketManager Add bucket with name " + bucket.Name + " already exists");
            return false;
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

            lock (_BucketConfigLock)
            {
                bool exists = _BucketConfigs.Exists(c => c.Name.Equals(bucket.Name));
                if (exists)
                {
                    _BucketConfigs.Remove(bucket);

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
                }
            }

            if (removed)
            {
                Save();
                if (!Destroy(bucket))
                    _Logging.Log(LoggingModule.Severity.Warn, "BucketManager Remove issues encountered removing data for bucket " + bucket.Name + ", cleanup required");
                else
                    _Logging.Log(LoggingModule.Severity.Info, "BucketManager Remove removed bucket with name " + bucket.Name + " with owner " + bucket.Owner);
                return true;
            }

            _Logging.Log(LoggingModule.Severity.Warn, "BucketManager Remove bucket with name " + bucket.Name + " not found");
            return false;
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
            if (bucketName == null) throw new ArgumentNullException(nameof(bucketName));

            lock (_BucketConfigLock)
            {
                bool exists = _BucketConfigs.Exists(c => c.Name.Equals(bucketName));
                if (exists)
                {
                    bucket = _BucketConfigs.First(c => c.Name.Equals(bucketName));
                    return true;
                }

                return false;
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
        /// <param name="user">User.</param>
        /// <param name="buckets">List of BucketConfiguration.</param>
        /// <returns>True if successful.</returns>
        public bool GetUserBuckets(string user, out List<BucketConfiguration> buckets)
        {
            buckets = null;
            if (String.IsNullOrEmpty(user)) throw new ArgumentNullException(nameof(user));

            lock (_BucketConfigLock)
            { 
                List<BucketConfiguration> configs = _BucketConfigs.Where(b => b.Owner.Equals(user)).ToList();
                buckets = configs;
                return true;
            }
        }

        #endregion

        #region Private-Methods

        private void InitializeBuckets()
        {
            lock (_BucketConfigLock)
            {
                foreach (BucketConfiguration curr in _BucketConfigs)
                {
                    InitializeBucket(curr);
                }
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
                _Logging.Log(LoggingModule.Severity.Warn, "Destroy bucket " + bucket.Name + " failed");
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

            _Logging.Log(LoggingModule.Severity.Info, "Destroy bucket " + bucket.Name + ": " +
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
