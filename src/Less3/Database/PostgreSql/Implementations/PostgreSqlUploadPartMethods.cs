namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
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

        public List<UploadPart> GetByUploadGuid(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) throw new ArgumentNullException(nameof(uploadGuid));
            DataTable result = _Driver.ExecuteQuery(UploadPartQueries.SelectByUploadGuid(uploadGuid)).Result;
            return MapUploadParts(result);
        }

        public void DeleteByUploadGuid(string uploadGuid)
        {
            if (String.IsNullOrEmpty(uploadGuid)) throw new ArgumentNullException(nameof(uploadGuid));
            _Driver.ExecuteQuery(UploadPartQueries.DeleteByUploadGuid(uploadGuid), true).Wait();
        }

        private List<UploadPart> MapUploadParts(DataTable dt)
        {
            List<UploadPart> parts = new List<UploadPart>();
            if (dt == null || dt.Rows.Count == 0) return parts;

            foreach (DataRow row in dt.Rows)
            {
                UploadPart part = new UploadPart();
                part.Id = Convert.ToInt32(row["id"]);
                part.GUID = row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
                part.BucketGUID = row["bucketguid"] != DBNull.Value ? row["bucketguid"].ToString() : null;
                part.OwnerGUID = row["ownerguid"] != DBNull.Value ? row["ownerguid"].ToString() : null;
                part.UploadGUID = row["uploadguid"] != DBNull.Value ? row["uploadguid"].ToString() : null;
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
