namespace Less3.Database.MySql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
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
        public List<UploadPart> GetByUploadId(string uploadId)
        {
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            DataTable result = _Database.ExecuteQuery(UploadPartQueries.SelectByUploadId(uploadId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public List<UploadPart> GetByUploadId(string tenantId, string uploadId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            DataTable result = _Database.ExecuteQuery(UploadPartQueries.SelectByUploadId(tenantId, uploadId)).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public void DeleteByUploadId(string uploadId)
        {
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            _Database.ExecuteQuery(UploadPartQueries.DeleteByUploadId(uploadId), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByUploadId(string tenantId, string uploadId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            _Database.ExecuteQuery(UploadPartQueries.DeleteByUploadId(tenantId, uploadId), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByUploadIdAndPartNumber(string uploadId, int partNumber)
        {
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            if (partNumber < 1) throw new ArgumentOutOfRangeException(nameof(partNumber));
            _Database.ExecuteQuery(UploadPartQueries.DeleteByUploadIdAndPartNumber(uploadId, partNumber), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByUploadIdAndPartNumber(string tenantId, string uploadId, int partNumber)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            if (partNumber < 1) throw new ArgumentOutOfRangeException(nameof(partNumber));
            _Database.ExecuteQuery(UploadPartQueries.DeleteByUploadIdAndPartNumber(tenantId, uploadId, partNumber), true).Wait();
        }

        private UploadPart MapFromRow(DataRow row)
        {
            UploadPart part = new UploadPart();
            part.Id = row["id"] != null && row["id"] != DBNull.Value ? row["id"].ToString() : null;
            part.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
            part.BucketId = row["bucket_id"] != null && row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
            part.OwnerId = row["owner_id"] != null && row["owner_id"] != DBNull.Value ? row["owner_id"].ToString() : null;
            part.UploadId = row["upload_id"] != null && row["upload_id"] != DBNull.Value ? row["upload_id"].ToString() : null;
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
