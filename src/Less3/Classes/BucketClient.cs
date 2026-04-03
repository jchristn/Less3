namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Less3.Database;
    using Less3.Settings;
    using Less3.Storage;
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

        internal string GUID
        {
            get
            {
                return _Bucket.GUID;
            }
        }

        #endregion

        #region Private-Members

        private SettingsBase _Settings = null;
        private LoggingModule _Logging = null;
        private Bucket _Bucket = null;
        private DatabaseDriverBase _Database = null;
        private long _StreamReadBufferSize = 65536;
        private StorageDriverBase _StorageDriver = null;

        #endregion

        #region Constructors-and-Factories

        internal BucketClient()
        {

        }

        internal BucketClient(SettingsBase settings, LoggingModule logging, Bucket bucket, DatabaseDriverBase database)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
            _Database = database ?? throw new ArgumentNullException(nameof(database));

            InitializeStorageDriver();
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
            if (String.IsNullOrEmpty(obj.GUID)) obj.GUID = Guid.NewGuid().ToString();
            obj.BucketGUID = _Bucket.GUID;

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

            obj.Md5 = Common.BytesToHexString(_StorageDriver.Write(obj.BlobFilename, obj.ContentLength, stream)).ToLowerInvariant();

            if (String.IsNullOrEmpty(obj.Etag)) obj.Etag = obj.Md5;

            DateTime ts = DateTime.Now.ToUniversalTime();
            obj.CreatedUtc = ts;
            obj.LastAccessUtc = ts;
            obj.LastUpdateUtc = ts;
            obj.ExpirationUtc = null;

            _Database.Objects.Insert(obj);
            return true;
        }

        internal bool AddObjectMetadata(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            obj.BucketGUID = _Bucket.GUID;

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

            _Database.Objects.Insert(obj);
            return true;
        }

        internal bool GetObjectLatest(string key, out byte[] data)
        {
            data = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectLatestMetadata(key);
            if (obj == null) return false;

            data = _StorageDriver.Read(obj.BlobFilename);
            return true;
        }

        internal bool GetObjectLatest(string key, out long contentLength, out Stream stream)
        {
            contentLength = 0;
            stream = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectLatestMetadata(key);
            if (obj == null) return false;

            ObjectStream objStream = _StorageDriver.ReadStream(obj.BlobFilename);
            contentLength = objStream.ContentLength;
            stream = objStream.Data;
            return true;
        }

        internal bool GetObjectLatestRange(string key, long startPosition, long length, out Stream stream)
        {
            stream = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (startPosition < 0) throw new ArgumentNullException(nameof(startPosition));
            if (length < 0) throw new ArgumentNullException(nameof(length));

            Obj obj = GetObjectLatestMetadata(key);
            if (obj == null) return false;

            ObjectStream objStream = _StorageDriver.ReadRangeStream(obj.BlobFilename, startPosition, length);
            stream = objStream.Data;
            return true;
        }

        internal long GetObjectLatestVersion(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return _Database.Objects.GetLatestVersion(key, _Bucket.GUID);
        }

        internal BucketStatistics GetFullStatistics()
        {
            BucketStatistics ret = _Database.Objects.GetStatistics(_Bucket.GUID);
            ret.Name = _Bucket.Name;
            return ret;
        }

        internal BucketStatistics GetStatistics(List<Obj> objects)
        {
            BucketStatistics ret = new BucketStatistics(_Bucket.Name, _Bucket.GUID, 0, 0);

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
            return _Database.Objects.GetLatestByKey(key, _Bucket.GUID);
        }

        internal Obj GetObjectVersionMetadata(string key, long version = 1)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return _Database.Objects.GetByKeyAndVersion(key, version, _Bucket.GUID);
        }

        internal Obj GetObjectMetadataByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            return _Database.Objects.GetByGuid(guid, _Bucket.GUID);
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

            Obj obj = GetObjectLatestMetadata(key);
            if (obj == null)
            {
                _Logging.Debug("Delete unable to find key " + _Bucket.Name + "/" + key);
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
                _StorageDriver.Delete(obj.BlobFilename);
                return true;
            }
        }

        internal bool DeleteObjectVersion(string key, long version)
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
                    _Bucket.GUID,
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

                    nextStartIndex = obj.Id + 1;
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
                    tag.BucketGUID = _Bucket.GUID;
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
                    tag.BucketGUID = _Bucket.GUID;
                    _Database.ObjectTags.Insert(tag);
                }
            }
        }

        internal List<BucketTag> GetBucketTags()
        {
            return _Database.BucketTags.GetByBucketGuid(_Bucket.GUID);
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

            return _Database.ObjectTags.GetByObjectGuid(obj.GUID, _Bucket.GUID);
        }

        internal List<ObjectTag> GetObjectTags(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            return _Database.ObjectTags.GetByObjectGuid(guid, _Bucket.GUID);
        }

        internal void DeleteBucketTags()
        {
            _Database.BucketTags.DeleteByBucketGuid(_Bucket.GUID);
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

            _Database.ObjectTags.DeleteByObjectGuid(obj.GUID, _Bucket.GUID);
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

            return _Database.ObjectAcls.ExistsByGroupName(groupName, obj.GUID, _Bucket.GUID);
        }

        internal bool ObjectUserAclExists(string userGuid, string key, long version)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectVersionMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("Exists unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return false;
            }

            return _Database.ObjectAcls.ExistsByUserGuid(userGuid, obj.GUID, _Bucket.GUID);
        }

        internal bool BucketGroupAclExists(string groupName)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            return _Database.BucketAcls.ExistsByGroupName(groupName, _Bucket.GUID);
        }

        internal bool BucketUserAclExists(string userGuid)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            return _Database.BucketAcls.ExistsByUserGuid(userGuid, _Bucket.GUID);
        }

        internal List<BucketAcl> GetBucketAcl()
        {
            return _Database.BucketAcls.GetByBucketGuid(_Bucket.GUID);
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

            return _Database.ObjectAcls.GetByObjectGuid(obj.GUID, _Bucket.GUID);
        }

        internal List<ObjectAcl> GetObjectAcl(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            return _Database.ObjectAcls.GetByObjectGuid(guid, _Bucket.GUID);
        }

        internal void AddBucketAcl(BucketAcl acl)
        {
            if (acl != null)
            {
                acl.BucketGUID = _Bucket.GUID;
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
                    acl.BucketGUID = _Bucket.GUID;
                    _Database.BucketAcls.Insert(acl);
                }
            }
        }

        internal void AddObjectAcl(ObjectAcl acl)
        {
            if (acl != null)
            {
                Obj obj = GetObjectMetadataByGuid(acl.ObjectGUID);
                if (obj == null)
                {
                    _Logging.Debug("SetAcl unable to find object GUID " + acl.ObjectGUID + " in bucket " + _Bucket.Name);
                    return;
                }

                acl.BucketGUID = _Bucket.GUID;
                acl.ObjectGUID = obj.GUID;
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
                    acl.BucketGUID = _Bucket.GUID;
                    acl.ObjectGUID = obj.GUID;
                    _Database.ObjectAcls.Insert(acl);
                }
            }
        }

        internal void DeleteBucketAcl()
        {
            _Database.BucketAcls.DeleteByBucketGuid(_Bucket.GUID);
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

            _Database.ObjectAcls.DeleteByObjectGuidAndBucketGuid(obj.GUID, _Bucket.GUID);
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

            _Database.ObjectAcls.DeleteByObjectGuidAndBucketGuid(obj.GUID, _Bucket.GUID);
        }

        #endregion

        #region Private-Methods

        private void InitializeStorageDriver()
        {
            switch (_Bucket.StorageType)
            {
                case StorageDriverType.Disk:
                    if (!Directory.Exists(_Bucket.DiskDirectory)) Directory.CreateDirectory(_Bucket.DiskDirectory);
                    _StorageDriver = new DiskStorageDriver(_Bucket.DiskDirectory);
                    break;

                default:
                    throw new ArgumentException("Unknown storage driver type '" + _Bucket.StorageType.ToString() + "' in bucket GUID " + _Bucket.GUID + ".");
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

        #endregion
    }
}
