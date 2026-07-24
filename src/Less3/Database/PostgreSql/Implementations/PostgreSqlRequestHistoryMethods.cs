namespace Less3.Database.PostgreSql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.PostgreSql.Queries;

    internal class PostgreSqlRequestHistoryMethods : IRequestHistoryMethods
    {
        private PostgreSqlDatabaseDriver _Driver;

        internal PostgreSqlRequestHistoryMethods(PostgreSqlDatabaseDriver driver)
        {
            _Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public List<RequestHistory> GetAll()
        {
            DataTable result = _Driver.ExecuteQuery(RequestHistoryQueries.SelectAll()).Result;
            return MapRequestHistory(result);
        }

        public RequestHistory GetById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Driver.ExecuteQuery(RequestHistoryQueries.SelectById(id)).Result;
            List<RequestHistory> entries = MapRequestHistory(result);
            if (entries.Count > 0) return entries[0];
            return null;
        }

        public void Insert(RequestHistory entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            _Driver.ExecuteQuery(RequestHistoryQueries.InsertQuery(entry), true).Wait();
        }

        public void DeleteById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Driver.ExecuteQuery(RequestHistoryQueries.DeleteById(id), true).Wait();
        }

        public void DeleteOlderThan(DateTime cutoff)
        {
            _Driver.ExecuteQuery(RequestHistoryQueries.DeleteOlderThan(cutoff), true).Wait();
        }

        public List<RequestHistory> GetInRange(DateTime startUtc, DateTime endUtc)
        {
            DataTable result = _Driver.ExecuteQuery(RequestHistoryQueries.SelectInRange(startUtc, endUtc)).Result;
            return MapRequestHistory(result);
        }

        private List<RequestHistory> MapRequestHistory(DataTable dt)
        {
            List<RequestHistory> entries = new List<RequestHistory>();
            if (dt == null || dt.Rows.Count == 0) return entries;

            foreach (DataRow row in dt.Rows)
            {
                RequestHistory entry = new RequestHistory();
                entry.TenantId = row.Table.Columns.Contains("tenant_id") && row["tenant_id"] != DBNull.Value ? row["tenant_id"].ToString() : "default";
                entry.Id = row["id"] != DBNull.Value ? row["id"].ToString() : null;
                entry.HttpMethod = row["httpmethod"] != DBNull.Value ? row["httpmethod"].ToString() : null;
                entry.RequestUrl = row["requesturl"] != DBNull.Value ? row["requesturl"].ToString() : null;
                entry.SourceIp = row["sourceip"] != DBNull.Value ? row["sourceip"].ToString() : null;
                entry.StatusCode = Convert.ToInt32(row["statuscode"]);
                entry.Success = Convert.ToBoolean(row["success"]);
                entry.DurationMs = Convert.ToInt64(row["durationms"]);
                entry.RequestType = row["requesttype"] != DBNull.Value ? row["requesttype"].ToString() : null;
                entry.UserId = row["user_id"] != DBNull.Value ? row["user_id"].ToString() : null;
                entry.AccessKey = row["accesskey"] != DBNull.Value ? row["accesskey"].ToString() : null;
                entry.RequestContentType = row["requestcontenttype"] != DBNull.Value ? row["requestcontenttype"].ToString() : null;
                entry.RequestBodyLength = Convert.ToInt64(row["requestbodylength"]);
                entry.ResponseContentType = row["responsecontenttype"] != DBNull.Value ? row["responsecontenttype"].ToString() : null;
                entry.ResponseBodyLength = Convert.ToInt64(row["responsebodylength"]);
                entry.RequestBody = row["requestbody"] != DBNull.Value ? row["requestbody"].ToString() : null;
                entry.ResponseBody = row["responsebody"] != DBNull.Value ? row["responsebody"].ToString() : null;
                entry.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                entries.Add(entry);
            }

            return entries;
        }
    }
}
