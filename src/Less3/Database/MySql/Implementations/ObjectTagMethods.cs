namespace Less3.Database.MySql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.MySql.Queries;

    internal class ObjectTagMethods : IObjectTagMethods
    {
        private DatabaseDriverBase _Database;

        internal ObjectTagMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public void Insert(ObjectTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            _Database.ExecuteQuery(ObjectTagQueries.InsertQuery(tag), true).Wait();
        }

        /// <inheritdoc />
        public List<ObjectTag> GetByObjectId(string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjectTagQueries.SelectByObjectId(objectId, bucketId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<ObjectTag> GetByObjectId(string tenantId, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjectTagQueries.SelectByObjectId(tenantId, objectId, bucketId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public ObjectTag GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(ObjectTagQueries.SelectById(tenantId, id)).Result;
            List<ObjectTag> tags = MapList(result);
            if (tags == null || tags.Count < 1) return null;
            return tags[0];
        }

        /// <inheritdoc />
        public bool ExistsById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(ObjectTagQueries.ExistsById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0) return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public void Update(ObjectTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            _Database.ExecuteQuery(ObjectTagQueries.UpdateQuery(tag), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByObjectId(string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            _Database.ExecuteQuery(ObjectTagQueries.DeleteByObjectId(objectId, bucketId), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(ObjectTagQueries.DeleteById(tenantId, id), true).Wait();
        }

        private ObjectTag MapFromRow(DataRow row)
        {
            ObjectTag tag = new ObjectTag();
            tag.Id = row["id"] != null && row["id"] != DBNull.Value ? row["id"].ToString() : null;
            tag.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
            tag.BucketId = row["bucket_id"] != null && row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
            tag.ObjectId = row["object_id"] != null && row["object_id"] != DBNull.Value ? row["object_id"].ToString() : null;
            tag.Key = row["key"] != null && row["key"] != DBNull.Value ? row["key"].ToString() : null;
            tag.Value = row["value"] != null && row["value"] != DBNull.Value ? row["value"].ToString() : null;
            tag.CreatedUtc = DateTime.Parse(row["createdutc"].ToString());
            return tag;
        }

        private List<ObjectTag> MapList(DataTable table)
        {
            List<ObjectTag> list = new List<ObjectTag>();
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
