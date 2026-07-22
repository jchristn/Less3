namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
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

        public List<ObjectTag> GetByObjectId(string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjectTagQueries.SelectByObjectId(objectId, bucketId)).Result;
            return MapObjectTags(result);
        }

        public List<ObjectTag> GetByObjectId(string tenantId, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjectTagQueries.SelectByObjectId(tenantId, objectId, bucketId)).Result;
            return MapObjectTags(result);
        }

        public ObjectTag GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(ObjectTagQueries.SelectById(tenantId, id)).Result;
            List<ObjectTag> tags = MapObjectTags(result);
            if (tags == null || tags.Count < 1) return null;
            return tags[0];
        }

        public bool ExistsById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(ObjectTagQueries.ExistsById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0) return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public void Update(ObjectTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            _Driver.ExecuteQuery(ObjectTagQueries.UpdateQuery(tag), true).Wait();
        }

        public void DeleteByObjectId(string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            _Driver.ExecuteQuery(ObjectTagQueries.DeleteByObjectId(objectId, bucketId), true).Wait();
        }

        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(ObjectTagQueries.DeleteById(tenantId, id), true).Wait();
        }

        private List<ObjectTag> MapObjectTags(DataTable dt)
        {
            List<ObjectTag> tags = new List<ObjectTag>();
            if (dt == null || dt.Rows.Count == 0) return tags;

            foreach (DataRow row in dt.Rows)
            {
                ObjectTag tag = new ObjectTag();
                tag.Id = row["id"] != DBNull.Value ? row["id"].ToString() : null;
                tag.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
                tag.BucketId = row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
                tag.ObjectId = row["object_id"] != DBNull.Value ? row["object_id"].ToString() : null;
                tag.Key = row["key"] != DBNull.Value ? row["key"].ToString() : null;
                tag.Value = row["value"] != DBNull.Value ? row["value"].ToString() : null;
                tag.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                tags.Add(tag);
            }

            return tags;
        }
    }
}
