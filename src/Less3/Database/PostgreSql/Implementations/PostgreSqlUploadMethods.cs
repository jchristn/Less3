namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlUploadMethods : IUploadMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlUploadMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public Upload GetByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Driver.ExecuteQuery(UploadQueries.SelectByGuid(guid)).Result;
            List<Upload> uploads = MapUploads(result);
            if (uploads.Count > 0) return uploads[0];
            return null;
        }

        public List<Upload> GetAll()
        {
            DataTable result = _Driver.ExecuteQuery(UploadQueries.SelectAll()).Result;
            return MapUploads(result);
        }

        public List<Upload> GetByBucketGuid(string bucketGuid)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            DataTable result = _Driver.ExecuteQuery(UploadQueries.SelectByBucketGuid(bucketGuid)).Result;
            return MapUploads(result);
        }

        public void Insert(Upload upload)
        {
            if (upload == null) throw new ArgumentNullException(nameof(upload));
            _Driver.ExecuteQuery(UploadQueries.InsertQuery(upload), true).Wait();
        }

        public void DeleteByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Driver.ExecuteQuery(UploadQueries.DeleteByGuid(guid), true).Wait();
        }

        private List<Upload> MapUploads(DataTable dt)
        {
            List<Upload> uploads = new List<Upload>();
            if (dt == null || dt.Rows.Count == 0) return uploads;

            foreach (DataRow row in dt.Rows)
            {
                Upload upload = new Upload();
                upload.Id = Convert.ToInt32(row["id"]);
                upload.GUID = row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
                upload.BucketGUID = row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
                upload.OwnerGUID = row["ownerguid"] != DBNull.Value ? row["ownerguid"].ToString() : null;
                upload.AuthorGUID = row["authorguid"] != DBNull.Value ? row["authorguid"].ToString() : null;
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
