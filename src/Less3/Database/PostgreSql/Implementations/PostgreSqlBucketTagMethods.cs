namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlBucketTagMethods : IBucketTagMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlBucketTagMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public void Insert(BucketTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            _Driver.ExecuteQuery(BucketTagQueries.InsertQuery(tag), true).Wait();
        }

        public List<BucketTag> GetByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(BucketTagQueries.SelectByBucketId(bucketId)).Result;
            return MapBucketTags(result);
        }

        public void DeleteByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            _Driver.ExecuteQuery(BucketTagQueries.DeleteByBucketId(bucketId), true).Wait();
        }

        private List<BucketTag> MapBucketTags(DataTable dt)
        {
            List<BucketTag> tags = new List<BucketTag>();
            if (dt == null || dt.Rows.Count == 0) return tags;

            foreach (DataRow row in dt.Rows)
            {
                BucketTag tag = new BucketTag();
                tag.Id = row["id"] != DBNull.Value ? row["id"].ToString() : null;
                tag.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
                tag.BucketId = row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
                tag.Key = row["key"] != DBNull.Value ? row["key"].ToString() : null;
                tag.Value = row["value"] != DBNull.Value ? row["value"].ToString() : null;
                tag.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                tags.Add(tag);
            }

            return tags;
        }
    }
}
