namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlBucketAclMethods : IBucketAclMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlBucketAclMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public bool ExistsByGroupName(string groupName, string bucketId)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(BucketAclQueries.ExistsByGroupName(groupName, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public bool ExistsByUserId(string userId, string bucketId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(BucketAclQueries.ExistsByUserId(userId, bucketId)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public List<BucketAcl> GetByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(BucketAclQueries.SelectByBucketId(bucketId)).Result;
            return MapBucketAcls(result);
        }

        public void Insert(BucketAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            _Driver.ExecuteQuery(BucketAclQueries.InsertQuery(acl), true).Wait();
        }

        public void DeleteByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            _Driver.ExecuteQuery(BucketAclQueries.DeleteByBucketId(bucketId), true).Wait();
        }

        private List<BucketAcl> MapBucketAcls(DataTable dt)
        {
            List<BucketAcl> acls = new List<BucketAcl>();
            if (dt == null || dt.Rows.Count == 0) return acls;

            foreach (DataRow row in dt.Rows)
            {
                BucketAcl acl = new BucketAcl();
                acl.Id = row["id"] != DBNull.Value ? row["id"].ToString() : null;
                acl.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
                acl.UserGroup = row["usergroup"] != DBNull.Value ? row["usergroup"].ToString() : null;
                acl.BucketId = row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
                acl.UserId = row["user_id"] != DBNull.Value ? row["user_id"].ToString() : null;
                acl.IssuedByUserId = row["issued_by_user_id"] != DBNull.Value ? row["issued_by_user_id"].ToString() : null;
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
