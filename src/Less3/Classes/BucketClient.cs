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

        /// <summary>
        /// True when versioning is enabled or suspended, i.e. the bucket reports version IDs.
        /// </summary>
        internal bool VersioningConfigured
        {
            get { return _Bucket.EnableVersioning || _Bucket.VersioningSuspended; }
        }

        /// <summary>
        /// The version ID Amazon S3 would report for a row: "null" for the null version (and for every row
        /// in a bucket that has never had versioning enabled), otherwise the version number.
        /// </summary>
        internal string VersionIdString(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (obj.NullVersion || !VersioningConfigured) return ObjectVersionReference.NullVersionId;
            return obj.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

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
            return AddObject(obj, stream, null, null, null);
        }

        /// <summary>
        /// Write a new object version. With versioning enabled the write becomes the new latest version;
        /// otherwise it replaces the key's null version (versions written while versioning was enabled
        /// are kept). The new row is committed before the replaced row and its blob are removed, so a
        /// crash part-way through never loses the only copy.
        /// </summary>
        /// <param name="obj">Object row to insert; its version and null-version flag are assigned here.</param>
        /// <param name="stream">Object content.</param>
        /// <param name="preconditions">Conditional-write headers, or null.</param>
        /// <param name="acls">ACL entries for the new row, or null. Applied while the write lock is held.</param>
        /// <param name="tags">Tags for the new row, or null. Applied while the write lock is held.</param>
        /// <exception cref="ObjectPreconditionException">Thrown when a conditional-write precondition is not met.</exception>
        internal bool AddObject(Obj obj, Stream stream, ObjectWritePreconditions preconditions, List<ObjectAcl> acls, List<ObjectTag> tags)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (String.IsNullOrEmpty(obj.Id)) obj.Id = Less3.Helpers.IdGenerator.GenerateObjectId();
            obj.BucketId = _Bucket.Id;
            obj.TenantId = _Bucket.TenantId;

            // Serialize the entire read-modify-write on this object key across all nodes. The
            // exclusive Write lock prevents two writers from both computing the next version, and
            // the fencing token re-checked before the commit rejects a holder whose lease lapsed.
            string lockKey = LockKeys.Object(_Bucket.TenantId, _Bucket.Name, obj.Key);
            Activity activity = Less3Telemetry.StartObjectOperation("PutObject");
            Stopwatch sw = Stopwatch.StartNew();
            LockHandle handle = _LockManager.AcquireAsync(lockKey, LockMode.Write, new AcquireOptions(_Settings.Cluster.Lock.AcquireTimeoutMs), CancellationToken.None).GetAwaiter().GetResult();
            Less3Telemetry.ObjectStage(activity, "PutObject", "lock_acquire", sw.Elapsed.TotalMilliseconds);
            sw.Restart();

            bool blobWritten = false;
            bool committed = false;

            try
            {
                BucketVersioningState versioning = CurrentVersioning();
                Obj latest = GetObjectLatestMetadata(obj.Key);
                CheckWritePreconditions(latest, preconditions);

                Obj superseded = SelectReplacedVersion(obj.Key, latest, versioning);
                Less3Telemetry.ObjectStage(activity, "PutObject", "metadata_read", sw.Elapsed.TotalMilliseconds);
                sw.Restart();

                obj.Version = (latest != null ? latest.Version : 0) + 1;
                obj.NullVersion = !versioning.Enabled;
                obj.DeleteMarker = false;
                if (String.IsNullOrEmpty(obj.BlobFilename)) obj.BlobFilename = obj.Id;

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
                committed = true;
                Less3Telemetry.ObjectStage(activity, "PutObject", "db_commit", sw.Elapsed.TotalMilliseconds);
                sw.Restart();

                // ACLs and tags are written before the lock is released, so no reader or writer ever sees
                // the new version without them.
                InsertAclsAndTags(obj, acls, tags);

                // Remove the replaced null version only after the new row is committed (R15). A crash
                // before this point leaves the old version in place as an extra version, never data loss.
                if (superseded != null)
                {
                    RemoveReplacedVersionQuietly(superseded, "AddObject");
                    Less3Telemetry.ObjectStage(activity, "PutObject", "blob_delete", sw.Elapsed.TotalMilliseconds);
                }

                return true;
            }
            catch (Exception e)
            {
                // A new blob's filename is its unique object id, so a failed commit leaves only a
                // harmless orphan; remove it eagerly. Once the row is committed the blob is live and must stay.
                if (blobWritten && !committed)
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

        /// <summary>
        /// Delete an object following Amazon S3 semantics.
        /// A specific version (numbered or "null") is permanently removed along with its ACLs, tags and blob.
        /// Without a version, a versioning-enabled bucket gets a new delete marker; a suspended bucket
        /// replaces the key's null version with a null delete marker; an unversioned bucket permanently
        /// removes the object. Deleting a key or version that does not exist succeeds with Found = false,
        /// except that a versioned bucket still records a delete marker, as Amazon S3 does.
        /// </summary>
        internal ObjectDeleteResult DeleteObject(string key, ObjectVersionReference reference, string requesterId)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (reference == null) throw new ArgumentNullException(nameof(reference));

            string lockKey = LockKeys.Object(_Bucket.TenantId, _Bucket.Name, key);
            Activity activity = Less3Telemetry.StartObjectOperation("DeleteObject");
            Stopwatch sw = Stopwatch.StartNew();
            LockHandle handle = AcquireObjectLock(key, LockMode.Delete);
            Less3Telemetry.ObjectStage(activity, "DeleteObject", "lock_acquire", sw.Elapsed.TotalMilliseconds);
            sw.Restart();

            try
            {
                ObjectDeleteResult result = new ObjectDeleteResult();

                if (!reference.IsLatest)
                {
                    Obj target = ResolveObject(key, reference);
                    Less3Telemetry.ObjectStage(activity, "DeleteObject", "metadata_read", sw.Elapsed.TotalMilliseconds);
                    sw.Restart();

                    result.VersionId = reference.ToString();
                    if (target == null)
                    {
                        _Logging.Debug("DeleteObject version " + reference.ToString() + " of " + _Bucket.Name + "/" + key + " does not exist");
                        return result;
                    }

                    ValidateFence(handle, lockKey, "DeleteObject");
                    string blob = RemoveVersionRecord(target);
                    Less3Telemetry.ObjectStage(activity, "DeleteObject", "db_commit", sw.Elapsed.TotalMilliseconds);
                    sw.Restart();
                    DeleteBlobQuietly(blob, "DeleteObject");
                    Less3Telemetry.ObjectStage(activity, "DeleteObject", "blob_delete", sw.Elapsed.TotalMilliseconds);

                    _Logging.Info("DeleteObject permanently removed " + _Bucket.Name + "/" + key + " version " + VersionIdString(target));
                    result.Found = true;
                    result.DeleteMarker = target.DeleteMarker;
                    result.VersionId = VersionIdString(target);
                    return result;
                }

                BucketVersioningState versioning = CurrentVersioning();
                Obj latest = GetObjectLatestMetadata(key);
                Less3Telemetry.ObjectStage(activity, "DeleteObject", "metadata_read", sw.Elapsed.TotalMilliseconds);
                sw.Restart();

                if (versioning.Configured)
                {
                    Obj replaced = versioning.Suspended ? _Database.Objects.GetNullVersion(key, _Bucket.Id) : null;

                    DateTime ts = DateTime.UtcNow;
                    Obj marker = new Obj();
                    marker.Id = Less3.Helpers.IdGenerator.GenerateObjectId();
                    marker.TenantId = _Bucket.TenantId;
                    marker.BucketId = _Bucket.Id;
                    marker.Key = key;
                    marker.OwnerId = requesterId;
                    marker.AuthorId = requesterId;
                    marker.ContentType = null;
                    marker.ContentLength = 0;
                    marker.BlobFilename = null;
                    marker.DeleteMarker = true;
                    marker.NullVersion = versioning.Suspended;
                    marker.Version = (latest != null ? latest.Version : 0) + 1;
                    marker.CreatedUtc = ts;
                    marker.LastUpdateUtc = ts;
                    marker.LastAccessUtc = ts;

                    ValidateFence(handle, lockKey, "DeleteObject");
                    _Database.Objects.Insert(marker);
                    Less3Telemetry.ObjectStage(activity, "DeleteObject", "db_commit", sw.Elapsed.TotalMilliseconds);
                    sw.Restart();

                    if (replaced != null)
                    {
                        RemoveReplacedVersionQuietly(replaced, "DeleteObject");
                        Less3Telemetry.ObjectStage(activity, "DeleteObject", "blob_delete", sw.Elapsed.TotalMilliseconds);
                    }

                    _Logging.Info("DeleteObject created delete marker " + VersionIdString(marker) + " for " + _Bucket.Name + "/" + key);
                    result.Found = true;
                    result.DeleteMarker = true;
                    result.CreatedDeleteMarker = true;
                    result.VersionId = VersionIdString(marker);
                    return result;
                }

                if (latest == null)
                {
                    _Logging.Debug("DeleteObject key " + _Bucket.Name + "/" + key + " does not exist");
                    return result;
                }

                ValidateFence(handle, lockKey, "DeleteObject");
                string latestBlob = RemoveVersionRecord(latest);
                Less3Telemetry.ObjectStage(activity, "DeleteObject", "db_commit", sw.Elapsed.TotalMilliseconds);
                sw.Restart();
                DeleteBlobQuietly(latestBlob, "DeleteObject");
                Less3Telemetry.ObjectStage(activity, "DeleteObject", "blob_delete", sw.Elapsed.TotalMilliseconds);

                _Logging.Info("DeleteObject deleted " + _Bucket.Name + "/" + key);
                result.Found = true;
                return result;
            }
            finally
            {
                _LockManager.ReleaseAsync(handle, CancellationToken.None).GetAwaiter().GetResult();
                activity?.Dispose();
            }
        }

        /// <summary>
        /// Resolve the row a version reference refers to, or null. The row may be a delete marker.
        /// </summary>
        internal Obj ResolveObject(string key, ObjectVersionReference reference)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (reference == null) throw new ArgumentNullException(nameof(reference));

            if (reference.IsLatest) return GetObjectLatestMetadata(key);

            if (reference.IsNullVersion)
            {
                Obj nullVersion = _Database.Objects.GetNullVersion(key, _Bucket.Id);
                if (nullVersion != null) return nullVersion;

                // Rows written before null versions were tracked, in a bucket that has never had
                // versioning enabled, are that key's null version.
                if (!VersioningConfigured) return GetObjectLatestMetadata(key);
                return null;
            }

            return GetObjectVersionMetadata(key, reference.Version);
        }

        /// <summary>
        /// Open an object for reading while holding the shared read lock on its key; the lock is released
        /// when the returned stream is disposed. The row is re-resolved under the lock: with followLatest the
        /// current latest version is served (so a read racing an overwrite gets one complete version rather
        /// than a not-found), otherwise the exact row requested. selectRange chooses the bytes from the row
        /// actually served and may throw to reject an unsatisfiable range; null selects the whole object.
        /// Returns null when there is nothing to serve (deleted, or the latest version is a delete marker).
        /// </summary>
        internal ObjectReadResult OpenObject(Obj requested, bool followLatest, Func<Obj, ObjectByteRange> selectRange)
        {
            if (requested == null) throw new ArgumentNullException(nameof(requested));

            Activity activity = Less3Telemetry.StartObjectOperation("GetObject");
            Stopwatch sw = Stopwatch.StartNew();
            LockHandle handle = AcquireObjectLock(requested.Key, LockMode.Read);
            Less3Telemetry.ObjectStage(activity, "GetObject", "lock_acquire", sw.Elapsed.TotalMilliseconds);
            sw.Restart();
            bool handedOff = false;

            try
            {
                Obj current = followLatest
                    ? GetObjectLatestMetadata(requested.Key)
                    : _Database.Objects.GetById(requested.Id, _Bucket.Id);

                if (current == null || current.DeleteMarker || String.IsNullOrEmpty(current.BlobFilename)) return null;

                ObjectByteRange range = selectRange != null ? selectRange(current) : new ObjectByteRange(0, current.ContentLength);
                ObjectStream objStream = _StorageDriver.ReadRangeStream(current.BlobFilename, range.Start, range.Length);
                Less3Telemetry.ObjectStage(activity, "GetObject", "storage_open", sw.Elapsed.TotalMilliseconds);

                ObjectReadResult result = new ObjectReadResult();
                result.Object = current;
                result.Start = range.Start;
                result.Length = range.Length;
                result.Data = new LockReleasingStream(objStream.Data, _LockManager, handle);
                handedOff = true;
                return result;
            }
            finally
            {
                if (!handedOff) _LockManager.ReleaseAsync(handle, CancellationToken.None).GetAwaiter().GetResult();
                activity?.Dispose();
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

        /// <summary>
        /// True when the bucket holds any object version or delete marker. Amazon S3 refuses to delete
        /// a bucket until every version and delete marker is gone.
        /// </summary>
        internal bool HasAnyVersions()
        {
            return _Database.Objects.EnumerateVersions(_Bucket.Id, null, null, null, 1).Count > 0;
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

        internal bool ObjectExists(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            Obj obj = GetObjectLatestMetadata(key);
            return obj != null && !obj.DeleteMarker;
        }

        /// <summary>
        /// List the latest version of each key, in ascending binary key order, rolling keys up into
        /// common prefixes at the delimiter. Common prefixes count toward maxKeys, as in Amazon S3.
        /// </summary>
        /// <param name="prefix">Key prefix, or null.</param>
        /// <param name="delimiter">Delimiter, or null.</param>
        /// <param name="startAfter">Exclusive start key or common prefix (marker, start-after, or continuation), or null.</param>
        /// <param name="maxKeys">Maximum keys and common prefixes to return. Minimum value is 0.</param>
        internal ObjectListing ListObjects(string prefix, string delimiter, string startAfter, int maxKeys)
        {
            return ListInternal(prefix, delimiter, startAfter, null, maxKeys, false);
        }

        /// <summary>
        /// List every version and delete marker, ordered by key ascending (binary) and version descending.
        /// </summary>
        /// <param name="prefix">Key prefix, or null.</param>
        /// <param name="delimiter">Delimiter, or null.</param>
        /// <param name="keyMarker">Exclusive start key, or null.</param>
        /// <param name="versionIdMarker">Version of keyMarker to start after, or null to start after every version of keyMarker.</param>
        /// <param name="maxKeys">Maximum versions and common prefixes to return. Minimum value is 0.</param>
        internal ObjectListing ListObjectVersions(string prefix, string delimiter, string keyMarker, long? versionIdMarker, int maxKeys)
        {
            return ListInternal(prefix, delimiter, keyMarker, versionIdMarker, maxKeys, true);
        }

        /// <summary>
        /// Replace the tag set of a specific object row under the key's write lock.
        /// Returns false when the row no longer exists or is a delete marker.
        /// </summary>
        internal bool SetObjectTags(Obj obj, List<ObjectTag> tags)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            LockHandle handle = AcquireObjectLock(obj.Key, LockMode.Write);
            try
            {
                Obj current = _Database.Objects.GetById(obj.Id, _Bucket.Id);
                if (current == null || current.DeleteMarker) return false;

                ValidateFence(handle, LockKeys.Object(_Bucket.TenantId, _Bucket.Name, obj.Key), "SetObjectTags");
                _Database.ObjectTags.DeleteByObjectId(current.Id, _Bucket.Id);

                if (tags != null)
                {
                    foreach (ObjectTag tag in tags)
                    {
                        tag.TenantId = _Bucket.TenantId;
                        tag.BucketId = _Bucket.Id;
                        tag.ObjectId = current.Id;
                        _Database.ObjectTags.Insert(tag);
                    }
                }

                return true;
            }
            finally
            {
                _LockManager.ReleaseAsync(handle, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Replace the ACL of a specific object row under the key's write lock.
        /// Returns false when the row no longer exists or is a delete marker.
        /// </summary>
        internal bool SetObjectAcls(Obj obj, List<ObjectAcl> acls)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            LockHandle handle = AcquireObjectLock(obj.Key, LockMode.Write);
            try
            {
                Obj current = _Database.Objects.GetById(obj.Id, _Bucket.Id);
                if (current == null || current.DeleteMarker) return false;

                ValidateFence(handle, LockKeys.Object(_Bucket.TenantId, _Bucket.Name, obj.Key), "SetObjectAcls");
                _Database.ObjectAcls.DeleteByObjectIdAndBucketId(current.Id, _Bucket.Id);

                if (acls != null)
                {
                    foreach (ObjectAcl acl in acls)
                    {
                        acl.TenantId = _Bucket.TenantId;
                        acl.BucketId = _Bucket.Id;
                        acl.ObjectId = current.Id;
                        _Database.ObjectAcls.Insert(acl);
                    }
                }

                return true;
            }
            finally
            {
                _LockManager.ReleaseAsync(handle, CancellationToken.None).GetAwaiter().GetResult();
            }
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

        internal List<BucketTag> GetBucketTags()
        {
            return _Database.BucketTags.GetByBucketId(_Bucket.TenantId, _Bucket.Id);
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

        internal void DeleteBucketAcl()
        {
            _Database.BucketAcls.DeleteByBucketId(_Bucket.Id);
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

        private void CheckWritePreconditions(Obj latest, ObjectWritePreconditions preconditions)
        {
            if (preconditions == null || !preconditions.Any) return;

            bool exists = latest != null && !latest.DeleteMarker;

            if (!String.IsNullOrEmpty(preconditions.IfNoneMatch) && exists)
            {
                if (Less3.Helpers.EtagMatcher.Matches(preconditions.IfNoneMatch, latest.Etag ?? latest.Md5))
                    throw new ObjectPreconditionException("If-None-Match precondition failed: " + _Bucket.Name + "/" + latest.Key + " exists.", false);
            }

            if (!String.IsNullOrEmpty(preconditions.IfMatch))
            {
                if (!exists)
                    throw new ObjectPreconditionException("If-Match precondition failed: object does not exist.", true);

                if (!Less3.Helpers.EtagMatcher.Matches(preconditions.IfMatch, latest.Etag ?? latest.Md5))
                    throw new ObjectPreconditionException("If-Match precondition failed: ETag does not match.", false);
            }
        }

        private Obj SelectReplacedVersion(string key, Obj latest, BucketVersioningState versioning)
        {
            // Versioning enabled: every write adds a version and nothing is replaced.
            if (versioning.Enabled) return null;

            // Suspended or unversioned: the write replaces the key's null version. Versions written
            // while versioning was enabled are not null versions and are kept.
            Obj nullVersion = _Database.Objects.GetNullVersion(key, _Bucket.Id);
            if (nullVersion != null) return nullVersion;

            // Rows written before null versions were tracked, in a bucket that has never had versioning
            // enabled, are the key's null version.
            if (!versioning.Suspended) return latest;
            return null;
        }

        private BucketVersioningState CurrentVersioning()
        {
            // Called with the object's lock held. In cluster mode another node may have changed the
            // bucket's versioning within this node's client cache window, so read it from the database.
            Bucket bucket = _Bucket;
            if (_Settings.Cluster.Enabled)
            {
                Bucket fresh = _Database.Buckets.GetById(bucket.TenantId, bucket.Id);
                if (fresh != null) bucket = fresh;
            }

            return new BucketVersioningState(bucket.EnableVersioning, bucket.VersioningSuspended);
        }

        private void InsertAclsAndTags(Obj obj, List<ObjectAcl> acls, List<ObjectTag> tags)
        {
            if (acls != null)
            {
                foreach (ObjectAcl acl in acls)
                {
                    acl.TenantId = _Bucket.TenantId;
                    acl.BucketId = _Bucket.Id;
                    acl.ObjectId = obj.Id;
                    _Database.ObjectAcls.Insert(acl);
                }
            }

            if (tags != null)
            {
                foreach (ObjectTag tag in tags)
                {
                    tag.TenantId = _Bucket.TenantId;
                    tag.BucketId = _Bucket.Id;
                    tag.ObjectId = obj.Id;
                    _Database.ObjectTags.Insert(tag);
                }
            }
        }

        private void RemoveReplacedVersionQuietly(Obj replaced, string operation)
        {
            // Runs after the new row is committed, so a failure here must not fail the request or touch the
            // new version: the old row simply remains as an extra version, which is never data loss.
            try
            {
                DeleteBlobQuietly(RemoveVersionRecord(replaced), operation);
            }
            catch (Exception e)
            {
                _Logging.Warn(operation + " committed the new version of " + _Bucket.Name + "/" + replaced.Key + " but could not remove replaced version " + replaced.Id + ": " + e.Message);
            }
        }

        private string RemoveVersionRecord(Obj obj)
        {
            // Remove the object row first, then its ACL and tag rows. A crash in between leaves only
            // orphaned ACL/tag rows keyed by an object ID that is never reused.
            _Database.Objects.Delete(obj);
            _Database.ObjectAcls.DeleteByObjectIdAndBucketId(obj.Id, _Bucket.Id);
            _Database.ObjectTags.DeleteByObjectId(obj.Id, _Bucket.Id);

            if (String.IsNullOrEmpty(obj.BlobFilename)) return null;
            return obj.BlobFilename;
        }

        private void DeleteBlobQuietly(string blobFilename, string operation)
        {
            if (String.IsNullOrEmpty(blobFilename)) return;

            try
            {
                if (_StorageDriver.Exists(blobFilename)) _StorageDriver.Delete(blobFilename);
            }
            catch (Exception e)
            {
                _Logging.Warn(operation + " failed to delete blob " + blobFilename + " in bucket " + _Bucket.Name + ": " + e.Message);
            }
        }

        private void ValidateFence(LockHandle handle, string lockKey, string operation)
        {
            if (!_LockManager.ValidateAsync(handle, CancellationToken.None).GetAwaiter().GetResult())
            {
                Less3Telemetry.FencingConflict(operation);
                throw new LockLostException(lockKey, handle.HolderId);
            }
        }

        private ObjectListing ListInternal(string prefix, string delimiter, string marker, long? versionMarker, int maxKeys, bool versions)
        {
            ObjectListing listing = new ObjectListing();
            if (maxKeys < 1) return listing;

            string effectivePrefix = prefix ?? "";
            bool useDelimiter = !String.IsNullOrEmpty(delimiter);
            int batchSize = Math.Min(Math.Max(maxKeys + 1, 100), 1000);

            string cursorKey = marker;
            long? cursorVersion = versions ? versionMarker : null;

            // A marker that is itself a common prefix means the caller has already received every key
            // under it, so keys rolled up into that prefix are skipped rather than returned again.
            string lastPrefix = null;
            if (useDelimiter && marker != null && IsCommonPrefix(marker, effectivePrefix, delimiter)) lastPrefix = marker;

            // When resuming part-way through a key's versions, none of the remaining versions are latest.
            string previousKey = (versions && versionMarker != null) ? marker : null;
            int count = 0;

            while (true)
            {
                List<Obj> rows = versions
                    ? _Database.Objects.EnumerateVersions(_Bucket.Id, prefix, cursorKey, cursorVersion, batchSize)
                    : _Database.Objects.EnumerateLatest(_Bucket.Id, prefix, cursorKey, batchSize);

                foreach (Obj row in rows)
                {
                    bool isLatest = versions && !String.Equals(row.Key, previousKey, StringComparison.Ordinal);
                    previousKey = row.Key;
                    cursorKey = row.Key;
                    if (versions) cursorVersion = row.Version;

                    if (useDelimiter && row.Key.Length > effectivePrefix.Length)
                    {
                        int index = row.Key.IndexOf(delimiter, effectivePrefix.Length, StringComparison.Ordinal);
                        if (index >= 0)
                        {
                            string commonPrefix = row.Key.Substring(0, index + delimiter.Length);
                            if (String.Equals(commonPrefix, lastPrefix, StringComparison.Ordinal)) continue;

                            if (count >= maxKeys)
                            {
                                listing.IsTruncated = true;
                                return listing;
                            }

                            listing.CommonPrefixes.Add(commonPrefix);
                            listing.NextMarker = commonPrefix;
                            listing.NextVersionIdMarker = null;
                            lastPrefix = commonPrefix;
                            count++;
                            continue;
                        }
                    }

                    if (count >= maxKeys)
                    {
                        listing.IsTruncated = true;
                        return listing;
                    }

                    listing.Objects.Add(row);
                    if (isLatest) listing.LatestObjectIds.Add(row.Id);
                    listing.NextMarker = row.Key;
                    // The marker is the internal version number, which stays stable even when the row is the
                    // null version and that null version is later replaced; clients treat it as opaque.
                    listing.NextVersionIdMarker = versions ? row.Version.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
                    count++;
                }

                if (rows.Count < batchSize) break;
            }

            return listing;
        }

        private static bool IsCommonPrefix(string value, string prefix, string delimiter)
        {
            if (!value.StartsWith(prefix, StringComparison.Ordinal)) return false;
            if (value.Length <= prefix.Length || !value.EndsWith(delimiter, StringComparison.Ordinal)) return false;
            return value.IndexOf(delimiter, prefix.Length, StringComparison.Ordinal) == value.Length - delimiter.Length;
        }

        #endregion
    }
}
