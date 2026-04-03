namespace Less3.Database.SqlServer.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.SqlServer.Queries;

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
        public List<BucketTag> GetByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(BucketTagQueries.SelectByBucketGuid(bucketGuid)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public void DeleteByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            _Database.ExecuteQuery(BucketTagQueries.DeleteByBucketGuid(bucketGuid), true).Wait();
        }

        private BucketTag MapFromRow(DataRow row)
        {
            BucketTag tag = new BucketTag();
            tag.Id = Convert.ToInt32(row["id"]);
            tag.GUID = row["guid"] != null && row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
            tag.BucketGUID = row["bucketguid"] != null && row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
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
