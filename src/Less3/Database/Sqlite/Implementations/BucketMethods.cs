namespace Less3.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.Sqlite.Queries;
    using Less3.Storage;

    internal class BucketMethods : IBucketMethods
    {
        private DatabaseDriverBase _Database;

        internal BucketMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public List<Bucket> GetAll()
        {
            DataTable result = _Database.ExecuteQuery(BucketQueries.SelectAll()).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<Bucket> GetAll(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            DataTable result = _Database.ExecuteQuery(BucketQueries.SelectAll(tenantId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public bool ExistsByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Database.ExecuteQuery(BucketQueries.ExistsByName(name)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsByName(string tenantId, string name)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Database.ExecuteQuery(BucketQueries.ExistsByName(tenantId, name)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public List<Bucket> GetByOwnerId(string ownerId)
        {
            if (String.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            DataTable result = _Database.ExecuteQuery(BucketQueries.SelectByOwnerId(ownerId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<Bucket> GetByOwnerId(string tenantId, string ownerId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            DataTable result = _Database.ExecuteQuery(BucketQueries.SelectByOwnerId(tenantId, ownerId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public Bucket GetById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(BucketQueries.SelectById(id)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public Bucket GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(BucketQueries.SelectById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public Bucket GetByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Database.ExecuteQuery(BucketQueries.SelectByName(name)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public Bucket GetByName(string tenantId, string name)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Database.ExecuteQuery(BucketQueries.SelectByName(tenantId, name)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public void Insert(Bucket bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));
            _Database.ExecuteQuery(BucketQueries.InsertQuery(bucket), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(BucketQueries.DeleteById(id), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(BucketQueries.DeleteById(tenantId, id), true).Wait();
        }

        private Bucket MapFromRow(DataRow row)
        {
            Bucket bucket = new Bucket();
            bucket.Id = row["id"] != null && row["id"] != DBNull.Value ? row["id"].ToString() : null;
            bucket.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
            bucket.OwnerId = row["owner_id"] != null && row["owner_id"] != DBNull.Value ? row["owner_id"].ToString() : null;
            bucket.Name = row["name"] != null && row["name"] != DBNull.Value ? row["name"].ToString() : null;
            bucket.RegionString = row["regionstring"] != null && row["regionstring"] != DBNull.Value ? row["regionstring"].ToString() : null;
            bucket.StorageType = Enum.Parse<StorageDriverType>(row["storagetype"].ToString());
            bucket.DiskDirectory = row["diskdirectory"] != null && row["diskdirectory"] != DBNull.Value ? row["diskdirectory"].ToString() : null;
            bucket.EnableVersioning = ControlPlaneDataMapper.BoolValue(row, "enableversioning");
            bucket.EnablePublicWrite = ControlPlaneDataMapper.BoolValue(row, "enablepublicwrite");
            bucket.EnablePublicRead = ControlPlaneDataMapper.BoolValue(row, "enablepublicread");
            bucket.CreatedUtc = DateTime.Parse(row["createdutc"].ToString());
            return bucket;
        }

        private List<Bucket> MapList(DataTable table)
        {
            List<Bucket> list = new List<Bucket>();
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
