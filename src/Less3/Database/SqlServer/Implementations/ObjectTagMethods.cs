namespace Less3.Database.SqlServer.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.SqlServer.Queries;

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
        public List<ObjectTag> GetByObjectGuid(string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(ObjectTagQueries.SelectByObjectGuid(objectGuid, bucketGuid)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public void DeleteByObjectGuid(string objectGuid, string bucketGuid)
        {
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            _Database.ExecuteQuery(ObjectTagQueries.DeleteByObjectGuid(objectGuid, bucketGuid), true).Wait();
        }

        private ObjectTag MapFromRow(DataRow row)
        {
            ObjectTag tag = new ObjectTag();
            tag.Id = Convert.ToInt32(row["id"]);
            tag.GUID = row["guid"] != null && row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
            tag.BucketGUID = row["bucketguid"] != null && row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
            tag.ObjectGUID = row["objectguid"] != null && row["objectguid"] != DBNull.Value ? row["objectguid"].ToString() : null;
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
