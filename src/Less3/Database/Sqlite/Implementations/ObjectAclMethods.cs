namespace Less3.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.Sqlite.Queries;

    internal class ObjectAclMethods : IObjectAclMethods
    {
        private DatabaseDriverBase _Database;

        internal ObjectAclMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public bool ExistsByGroupName(string groupName, string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.ExistsByGroupName(groupName, objectGuid, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public bool ExistsByUserGuid(string userGuid, string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.ExistsByUserGuid(userGuid, objectGuid, bucketGuid)).Result;
            if (result != null && result.Rows.Count > 0)
                return Convert.ToInt32(result.Rows[0]["cnt"]) > 0;
            return false;
        }

        /// <inheritdoc />
        public List<ObjectAcl> GetByObjectGuid(string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.SelectByObjectGuid(objectGuid, bucketGuid)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<ObjectAcl> GetByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(ObjectAclQueries.SelectByBucketGuid(bucketGuid)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public void Insert(ObjectAcl acl)
        {
            if (acl == null) throw new ArgumentNullException(nameof(acl));
            _Database.ExecuteQuery(ObjectAclQueries.InsertQuery(acl), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByObjectGuidAndBucketGuid(string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            _Database.ExecuteQuery(ObjectAclQueries.DeleteByObjectGuidAndBucketGuid(objectGuid, bucketGuid), true).Wait();
        }

        private ObjectAcl MapFromRow(DataRow row)
        {
            ObjectAcl acl = new ObjectAcl();
            acl.Id = Convert.ToInt32(row["id"]);
            acl.GUID = row["guid"] != null && row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
            acl.UserGroup = row["usergroup"] != null && row["usergroup"] != DBNull.Value ? row["usergroup"].ToString() : null;
            acl.UserGUID = row["userguid"] != null && row["userguid"] != DBNull.Value ? row["userguid"].ToString() : null;
            acl.IssuedByUserGUID = row["issuedbyuserguid"] != null && row["issuedbyuserguid"] != DBNull.Value ? row["issuedbyuserguid"].ToString() : null;
            acl.BucketGUID = row["bucketguid"] != null && row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
            acl.ObjectGUID = row["objectguid"] != null && row["objectguid"] != DBNull.Value ? row["objectguid"].ToString() : null;
            acl.PermitRead = Convert.ToInt32(row["permitread"]) != 0;
            acl.PermitWrite = Convert.ToInt32(row["permitwrite"]) != 0;
            acl.PermitReadAcp = Convert.ToInt32(row["permitreadacp"]) != 0;
            acl.PermitWriteAcp = Convert.ToInt32(row["permitwriteacp"]) != 0;
            acl.FullControl = Convert.ToInt32(row["fullcontrol"]) != 0;
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
