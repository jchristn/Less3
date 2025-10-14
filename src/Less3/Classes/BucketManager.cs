namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Settings;
    using SyslogLogging;
    using Watson.ORM;

    /// <summary>
    /// Bucket manager.
    /// </summary>
    public class BucketManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SettingsBase _Settings;
        private LoggingModule _Logging;
        private ConfigManager _Config;
        private WatsonORM _ORM;

        private readonly object _BucketsLock = new object();
        private List<BucketClient> _Buckets = new List<BucketClient>();
         
        #endregion

        #region Constructors-and-Factories

        internal BucketManager(SettingsBase settings, LoggingModule logging, ConfigManager config, WatsonORM orm)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Config = config ?? throw new ArgumentNullException(nameof(config));
            _ORM = orm ?? throw new ArgumentNullException(nameof(orm));

            InitializeBuckets();
        }

        #endregion

        #region Internal-Methods
         
        internal bool Add(Bucket bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            bool success = _Config.AddBucket(bucket);
            if (success)
            {
                BucketClient client = new BucketClient(_Settings, _Logging, bucket, _ORM);

                lock (_BucketsLock)
                {
                    _Buckets.Add(client);
                }

                InitializeBucket(bucket);
            }

            return success; 
        }

        internal bool Remove(Bucket bucket, bool destroy)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            bool removed = false;

            if (_Config.BucketExists(bucket.Name))
            {
                BucketClient client = GetClient(bucket.Name);
                if (client != null)
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

        internal Bucket GetByName(string bucketName)
        {
            if (String.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            return _Config.GetBucketByName(bucketName);
        }

        internal Bucket GetByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            return _Config.GetBucketByGuid(guid);
        }

        internal BucketClient GetClient(string bucketName)
        { 
            if (String.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

            lock (_BucketsLock)
            {
                bool exists = _Buckets.Exists(b => b.Name.Equals(bucketName));
                if (!exists) return null;
                return _Buckets.First(b => b.Name.Equals(bucketName));
            }
        }

        internal List<Bucket> GetUserBuckets(string userGuid)
        { 
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            return _Config.GetBucketsByUser(userGuid);
        }

        #endregion

        #region Private-Methods

        private void InitializeBuckets()
        {
            List<Bucket> buckets = _Config.GetBuckets();

            if (buckets != null && buckets.Count > 0)
            {
                foreach (Bucket curr in buckets)
                {
                    InitializeBucket(curr);
                }
            }
        }

        private void InitializeBucket(Bucket bucket)
        {
            lock (_BucketsLock)
            {
                BucketClient client = new BucketClient(_Settings, _Logging, bucket, _ORM);
                _Buckets.Add(client);
            }
        }

        private bool Destroy(Bucket bucket)
        {  
            #region Delete-Object-Files

            bool objectFilesDelete = false;
            try
            {
                if (Directory.Exists(bucket.DiskDirectory))
                    ClearDirectory(bucket.DiskDirectory);
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
                if (Directory.Exists(bucket.DiskDirectory))
                    Directory.Delete(bucket.DiskDirectory);
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
                if (Directory.Exists(_Settings.Storage.DiskDirectory + bucket.Name))
                {
                    ClearDirectory(_Settings.Storage.DiskDirectory + bucket.Name);
                    rootFilesDelete = true;
                }
            }
            catch (Exception)
            {

            }

            #endregion

            #region Remove-Root-Directory

            bool rootDirectoryDelete = false;

            try
            {
                if (Directory.Exists(_Settings.Storage.DiskDirectory + bucket.Name))
                {
                    Directory.Delete(_Settings.Storage.DiskDirectory + bucket.Name);
                    rootDirectoryDelete = true;
                }
            }
            catch (Exception)
            {

            }

            #endregion

            _Logging.Info("Destroy bucket " + bucket.Name + ": " + 
                "obj files [" + objectFilesDelete + "] " +
                "obj dir [" + objectsDirectoryDelete + "] " +
                "root files [" + rootFilesDelete + "] " +
                "root dir [" + rootDirectoryDelete + "]");

            return objectFilesDelete && objectsDirectoryDelete && rootFilesDelete && rootDirectoryDelete;
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
