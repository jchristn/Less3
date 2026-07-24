namespace Less3.Database.SqlServer.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.SqlServer.Queries;

    internal class ObjMethods : IObjMethods
    {
        private DatabaseDriverBase _Database;

        internal ObjMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public void Insert(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            _Database.ExecuteQuery(ObjQueries.InsertQuery(obj), true).Wait();
        }

        /// <inheritdoc />
        public Obj GetLatestByKey(string key, string bucketId)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjQueries.SelectLatestByKey(key, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public Obj GetByKeyAndVersion(string key, long version, string bucketId)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjQueries.SelectByKeyAndVersion(key, version, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public Obj GetById(string id, string bucketId)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjQueries.SelectById(id, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public long GetLatestVersion(string key, string bucketId)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjQueries.SelectLatestVersion(key, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
            {
                object val = result.Rows[0]["version"];
                if (val != null && val != DBNull.Value && !String.IsNullOrEmpty(val.ToString()))
                    return Convert.ToInt64(val);
            }
            return 0;
        }

        /// <inheritdoc />
        public void Update(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            _Database.ExecuteQuery(ObjQueries.UpdateQuery(obj), true).Wait();
        }

        /// <inheritdoc />
        public void Delete(Obj obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            _Database.ExecuteQuery(ObjQueries.DeleteById(obj.Id), true).Wait();
        }

        /// <inheritdoc />
        public List<Obj> Enumerate(string bucketId, int startIndex, int maxResults, bool excludeDeleteMarkers, string prefix)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjQueries.Enumerate(bucketId, startIndex, maxResults, excludeDeleteMarkers, prefix)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public BucketStatistics GetStatistics(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjQueries.GetStatistics(bucketId)).Result;

            long numObjects = 0;
            long totalBytes = 0;

            if (result != null && result.Rows.Count > 0)
            {
                object numObj = result.Rows[0]["numobjects"];
                object totalObj = result.Rows[0]["totalbytes"];

                if (numObj != null && numObj != DBNull.Value && !String.IsNullOrEmpty(numObj.ToString()))
                    numObjects = Convert.ToInt64(numObj);

                if (totalObj != null && totalObj != DBNull.Value && !String.IsNullOrEmpty(totalObj.ToString()))
                    totalBytes = Convert.ToInt64(totalObj);
            }

            return new BucketStatistics("", bucketId, numObjects, totalBytes);
        }

        private Obj MapFromRow(DataRow row)
        {
            Obj obj = new Obj();
            obj.Id = row["id"] != null && row["id"] != DBNull.Value ? row["id"].ToString() : null;
            obj.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
            obj.BucketId = row["bucket_id"] != null && row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
            obj.OwnerId = row["owner_id"] != null && row["owner_id"] != DBNull.Value ? row["owner_id"].ToString() : null;
            obj.AuthorId = row["author_id"] != null && row["author_id"] != DBNull.Value ? row["author_id"].ToString() : null;
            obj.Key = row["key"] != null && row["key"] != DBNull.Value ? row["key"].ToString() : null;
            obj.ContentType = row["contenttype"] != null && row["contenttype"] != DBNull.Value ? row["contenttype"].ToString() : null;
            obj.ContentLength = Convert.ToInt64(row["contentlength"]);
            obj.Version = Convert.ToInt64(row["version"]);
            obj.Etag = row["etag"] != null && row["etag"] != DBNull.Value ? row["etag"].ToString() : null;
            obj.Retention = Enum.Parse<RetentionType>(row["retention"].ToString());
            obj.BlobFilename = row["blobfilename"] != null && row["blobfilename"] != DBNull.Value ? row["blobfilename"].ToString() : null;
            obj.IsFolder = ControlPlaneDataMapper.BoolValue(row, "isfolder");
            obj.DeleteMarker = ControlPlaneDataMapper.BoolValue(row, "deletemarker");
            obj.Md5 = row["md5"] != null && row["md5"] != DBNull.Value ? row["md5"].ToString() : null;
            obj.CreatedUtc = DateTime.Parse(row["createdutc"].ToString());
            obj.LastUpdateUtc = DateTime.Parse(row["lastupdateutc"].ToString());
            obj.LastAccessUtc = DateTime.Parse(row["lastaccessutc"].ToString());
            obj.Metadata = row["metadata"] != null && row["metadata"] != DBNull.Value ? row["metadata"].ToString() : null;

            if (row["expirationutc"] != null && row["expirationutc"] != DBNull.Value && !String.IsNullOrEmpty(row["expirationutc"].ToString()))
                obj.ExpirationUtc = DateTime.Parse(row["expirationutc"].ToString());
            else
                obj.ExpirationUtc = null;

            return obj;
        }

        private List<Obj> MapList(DataTable table)
        {
            List<Obj> list = new List<Obj>();
            if (table != null && table.Rows.Count > 0)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapFromRow(row));
                }
            }
            return list;
        }
    }
}
