namespace Less3.Database.MySql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.MySql.Queries;

    internal class UploadPartMethods : IUploadPartMethods
    {
        private DatabaseDriverBase _Database;

        internal UploadPartMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public void Insert(UploadPart part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            _Database.ExecuteQuery(UploadPartQueries.InsertQuery(part), true).Wait();
        }

        /// <inheritdoc />
        public List<UploadPart> GetByUploadGuid(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) throw new ArgumentNullException(nameof(uploadGuid));
            DataTable result = _Database.ExecuteQuery(UploadPartQueries.SelectByUploadGuid(uploadGuid)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public void DeleteByUploadGuid(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) throw new ArgumentNullException(nameof(uploadGuid));
            _Database.ExecuteQuery(UploadPartQueries.DeleteByUploadGuid(uploadGuid), true).Wait();
        }

        private UploadPart MapFromRow(DataRow row)
        {
            UploadPart part = new UploadPart();

            int id = Convert.ToInt32(row["id"]);
            if (id > 0) part.Id = id;

            part.GUID = row["guid"] != null && row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
            part.BucketGUID = row["bucketguid"] != null && row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
            part.OwnerGUID = row["ownerguid"] != null && row["ownerguid"] != DBNull.Value ? row["ownerguid"].ToString() : null;
            part.UploadGUID = row["uploadguid"] != null && row["uploadguid"] != DBNull.Value ? row["uploadguid"].ToString() : null;
            part.PartNumber = Convert.ToInt32(row["partnumber"]);
            part.PartLength = Convert.ToInt32(row["partlength"]);
            part.MD5Hash = row["md5hash"] != null && row["md5hash"] != DBNull.Value ? row["md5hash"].ToString() : null;
            part.Sha1Hash = row["sha1hash"] != null && row["sha1hash"] != DBNull.Value ? row["sha1hash"].ToString() : null;
            part.Sha256Hash = row["sha256hash"] != null && row["sha256hash"] != DBNull.Value ? row["sha256hash"].ToString() : null;
            part.LastAccessUtc = DateTime.Parse(row["lastaccessutc"].ToString());
            part.CreatedUtc = DateTime.Parse(row["createdutc"].ToString());
            return part;
        }

        private List<UploadPart> MapList(DataTable table)
        {
            List<UploadPart> list = new List<UploadPart>();
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
