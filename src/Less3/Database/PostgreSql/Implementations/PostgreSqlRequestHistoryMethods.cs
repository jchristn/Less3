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

        public RequestHistory GetByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Driver.ExecuteQuery(RequestHistoryQueries.SelectByGuid(guid)).Result;
            List<RequestHistory> entries = MapRequestHistory(result);
            if (entries.Count > 0) return entries[0];
            return null;
        }

        public void Insert(RequestHistory entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            _Driver.ExecuteQuery(RequestHistoryQueries.InsertQuery(entry), true).Wait();
        }

        public void DeleteByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Driver.ExecuteQuery(RequestHistoryQueries.DeleteByGuid(guid), true).Wait();
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
                entry.Id = Convert.ToInt32(row["id"]);
                entry.GUID = row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
                entry.HttpMethod = row["httpmethod"] != DBNull.Value ? row["httpmethod"].ToString() : null;
                entry.RequestUrl = row["requesturl"] != DBNull.Value ? row["requesturl"].ToString() : null;
                entry.SourceIp = row["sourceip"] != DBNull.Value ? row["sourceip"].ToString() : null;
                entry.StatusCode = Convert.ToInt32(row["statuscode"]);
                entry.Success = Convert.ToBoolean(row["success"]);
                entry.DurationMs = Convert.ToInt64(row["durationms"]);
                entry.RequestType = row["requesttype"] != DBNull.Value ? row["requesttype"].ToString() : null;
                entry.UserGUID = row["userguid"] != DBNull.Value ? row["userguid"].ToString() : null;
                entry.AccessKey = row["accesskey"] != DBNull.Value ? row["accesskey"].ToString() : null;
                entry.RequestContentType = row["requestcontenttype"] != DBNull.Value ? row["requestcontenttype"].ToString() : null;
                entry.RequestBodyLength = Convert.ToInt64(row["requestbodylength"]);
                entry.ResponseContentType = row["responsecontenttype"] != DBNull.Value ? row["responsecontenttype"].ToString() : null;
                entry.ResponseBodyLength = Convert.ToInt64(row["responsebodylength"]);
                entry.CreatedUtc = Convert.ToDateTime(row["createdutc"]).ToUniversalTime();
                entries.Add(entry);
            }

            return entries;
        }
    }
}
