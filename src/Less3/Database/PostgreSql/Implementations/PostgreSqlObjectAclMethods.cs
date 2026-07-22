namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlObjectAclMethods : IObjectAclMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlObjectAclMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public bool ExistsByGroupName(string groupName, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.ExistsByGroupName(groupName, objectId, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public bool ExistsByGroupName(string tenantId, string groupName, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.ExistsByGroupName(tenantId, groupName, objectId, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public bool ExistsByUserId(string userId, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.ExistsByUserId(userId, objectId, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public bool ExistsByUserId(string tenantId, string userId, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.ExistsByUserId(tenantId, userId, objectId, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public List<ObjectAcl> GetByObjectId(string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.SelectByObjectId(objectId, bucketId)).Result;
            return MapObjectAcls(result);
        }

        public List<ObjectAcl> GetByObjectId(string tenantId, string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.SelectByObjectId(tenantId, objectId, bucketId)).Result;
            return MapObjectAcls(result);
        }

        public List<ObjectAcl> GetByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.SelectByBucketId(bucketId)).Result;
            return MapObjectAcls(result);
        }

        public List<ObjectAcl> GetByBucketId(string tenantId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.SelectByBucketId(tenantId, bucketId)).Result;
            return MapObjectAcls(result);
        }

        public ObjectAcl GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.SelectById(tenantId, id)).Result;
            List<ObjectAcl> acls = MapObjectAcls(result);
            if (acls == null || acls.Count < 1) return null;
            return acls[0];
        }

        public bool ExistsById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.ExistsById(tenantId, id)).Result;
            if (result != null && result.Rows.Count > 0) return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public void Insert(ObjectAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            _Driver.ExecuteQuery(ObjectAclQueries.InsertQuery(acl), true).Wait();
        }

        public void Update(ObjectAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            _Driver.ExecuteQuery(ObjectAclQueries.UpdateQuery(acl), true).Wait();
        }

        public void DeleteByObjectIdAndBucketId(string objectId, string bucketId)
        {
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            _Driver.ExecuteQuery(ObjectAclQueries.DeleteByObjectIdAndBucketId(objectId, bucketId), true).Wait();
        }

        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(ObjectAclQueries.DeleteById(tenantId, id), true).Wait();
        }

        private List<ObjectAcl> MapObjectAcls(DataTable dt)
        {
            List<ObjectAcl> acls = new List<ObjectAcl>();
            if (dt == null || dt.Rows.Count == 0) return acls;

            foreach (DataRow row in dt.Rows)
            {
                ObjectAcl acl = new ObjectAcl();
                acl.Id = row["id"] != DBNull.Value ? row["id"].ToString() : null;
                acl.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
                acl.UserGroup = row["usergroup"] != DBNull.Value ? row["usergroup"].ToString() : null;
                acl.UserId = row["user_id"] != DBNull.Value ? row["user_id"].ToString() : null;
                acl.IssuedByUserId = row["issued_by_user_id"] != DBNull.Value ? row["issued_by_user_id"].ToString() : null;
                acl.BucketId = row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
                acl.ObjectId = row["object_id"] != DBNull.Value ? row["object_id"].ToString() : null;
                acl.PermitRead = ControlPlaneDataMapper.BoolValue(row, "permitread");
                acl.PermitWrite = ControlPlaneDataMapper.BoolValue(row, "permitwrite");
                acl.PermitReadAcp = ControlPlaneDataMapper.BoolValue(row, "permitreadacp");
                acl.PermitWriteAcp = ControlPlaneDataMapper.BoolValue(row, "permitwriteacp");
                acl.FullControl = ControlPlaneDataMapper.BoolValue(row, "fullcontrol");
                acl.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                acls.Add(acl);
            }

            return acls;
        }
    }
}
