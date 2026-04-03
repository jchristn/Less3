namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlBucketAclMethods : IBucketAclMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlBucketAclMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public bool ExistsByGroupName(string groupName, string bucketGuid)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(BucketAclQueries.ExistsByGroupName(groupName, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public bool ExistsByUserGuid(string userGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(BucketAclQueries.ExistsByUserGuid(userGuid, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public List<BucketAcl> GetByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(BucketAclQueries.SelectByBucketGuid(bucketGuid)).Result;
            return MapBucketAcls(result);
        }

        public void Insert(BucketAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            _Driver.ExecuteQuery(BucketAclQueries.InsertQuery(acl), true).Wait();
        }

        public void DeleteByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            _Driver.ExecuteQuery(BucketAclQueries.DeleteByBucketGuid(bucketGuid), true).Wait();
        }

        private List<BucketAcl> MapBucketAcls(DataTable dt)
        {
            List<BucketAcl> acls = new List<BucketAcl>();
            if (dt == null || dt.Rows.Count == 0) return acls;

            foreach (DataRow row in dt.Rows)
            {
                BucketAcl acl = new BucketAcl();
                acl.Id = Convert.ToInt32(row["id"]);
                acl.GUID = row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
                acl.UserGroup = row["usergroup"] != DBNull.Value ? row["usergroup"].ToString() : null;
                acl.BucketGUID = row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
                acl.UserGUID = row["userguid"] != DBNull.Value ? row["userguid"].ToString() : null;
                acl.IssuedByUserGUID = row["issuedbyuserguid"] != DBNull.Value ? row["issuedbyuserguid"].ToString() : null;
                acl.PermitRead = Convert.ToBoolean(row["permitread"]);
                acl.PermitWrite = Convert.ToBoolean(row["permitwrite"]);
                acl.PermitReadAcp = Convert.ToBoolean(row["permitreadacp"]);
                acl.PermitWriteAcp = Convert.ToBoolean(row["permitwriteacp"]);
                acl.FullControl = Convert.ToBoolean(row["fullcontrol"]);
                acl.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                acls.Add(acl);
            }

            return acls;
        }
    }
}
