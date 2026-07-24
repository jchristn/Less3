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

        public List<BucketTag> GetByBucketId(string tenantId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(BucketTagQueries.SelectByBucketId(tenantId, bucketId)).Result;
            return MapBucketTags(result);
        }

        public BucketTag GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(BucketTagQueries.SelectById(tenantId, id)).Result;
            List<BucketTag> tags = MapBucketTags(result);
            if (tags == null || tags.Count < 1) return null;
            return tags[0];
        }

        public bool ExistsById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(BucketTagQueries.ExistsById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0) return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public void Update(BucketTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            _Driver.ExecuteQuery(BucketTagQueries.UpdateQuery(tag), true).Wait();
        }

        public void DeleteByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            _Driver.ExecuteQuery(BucketTagQueries.DeleteByBucketId(bucketId), true).Wait();
        }

        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(BucketTagQueries.DeleteById(tenantId, id), true).Wait();
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
