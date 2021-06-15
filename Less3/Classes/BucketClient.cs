using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

using Watson.ORM;
using Watson.ORM.Core;
using SyslogLogging;

using Less3.Storage;

namespace Less3.Classes
{
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

        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private Bucket _Bucket = null;
        private WatsonORM _ORM = null;
        private long _StreamReadBufferSize = 65536;
        private StorageDriver _StorageDriver = null;

        #endregion

        #region Constructors-and-Factories

        internal BucketClient()
        {

        }

        internal BucketClient(Settings settings, LoggingModule logging, Bucket bucket, WatsonORM orm)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
            _ORM = orm ?? throw new ArgumentNullException(nameof(orm));
             
            InitializeStorageDriver(); 
        }

        #endregion

        #region Public-Methods

        public void Dispose()
        {
            if (_ORM != null)
            {
                _ORM.Dispose();
                _ORM = null;
            }

            if (_StorageDriver != null)
            { 
                _StorageDriver = null;
            }
        }

        #endregion

        #region Internal-Methods

        internal bool AddObject(Obj obj, byte[] data)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
             
            long len = 0;
            MemoryStream ms = new MemoryStream();

            if (data != null && data.Length > 0)
            { 
                len = data.Length;
                ms.Write(data, 0, data.Length);
                ms.Seek(0, SeekOrigin.Begin);
            }
             
            obj.ContentLength = len;
            return AddObject(obj, ms);
        }

        internal bool AddObject(Obj obj, Stream stream)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (String.IsNullOrEmpty(obj.GUID)) obj.GUID = Guid.NewGuid().ToString();
            obj.BucketGUID = _Bucket.GUID;

            Obj test = GetObjectMetadata(obj.Key);
            if (test != null)
            {
                if (!_Bucket.EnableVersioning)
                {
                    _Logging.Warn("BucketClient Add versioning disabled and object " + _Bucket.Name + "/" + obj.Key + " already exists");
                    return false;
                }

                obj.Version = (test.Version + 1);
            }
            else
            {
                obj.Version = 1;
            }

            obj.Md5 = Common.BytesToHexString(_StorageDriver.Write(obj.BlobFilename, obj.ContentLength, stream));

            if (String.IsNullOrEmpty(obj.Etag)) obj.Etag = obj.Md5;

            DateTime ts = DateTime.Now.ToUniversalTime();
            obj.CreatedUtc = ts;
            obj.LastAccessUtc = ts;
            obj.LastUpdateUtc = ts;
            obj.ExpirationUtc = null;

            _ORM.Insert<Obj>(obj);
            return true;
        }

        internal bool AddObjectMetadata(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            obj.BucketGUID = _Bucket.GUID;

            Obj test = GetObjectMetadata(obj.Key);
            if (test != null)
            {
                if (!_Bucket.EnableVersioning)
                {
                    _Logging.Warn("BucketClient Add versioning disabled and object " + _Bucket.Name + "/" + obj.Key + " already exists");
                    return false;
                }

                obj.Version = (test.Version + 1);
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

            _ORM.Insert<Obj>(obj);
            return true;
        }

        internal bool GetObject(string key, out byte[] data)
        {
            data = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key);
            if (obj == null) return false;

            data = _StorageDriver.Read(obj.BlobFilename);
            return true;
        }
         
        internal bool GetObject(string key, out long contentLength, out Stream stream)
        {
            contentLength = 0;
            stream = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key);
            if (obj == null) return false;

            ObjectStream objStream = _StorageDriver.ReadStream(obj.BlobFilename);
            contentLength = objStream.ContentLength;
            stream = objStream.Data;
            return true;
        }

        internal bool GetObject(string key, long startPosition, long length, out Stream stream)
        {
            stream = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (startPosition < 0) throw new ArgumentNullException(nameof(startPosition));
            if (length < 0) throw new ArgumentNullException(nameof(length));

            Obj obj = GetObjectMetadata(key);
            if (obj == null) return false;

            ObjectStream objStream = _StorageDriver.ReadRangeStream(obj.BlobFilename, startPosition, length);
            stream = objStream.Data;
            return true; 
        }

        internal BucketStatistics GetFullStatistics()
        {
            BucketStatistics ret = new BucketStatistics();
            ret.Name = _Bucket.Name;
            ret.GUID = _Bucket.GUID;
            ret.Objects = 0;
            ret.Bytes = 0;

            string countQuery = "SELECT COUNT(*) AS numobjects, SUM(contentlength) AS totalbytes FROM objects WHERE bucketguid = '" + _Bucket.GUID + "'";
            DataTable result = _ORM.Query(countQuery);

            if (result != null && result.Rows.Count == 1)
            {
                if (result.Rows[0].Table.Columns.Contains("numobjects")
                    && result.Rows[0]["NumObjects"] != DBNull.Value
                    && result.Rows[0]["NumObjects"] != null)
                {
                    ret.Objects = Convert.ToInt64(result.Rows[0]["numobjects"]);
                }

                if (result.Rows[0].Table.Columns.Contains("totalbytes")
                    && result.Rows[0]["TotalBytes"] != DBNull.Value
                    && result.Rows[0]["TotalBytes"] != null)
                {
                    ret.Bytes = Convert.ToInt64(result.Rows[0]["totalbytes"]);
                } 
            }

            return ret;
        }

        internal BucketStatistics GetStatistics(List<Obj> objects)
        {
            BucketStatistics ret = new BucketStatistics();
            ret.Name = _Bucket.Name;
            ret.GUID = _Bucket.GUID;
            ret.Objects = 0;
            ret.Bytes = 0;
            
            if (objects != null && objects.Count > 0)
            {
                ret.Objects = objects.Count;
                ret.Bytes = objects.Sum(o => o.ContentLength);
            }

            return ret;
        }

        internal Obj GetObjectMetadata(string key)
        { 
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            DbExpression eKey = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(Obj.Key)),
                DbOperators.Equals,
                key);

            DbExpression eBucket = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(Obj.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            eKey.PrependAnd(eBucket);

            return _ORM.SelectFirst<Obj>(eKey);
        }

        internal Obj GetObjectMetadata(string key, long version)
        { 
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            DbExpression eKey = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(Obj.Key)),
                DbOperators.Equals,
                key);

            DbExpression eVersion = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(Obj.Version)),
                DbOperators.Equals,
                version);

            DbExpression eBucket = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(Obj.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            eKey.PrependAnd(eVersion);
            eKey.PrependAnd(eBucket);

            return _ORM.SelectFirst<Obj>(eKey);
        }

        internal Obj GetObjectMetadataByGuid(string guid)
        { 
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));

            DbExpression eGuid = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(Obj.GUID)),
                DbOperators.Equals,
                guid);

            DbExpression eBucket = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(Obj.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            eGuid.PrependAnd(eBucket);

            return _ORM.SelectFirst<Obj>(eGuid);
        }

        internal bool ObjectExists(string key)
        { 
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            Obj obj = GetObjectMetadata(key);
            if (obj != null) return true;
            return false;
        }

        internal bool ObjectExists(string key, long version)
        { 
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            Obj obj = GetObjectMetadata(key, version);
            if (obj != null) return true;
            return false;
        }

        internal bool DeleteObject(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key);
            if (obj == null)
            {
                _Logging.Debug("Delete unable to find key " + _Bucket.Name + "/" + key);
                return false;
            }
              
            if (_Bucket.EnableVersioning)
            {
                _Logging.Info("Delete marking key " + _Bucket.Name + "/" + key + " as deleted");
                obj.DeleteMarker = true;
                _ORM.Update<Obj>(obj);
                return true;
            }
            else
            {
                _Logging.Info("Delete deleting key " + _Bucket.Name + "/" + key);
                _ORM.Delete<Obj>(obj);
                _StorageDriver.Delete(obj.BlobFilename);
                return true;
            }
        }

        internal bool DeleteObject(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("Delete unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return false;
            }
             
            if (_Bucket.EnableVersioning)
            {
                _Logging.Info("Delete marking key " + _Bucket.Name + "/" + key + " version " + version + " as deleted");
                obj.DeleteMarker = true;
                _ORM.Update<Obj>(obj);
                return true; 
            }
            else
            {
                _Logging.Info("Delete deleting key " + _Bucket.Name + "/" + key + " version " + version);
                _ORM.Delete<Obj>(obj);
                _StorageDriver.Delete(obj.BlobFilename);
                return true;
            }
        }

        internal bool DeleteObjectMetadata(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("Delete unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return false;
            }
             
            if (_Bucket.EnableVersioning)
            {
                _Logging.Info("Delete marking key " + _Bucket.Name + "/" + key + " as deleted");
                obj.DeleteMarker = true;
                _ORM.Update<Obj>(obj);
                return true;
            }
            else
            {
                _Logging.Info("Delete deleting key " + _Bucket.Name + "/" + key);
                _ORM.Delete<Obj>(obj); 
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
            objects = new List<Obj>();
            prefixes = new List<string>();
            nextStartIndex = startIndex;
            isTruncated = false;
             
            while (true)
            {
                #region Retrieve-Records

                DbExpression e = new DbExpression(
                    _ORM.GetColumnName<Obj>(nameof(Obj.BucketGUID)),
                    DbOperators.Equals,
                    _Bucket.GUID);

                e.PrependAnd(
                    _ORM.GetColumnName<Obj>(nameof(Obj.Id)),
                    DbOperators.GreaterThanOrEqualTo,
                    nextStartIndex);

                if (!String.IsNullOrEmpty(prefix))
                {
                    e.PrependAnd(
                    _ORM.GetColumnName<Obj>(nameof(Obj.Key)),
                    DbOperators.StartsWith,
                    prefix);
                }

                List<Obj> tempObjects = _ORM.SelectMany<Obj>(null, maxResults, e);
                if (tempObjects == null || tempObjects.Count < 1)
                {
                    break;
                }

                #endregion

                #region Process-Records

                foreach (Obj obj in tempObjects)
                {
                    string currPrefix = null;
                    string tempKey = new string(obj.Key);
                    if (!String.IsNullOrEmpty(prefix)) tempKey = tempKey.Replace(prefix, "");

                    if (!String.IsNullOrEmpty(delimiter))
                    {
                        if (tempKey.Contains(delimiter))
                        {
                            int delimiterPos = tempKey.IndexOf(delimiter);
                            currPrefix = tempKey.Substring(0, delimiterPos + delimiter.Length);
                            if (!prefixes.Contains(currPrefix))
                            {
                                prefixes.Add(currPrefix);
                            }
                        }
                    }

                    if (String.IsNullOrEmpty(currPrefix) && objects.Count <= maxResults)
                    {
                        objects.Add(obj);
                    }

                    if (obj.IsFolder && obj.ContentLength == 0)
                    {
                        prefixes.Add(obj.Key);
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
                    _ORM.Insert<BucketTag>(tag);
                }
            } 
        }

        internal void AddObjectTags(string key, long version, List<ObjectTag> tags)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (version < 1) throw new ArgumentException("Version ID must be one or greater.");

            DeleteObjectTags(key, version);

            if (tags != null && tags.Count > 0)
            {
                foreach (ObjectTag tag in tags)
                {
                    tag.BucketGUID = _Bucket.GUID;
                    _ORM.Insert<ObjectTag>(tag);
                }
            }
        }

        internal List<BucketTag> GetBucketTags()
        {
            DbExpression eBucket = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(Obj.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            return _ORM.SelectMany<BucketTag>(eBucket); 
        }

        internal List<ObjectTag> GetObjectTags(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (version < 1) throw new ArgumentException("Version ID must be one or greater.");

            Obj obj = GetObjectMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("GetTags unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return null;
            }

            DbExpression eKey = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(Obj.Key)),
                DbOperators.Equals,
                key);

            DbExpression eVersion = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(Obj.Version)),
                DbOperators.Equals,
                version);

            DbExpression eBucket = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(Obj.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            eKey.PrependAnd(eVersion);
            eKey.PrependAnd(eBucket);

            return _ORM.SelectMany<ObjectTag>(eKey);
        }

        internal List<ObjectTag> GetObjectTags(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid)); 
             
            DbExpression eObj = new DbExpression(
                _ORM.GetColumnName<ObjectTag>(nameof(ObjectTag.ObjectGUID)),
                DbOperators.Equals,
                guid);

            DbExpression eBucket = new DbExpression(
                _ORM.GetColumnName<Obj>(nameof(ObjectTag.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);
             
            eObj.PrependAnd(eBucket);

            return _ORM.SelectMany<ObjectTag>(eObj);
        }

        internal void DeleteBucketTags()
        {
            DbExpression eBucket = new DbExpression(
                _ORM.GetColumnName<BucketTag>(nameof(BucketTag.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            _ORM.DeleteMany<BucketTag>(eBucket);
        }

        internal void DeleteObjectTags(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("Exists unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return;
            }

            DbExpression eBucket = new DbExpression(
                _ORM.GetColumnName<ObjectTag>(nameof(ObjectTag.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            DbExpression eObj = new DbExpression(
                _ORM.GetColumnName<ObjectTag>(nameof(ObjectTag.ObjectGUID)),
                DbOperators.Equals,
                obj.GUID);
             
            eBucket.PrependAnd(eObj); 

            _ORM.DeleteMany<ObjectTag>(eBucket);
        }

        internal bool ObjectGroupAclExists(string groupName, string key, long version)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("Exists unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return false;
            }

            DbExpression expr = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            DbExpression eGroup = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.UserGroup)),
                DbOperators.Equals,
                groupName);

            DbExpression eObj = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.ObjectGUID)),
                DbOperators.Equals,
                obj.GUID);
             
            expr.PrependAnd(eGroup);
            expr.PrependAnd(eObj);

            List<ObjectAcl> acls = _ORM.SelectMany<ObjectAcl>(expr);
            if (acls != null && acls.Count > 0) return true;
            return false;
        }

        internal bool ObjectUserAclExists(string userGuid, string key, long version)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("Exists unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return false;
            }

            DbExpression expr = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            DbExpression eUser = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.UserGUID)),
                DbOperators.Equals,
                userGuid);

            DbExpression eObj = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.ObjectGUID)),
                DbOperators.Equals,
                obj.GUID);

            expr.PrependAnd(eUser);
            expr.PrependAnd(eObj);

            List<ObjectAcl> acls = _ORM.SelectMany<ObjectAcl>(expr);
            if (acls != null && acls.Count > 0) return true;
            return false;
        }

        internal bool BucketGroupAclExists(string groupName)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName)); 
             
            DbExpression expr = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            DbExpression eGroup = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.UserGroup)),
                DbOperators.Equals,
                groupName);
             
            expr.PrependAnd(eGroup); 

            List<BucketAcl> acls = _ORM.SelectMany<BucketAcl>(expr);
            if (acls != null && acls.Count > 0) return true;
            return false;
        }

        internal bool BucketUserAclExists(string userGuid)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));

            DbExpression expr = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            DbExpression eUser = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.UserGUID)),
                DbOperators.Equals,
                userGuid);
             
            expr.PrependAnd(eUser); 

            List<BucketAcl> acls = _ORM.SelectMany<BucketAcl>(expr);
            if (acls != null && acls.Count > 0) return true;
            return false;
        }

        internal List<BucketAcl> GetBucketAcl()
        {
            DbExpression expr = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            return _ORM.SelectMany<BucketAcl>(expr); 
        }

        internal List<ObjectAcl> GetObjectAcl(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("GetAcl unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return null;
            }

            DbExpression expr = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            DbExpression eObj = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.ObjectGUID)),
                DbOperators.Equals,
                obj.GUID);

            expr.PrependAnd(eObj);

            return _ORM.SelectMany<ObjectAcl>(expr);
        }

        internal List<ObjectAcl> GetObjectAcl(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
              
            DbExpression expr = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            DbExpression eObj = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.ObjectGUID)),
                DbOperators.Equals,
                guid);

            expr.PrependAnd(eObj);

            return _ORM.SelectMany<ObjectAcl>(expr);
        }

        internal void AddBucketAcl(BucketAcl acl)
        {
            if (acl != null)
            {
                acl.BucketGUID = _Bucket.GUID;
                _ORM.Insert<BucketAcl>(acl);
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
                    _ORM.Insert<BucketAcl>(acl);
                }
            }
        }

        internal void AddObjectAcl(ObjectAcl acl)
        {
            if (acl == null) return;

            Obj obj = GetObjectMetadataByGuid(acl.ObjectGUID);
            if (obj == null)
            {
                _Logging.Debug("SetAcl unable to find object GUID " + acl.ObjectGUID + " in bucket " + _Bucket.Name);
                return;
            }

            acl.BucketGUID = _Bucket.GUID;
            acl.ObjectGUID = obj.GUID;
            _ORM.Insert<ObjectAcl>(acl);
        }

        internal void SetObjectAcls(string key, long version, List<ObjectAcl> acls)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("SetAcl unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return;
            }

            DeleteObjectAcl(key, version);

            if (acls != null && acls.Count > 0)
            {
                foreach (ObjectAcl acl in acls)
                {
                    acl.BucketGUID = _Bucket.GUID;
                    acl.ObjectGUID = obj.GUID;
                    _ORM.Insert<ObjectAcl>(acl);
                }
            }
        }

        internal void DeleteBucketAcl()
        {
            DbExpression expr = new DbExpression(
                _ORM.GetColumnName<BucketAcl>(nameof(BucketAcl.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            _ORM.DeleteMany<BucketAcl>(expr);
        }

        internal void DeleteObjectAcl(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key, version);
            if (obj == null)
            {
                _Logging.Debug("DeleteAcl unable to find key " + _Bucket.Name + "/" + key + " version " + version);
                return;
            }

            DbExpression expr = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            DbExpression eObjGuid = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.ObjectGUID)),
                DbOperators.Equals,
                obj.GUID);

            expr.PrependAnd(eObjGuid);
            _ORM.DeleteMany<ObjectAcl>(expr);
        }

        internal void DeleteObjectAcl(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = GetObjectMetadata(key);
            if (obj == null)
            {
                _Logging.Debug("DeleteAcl unable to find key " + _Bucket.Name + "/" + key);
                return;
            }

            DbExpression expr = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.BucketGUID)),
                DbOperators.Equals,
                _Bucket.GUID);

            DbExpression eObjGuid = new DbExpression(
                _ORM.GetColumnName<ObjectAcl>(nameof(ObjectAcl.ObjectGUID)),
                DbOperators.Equals,
                obj.GUID);

            expr.PrependAnd(eObjGuid);
            _ORM.DeleteMany<ObjectAcl>(expr);
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
         
        private void Logger(string msg)
        {
            Console.WriteLine(msg);
        }

        #endregion
    }
}
