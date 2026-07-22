namespace Less3.Database.MySql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.MySql.Queries;

    internal class ObjectAclMethods : IObjectAclMethods
    {
        private DatabaseDriverBase _Database;

        internal ObjectAclMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public bool ExistsByGroupName(string groupName, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.ExistsByGroupName(groupName, objectId, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsByGroupName(string tenantId, string groupName, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.ExistsByGroupName(tenantId, groupName, objectId, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsByUserId(string userId, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.ExistsByUserId(userId, objectId, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsByUserId(string tenantId, string userId, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.ExistsByUserId(tenantId, userId, objectId, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public List<ObjectAcl> GetByObjectId(string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.SelectByObjectId(objectId, bucketId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<ObjectAcl> GetByObjectId(string tenantId, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.SelectByObjectId(tenantId, objectId, bucketId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<ObjectAcl> GetByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.SelectByBucketId(bucketId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<ObjectAcl> GetByBucketId(string tenantId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.SelectByBucketId(tenantId, bucketId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public ObjectAcl GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.SelectById(tenantId, id)).Result;
            List<ObjectAcl> acls = MapList(result);
            if (acls == null || acls.Count < 1) return null;
            return acls[0];
        }

        /// <inheritdoc />
        public bool ExistsById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.ExistsById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0) return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public void Insert(ObjectAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            _Database.ExecuteQuery(ObjectAclQueries.InsertQuery(acl), true).Wait();
        }

        /// <inheritdoc />
        public void Update(ObjectAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            _Database.ExecuteQuery(ObjectAclQueries.UpdateQuery(acl), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByObjectIdAndBucketId(string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            _Database.ExecuteQuery(ObjectAclQueries.DeleteByObjectIdAndBucketId(objectId, bucketId), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(ObjectAclQueries.DeleteById(tenantId, id), true).Wait();
        }

        private ObjectAcl MapFromRow(DataRow row)
        {
            ObjectAcl acl = new ObjectAcl();
            acl.Id = row["id"] != null && row["id"] != DBNull.Value ? row["id"].ToString() : null;
            acl.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
            acl.UserGroup = row["usergroup"] != null && row["usergroup"] != DBNull.Value ? row["usergroup"].ToString() : null;
            acl.UserId = row["user_id"] != null && row["user_id"] != DBNull.Value ? row["user_id"].ToString() : null;
            acl.IssuedByUserId = row["issued_by_user_id"] != null && row["issued_by_user_id"] != DBNull.Value ? row["issued_by_user_id"].ToString() : null;
            acl.BucketId = row["bucket_id"] != null && row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
            acl.ObjectId = row["object_id"] != null && row["object_id"] != DBNull.Value ? row["object_id"].ToString() : null;
            acl.PermitRead = ControlPlaneDataMapper.BoolValue(row, "permitread");
            acl.PermitWrite = ControlPlaneDataMapper.BoolValue(row, "permitwrite");
            acl.PermitReadAcp = ControlPlaneDataMapper.BoolValue(row, "permitreadacp");
            acl.PermitWriteAcp = ControlPlaneDataMapper.BoolValue(row, "permitwriteacp");
            acl.FullControl = ControlPlaneDataMapper.BoolValue(row, "fullcontrol");
            acl.CreatedUtc = DateTime.Parse(row["createdutc"].ToString());
            return acl;
        }

        private List<ObjectAcl> MapList(DataTable table)
        {
            List<ObjectAcl> list = new List<ObjectAcl>();
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
