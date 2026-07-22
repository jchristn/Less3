namespace Less3.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.Sqlite.Queries;

    internal class BucketTagMethods : IBucketTagMethods
    {
        private DatabaseDriverBase _Database;

        internal BucketTagMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public void Insert(BucketTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            _Database.ExecuteQuery(BucketTagQueries.InsertQuery(tag), true).Wait();
        }

        /// <inheritdoc />
        public List<BucketTag> GetByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(BucketTagQueries.SelectByBucketId(bucketId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public void DeleteByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            _Database.ExecuteQuery(BucketTagQueries.DeleteByBucketId(bucketId), true).Wait();
        }

        private BucketTag MapFromRow(DataRow row)
        {
            BucketTag tag = new BucketTag();
            tag.Id = row["id"] != null && row["id"] != DBNull.Value ? row["id"].ToString() : null;
            tag.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
            tag.BucketId = row["bucket_id"] != null && row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
            tag.Key = row["key"] != null && row["key"] != DBNull.Value ? row["key"].ToString() : null;
            tag.Value = row["value"] != null && row["value"] != DBNull.Value ? row["value"].ToString() : null;
            tag.CreatedUtc = DateTime.Parse(row["createdutc"].ToString());
            return tag;
        }

        private List<BucketTag> MapList(DataTable table)
        {
            List<BucketTag> list = new List<BucketTag>();
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
