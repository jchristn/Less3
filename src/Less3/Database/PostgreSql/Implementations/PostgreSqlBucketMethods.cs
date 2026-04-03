namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;
    using Less3.Storage;

    internal class PostgreSqlBucketMethods : IBucketMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlBucketMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public List<Bucket> GetAll()
        {
            DataTable result = _Driver.ExecuteQuery(BucketQueries.SelectAll()).Result;
            return MapBuckets(result);
        }

        public bool ExistsByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Driver.ExecuteQuery(BucketQueries.ExistsByName(name)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public List<Bucket> GetByOwnerGuid(string ownerGuid)
        {
            if (String.IsNullOrEmpty(ownerGuid)) throw new ArgumentNullException(nameof(ownerGuid));
            DataTable result = _Driver.ExecuteQuery(BucketQueries.SelectByOwnerGuid(ownerGuid)).Result;
            return MapBuckets(result);
        }

        public Bucket GetByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Driver.ExecuteQuery(BucketQueries.SelectByGuid(guid)).Result;
            List<Bucket> buckets = MapBuckets(result);
            if (buckets.Count > 0) return buckets[0];
            return null;
        }

        public Bucket GetByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Driver.ExecuteQuery(BucketQueries.SelectByName(name)).Result;
            List<Bucket> buckets = MapBuckets(result);
            if (buckets.Count > 0) return buckets[0];
            return null;
        }

        public void Insert(Bucket bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));
            _Driver.ExecuteQuery(BucketQueries.InsertQuery(bucket), true).Wait();
        }

        public void DeleteByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Driver.ExecuteQuery(BucketQueries.DeleteByGuid(guid), true).Wait();
        }

        private List<Bucket> MapBuckets(DataTable dt)
        {
            List<Bucket> buckets = new List<Bucket>();
            if (dt == null || dt.Rows.Count == 0) return buckets;

            foreach (DataRow row in dt.Rows)
            {
                Bucket bucket = new Bucket();
                bucket.Id = Convert.ToInt32(row["id"]);
                bucket.GUID = row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
                bucket.OwnerGUID = row["ownerguid"] != DBNull.Value ? row["ownerguid"].ToString() : null;
                bucket.Name = row["name"] != DBNull.Value ? row["name"].ToString() : null;
                bucket.RegionString = row["regionstring"] != DBNull.Value ? row["regionstring"].ToString() : null;

                string storageTypeStr = row["storagetype"] != DBNull.Value ? row["storagetype"].ToString() : "Disk";
                if (Enum.TryParse<StorageDriverType>(storageTypeStr, true, out StorageDriverType storageType))
                    bucket.StorageType = storageType;
                else
                    bucket.StorageType = StorageDriverType.Disk;

                bucket.DiskDirectory = row["diskdirectory"] != DBNull.Value ? row["diskdirectory"].ToString() : null;
                bucket.EnableVersioning = Convert.ToBoolean(row["enableversioning"]);
                bucket.EnablePublicWrite = Convert.ToBoolean(row["enablepublicwrite"]);
                bucket.EnablePublicRead = Convert.ToBoolean(row["enablepublicread"]);
                bucket.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                buckets.Add(bucket);
            }

            return buckets;
        }
    }
}
