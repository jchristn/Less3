namespace Less3.Classes
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Less3.Database;
    using Less3.Settings;
    using Padlocks;
    using SyslogLogging;

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
        private DatabaseDriverBase _Database;

        private readonly ConcurrentDictionary<string, BucketClient> _Buckets = new ConcurrentDictionary<string, BucketClient>();
        private readonly Padlock<string> _BucketLocks = new Padlock<string>();

        #endregion

        #region Constructors-and-Factories

        internal BucketManager(SettingsBase settings, LoggingModule logging, ConfigManager config, DatabaseDriverBase database)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Config = config ?? throw new ArgumentNullException(nameof(config));
            _Database = database ?? throw new ArgumentNullException(nameof(database));

            InitializeBuckets();
        }

        #endregion

        #region Internal-Methods

        internal bool Add(Bucket bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));
            string key = ClientKey(bucket.TenantId, bucket.Name);

            using (_BucketLocks.Lock(key))
            {
                if (BucketNameValidator.IsInvalid(bucket.Name))
                {
                    _Logging.Warn("BucketManager Add invalid bucket name " + bucket.Name);
                    return false;
                }

                bool success = _Config.AddBucket(bucket);
                if (success)
                {
                    BucketClient client = new BucketClient(_Settings, _Logging, bucket, _Database);
                    _Buckets[key] = client;
                }

                return success;
            }
        }

        internal bool Remove(Bucket bucket, bool destroy)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            bool removed = false;
            BucketClient client = null;
            string key = ClientKey(bucket.TenantId, bucket.Name);

            using (_BucketLocks.Lock(key))
            {
                if (_Config.BucketExists(bucket.TenantId, bucket.Name))
                {
                    _Buckets.TryRemove(key, out client);

                    removed = true;

                    _Config.DeleteBucket(bucket.Id);
                }
            }

            if (client != null)
            {
                client.Dispose();
            }

            if (removed)
            {
                if (destroy)
                {
                    if (!Destroy(bucket))
                        _Logging.Warn("BucketManager Remove issues encountered removing data for bucket " + bucket.Name + ", cleanup required");
                    else
                        _Logging.Info("BucketManager Remove removed bucket with name " + bucket.Name + " with owner " + bucket.OwnerId);
                }
                else
                {
                    _Logging.Info("BucketManager Remove removed bucket with name " + bucket.Name + " with owner " + bucket.OwnerId);
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

        internal Bucket GetByName(string tenantId, string bucketName)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            return _Config.GetBucketByName(tenantId, bucketName);
        }

        internal Bucket GetById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Config.GetBucketById(id);
        }

        internal BucketClient GetClient(string bucketName)
        {
            if (String.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

            return _Buckets.Values.FirstOrDefault(b => b.Name.Equals(bucketName));
        }

        internal BucketClient GetClient(string tenantId, string bucketName)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));

            _Buckets.TryGetValue(ClientKey(tenantId, bucketName), out BucketClient client);
            return client;
        }

        internal List<Bucket> GetUserBuckets(string userId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            return _Config.GetBucketsByUser(userId);
        }

        internal List<Bucket> GetUserBuckets(string tenantId, string userId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            return _Config.GetBucketsByUser(tenantId, userId);
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
            string key = ClientKey(bucket.TenantId, bucket.Name);

            using (_BucketLocks.Lock(key))
            {
                BucketClient client = new BucketClient(_Settings, _Logging, bucket, _Database);
                if (!_Buckets.TryAdd(key, client))
                {
                    client.Dispose();
                }
            }
        }

        private static string ClientKey(string tenantId, string bucketName)
        {
            return (tenantId ?? "default") + ":" + bucketName;
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
            catch (Exception e)
            {
                _Logging.Warn("Destroy bucket " + bucket.Name + " failed to clear object files: " + e.Message);
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
            catch (Exception e)
            {
                _Logging.Warn("Destroy bucket " + bucket.Name + " failed to remove objects directory: " + e.Message);
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
            catch (Exception e)
            {
                _Logging.Warn("Destroy bucket " + bucket.Name + " failed to clear root files: " + e.Message);
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
            catch (Exception e)
            {
                _Logging.Warn("Destroy bucket " + bucket.Name + " failed to remove root directory: " + e.Message);
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
