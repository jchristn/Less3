namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlObjMethods : IObjMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlObjMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public void Insert(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            _Driver.ExecuteQuery(ObjQueries.InsertQuery(obj), true).Wait();
        }

        public Obj GetLatestByKey(string key, string bucketGuid)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.SelectLatestByKey(key, bucketGuid)).Result;
            List<Obj> objects = MapObjects(result);
            if (objects.Count > 0) return objects[0];
            return null;
        }

        public Obj GetByKeyAndVersion(string key, long version, string bucketGuid)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.SelectByKeyAndVersion(key, version, bucketGuid)).Result;
            List<Obj> objects = MapObjects(result);
            if (objects.Count > 0) return objects[0];
            return null;
        }

        public Obj GetByGuid(string guid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.SelectByGuid(guid, bucketGuid)).Result;
            List<Obj> objects = MapObjects(result);
            if (objects.Count > 0) return objects[0];
            return null;
        }

        public long GetLatestVersion(string key, string bucketGuid)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.SelectLatestVersion(key, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt64(result.Rows[0]["maxversion"]);
            return 0;
        }

        public void Update(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            _Driver.ExecuteQuery(ObjQueries.UpdateQuery(obj), true).Wait();
        }

        public void Delete(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            _Driver.ExecuteQuery(ObjQueries.DeleteQuery(obj), true).Wait();
        }

        public List<Obj> Enumerate(string bucketGuid, int startIndex, int maxResults, bool excludeDeleteMarkers, string prefix)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.Enumerate(bucketGuid, startIndex, maxResults, excludeDeleteMarkers, prefix)).Result;
            return MapObjects(result);
        }

        public BucketStatistics GetStatistics(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.GetStatistics(bucketGuid)).Result;
            BucketStatistics stats = new BucketStatistics();
            if (result != null && result.Rows.Count > 0)
            {
                stats.Objects = Convert.ToInt64(result.Rows[0]["objectcount"]);
                stats.Bytes = Convert.ToInt64(result.Rows[0]["totalbytes"]);
            }
            return stats;
        }

        private List<Obj> MapObjects(DataTable dt)
        {
            List<Obj> objects = new List<Obj>();
            if (dt == null || dt.Rows.Count == 0) return objects;

            foreach (DataRow row in dt.Rows)
            {
                Obj obj = new Obj();
                obj.Id = Convert.ToInt32(row["id"]);
                obj.GUID = row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
                obj.BucketGUID = row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
                obj.OwnerGUID = row["ownerguid"] != DBNull.Value ? row["ownerguid"].ToString() : null;
                obj.AuthorGUID = row["authorguid"] != DBNull.Value ? row["authorguid"].ToString() : null;
                obj.Key = row["key"] != DBNull.Value ? row["key"].ToString() : null;
                obj.ContentType = row["contenttype"] != DBNull.Value ? row["contenttype"].ToString() : null;
                obj.ContentLength = Convert.ToInt64(row["contentlength"]);
                obj.Version = Convert.ToInt64(row["version"]);
                obj.Etag = row["etag"] != DBNull.Value ? row["etag"].ToString() : null;

                string retentionStr = row["retention"] != DBNull.Value ? row["retention"].ToString() : "NONE";
                if (Enum.TryParse<RetentionType>(retentionStr, true, out RetentionType retention))
                    obj.Retention = retention;
                else
                    obj.Retention = RetentionType.NONE;

                obj.BlobFilename = row["blobfilename"] != DBNull.Value ? row["blobfilename"].ToString() : null;
                obj.IsFolder = Convert.ToBoolean(row["isfolder"]);
                obj.DeleteMarker = Convert.ToBoolean(row["deletemarker"]);
                obj.Md5 = row["md5"] != DBNull.Value ? row["md5"].ToString() : null;
                obj.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                obj.LastUpdateUtc = Convert.ToDateTime(row["lastupdateutc"]).ToUniversalTime();
                obj.LastAccessUtc = Convert.ToDateTime(row["lastaccessutc"]).ToUniversalTime();
                obj.Metadata = row["metadata"] != DBNull.Value ? row["metadata"].ToString() : null;
                obj.ExpirationUtc = row["expirationutc"] != DBNull.Value ? Convert.ToDateTime(row["expirationutc"]).ToUniversalTime() : (DateTime?)null;
                objects.Add(obj);
            }

            return objects;
        }
    }
}
