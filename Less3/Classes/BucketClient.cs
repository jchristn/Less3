using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

using SqliteWrapper;
using SyslogLogging;

namespace Less3.Classes
{
    /// <summary>
    /// Bucket client.  All object construction, authentication, and authorization must occur prior to using bucket methods.
    /// </summary>
    public class BucketClient : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Buffer size to use when reading streams.
        /// </summary>
        public long StreamReadBufferSize
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

        /// <summary>
        /// Name of the bucket.
        /// </summary>
        public string Name
        {
            get
            {
                return _BucketConfiguration.Name;
            } 
        }

        #endregion

        #region Private-Members

        private bool _Disposed = false;

        private Settings _Settings;
        private LoggingModule _Logging;
        private BucketConfiguration _BucketConfiguration;
        private DatabaseClient _Database;

        private long _StreamReadBufferSize = 65536;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public BucketClient()
        {

        }

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">LoggingModule.</param>
        /// <param name="bucket">BucketConfiguration.</param>
        public BucketClient(Settings settings, LoggingModule logging, BucketConfiguration bucket)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));

            _Settings = settings;
            _Logging = logging;
            _BucketConfiguration = bucket;

            if (!Directory.Exists(_BucketConfiguration.ObjectsDirectory)) Directory.CreateDirectory(_BucketConfiguration.ObjectsDirectory);
            _Database = new DatabaseClient(_BucketConfiguration.DatabaseFilename, _BucketConfiguration.DatabaseDebug);

            InitializeDatabase();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Tear down the client and dispose of background workers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Add an object.
        /// </summary>
        /// <param name="obj">Object metadata.</param>
        /// <param name="data">Data in byte array.</param>
        /// <returns>True if successful.</returns>
        public bool Add(Obj obj, byte[] data)
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
            return Add(obj, ms);
        }

        /// <summary>
        /// Add an object.
        /// </summary>
        /// <param name="obj">Object metadata.</param>
        /// <param name="stream">Stream containing data.</param>
        /// <returns>True if successful.</returns>
        public bool Add(Obj obj, Stream stream)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
             
            Obj test = null;
            if (GetMetadata(obj.Key, out test))
            {
                if (!_BucketConfiguration.EnableVersioning)
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "BucketClient Add versioning disabled and object " + _BucketConfiguration.Name + "/" + obj.Key + " already exists");
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
            obj.BlobFilename = Guid.NewGuid().ToString();
            
            string query = DatabaseQueries.InsertObject(obj);
            DataTable result = _Database.Query(query);
            
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
            }
            
            return true;
        }

        /// <summary>
        /// Get an object.
        /// </summary>
        /// <param name="key">Object's key.</param>
        /// <param name="data">Object data.</param>
        /// <returns>True if successful.</returns>
        public bool Get(string key, out byte[] data)
        {
            data = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = null;
            if (!GetMetadata(key, out obj)) return false;

            data = Common.ReadBinaryFile(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename);
            return true;
        }

        /// <summary>
        /// Get an object.
        /// </summary>
        /// <param name="key">Object's key.</param>
        /// <param name="startPosition">Starting position.</param>
        /// <param name="length">Number of bytes to retrieve.</param>
        /// <param name="data">Object data.</param>
        /// <returns>True if successful.</returns>
        public bool Get(string key, long startPosition, long length, out byte[] data)
        {
            data = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (startPosition < 0) throw new ArgumentNullException(nameof(startPosition));
            if (length < 0) throw new ArgumentNullException(nameof(length));

            Obj obj = null;
            if (!GetMetadata(key, out obj)) return false;
            FileInfo fi = new FileInfo(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename);
            long fileLength = fi.Length;
            if (startPosition > fileLength || startPosition + length > fileLength) return false;

            Stream stream = null;
            if (!Get(key, startPosition, length, out stream)) return false;

            MemoryStream ms = (MemoryStream)stream;
            if (ms != null) data = ms.ToArray();
            return true;
        }

        /// <summary>
        /// Get an object.
        /// </summary>
        /// <param name="key">Object's key.</param>
        /// <param name="contentLength">Content length.</param>
        /// <param name="stream">Stream containing data.</param>
        /// <returns>True if successful.</returns>
        public bool Get(string key, out long contentLength, out Stream stream)
        {
            contentLength = 0;
            stream = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
             
            Obj obj = null;
            if (!GetMetadata(key, out obj)) return false;
            FileInfo fi = new FileInfo(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename);
            contentLength = fi.Length;

            return Get(key, 0, contentLength, out stream);
        }

        /// <summary>
        /// Get an object.
        /// </summary>
        /// <param name="key">Object's key.</param>
        /// <param name="startPosition">Start position.</param>
        /// <param name="length">Number of bytes to read.</param>
        /// <param name="stream">Stream containing data.</param>
        /// <returns>True if successful.</returns>
        public bool Get(string key, long startPosition, long length, out Stream stream)
        {
            stream = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (startPosition < 0) throw new ArgumentNullException(nameof(startPosition));
            if (length < 0) throw new ArgumentNullException(nameof(length));

            Obj obj = null;
            if (!GetMetadata(key, out obj)) return false;
            FileInfo fi = new FileInfo(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename);
            long fileLength = fi.Length;
            if (startPosition > fileLength || startPosition + length > fileLength) return false;

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
            return true;
        }

        /// <summary>
        /// Get counts associated with the bucket.
        /// </summary>
        /// <param name="objects">Number of objects.</param>
        /// <param name="bytes">Number of bytes.</param>
        public void GetCounts(out long objects, out long bytes)
        {
            objects = 0;
            bytes = 0;

            string query = DatabaseQueries.GetObjectCount();
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

        /// <summary>
        /// Get object metadata.
        /// </summary>
        /// <param name="key">Object's key.</param>
        /// <param name="obj">Object metadata.</param>
        /// <returns>True if successful.</returns>
        public bool GetMetadata(string key, out Obj obj)
        {
            obj = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string query = DatabaseQueries.ObjectExists(key);
            DataTable result = _Database.Query(query);
            if (result == null || result.Rows.Count < 1) return false;

            obj = Obj.FromDataRow(result.Rows[0]); 
            return true;
        }

        /// <summary>
        /// Get object metadata for a specific object version.
        /// </summary>
        /// <param name="key">Object's key.</param>
        /// <param name="version">Object version.</param>
        /// <param name="obj">Object metadata.</param>
        /// <returns>True if successful.</returns>
        public bool GetMetadata(string key, long version, out Obj obj)
        {
            obj = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string query = DatabaseQueries.VersionExists(key, version);
            DataTable result = _Database.Query(query);
            if (result == null || result.Rows.Count < 1) return false;

            obj = Obj.FromDataRow(result.Rows[0]); 
            return true;
        }

        /// <summary>
        /// Check if an object exists.
        /// </summary>
        /// <param name="key">Object's key.</param>
        /// <returns>True if exists.</returns>
        public bool Exists(string key)
        {
            Obj obj = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (GetMetadata(key, out obj)) return true;
            return false;
        }

        /// <summary>
        /// Check if a specific version of an object exists.
        /// </summary>
        /// <param name="key">Object's key.</param>
        /// <param name="versionId">Object version.</param>
        /// <returns>True if exists.</returns>
        public bool Exists(string key, long versionId)
        {
            Obj obj = null;
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (GetMetadata(key, versionId, out obj)) return true;
            return false;
        }

        /// <summary>
        /// Delete an object.
        /// </summary>
        /// <param name="key">Object's key.</param>
        /// <returns>True if successful.</returns>
        public bool Delete(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = null;
            if (!GetMetadata(key, out obj))
            {
                _Logging.Log(LoggingModule.Severity.Debug, "Delete unable to find key " + _BucketConfiguration.Name + "/" + key);
                return false;
            }
             
            string query = null;
            DataTable result = null;

            if (_BucketConfiguration.EnableVersioning)
            {
                _Logging.Log(LoggingModule.Severity.Info, "Delete marking key " + _BucketConfiguration.Name + "/" + key + " as deleted");
                query = DatabaseQueries.MarkObjectDeleted(obj);
                result = _Database.Query(query);
                return true;
            }
            else
            {
                _Logging.Log(LoggingModule.Severity.Info, "Delete deleting key " + _BucketConfiguration.Name + "/" + key);
                query = DatabaseQueries.DeleteObject(obj.Key, obj.Version); 
                result = _Database.Query(query);
                File.Delete(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename);
                return true;
            }
        }

        /// <summary>
        /// Delete an object version.
        /// </summary>
        /// <param name="key">Object's key.</param>
        /// <param name="version">Object version.</param>
        /// <returns>True if successful.</returns>
        public bool Delete(string key, long version)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Obj obj = null;
            if (!GetMetadata(key, version, out obj)) return false;
        
            string query = null;
            DataTable result = null;

            if (_BucketConfiguration.EnableVersioning)
            {
                query = DatabaseQueries.MarkObjectDeleted(obj);
                result = _Database.Query(query);
                return true;
            }
            else
            {
                query = DatabaseQueries.DeleteObject(obj.Key, obj.Version);
                result = _Database.Query(query);
                File.Delete(_BucketConfiguration.ObjectsDirectory + obj.BlobFilename);
                return true;
            }
        }

        /// <summary>
        /// Enumerate a bucket.
        /// </summary>
        /// <param name="prefix">Prefix for enumerated objects.</param>
        /// <param name="startIndex">Starting index.</param>
        /// <param name="maxResults">Number of results to retrieve.</param>
        /// <param name="objs">Object metadata.</param>
        public void Enumerate(string prefix, long startIndex, int maxResults, out List<Obj> objs)
        {
            objs = new List<Obj>();
            string query = DatabaseQueries.Enumerate(prefix, startIndex, maxResults);
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

        /// <summary>
        /// Update an object's metadata.
        /// </summary>
        /// <param name="key">Object's key.</param>
        /// <param name="version">Object version.</param>
        /// <param name="vals">Dictionary containing key-value pairs of data to apply to the metadata.</param>
        public void Update(string key, long version, Dictionary<string, object> vals)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (vals == null || vals.Count < 1) throw new ArgumentException("At least one value must be supplied.");

            string query = DatabaseQueries.UpdateRecord(key, version, vals);
            DataTable result = _Database.Query(query);
            return;
        }

        #endregion

        #region Private-Methods

        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
            {
                return;
            }

            if (disposing)
            {
                try
                {
                    _Database.Dispose();
                    _Database = null;
                }
                catch (Exception e)
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "BucketClient Dispose exception disposing bucket " + Name);
                    _Logging.LogException("BucketClient", "Dispose", e);
                }
            }

            _Disposed = true;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void InitializeDatabase()
        {
            string query = null;
            DataTable result = null;

            query = DatabaseQueries.CreateObjectTable();
            result = _Database.Query(query);

            query = DatabaseQueries.CreateTagsTable();
            result = _Database.Query(query);
        }

        #endregion
    }
}
