namespace Less3.Database.SqlServer.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
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
        public Obj GetLatestByKey(string key, string bucketGuid)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(ObjQueries.SelectLatestByKey(key, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public Obj GetByKeyAndVersion(string key, long version, string bucketGuid)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(ObjQueries.SelectByKeyAndVersion(key, version, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public Obj GetByGuid(string guid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(ObjQueries.SelectByGuid(guid, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public long GetLatestVersion(string key, string bucketGuid)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(ObjQueries.SelectLatestVersion(key, bucketGuid)).Result;
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
        public List<Obj> Enumerate(string bucketGuid, int startIndex, int maxResults, bool excludeDeleteMarkers, string prefix)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(ObjQueries.Enumerate(bucketGuid, startIndex, maxResults, excludeDeleteMarkers, prefix)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public BucketStatistics GetStatistics(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(ObjQueries.GetStatistics(bucketGuid)).Result;

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

            return new BucketStatistics("", bucketGuid, numObjects, totalBytes);
        }

        private Obj MapFromRow(DataRow row)
        {
            Obj obj = new Obj();
            obj.Id = Convert.ToInt32(row["id"]);
            obj.GUID = row["guid"] != null && row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
            obj.BucketGUID = row["bucketguid"] != null && row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
            obj.OwnerGUID = row["ownerguid"] != null && row["ownerguid"] != DBNull.Value ? row["ownerguid"].ToString() : null;
            obj.AuthorGUID = row["authorguid"] != null && row["authorguid"] != DBNull.Value ? row["authorguid"].ToString() : null;
            obj.Key = row["key"] != null && row["key"] != DBNull.Value ? row["key"].ToString() : null;
            obj.ContentType = row["contenttype"] != null && row["contenttype"] != DBNull.Value ? row["contenttype"].ToString() : null;
            obj.ContentLength = Convert.ToInt64(row["contentlength"]);
            obj.Version = Convert.ToInt64(row["version"]);
            obj.Etag = row["etag"] != null && row["etag"] != DBNull.Value ? row["etag"].ToString() : null;
            obj.Retention = Enum.Parse<RetentionType>(row["retention"].ToString());
            obj.BlobFilename = row["blobfilename"] != null && row["blobfilename"] != DBNull.Value ? row["blobfilename"].ToString() : null;
            obj.IsFolder = IsBitTrue(row["isfolder"]);
            obj.DeleteMarker = IsBitTrue(row["deletemarker"]);
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

        private bool IsBitTrue(object val)
        {
            if (val == null || val == DBNull.Value) return false;
            string s = val.ToString();
            return s == "1" || s.Equals("True", StringComparison.OrdinalIgnoreCase);
        }
    }
}
