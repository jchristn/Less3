namespace Less3.Database.SqlServer.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.SqlServer.Queries;

    internal class BucketAclMethods : IBucketAclMethods
    {
        private DatabaseDriverBase _Database;

        internal BucketAclMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public bool ExistsByGroupName(string groupName, string bucketGuid)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(BucketAclQueries.ExistsByGroupName(groupName, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsByUserGuid(string userGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(BucketAclQueries.ExistsByUserGuid(userGuid, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public List<BucketAcl> GetByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(BucketAclQueries.SelectByBucketGuid(bucketGuid)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public void Insert(BucketAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            _Database.ExecuteQuery(BucketAclQueries.InsertQuery(acl), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            _Database.ExecuteQuery(BucketAclQueries.DeleteByBucketGuid(bucketGuid), true).Wait();
        }

        private BucketAcl MapFromRow(DataRow row)
        {
            BucketAcl acl = new BucketAcl();
            acl.Id = Convert.ToInt32(row["id"]);
            acl.GUID = row["guid"] != null && row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
            acl.UserGroup = row["usergroup"] != null && row["usergroup"] != DBNull.Value ? row["usergroup"].ToString() : null;
            acl.BucketGUID = row["bucketguid"] != null && row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
            acl.UserGUID = row["userguid"] != null && row["userguid"] != DBNull.Value ? row["userguid"].ToString() : null;
            acl.IssuedByUserGUID = row["issuedbyuserguid"] != null && row["issuedbyuserguid"] != DBNull.Value ? row["issuedbyuserguid"].ToString() : null;
            acl.PermitRead = IsBitTrue(row["permitread"]);
            acl.PermitWrite = IsBitTrue(row["permitwrite"]);
            acl.PermitReadAcp = IsBitTrue(row["permitreadacp"]);
            acl.PermitWriteAcp = IsBitTrue(row["permitwriteacp"]);
            acl.FullControl = IsBitTrue(row["fullcontrol"]);
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

        private bool IsBitTrue(object val)
        {
            if (val == null || val == DBNull.Value) return false;
            string s = val.ToString();
            return s == "1" || s.Equals("True", StringComparison.OrdinalIgnoreCase);
        }
    }
}
