namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
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

        public Obj GetLatestByKey(string key, string bucketId)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.SelectLatestByKey(key, bucketId)).Result;
            List<Obj> objects = MapObjects(result);
            if (objects.Count > 0) return objects[0];
            return null;
        }

        public Obj GetByKeyAndVersion(string key, long version, string bucketId)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.SelectByKeyAndVersion(key, version, bucketId)).Result;
            List<Obj> objects = MapObjects(result);
            if (objects.Count > 0) return objects[0];
            return null;
        }

        public Obj GetById(string id, string bucketId)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.SelectById(id, bucketId)).Result;
            List<Obj> objects = MapObjects(result);
            if (objects.Count > 0) return objects[0];
            return null;
        }

        public long GetLatestVersion(string key, string bucketId)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.SelectLatestVersion(key, bucketId)).Result;
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

        public List<Obj> Enumerate(string bucketId, int startIndex, int maxResults, bool excludeDeleteMarkers, string prefix)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.Enumerate(bucketId, startIndex, maxResults, excludeDeleteMarkers, prefix)).Result;
            return MapObjects(result);
        }

        public BucketStatistics GetStatistics(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjQueries.GetStatistics(bucketId)).Result;
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
                obj.Id = row["id"] != DBNull.Value ? row["id"].ToString() : null;
                obj.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
                obj.BucketId = row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
                obj.OwnerId = row["owner_id"] != DBNull.Value ? row["owner_id"].ToString() : null;
                obj.AuthorId = row["author_id"] != DBNull.Value ? row["author_id"].ToString() : null;
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
                obj.IsFolder = ControlPlaneDataMapper.BoolValue(row, "isfolder");
                obj.DeleteMarker = ControlPlaneDataMapper.BoolValue(row, "deletemarker");
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
