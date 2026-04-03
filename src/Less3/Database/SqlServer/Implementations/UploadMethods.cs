namespace Less3.Database.SqlServer.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.SqlServer.Queries;

    internal class UploadMethods : IUploadMethods
    {
        private DatabaseDriverBase _Database;

        internal UploadMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public Upload GetByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Database.ExecuteQuery(UploadQueries.SelectByGuid(guid)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public List<Upload> GetAll()
        {
            DataTable result = _Database.ExecuteQuery(UploadQueries.SelectAll()).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<Upload> GetByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Database.ExecuteQuery(UploadQueries.SelectByBucketGuid(bucketGuid)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public void Insert(Upload upload)
        {
            if (upload == null) throw new ArgumentNullException(nameof(upload));
            _Database.ExecuteQuery(UploadQueries.InsertQuery(upload), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Database.ExecuteQuery(UploadQueries.DeleteByGuid(guid), true).Wait();
        }

        private Upload MapFromRow(DataRow row)
        {
            Upload upload = new Upload();
            upload.Id = Convert.ToInt32(row["id"]);
            upload.GUID = row["guid"] != null && row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
            upload.BucketGUID = row["bucketguid"] != null && row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
            upload.OwnerGUID = row["ownerguid"] != null && row["ownerguid"] != DBNull.Value ? row["ownerguid"].ToString() : null;
            upload.AuthorGUID = row["authorguid"] != null && row["authorguid"] != DBNull.Value ? row["authorguid"].ToString() : null;
            upload.Key = row["key"] != null && row["key"] != DBNull.Value ? row["key"].ToString() : null;
            upload.CreatedUtc = DateTime.Parse(row["createdutc"].ToString());
            upload.LastAccessUtc = DateTime.Parse(row["lastaccessutc"].ToString());
            upload.ExpirationUtc = DateTime.Parse(row["expirationutc"].ToString());
            upload.ContentType = row["contenttype"] != null && row["contenttype"] != DBNull.Value ? row["contenttype"].ToString() : null;
            upload.Metadata = row["metadata"] != null && row["metadata"] != DBNull.Value ? row["metadata"].ToString() : null;
            return upload;
        }

        private List<Upload> MapList(DataTable table)
        {
            List<Upload> list = new List<Upload>();
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
