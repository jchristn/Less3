namespace Less3.Database.MySql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.MySql.Queries;
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
        public bool ExistsByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable result = _Database.ExecuteQuery(BucketQueries.ExistsByName(name)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public List<Bucket> GetByOwnerGuid(string ownerGuid)
        {
            if (String.IsNullOrEmpty(ownerGuid)) throw new ArgumentNullException(nameof(ownerGuid));
            DataTable result = _Database.ExecuteQuery(BucketQueries.SelectByOwnerGuid(ownerGuid)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public Bucket GetByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Database.ExecuteQuery(BucketQueries.SelectByGuid(guid)).Result;
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
        public void Insert(Bucket bucket)
        {
            if (bucket == null) throw new ArgumentNullException(nameof(bucket));
            _Database.ExecuteQuery(BucketQueries.InsertQuery(bucket), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Database.ExecuteQuery(BucketQueries.DeleteByGuid(guid), true).Wait();
        }

        private Bucket MapFromRow(DataRow row)
        {
            Bucket bucket = new Bucket();
            bucket.Id = Convert.ToInt32(row["id"]);
            bucket.GUID = row["guid"] != null && row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
            bucket.OwnerGUID = row["ownerguid"] != null && row["ownerguid"] != DBNull.Value ? row["ownerguid"].ToString() : null;
            bucket.Name = row["name"] != null && row["name"] != DBNull.Value ? row["name"].ToString() : null;
            bucket.RegionString = row["regionstring"] != null && row["regionstring"] != DBNull.Value ? row["regionstring"].ToString() : null;
            bucket.StorageType = Enum.Parse<StorageDriverType>(row["storagetype"].ToString());
            bucket.DiskDirectory = row["diskdirectory"] != null && row["diskdirectory"] != DBNull.Value ? row["diskdirectory"].ToString() : null;
            bucket.EnableVersioning = Convert.ToInt32(row["enableversioning"]) != 0;
            bucket.EnablePublicWrite = Convert.ToInt32(row["enablepublicwrite"]) != 0;
            bucket.EnablePublicRead = Convert.ToInt32(row["enablepublicread"]) != 0;
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
