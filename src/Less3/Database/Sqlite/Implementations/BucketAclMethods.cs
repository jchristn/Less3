namespace Less3.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.Sqlite.Queries;

    internal class BucketAclMethods : IBucketAclMethods
    {
        private DatabaseDriverBase _Database;

        internal BucketAclMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public bool ExistsByGroupName(string groupName, string bucketId)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(BucketAclQueries.ExistsByGroupName(groupName, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsByGroupName(string tenantId, string groupName, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(BucketAclQueries.ExistsByGroupName(tenantId, groupName, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsByUserId(string userId, string bucketId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(BucketAclQueries.ExistsByUserId(userId, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsByUserId(string tenantId, string userId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(BucketAclQueries.ExistsByUserId(tenantId, userId, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public List<BucketAcl> GetByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(BucketAclQueries.SelectByBucketId(bucketId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<BucketAcl> GetByBucketId(string tenantId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(BucketAclQueries.SelectByBucketId(tenantId, bucketId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public BucketAcl GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(BucketAclQueries.SelectById(tenantId, id)).Result;
            List<BucketAcl> acls = MapList(result);
            if (acls == null || acls.Count < 1) return null;
            return acls[0];
        }

        /// <inheritdoc />
        public bool ExistsById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(BucketAclQueries.ExistsById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0) return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public void Insert(BucketAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            _Database.ExecuteQuery(BucketAclQueries.InsertQuery(acl), true).Wait();
        }

        /// <inheritdoc />
        public void Update(BucketAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            _Database.ExecuteQuery(BucketAclQueries.UpdateQuery(acl), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            _Database.ExecuteQuery(BucketAclQueries.DeleteByBucketId(bucketId), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(BucketAclQueries.DeleteById(tenantId, id), true).Wait();
        }

        private BucketAcl MapFromRow(DataRow row)
        {
            BucketAcl acl = new BucketAcl();
            acl.Id = row["id"] != null && row["id"] != DBNull.Value ? row["id"].ToString() : null;
            acl.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
            acl.UserGroup = row["usergroup"] != null && row["usergroup"] != DBNull.Value ? row["usergroup"].ToString() : null;
            acl.BucketId = row["bucket_id"] != null && row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
            acl.UserId = row["user_id"] != null && row["user_id"] != DBNull.Value ? row["user_id"].ToString() : null;
            acl.IssuedByUserId = row["issued_by_user_id"] != null && row["issued_by_user_id"] != DBNull.Value ? row["issued_by_user_id"].ToString() : null;
            acl.PermitRead = ControlPlaneDataMapper.BoolValue(row, "permitread");
            acl.PermitWrite = ControlPlaneDataMapper.BoolValue(row, "permitwrite");
            acl.PermitReadAcp = ControlPlaneDataMapper.BoolValue(row, "permitreadacp");
            acl.PermitWriteAcp = ControlPlaneDataMapper.BoolValue(row, "permitwriteacp");
            acl.FullControl = ControlPlaneDataMapper.BoolValue(row, "fullcontrol");
            acl.CreatedUtc = DateTime.Parse(row["createdutc"].ToString());
            return acl;
        }

        private List<BucketAcl> MapList(DataTable table)
        {
            List<BucketAcl> list = new List<BucketAcl>();
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
