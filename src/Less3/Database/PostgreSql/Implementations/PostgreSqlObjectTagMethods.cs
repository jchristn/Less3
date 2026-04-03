namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlObjectTagMethods : IObjectTagMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlObjectTagMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public void Insert(ObjectTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            _Driver.ExecuteQuery(ObjectTagQueries.InsertQuery(tag), true).Wait();
        }

        public List<ObjectTag> GetByObjectGuid(string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(ObjectTagQueries.SelectByObjectGuid(objectGuid, bucketGuid)).Result;
            return MapObjectTags(result);
        }

        public void DeleteByObjectGuid(string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            _Driver.ExecuteQuery(ObjectTagQueries.DeleteByObjectGuid(objectGuid, bucketGuid), true).Wait();
        }

        private List<ObjectTag> MapObjectTags(DataTable dt)
        {
            List<ObjectTag> tags = new List<ObjectTag>();
            if (dt == null || dt.Rows.Count == 0) return tags;

            foreach (DataRow row in dt.Rows)
            {
                ObjectTag tag = new ObjectTag();
                tag.Id = Convert.ToInt32(row["id"]);
                tag.GUID = row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
                tag.BucketGUID = row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
                tag.ObjectGUID = row["objectguid"] != DBNull.Value ? row["objectguid"].ToString() : null;
                tag.Key = row["key"] != DBNull.Value ? row["key"].ToString() : null;
                tag.Value = row["value"] != DBNull.Value ? row["value"].ToString() : null;
                tag.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                tags.Add(tag);
            }

            return tags;
        }
    }
}
