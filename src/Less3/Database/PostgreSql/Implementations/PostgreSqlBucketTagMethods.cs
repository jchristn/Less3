namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
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

        public List<BucketTag> GetByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(BucketTagQueries.SelectByBucketGuid(bucketGuid)).Result;
            return MapBucketTags(result);
        }

        public void DeleteByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            _Driver.ExecuteQuery(BucketTagQueries.DeleteByBucketGuid(bucketGuid), true).Wait();
        }

        private List<BucketTag> MapBucketTags(DataTable dt)
        {
            List<BucketTag> tags = new List<BucketTag>();
            if (dt == null || dt.Rows.Count == 0) return tags;

            foreach (DataRow row in dt.Rows)
            {
                BucketTag tag = new BucketTag();
                tag.Id = Convert.ToInt32(row["id"]);
                tag.GUID = row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
                tag.BucketGUID = row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
                tag.Key = row["key"] != DBNull.Value ? row["key"].ToString() : null;
                tag.Value = row["value"] != DBNull.Value ? row["value"].ToString() : null;
                tag.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                tags.Add(tag);
            }

            return tags;
        }
    }
}
