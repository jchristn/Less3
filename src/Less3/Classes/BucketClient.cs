namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Less3.Database;
    using Less3.Locking;
    using Less3.Settings;
    using Less3.Storage;
    using Less3.Telemetry;
    using SyslogLogging;

    /// <summary>
    /// Bucket client.  All object construction, authentication, and authorization must occur prior to using bucket methods.
    /// </summary>
    internal class BucketClient : IDisposable
    {
        #region Internal-Members

        internal long StreamReadBufferSize
        {
            get
            {
                return _StreamReadBufferSize;
            }
            set
            {
                if (value < 1) throw new ArgumentException("StreamReadBufferSize must be greater than zero.");
                _StreamReadBufferSize = value;
            }
        }

        internal string Name
        {
            get
            {
                return _Bucket.Name;
            }
        }

        internal string Id
        {
            get
            {
                return _Bucket.Id;
            }
        }

        internal string TenantId
        {
            get
            {
                return _Bucket.TenantId;
            }
        }

        #endregion

        #region Private-Members

        private SettingsBase _Settings = null;
        private LoggingModule _Logging = null;
        private Bucket _Bucket = null;
        private DatabaseDriverBase _Database = null;
        private ILockManager _LockManager = null;
        private long _StreamReadBufferSize = 65536;
        private StorageDriverBase _StorageDriver = null;

        #endregion

        #region Constructors-and-Factories

        internal BucketClient()
        {

        }

        internal BucketClient(SettingsBase settings, LoggingModule logging, Bucket bucket, DatabaseDriverBase database, ILockManager lockManager)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _LockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));

            InitializeStorageDriver();
        }

        internal void UpdateBucket(Bucket bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            bool storageChanged =
                _Bucket == null ||
                _Bucket.StorageType != bucket.StorageType ||
                !String.Equals(_Bucket.DiskDirectory, bucket.DiskDirectory, StringComparison.Ordinal);

            _Bucket = bucket;

            if (storageChanged)
            {
                if (_StorageDriver is IDisposable disposable) disposable.Dispose();
                _StorageDriver = null;
                InitializeStorageDriver();
            }
        }

        #endregion

        #region Public-Methods

        public void Dispose()
        {
            if (_StorageDriver != null)
            {
                if (_StorageDriver is IDisposable disposable)
                    disposable.Dispose();
                _StorageDriver = null;
            }
        }

        #endregion

        #region Internal-Methods

        internal bool AddObject(Obj obj, byte[] data)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            long len = 0;
            using (MemoryStream ms = new MemoryStream())
            {
                if (data != null && data.Length > 0)
                {
                    len = data.Length;
                    ms.Write(data, 0, data.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                }

                obj.ContentLength = len;
                return AddObject(obj, ms);
            }
        }

        internal bool AddObject(Obj obj, Stream stream)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (String.IsNullOrEmpty(obj.Id)) obj.Id = Less3.Helpers.IdGenerator.GenerateObjectId();
            obj.BucketId = _Bucket.Id;

            // Serialize the entire read-modify-write on this object key across all nodes. The
            // exclusive Write lock prevents two writers from both computing the next version, and
            // the fencing token re-checked before the commit rejects a holder whose lease lapsed.
            string lockKey = LockKeys.Object(_Bucket.TenantId, _Bucket.Name, obj.Key);
            Activity activity = Less3Telemetry.StartObjectOperation("PutObject");
            Stopwatch sw = Stopwatch.StartNew();
            LockHandle handle = _LockManager.AcquireAsync(lockKey, LockMode.Write, new AcquireOptions(_Settings.Cluster.Lock.AcquireTimeoutMs), CancellationToken.None).GetAwaiter().GetResult();
            Less3Telemetry.ObjectStage(activity, "PutObject", "lock_acquire", sw.Elapsed.TotalMilliseconds);
            sw.Restart();

            string supersededBlob = null;
            bool blobWritten = false;

            try
            {
                Obj test = GetObjectLatestMetadata(obj.Key);
                Less3Telemetry.ObjectStage(activity, "PutObject", "metadata_read", sw.Elapsed.TotalMilliseconds);
                sw.Restart();
                if (test != null)
                {
                    if (!_Bucket.EnableVersioning)
                    {
                        supersededBlob = RemoveSupersededUnversionedObject(test);
                        obj.Version = 1;
                    }
                    else
                    {
                        obj.Version = (test.Version + 1);
                    }
                }
                else
                {
                    obj.Version = 1;
                }

                obj.Md5 = Common.BytesToHexString(_StorageDriver.Write(obj.BlobFilename, obj.ContentLength, stream)).ToLowerInvariant();
                blobWritten = true;
                Less3Telemetry.BlobWritten(obj.ContentLength);
                Less3Telemetry.ObjectStage(activity, "PutObject", "storage_write", sw.Elapsed.TotalMilliseconds);
                sw.Restart();

                if (String.IsNullOrEmpty(obj.Etag)) obj.Etag = obj.Md5;

                DateTime ts = DateTime.Now.ToUniversalTime();
                obj.CreatedUtc = ts;
                obj.LastAccessUtc = ts;
                obj.LastUpdateUtc = ts;
                obj.ExpirationUtc = null;

                if (!_LockManager.ValidateAsync(handle, CancellationToken.None).GetAwaiter().GetResult())
                {
                    Less3.Telemetry.Less3Telemetry.FencingConflict("AddObject");
                    throw new LockLostException(lockKey, handle.HolderId);
                }

                _Database.Objects.Insert(obj);
                Less3Telemetry.ObjectStage(activity, "PutObject", "db_commit", sw.Elapsed.TotalMilliseconds);
                sw.Restart();

                // Delete the superseded blob only after the new metadata is committed (R15). A
                // crash before this point leaves the superseded blob in place, never data loss.
                if (!String.IsNullOrEmpty(supersededBlob) && _StorageDriver.Exists(supersededBlob))
                {
                    try { _StorageDriver.Delete(supersededBlob); }
                    catch (Exception e) { _Logging.Warn("AddObject failed to delete superseded blob " + supersededBlob + ": " + e.Message); }
                    Less3Telemetry.ObjectStage(activity, "PutObject", "blob_delete", sw.Elapsed.TotalMilliseconds);
                }

                return true;
            }
            catch (Exception e)
            {
                // A new blob's filename is its unique object id, so a failed commit leaves only a
                // harmless orphan; remove it eagerly.
                if (blobWritten)
                {
                    try { if (_StorageDriver.Exists(obj.BlobFilename)) _StorageDriver.Delete(obj.BlobFilename); }
                    catch (Exception) { }
                }

                // The unique (tenant, bucket, key, version) index is the database-enforced backstop
                // behind the write lock. If it rejects this insert, another writer committed the same
                // version concurrently (or a lease lapsed and a superseding holder won the race). That
                // is a data-integrity conflict caught before any corruption, so surface it as such.
                if (SqlErrorClassifier.IsUniqueConstraintViolation(e))
                {
                    Less3Telemetry.FencingConflict("AddObject");
                    _Logging.Warn("AddObject version conflict on " + _Bucket.Name + "/" + obj.Key + " version " + obj.Version + "; concurrent write rejected by unique constraint");
                }

                throw;
            }
            finally
            {
                _LockManager.ReleaseAsync(handle, CancellationToken.None).GetAwaiter().GetResult();
                activity?.Dispose();
            }
        }

        internal bool AddObjectMetadata(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            obj.BucketId = _Bucket.Id;

            LockHandle handle = AcquireObjectLock(obj.Key, LockMode.Write);
            try
            {
                Obj test = GetObjectLatestMetadata(obj.Key);
                if (test != null)
                {
                    if (!_Bucket.EnableVersioning)
                    {
                        ReplaceLatestUnversionedObject(test);
                        obj.Version = 1;
                    }
                    else
                    {
                        obj.Version = (test.Version + 1);
                    }
                }
                else
                {
                    obj.Version = 1;
                }

                DateTime ts = DateTime.Now.ToUniversalTime();
                obj.CreatedUtc = ts;
                obj.LastAccessUtc = ts;
                obj.LastUpdateUtc = ts;
                obj.ExpirationUtc = null;

                if (!_LockManager.ValidateAsync(handle, CancellationToken.None).GetAwaiter().GetResult())
                    throw new LockLostException(LockKeys.Object(_Bucket.TenantId, _Bucket.Name, obj.Key), handle.HolderId);

                _Database.Objects.Insert(obj);
                return true;
            }
            finally
            {
                _LockManager.ReleaseAsync(handle, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        internal bool GetObjectLatest(string key, out byte[] data)
        {
            data = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            // Shared read lock: any number of reads run concurrently, but a write or delete on this
            // key waits until in-flight reads flush.
            LockHandle handle = AcquireObjectLock(key, LockMode.Read);
            try
            {
                Obj obj = GetObjectLatestMetadata(key);
                if (obj == null) return false;

                data = _StorageDriver.Read(obj.BlobFilename);
                return true;
            }
            finally
            {
                _LockManager.ReleaseAsync(handle, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        internal bool GetObjectLatest(string key, out long contentLength, out Stream stream)
        {
            contentLength = 0;
            stream = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            // The shared read lock is held for the whole streamed response and released when the
            // returned stream is disposed (see LockReleasingStream).
            Activity activity = Less3Telemetry.StartObjectOperation("GetObject");
            Stopwatch sw = Stopwatch.StartNew();
            LockHandle handle = AcquireObjectLock(key, LockMode.Read);
            Less3Telemetry.ObjectStage(activity, "GetObject", "lock_acquire", sw.Elapsed.TotalMilliseconds);
            sw.Restart();
            bool handedOff = false;
            try
            {
                Obj obj = GetObjectLatestMetadata(key);
                Less3Telemetry.ObjectStage(activity, "GetObject", "metadata_read", sw.Elapsed.TotalMilliseconds);
                sw.Restart();
                if (obj == null) return false;

                ObjectStream objStream = _StorageDriver.ReadStream(obj.BlobFilename);
                Less3Telemetry.ObjectStage(activity, "GetObject", "storage_open", sw.Elapsed.TotalMilliseconds);
                contentLength = objStream.ContentLength;
                stream = new LockReleasingStream(objStream.Data, _LockManager, handle);
                handedOff = true;
                return true;
            }
            finally
            {
                if (!handedOff) _LockManager.ReleaseAsync(handle, CancellationToken.None).GetAwaiter().GetResult();
                activity?.Dispose();
            }
        }

        internal bool GetObjectLatestRange(string key, long startPosition, long length, out Stream stream)
        {
            stream = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (startPosition < 0) throw new ArgumentNullException(nameof(startPosition));
            if (length < 0) throw new ArgumentNullException(nameof(length));

            LockHandle handle = AcquireObjectLock(key, LockMode.Read);
            bool handedOff = false;
            try
            {
                Obj obj = GetObjectLatestMetadata(key);
                if (obj == null) return false;

                ObjectStream objStream = _StorageDriver.ReadRangeStream(obj.BlobFilename, startPosition, length);
                stream = new LockReleasingStream(objStream.Data, _LockManager, handle);
                handedOff = true;
                return true;
            }
            finally
            {
                if (!handedOff) _LockManager.ReleaseAsync(handle, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        internal long GetObjectLatestVersion(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return _Database.Objects.GetLatestVersion(key, _Bucket.Id);
        }

        internal BucketStatistics GetFullStatistics()
        {
            BucketStatistics ret = _Database.Objects.GetStatistics(_Bucket.Id);
            ret.Id = _Bucket.Id;
            ret.Name = _Bucket.Name;
            return ret;
        }

        internal BucketStatistics GetStatistics(List<Obj> objects)
        {
            BucketStatistics ret = new BucketStatistics(_Bucket.Name, _Bucket.Id, 0, 0);

            if (objects != null && objects.Count > 0)
            {
                ret.Objects = objects.Count;
                ret.Bytes = objects.Sum(o => o.ContentLength);
            }

            return ret;
        }

        internal Obj GetObjectLatestMetadata(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return _Database.Objects.GetLatestByKey(key, _Bucket.Id);
        }

        internal Obj GetObjectVersionMetadata(string key, long version = 1)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return _Database.Objects.GetByKeyAndVersion(key, version, _Bucket.Id);
        }

        internal Obj GetObjectMetadataById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.Objects.GetById(id, _Bucket.Id);
        }

        internal bool ObjectExists(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            Obj obj = GetObjectLatestMetadata(key);
            if (obj != null) return true;
            return false;
        }

        internal bool ObjectVersionExists(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            Obj obj = GetObjectVersionMetadata(key, version);
            if (obj != null) return true;
            return false;
        }

        internal bool DeleteLatestObject(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Activity activity = Less3Telemetry.StartObjectOperation("DeleteObject");
            Stopwatch sw = Stopwatch.StartNew();
            LockHandle handle = AcquireObjectLock(key, LockMode.Delete);
            Less3Telemetry.ObjectStage(activity, "DeleteObject", "lock_acquire", sw.Elapsed.TotalMilliseconds);
            sw.Restart();
            try
            {
                Obj obj = GetObjectLatestMetadata(key);
                Less3Telemetry.ObjectStage(activity, "DeleteObject", "metadata_read", sw.Elapsed.TotalMilliseconds);
                sw.Restart();
                if (obj == null)
                {
                    _Logging.Debug("Delete unable to find key " + _Bucket.Name + "/" + key);
                    return false;
                }

                if (!_LockManager.ValidateAsync(handle, CancellationToken.None).GetAwaiter().GetResult())
                    throw new LockLostException(LockKeys.Object(_Bucket.TenantId, _Bucket.Name, key), handle.HolderId);

                if (_Bucket.EnableVersioning)
                {
                    _Logging.Info("Delete marking key " + _Bucket.Name + "/" + key + " as deleted");
                    obj.DeleteMarker = true;
                    _Database.Objects.Update(obj);
                    Less3Telemetry.ObjectStage(activity, "DeleteObject", "db_commit", sw.Elapsed.TotalMilliseconds);
                    return true;
                }
                else
                {
                    _Logging.Info("Delete deleting key " + _Bucket.Name + "/" + key);
                    _Database.Objects.Delete(obj);
                    Less3Telemetry.ObjectStage(activity, "DeleteObject", "db_commit", sw.Elapsed.TotalMilliseconds);
                    sw.Restart();
                    _StorageDriver.Delete(obj.BlobFilename);
                    Less3Telemetry.ObjectStage(activity, "DeleteObject", "blob_delete", sw.Elapsed.TotalMilliseconds);
                    return true;
                }
            }
            finally
            {
                _LockManager.ReleaseAsync(handle, CancellationToken.None).GetAwaiter().GetResult();
                activity?.Dispose();
            }
        }

        internal bool DeleteObjectVersion(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            LockHandle handle = AcquireObjectLock(key, LockMode.Delete);
            try
            {
                Obj obj = GetObjectVersionMetadata(key, version);
                if (obj == null)
                {
                    _Logging.Debug("Delete unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                    return false;
                }

                if (!_LockManager.ValidateAsync(handle, CancellationToken.None).GetAwaiter().GetResult())
                    throw new LockLostException(LockKeys.Object(_Bucket.TenantId, _Bucket.Name, key), handle.HolderId);

                if (_Bucket.EnableVersioning)
                {
                    _Logging.Info("Delete marking key " + _Bucket.Name + "/" + key + " version " + version + " as deleted");
                    obj.DeleteMarker = true;
                    _Database.Objects.Update(obj);
                    return true;
                }
                else
                {
                    _Logging.Info("Delete deleting key " + _Bucket.Name + "/" + key + " version " + version);
                    _Database.Objects.Delete(obj);
                    _StorageDriver.Delete(obj.BlobFilename);
                    return true;
                }
            }
            finally
            {
                _LockManager.ReleaseAsync(handle, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        internal bool DeleteObjectVersionMetadata(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectVersionMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("Delete unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return false;
            }

            if (_Bucket.EnableVersioning)
            {
                _Logging.Info("Delete marking key " + _Bucket.Name + "/" + key + " as deleted");
                obj.DeleteMarker = true;
                _Database.Objects.Update(obj);
                return true;
            }
            else
            {
                _Logging.Info("Delete deleting key " + _Bucket.Name + "/" + key);
                _Database.Objects.Delete(obj);
                return true;
            }
        }

        internal void Enumerate(
            string delimiter,
            string prefix,
            int startIndex,
            int maxResults,
            out List<Obj> objects,
            out List<string> prefixes,
            out int nextStartIndex,
            out bool isTruncated)
        {
            EnumerateInternal(delimiter, prefix, startIndex, maxResults, true, true, out objects, out prefixes, out nextStartIndex, out isTruncated);
        }

        internal void EnumerateVersions(
            string delimiter,
            string prefix,
            int startIndex,
            int maxResults,
            out List<Obj> objects,
            out List<string> prefixes,
            out int nextStartIndex,
            out bool isTruncated)
        {
            EnumerateInternal(delimiter, prefix, startIndex, maxResults, false, false, out objects, out prefixes, out nextStartIndex, out isTruncated);
        }

        private void EnumerateInternal(
            string delimiter,
            string prefix,
            int startIndex,
            int maxResults,
            bool excludeDeleteMarkers,
            bool latestOnly,
            out List<Obj> objects,
            out List<string> prefixes,
            out int nextStartIndex,
            out bool isTruncated)
        {
            objects = new List<Obj>();
            prefixes = new List<string>();
            nextStartIndex = startIndex;
            isTruncated = false;

            while (true)
            {
                #region Retrieve-Records

                List<Obj> tempObjects = _Database.Objects.Enumerate(
                    _Bucket.Id,
                    nextStartIndex,
                    maxResults,
                    excludeDeleteMarkers,
                    prefix);

                if (tempObjects == null || tempObjects.Count < 1)
                {
                    break;
                }

                #endregion

                #region Process-Records

                foreach (Obj obj in tempObjects)
                {
                    string currPrefix = null;
                    string tempKey = obj.Key;

                    if (!String.IsNullOrEmpty(prefix) && tempKey.StartsWith(prefix))
                        tempKey = tempKey.Substring(prefix.Length);

                    if (!String.IsNullOrEmpty(delimiter))
                    {
                        if (tempKey.Contains(delimiter))
                        {
                            int delimiterPos = tempKey.IndexOf(delimiter);
                            currPrefix = prefix + tempKey.Substring(0, delimiterPos + delimiter.Length);
                            if (!prefixes.Contains(currPrefix))
                            {
                                prefixes.Add(currPrefix);
                            }
                        }
                        else if (obj.IsFolder && obj.ContentLength == 0 && !String.IsNullOrEmpty(tempKey))
                        {
                            currPrefix = prefix + tempKey;
                            if (!currPrefix.EndsWith(delimiter)) currPrefix += delimiter;
                            if (!prefixes.Contains(currPrefix))
                            {
                                prefixes.Add(currPrefix);
                            }
                        }
                    }

                    if (String.IsNullOrEmpty(currPrefix) && objects.Count < maxResults)
                    {
                        objects.Add(obj);
                    }

                    nextStartIndex++;
                }

                if (objects.Count >= maxResults)
                {
                    isTruncated = true;
                    break;
                }

                #endregion
            }

            if (latestOnly)
            {
                Dictionary<string, Obj> latestByKey = new Dictionary<string, Obj>();
                foreach (Obj obj in objects)
                {
                    if (!latestByKey.ContainsKey(obj.Key))
                    {
                        latestByKey[obj.Key] = obj;
                    }
                    else if (obj.Version > latestByKey[obj.Key].Version)
                    {
                        latestByKey[obj.Key] = obj;
                    }
                }

                objects = latestByKey.Values.OrderBy(o => o.Key).ThenBy(o => o.Id).ToList();
            }
            else
            {
                objects = objects
                    .OrderBy(o => o.Key, StringComparer.Ordinal)
                    .ThenByDescending(o => o.Version)
                    .ThenByDescending(o => o.Id)
                    .ToList();
            }

            return;
        }

        internal void AddBucketTags(List<BucketTag> tags)
        {
            DeleteBucketTags();

            if (tags != null && tags.Count > 0)
            {
                foreach (BucketTag tag in tags)
                {
                    tag.TenantId = _Bucket.TenantId;
                    tag.BucketId = _Bucket.Id;
                    _Database.BucketTags.Insert(tag);
                }
            }
        }

        internal void AddObjectVersionTags(string key, long version, List<ObjectTag> tags)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (version < 1) throw new ArgumentException("Version ID must be one or greater.");

            DeleteObjectVersionTags(key, version);

            if (tags != null && tags.Count > 0)
            {
                foreach (ObjectTag tag in tags)
                {
                    tag.TenantId = _Bucket.TenantId;
                    tag.BucketId = _Bucket.Id;
                    _Database.ObjectTags.Insert(tag);
                }
            }
        }

        internal List<BucketTag> GetBucketTags()
        {
            return _Database.BucketTags.GetByBucketId(_Bucket.TenantId, _Bucket.Id);
        }

        internal List<ObjectTag> GetObjectTags(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (version < 1) throw new ArgumentException("Version ID must be one or greater.");

            Obj obj = GetObjectVersionMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("GetTags unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return null;
            }

            return _Database.ObjectTags.GetByObjectId(_Bucket.TenantId, obj.Id, _Bucket.Id);
        }

        internal List<ObjectTag> GetObjectTags(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.ObjectTags.GetByObjectId(_Bucket.TenantId, id, _Bucket.Id);
        }

        internal void DeleteBucketTags()
        {
            _Database.BucketTags.DeleteByBucketId(_Bucket.Id);
        }

        internal void DeleteObjectVersionTags(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectVersionMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("Exists unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return;
            }

            _Database.ObjectTags.DeleteByObjectId(obj.Id, _Bucket.Id);
        }

        internal bool ObjectGroupAclExists(string groupName, string key, long version)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectVersionMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("Exists unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return false;
            }

            return _Database.ObjectAcls.ExistsByGroupName(_Bucket.TenantId, groupName, obj.Id, _Bucket.Id);
        }

        internal bool ObjectUserAclExists(string userId, string key, long version)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectVersionMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("Exists unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return false;
            }

            return _Database.ObjectAcls.ExistsByUserId(_Bucket.TenantId, userId, obj.Id, _Bucket.Id);
        }

        internal bool BucketGroupAclExists(string groupName)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            return _Database.BucketAcls.ExistsByGroupName(_Bucket.TenantId, groupName, _Bucket.Id);
        }

        internal bool BucketUserAclExists(string userId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            return _Database.BucketAcls.ExistsByUserId(_Bucket.TenantId, userId, _Bucket.Id);
        }

        internal List<BucketAcl> GetBucketAcl()
        {
            return _Database.BucketAcls.GetByBucketId(_Bucket.TenantId, _Bucket.Id);
        }

        internal List<ObjectAcl> GetObjectVersionAcl(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectVersionMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("GetAcl unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return null;
            }

            return _Database.ObjectAcls.GetByObjectId(_Bucket.TenantId, obj.Id, _Bucket.Id);
        }

        internal List<ObjectAcl> GetObjectAcl(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            return _Database.ObjectAcls.GetByObjectId(_Bucket.TenantId, id, _Bucket.Id);
        }

        internal void AddBucketAcl(BucketAcl acl)
        {
            if (acl != null)
            {
                acl.BucketId = _Bucket.Id;
                acl.TenantId = _Bucket.TenantId;
                _Database.BucketAcls.Insert(acl);
            }
        }

        internal void SetBucketAcls(List<BucketAcl> acls)
        {
            DeleteBucketAcl();

            if (acls != null && acls.Count > 0)
            {
                foreach (BucketAcl acl in acls)
                {
                    acl.BucketId = _Bucket.Id;
                    acl.TenantId = _Bucket.TenantId;
                    _Database.BucketAcls.Insert(acl);
                }
            }
        }

        internal void AddObjectAcl(ObjectAcl acl)
        {
            if (acl != null)
            {
                Obj obj = GetObjectMetadataById(acl.ObjectId);
                if (obj == null)
                {
                    _Logging.Debug("SetAcl unable to find object Id " + acl.ObjectId + " in bucket " + _Bucket.Name);
                    return;
                }

                acl.BucketId = _Bucket.Id;
                acl.ObjectId = obj.Id;
                acl.TenantId = _Bucket.TenantId;
                _Database.ObjectAcls.Insert(acl);
            }
        }

        internal void SetObjectAcls(string key, long version, List<ObjectAcl> acls)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectVersionMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("SetAcl unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return;
            }

            DeleteObjectVersionAcl(key, version);

            if (acls != null && acls.Count > 0)
            {
                foreach (ObjectAcl acl in acls)
                {
                    acl.BucketId = _Bucket.Id;
                    acl.ObjectId = obj.Id;
                    acl.TenantId = _Bucket.TenantId;
                    _Database.ObjectAcls.Insert(acl);
                }
            }
        }

        internal void DeleteBucketAcl()
        {
            _Database.BucketAcls.DeleteByBucketId(_Bucket.Id);
        }

        internal void DeleteObjectVersionAcl(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectVersionMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("DeleteAcl unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return;
            }

            _Database.ObjectAcls.DeleteByObjectIdAndBucketId(obj.Id, _Bucket.Id);
        }

        internal void DeleteObjectAcl(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectLatestMetadata(key);
            if (obj == null)
            {
                _Logging.Debug("DeleteAcl unable to find key " + _Bucket.Name + "/" + key);
                return;
            }

            _Database.ObjectAcls.DeleteByObjectIdAndBucketId(obj.Id, _Bucket.Id);
        }

        #endregion

        #region Private-Methods

        private LockHandle AcquireObjectLock(string key, LockMode mode)
        {
            string lockKey = LockKeys.Object(_Bucket.TenantId, _Bucket.Name, key);
            return _LockManager.AcquireAsync(lockKey, mode, new AcquireOptions(_Settings.Cluster.Lock.AcquireTimeoutMs), CancellationToken.None).GetAwaiter().GetResult();
        }

        private void InitializeStorageDriver()
        {
            switch (_Bucket.StorageType)
            {
                case StorageDriverType.Disk:
                    if (!Directory.Exists(_Bucket.DiskDirectory)) Directory.CreateDirectory(_Bucket.DiskDirectory);
                    _StorageDriver = new DiskStorageDriver(_Bucket.DiskDirectory);
                    break;

                default:
                    throw new ArgumentException("Unknown storage driver type '" + _Bucket.StorageType.ToString() + "' in bucket Id " + _Bucket.Id + ".");
            }
        }

        private void ReplaceLatestUnversionedObject(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            DeleteObjectVersionAcl(obj.Key, obj.Version);
            DeleteObjectVersionTags(obj.Key, obj.Version);
            _Database.Objects.Delete(obj);

            if (!obj.DeleteMarker && _StorageDriver.Exists(obj.BlobFilename))
            {
                _StorageDriver.Delete(obj.BlobFilename);
            }
        }

        private string RemoveSupersededUnversionedObject(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            DeleteObjectVersionAcl(obj.Key, obj.Version);
            DeleteObjectVersionTags(obj.Key, obj.Version);
            _Database.Objects.Delete(obj);

            // Return the blob to delete after the replacement is committed, rather than deleting it
            // now, so a mid-operation crash never destroys the only copy.
            if (!obj.DeleteMarker) return obj.BlobFilename;
            return null;
        }

        #endregion
    }
}
