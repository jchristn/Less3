using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

using SqliteWrapper;
using SyslogLogging;

using Less3.Database.Bucket;

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
                return _BucketConfiguration.Name;
            } 
        }

        #endregion

        #region Private-Members

        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private BucketConfiguration _BucketConfiguration = null;
        private DatabaseClient _Database = null;
        private DatabaseQueries _Queries = null;

        private long _StreamReadBufferSize = 65536;

        #endregion

        #region Constructors-and-Factories

        internal BucketClient()
        {

        }

        internal BucketClient(Settings settings, LoggingModule logging, BucketConfiguration bucket)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            _Settings = settings;
            _Logging = logging;
            _BucketConfiguration = bucket; 

            InitializeDatabase();
        }

        #endregion

        #region Public-Methods

        public void Dispose()
        {
            if (_Database != null)
            {
                _Database.Dispose();
                _Database = null;
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

            Obj test = null;
            if (GetObjectMetadata(obj.Key, out test))
            {
                if (!_BucketConfiguration.EnableVersioning)
                {
                    _Logging.Warn("BucketClient Add versioning disabled and object " + _BucketConfiguration.Name + "/" + obj.Key + " already exists");
                    return false;
                }

                obj.Version = (test.Version + 1);
            }
            else
            {
                obj.Version = 1;
            }

            int bytesRead = 0;
            long bytesRemaining = obj.ContentLength;
            byte[] buffer = new byte[_StreamReadBufferSize];

            using (FileStream fs = new FileStream(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename, FileMode.Create))
            {
                while (bytesRemaining > 0)
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        fs.Write(buffer, 0, bytesRead);
                        bytesRemaining -= bytesRead;
                    }
                }

                // calculate MD5
                fs.Seek(0, SeekOrigin.Begin);
                obj.Md5 = Common.Md5(fs);
            }

            DateTime ts = DateTime.Now.ToUniversalTime();
            obj.CreatedUtc = ts;
            obj.LastAccessUtc = ts;
            obj.LastUpdateUtc = ts;
            obj.ExpirationUtc = null;

            string query = _Queries.InsertObject(obj);
            DataTable result = _Database.Query(query);

            return true;
        }

        internal bool AddObjectMetadata(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            Obj test = null;
            if (GetObjectMetadata(obj.Key, out test))
            {
                if (!_BucketConfiguration.EnableVersioning)
                {
                    _Logging.Warn("BucketClient Add versioning disabled and object " + _BucketConfiguration.Name + "/" + obj.Key + " already exists");
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

            string query = _Queries.InsertObject(obj);
            DataTable result = _Database.Query(query);

            return true;
        }

        internal bool GetObject(string key, out byte[] data)
        {
            data = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = null;
            if (!GetObjectMetadata(key, out obj)) return false;

            data = Common.ReadBinaryFile(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename);
            return true;
        }
         
        internal bool GetObject(string key, out long contentLength, out Stream stream)
        {
            contentLength = 0;
            stream = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
             
            Obj obj = null;
            if (!GetObjectMetadata(key, out obj)) return false;
            FileInfo fi = new FileInfo(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename);
            contentLength = fi.Length;

            return GetObject(key, 0, contentLength, out stream);
        }

        internal bool GetObject(string key, long startPosition, long length, out Stream stream)
        {
            stream = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (startPosition < 0) throw new ArgumentNullException(nameof(startPosition));
            if (length < 0) throw new ArgumentNullException(nameof(length));

            Obj obj = null;
            if (!GetObjectMetadata(key, out obj)) return false;
            FileInfo fi = new FileInfo(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename);
            long fileLength = fi.Length;
            if (startPosition > fileLength || startPosition + length > fileLength) return false;

            /*
            stream = new MemoryStream();
            int bytesRead = 0;
            long bytesRemaining = length;
            byte[] buffer = new byte[_StreamReadBufferSize];

            using (FileStream fs = new FileStream(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename, FileMode.Open))
            {
                fs.Seek(startPosition, SeekOrigin.Begin);

                while (bytesRemaining > 0)
                {
                    bytesRead = fs.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                        bytesRemaining -= bytesRead;
                    }
                }
            }

            stream.Seek(0, SeekOrigin.Begin);
            */

            stream = new FileStream(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename, FileMode.Open);
            stream.Seek(startPosition, SeekOrigin.Begin);
            return true;
        }

        internal void GetCounts(out long objects, out long bytes)
        {
            objects = 0;
            bytes = 0;

            string query = _Queries.GetObjectCount();
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count == 1)
            {
                if (result.Rows[0].Table.Columns.Contains("NumObjects")
                    && result.Rows[0]["NumObjects"] != DBNull.Value
                    && result.Rows[0]["NumObjects"] != null)
                {
                    objects = Convert.ToInt64(result.Rows[0]["NumObjects"]);
                }

                if (result.Rows[0].Table.Columns.Contains("TotalBytes")
                    && result.Rows[0]["TotalBytes"] != DBNull.Value
                    && result.Rows[0]["TotalBytes"] != null)
                {
                    bytes = Convert.ToInt64(result.Rows[0]["TotalBytes"]);
                } 
            }

            return;
        }

        internal bool GetObjectMetadata(string key, out Obj obj)
        {
            obj = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string query = _Queries.ObjectExists(key);
            DataTable result = _Database.Query(query);
            if (result == null || result.Rows.Count < 1) return false;

            obj = Obj.FromDataRow(result.Rows[0]); 
            return true;
        }

        internal bool GetObjectMetadata(string key, long version, out Obj obj)
        {
            obj = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string query = _Queries.VersionExists(key, version);
            DataTable result = _Database.Query(query);
            if (result == null || result.Rows.Count < 1) return false;

            obj = Obj.FromDataRow(result.Rows[0]); 
            return true;
        }

        internal bool ObjectExists(string key)
        {
            Obj obj = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (GetObjectMetadata(key, out obj)) return true;
            return false;
        }

        internal bool ObjectExists(string key, long version)
        {
            Obj obj = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (GetObjectMetadata(key, version, out obj)) return true;
            return false;
        }

        internal bool DeleteObject(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = null;
            if (!GetObjectMetadata(key, out obj))
            {
                _Logging.Debug("Delete unable to find key " + _BucketConfiguration.Name + "/" + key);
                return false;
            }
             
            string query = null;
            DataTable result = null;

            if (_BucketConfiguration.EnableVersioning)
            {
                _Logging.Info("Delete marking key " + _BucketConfiguration.Name + "/" + key + " as deleted");
                query = _Queries.MarkObjectDeleted(obj);
                result = _Database.Query(query);
                return true;
            }
            else
            {
                _Logging.Info("Delete deleting key " + _BucketConfiguration.Name + "/" + key);
                query = _Queries.DeleteObject(obj.Key, obj.Version); 
                result = _Database.Query(query);
                File.Delete(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename);
                return true;
            }
        }

        internal bool DeleteObject(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = null;
            if (!GetObjectMetadata(key, version, out obj)) return false;

            string query = null;
            DataTable result = null;

            if (_BucketConfiguration.EnableVersioning)
            {
                query = _Queries.MarkObjectDeleted(obj);
                result = _Database.Query(query);
                return true;
            }
            else
            {
                query = _Queries.DeleteObject(obj.Key, obj.Version);
                result = _Database.Query(query);
                File.Delete(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename);
                return true;
            }
        }

        internal bool DeleteObjectMetadata(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = null;
            if (!GetObjectMetadata(key, version, out obj)) return false;

            string query = null;
            DataTable result = null;

            if (_BucketConfiguration.EnableVersioning)
            {
                query = _Queries.MarkObjectDeleted(obj);
                result = _Database.Query(query);
                return true;
            }
            else
            {
                query = _Queries.DeleteObject(obj.Key, obj.Version);
                result = _Database.Query(query);
                return true;
            }
        }

        internal void Enumerate(string prefix, long startIndex, int maxResults, out List<Obj> objs)
        {
            objs = new List<Obj>();
            string query = _Queries.Enumerate(prefix, startIndex, maxResults);
            DataTable result = _Database.Query(query);
            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow curr in result.Rows)
                {
                    Obj currObj = Obj.FromDataRow(curr);
                    objs.Add(currObj);
                }
            }
        }

        internal void UpdateObject(string key, long version, Dictionary<string, object> vals)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (vals == null || vals.Count < 1) throw new ArgumentException("At least one value must be supplied.");

            string query = _Queries.UpdateRecord(key, version, vals);
            DataTable result = _Database.Query(query);
            return;
        }

        internal void AddBucketTags(Dictionary<string, string> tags)
        {
            string query = _Queries.DeleteBucketTags();
            DataTable result = _Database.Query(query);

            if (tags != null && tags.Count > 0)
            {
                query = _Queries.InsertBucketTags(tags);
                result = _Database.Query(query);
            }
        }

        internal void AddObjectTags(string key, long version, Dictionary<string, string> tags)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (version < 1) throw new ArgumentException("Version ID must be one or greater.");

            string query = _Queries.DeleteObjectTags(key, version);
            DataTable result = _Database.Query(query);

            if (tags != null && tags.Count > 0)
            {
                query = _Queries.InsertObjectTags(key, version, tags);
                result = _Database.Query(query);
            }
        }

        internal Dictionary<string, string> GetBucketTags()
        {
            string query = _Queries.GetBucketTags();
            DataTable result = _Database.Query(query);

            Dictionary<string, string> ret = new Dictionary<string, string>();

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow curr in result.Rows)
                {
                    if (curr.Table.Columns.Contains("Key")
                        && curr["Key"] != DBNull.Value
                        && !String.IsNullOrEmpty(curr["Key"].ToString()))
                    {
                        if (curr.Table.Columns.Contains("Value")
                            && curr["Value"] != DBNull.Value
                            && !String.IsNullOrEmpty(curr["Value"].ToString()))
                        {
                            ret.Add(curr["Key"].ToString(), curr["Value"].ToString());
                        }
                    }
                }
            }

            return ret;
        }

        internal Dictionary<string, string> GetObjectTags(string key, long versionId)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (versionId < 1) throw new ArgumentException("Version ID must be one or greater.");

            string query = _Queries.GetObjectTags(key, versionId);
            DataTable result = _Database.Query(query);

            Dictionary<string, string> ret = new Dictionary<string, string>();

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow curr in result.Rows)
                {
                    if (curr.Table.Columns.Contains("Key")
                        && curr["Key"] != DBNull.Value
                        && !String.IsNullOrEmpty(curr["Key"].ToString()))
                    {
                        if (curr.Table.Columns.Contains("Value")
                            && curr["Value"] != DBNull.Value
                            && !String.IsNullOrEmpty(curr["Value"].ToString()))
                        {
                            ret.Add(curr["Key"].ToString(), curr["Value"].ToString());
                        }
                    }
                }
            }

            return ret;
        }

        internal void DeleteBucketTags()
        {
            string query = _Queries.DeleteBucketTags();
            DataTable result = _Database.Query(query);
        }

        internal void DeleteObjectTags(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (version < 1) throw new ArgumentException("Version ID must be one or greater.");

            string query = _Queries.DeleteObjectTags(key, version);
            DataTable result = _Database.Query(query);
        }
        
        internal bool ObjectGroupAclExists(string groupName, string objectKey, long versionId)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            string query = _Queries.ObjectGroupAclExists(groupName, objectKey, versionId);
            DataTable result = _Database.Query(query);
            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        internal bool ObjectUserAclExists(string userGuid, string objectKey, long versionId)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));

            string query = _Queries.ObjectUserAclExists(userGuid, objectKey, versionId);
            DataTable result = _Database.Query(query);
            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        internal bool BucketGroupAclExists(string groupName)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));

            string query = _Queries.BucketGroupAclExists(groupName);
            DataTable result = _Database.Query(query);
            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        internal bool BucketUserAclExists(string userGuid)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));

            string query = _Queries.BucketUserAclExists(userGuid);
            DataTable result = _Database.Query(query);
            if (result != null && result.Rows.Count > 0) return true;
            return false;
        }

        internal void GetBucketAcl(out List<BucketAcl> acls)
        {
            acls = new List<BucketAcl>();
            string query = _Queries.GetBucketAcl();
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    acls.Add(BucketAcl.FromDataRow(row));
                }
            }
            return;
        }

        internal void GetObjectAcl(string key, long version, out List<ObjectAcl> acls)
        {
            acls = new List<ObjectAcl>();
            string query = _Queries.GetObjectAcl(key, version);
            DataTable result = _Database.Query(query);

            if (result != null && result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    acls.Add(ObjectAcl.FromDataRow(row));
                }
            }

            return;
        }

        internal void AddBucketAcl(BucketAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));

            string query = null;
            DataTable result = null;

            string perm = null;
            if (acl.PermitRead) perm = "PermitRead";
            else if (acl.PermitWrite) perm = "PermitWrite";
            else if (acl.PermitReadAcp) perm = "PermitReadAcp";
            else if (acl.PermitWriteAcp) perm = "PermitWriteAcp";
            else if (acl.FullControl) perm = "FullControl";

            if (String.IsNullOrEmpty(perm)) throw new ArgumentException("Unknown permission specified in ACL.");

            if (!String.IsNullOrEmpty(acl.UserGUID))
            {
                #region User-ACL

                if (BucketUserAclExists(acl.UserGUID))
                {
                    _Logging.Debug("BucketClient AddBucketAcl ACL already exists for user " + acl.UserGUID + ", updating");
                    query = _Queries.UpdateBucketUserAcl(acl.UserGUID, perm); 
                    result = _Database.Query(query);
                    return;
                }
                else
                {
                    query = _Queries.InsertBucketAcl(acl); 
                    result = _Database.Query(query);
                    return;
                }

                #endregion
            }
            else if (!String.IsNullOrEmpty(acl.UserGroup))
            {
                #region Group-ACL

                if (BucketGroupAclExists(acl.UserGroup))
                {
                    _Logging.Debug("BucketClient AddBucketAcl ACL already exists for group " + acl.UserGroup + ", updating");
                    query = _Queries.UpdateBucketGroupAcl(acl.UserGroup, perm);
                    result = _Database.Query(query);
                    return;
                }
                else
                {
                    query = _Queries.InsertBucketAcl(acl);
                    result = _Database.Query(query);
                    return;
                }

                #endregion
            }
            else
            {
                throw new ArgumentException("No user GUID or user group specified.");
            }
        }

        internal void AddObjectAcl(ObjectAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
             
            string perm = null;
            if (acl.PermitRead) perm = "PermitRead";
            else if (acl.PermitWrite) perm = "PermitWrite";
            else if (acl.PermitReadAcp) perm = "PermitReadAcp";
            else if (acl.PermitWriteAcp) perm = "PermitWriteAcp";
            else if (acl.FullControl) perm = "FullControl";

            if (String.IsNullOrEmpty(perm)) throw new ArgumentException("Unknown permission specified in ACL.");

            string query = null;
            DataTable result = null;

            if (!String.IsNullOrEmpty(acl.UserGUID))
            {
                #region User-ACL

                if (ObjectUserAclExists(acl.UserGUID, acl.ObjectKey, acl.ObjectVersion))
                {
                    _Logging.Debug("BucketClient AddObjectAcl ACL already exists for user " + acl.UserGUID + " object " + _BucketConfiguration.Name + "/" + acl.ObjectKey + " version " + acl.ObjectVersion);
                    query = _Queries.UpdateObjectUserAcl(acl.UserGUID, acl.ObjectKey, acl.ObjectVersion, perm);
                    result = _Database.Query(query);
                    return;
                }
                else
                {
                    query = _Queries.InsertObjectAcl(acl);
                    result = _Database.Query(query);
                    return;
                }

                #endregion
            }
            else if (!String.IsNullOrEmpty(acl.UserGroup))
            {
                #region Group-ACL

                if (ObjectGroupAclExists(acl.UserGroup, acl.ObjectKey, acl.ObjectVersion))
                {
                    _Logging.Debug("BucketClient AddObjectAcl ACL already exists for group " + acl.UserGroup + " object " + _BucketConfiguration.Name + "/" + acl.ObjectKey + " version " + acl.ObjectVersion);
                    query = _Queries.UpdateObjectGroupAcl(acl.UserGroup, acl.ObjectKey, acl.ObjectVersion, perm);
                    result = _Database.Query(query);
                    return;
                }
                else
                {
                    query = _Queries.InsertObjectAcl(acl);
                    result = _Database.Query(query);
                    return;
                }

                #endregion
            }
            else
            {
                throw new ArgumentException("No user GUID or user group specified.");
            }
        }

        internal void DeleteBucketAcl()
        {
            string query = _Queries.DeleteBucketAcl();
            DataTable result = _Database.Query(query);
            return;
        }

        internal void DeleteObjectAcl(string key, long version)
        { 
            string query = _Queries.DeleteObjectAcl(key, version); 
            DataTable result = _Database.Query(query);  
            return;
        }

        internal void DeleteObjectAcl(string key)
        {
            string query = _Queries.DeleteObjectAcl(key);
            DataTable result = _Database.Query(query);
            return;
        }

        #endregion

        #region Private-Methods
         
        private void InitializeDatabase()
        {
            if (!Directory.Exists(_BucketConfiguration.ObjectsDirectory)) Directory.CreateDirectory(_BucketConfiguration.ObjectsDirectory);
            _Database = new DatabaseClient(_BucketConfiguration.DatabaseFilename);
            _Database.LogQueries = _Settings.Debug.DatabaseQueries;
            _Database.LogResults = _Settings.Debug.DatabaseResults;

            _Queries = new DatabaseQueries(_Database);

            string query = null;
            DataTable result = null;

            query = _Queries.CreateObjectTable();
            result = _Database.Query(query);

            query = _Queries.CreateBucketTagsTable();
            result = _Database.Query(query);

            query = _Queries.CreateObjectTagsTable();
            result = _Database.Query(query);

            query = _Queries.CreateBucketAclTable();
            result = _Database.Query(query);

            query = _Queries.CreateObjectAclTable();
            result = _Database.Query(query);
        }

        #endregion
    }
}
