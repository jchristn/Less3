namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlObjectAclMethods : IObjectAclMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlObjectAclMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public bool ExistsByGroupName(string groupName, string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.ExistsByGroupName(groupName, objectGuid, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public bool ExistsByUserGuid(string userGuid, string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.ExistsByUserGuid(userGuid, objectGuid, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        public List<ObjectAcl> GetByObjectGuid(string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.SelectByObjectGuid(objectGuid, bucketGuid)).Result;
            return MapObjectAcls(result);
        }

        public List<ObjectAcl> GetByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(ObjectAclQueries.SelectByBucketGuid(bucketGuid)).Result;
            return MapObjectAcls(result);
        }

        public void Insert(ObjectAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            _Driver.ExecuteQuery(ObjectAclQueries.InsertQuery(acl), true).Wait();
        }

        public void DeleteByObjectGuidAndBucketGuid(string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            _Driver.ExecuteQuery(ObjectAclQueries.DeleteByObjectGuidAndBucketGuid(objectGuid, bucketGuid), true).Wait();
        }

        private List<ObjectAcl> MapObjectAcls(DataTable dt)
        {
            List<ObjectAcl> acls = new List<ObjectAcl>();
            if (dt == null || dt.Rows.Count == 0) return acls;

            foreach (DataRow row in dt.Rows)
            {
                ObjectAcl acl = new ObjectAcl();
                acl.Id = Convert.ToInt32(row["id"]);
                acl.GUID = row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
                acl.UserGroup = row["usergroup"] != DBNull.Value ? row["usergroup"].ToString() : null;
                acl.UserGUID = row["userguid"] != DBNull.Value ? row["userguid"].ToString() : null;
                acl.IssuedByUserGUID = row["issuedbyuserguid"] != DBNull.Value ? row["issuedbyuserguid"].ToString() : null;
                acl.BucketGUID = row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
                acl.ObjectGUID = row["objectguid"] != DBNull.Value ? row["objectguid"].ToString() : null;
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
