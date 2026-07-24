namespace Less3.Database.MySql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.MySql.Queries;

    internal class UploadMethods : IUploadMethods
    {
        private DatabaseDriverBase _Database;

        internal UploadMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public Upload GetById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(UploadQueries.SelectById(id)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public Upload GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(UploadQueries.SelectById(tenantId, id)).Result;
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
        public List<Upload> GetAll(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            DataTable result = _Database.ExecuteQuery(UploadQueries.SelectAll(tenantId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<Upload> GetByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(UploadQueries.SelectByBucketId(bucketId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<Upload> GetByBucketId(string tenantId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Database.ExecuteQuery(UploadQueries.SelectByBucketId(tenantId, bucketId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public void Insert(Upload upload)
        {
            if (upload == null) throw new ArgumentNullException(nameof(upload));
            _Database.ExecuteQuery(UploadQueries.InsertQuery(upload), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(UploadQueries.DeleteById(id), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(UploadQueries.DeleteById(tenantId, id), true).Wait();
        }

        private Upload MapFromRow(DataRow row)
        {
            Upload upload = new Upload();
            upload.Id = row["id"] != null && row["id"] != DBNull.Value ? row["id"].ToString() : null;
            upload.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
            upload.BucketId = row["bucket_id"] != null && row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
            upload.OwnerId = row["owner_id"] != null && row["owner_id"] != DBNull.Value ? row["owner_id"].ToString() : null;
            upload.AuthorId = row["author_id"] != null && row["author_id"] != DBNull.Value ? row["author_id"].ToString() : null;
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
