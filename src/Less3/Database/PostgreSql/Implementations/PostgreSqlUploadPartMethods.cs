namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlUploadPartMethods : IUploadPartMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlUploadPartMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public void Insert(UploadPart part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            _Driver.ExecuteQuery(UploadPartQueries.InsertQuery(part), true).Wait();
        }

        public List<UploadPart> GetByUploadId(string uploadId)
        {
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            DataTable result = _Driver.ExecuteQuery(UploadPartQueries.SelectByUploadId(uploadId)).Result;
            return MapUploadParts(result);
        }

        public List<UploadPart> GetByUploadId(string tenantId, string uploadId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            DataTable result = _Driver.ExecuteQuery(UploadPartQueries.SelectByUploadId(tenantId, uploadId)).Result;
            return MapUploadParts(result);
        }

        public void DeleteByUploadId(string uploadId)
        {
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            _Driver.ExecuteQuery(UploadPartQueries.DeleteByUploadId(uploadId), true).Wait();
        }

        public void DeleteByUploadId(string tenantId, string uploadId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            _Driver.ExecuteQuery(UploadPartQueries.DeleteByUploadId(tenantId, uploadId), true).Wait();
        }

        public void DeleteByUploadIdAndPartNumber(string uploadId, int partNumber)
        {
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            if (partNumber < 1) throw new ArgumentOutOfRangeException(nameof(partNumber));
            _Driver.ExecuteQuery(UploadPartQueries.DeleteByUploadIdAndPartNumber(uploadId, partNumber), true).Wait();
        }

        public void DeleteByUploadIdAndPartNumber(string tenantId, string uploadId, int partNumber)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            if (partNumber < 1) throw new ArgumentOutOfRangeException(nameof(partNumber));
            _Driver.ExecuteQuery(UploadPartQueries.DeleteByUploadIdAndPartNumber(tenantId, uploadId, partNumber), true).Wait();
        }

        private List<UploadPart> MapUploadParts(DataTable dt)
        {
            List<UploadPart> parts = new List<UploadPart>();
            if (dt == null || dt.Rows.Count == 0) return parts;

            foreach (DataRow row in dt.Rows)
            {
                UploadPart part = new UploadPart();
                part.Id = row["id"] != DBNull.Value ? row["id"].ToString() : null;
                part.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
                part.BucketId = row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
                part.OwnerId = row["owner_id"] != DBNull.Value ? row["owner_id"].ToString() : null;
                part.UploadId = row["upload_id"] != DBNull.Value ? row["upload_id"].ToString() : null;
                part.PartNumber = Convert.ToInt32(row["partnumber"]);
                part.PartLength = Convert.ToInt32(row["partlength"]);
                part.MD5Hash = row["md5hash"] != DBNull.Value ? row["md5hash"].ToString() : null;
                part.Sha1Hash = row["sha1hash"] != DBNull.Value ? row["sha1hash"].ToString() : null;
                part.Sha256Hash = row["sha256hash"] != DBNull.Value ? row["sha256hash"].ToString() : null;
                part.LastAccessUtc = Convert.ToDateTime(row["lastaccessutc"]).ToUniversalTime();
                part.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                parts.Add(part);
            }

            return parts;
        }
    }
}
