namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlUploadMethods : IUploadMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlUploadMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public Upload GetById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(UploadQueries.SelectById(id)).Result;
            List<Upload> uploads = MapUploads(result);
            if (uploads.Count > 0) return uploads[0];
            return null;
        }

        public Upload GetById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(UploadQueries.SelectById(tenantId, id)).Result;
            List<Upload> uploads = MapUploads(result);
            if (uploads.Count > 0) return uploads[0];
            return null;
        }

        public List<Upload> GetAll()
        {
            DataTable result = _Driver.ExecuteQuery(UploadQueries.SelectAll()).Result;
            return MapUploads(result);
        }

        public List<Upload> GetAll(string tenantId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            DataTable result = _Driver.ExecuteQuery(UploadQueries.SelectAll(tenantId)).Result;
            return MapUploads(result);
        }

        public List<Upload> GetByBucketId(string bucketId)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(UploadQueries.SelectByBucketId(bucketId)).Result;
            return MapUploads(result);
        }

        public List<Upload> GetByBucketId(string tenantId, string bucketId)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            DataTable result = _Driver.ExecuteQuery(UploadQueries.SelectByBucketId(tenantId, bucketId)).Result;
            return MapUploads(result);
        }

        public void Insert(Upload upload)
        {
            if (upload == null) throw new ArgumentNullException(nameof(upload));
            _Driver.ExecuteQuery(UploadQueries.InsertQuery(upload), true).Wait();
        }

        public void DeleteById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(UploadQueries.DeleteById(id), true).Wait();
        }

        public void DeleteById(string tenantId, string id)
        {
            if (String.IsNullOrEmpty(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(UploadQueries.DeleteById(tenantId, id), true).Wait();
        }

        private List<Upload> MapUploads(DataTable dt)
        {
            List<Upload> uploads = new List<Upload>();
            if (dt == null || dt.Rows.Count == 0) return uploads;

            foreach (DataRow row in dt.Rows)
            {
                Upload upload = new Upload();
                upload.Id = row["id"] != DBNull.Value ? row["id"].ToString() : null;
                upload.TenantId = ControlPlaneDataMapper.StringValue(row, "tenant_id") ?? "default";
                upload.BucketId = row["bucket_id"] != DBNull.Value ? row["bucket_id"].ToString() : null;
                upload.OwnerId = row["owner_id"] != DBNull.Value ? row["owner_id"].ToString() : null;
                upload.AuthorId = row["author_id"] != DBNull.Value ? row["author_id"].ToString() : null;
                upload.Key = row["key"] != DBNull.Value ? row["key"].ToString() : null;
                upload.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                upload.LastAccessUtc = Convert.ToDateTime(row["lastaccessutc"]).ToUniversalTime();
                upload.ExpirationUtc = Convert.ToDateTime(row["expirationutc"]).ToUniversalTime();
                upload.ContentType = row["contenttype"] != DBNull.Value ? row["contenttype"].ToString() : null;
                upload.Metadata = row["metadata"] != DBNull.Value ? row["metadata"].ToString() : null;
                uploads.Add(upload);
            }

            return uploads;
        }
    }
}
