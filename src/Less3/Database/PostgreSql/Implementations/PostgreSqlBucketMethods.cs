namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
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

        public List<Bucket> GetAll(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            DataTable result = _Driver.ExecuteQuery(BucketQueries.SelectAll(tenantId)).Result;
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

        public bool ExistsByName(string tenantId, string name)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Driver.ExecuteQuery(BucketQueries.ExistsByName(tenantId, name)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public List<Bucket> GetByOwnerId(string ownerId)
        {
            if (String.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            DataTable result = _Driver.ExecuteQuery(BucketQueries.SelectByOwnerId(ownerId)).Result;
            return MapBuckets(result);
        }

        public List<Bucket> GetByOwnerId(string tenantId, string ownerId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            DataTable result = _Driver.ExecuteQuery(BucketQueries.SelectByOwnerId(tenantId, ownerId)).Result;
            return MapBuckets(result);
        }

        public Bucket GetById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(BucketQueries.SelectById(id)).Result;
            List<Bucket> buckets = MapBuckets(result);
            if (buckets.Count > 0) return buckets[0];
            return null;
        }

        public Bucket GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(BucketQueries.SelectById(tenantId, id)).Result;
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

        public Bucket GetByName(string tenantId, string name)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Driver.ExecuteQuery(BucketQueries.SelectByName(tenantId, name)).Result;
            List<Bucket> buckets = MapBuckets(result);
            if (buckets.Count > 0) return buckets[0];
            return null;
        }

        public void Insert(Bucket bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));
            _Driver.ExecuteQuery(BucketQueries.InsertQuery(bucket), true).Wait();
        }

        public void Update(Bucket bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));
            _Driver.ExecuteQuery(BucketQueries.UpdateQuery(bucket), true).Wait();
        }

        public void DeleteById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(BucketQueries.DeleteById(id), true).Wait();
        }

        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(BucketQueries.DeleteById(tenantId, id), true).Wait();
        }

        private List<Bucket> MapBuckets(DataTable dt)
        {
            List<Bucket> buckets = new List<Bucket>();
            if (dt == null || dt.Rows.Count == 0) return buckets;

            foreach (DataRow row in dt.Rows)
            {
                Bucket bucket = new Bucket();
                bucket.Id = row["id"] != DBNull.Value ? row["id"].ToString() : null;
                bucket.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
                bucket.OwnerId = row["owner_id"] != DBNull.Value ? row["owner_id"].ToString() : null;
                bucket.Name = row["name"] != DBNull.Value ? row["name"].ToString() : null;
                bucket.RegionString = row["regionstring"] != DBNull.Value ? row["regionstring"].ToString() : null;

                string storageTypeStr = row["storagetype"] != DBNull.Value ? row["storagetype"].ToString() : "Disk";
                if (Enum.TryParse<StorageDriverType>(storageTypeStr, true, out StorageDriverType storageType))
                    bucket.StorageType = storageType;
                else
                    bucket.StorageType = StorageDriverType.Disk;

                bucket.DiskDirectory = row["diskdirectory"] != DBNull.Value ? row["diskdirectory"].ToString() : null;
                bucket.EnableVersioning = ControlPlaneDataMapper.BoolValue(row, "enableversioning");
                bucket.EnablePublicWrite = ControlPlaneDataMapper.BoolValue(row, "enablepublicwrite");
                bucket.EnablePublicRead = ControlPlaneDataMapper.BoolValue(row, "enablepublicread");
                bucket.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                buckets.Add(bucket);
            }

            return buckets;
        }
    }
}
