namespace Less3.Database.SqlServer.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.SqlServer.Queries;

    internal class RequestHistoryMethods : IRequestHistoryMethods
    {
        private DatabaseDriverBase _Database;

        internal RequestHistoryMethods(DatabaseDriverBase database)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <inheritdoc />
        public List<RequestHistory> GetAll()
        {
            DataTable result = _Database.ExecuteQuery(RequestHistoryQueries.SelectAll()).Result;
            return MapList(result);
        }

        /// <inheritdoc />
        public RequestHistory GetByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable result = _Database.ExecuteQuery(RequestHistoryQueries.SelectByGuid(guid)).Result;
            if (result != null && result.Rows.Count > 0)
                return MapFromRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public void Insert(RequestHistory entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            _Database.ExecuteQuery(RequestHistoryQueries.InsertQuery(entry), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Database.ExecuteQuery(RequestHistoryQueries.DeleteByGuid(guid), true).Wait();
        }

        /// <inheritdoc />
        public void DeleteOlderThan(DateTime cutoff)
        {
            _Database.ExecuteQuery(RequestHistoryQueries.DeleteOlderThan(cutoff), true).Wait();
        }

        /// <inheritdoc />
        public List<RequestHistory> GetInRange(DateTime startUtc, DateTime endUtc)
        {
            DataTable result = _Database.ExecuteQuery(RequestHistoryQueries.SelectInRange(startUtc, endUtc)).Result;
            return MapList(result);
        }

        private RequestHistory MapFromRow(DataRow row)
        {
            RequestHistory entry = new RequestHistory();
            entry.Id = Convert.ToInt32(row["id"]);
            entry.GUID = row["guid"] != null && row["guid"] != DBNull.Value ? row["guid"].ToString() : null;
            entry.HttpMethod = row["httpmethod"] != null && row["httpmethod"] != DBNull.Value ? row["httpmethod"].ToString() : null;
            entry.RequestUrl = row["requesturl"] != null && row["requesturl"] != DBNull.Value ? row["requesturl"].ToString() : null;
            entry.SourceIp = row["sourceip"] != null && row["sourceip"] != DBNull.Value ? row["sourceip"].ToString() : null;
            entry.StatusCode = Convert.ToInt32(row["statuscode"]);
            entry.Success = IsBitTrue(row["success"]);
            entry.DurationMs = Convert.ToInt64(row["durationms"]);
            entry.RequestType = row["requesttype"] != null && row["requesttype"] != DBNull.Value ? row["requesttype"].ToString() : null;
            entry.UserGUID = row["userguid"] != null && row["userguid"] != DBNull.Value ? row["userguid"].ToString() : null;
            entry.AccessKey = row["accesskey"] != null && row["accesskey"] != DBNull.Value ? row["accesskey"].ToString() : null;
            entry.RequestContentType = row["requestcontenttype"] != null && row["requestcontenttype"] != DBNull.Value ? row["requestcontenttype"].ToString() : null;
            entry.RequestBodyLength = Convert.ToInt64(row["requestbodylength"]);
            entry.ResponseContentType = row["responsecontenttype"] != null && row["responsecontenttype"] != DBNull.Value ? row["responsecontenttype"].ToString() : null;
            entry.ResponseBodyLength = Convert.ToInt64(row["responsebodylength"]);
            entry.RequestBody = row["requestbody"] != null && row["requestbody"] != DBNull.Value ? row["requestbody"].ToString() : null;
            entry.ResponseBody = row["responsebody"] != null && row["responsebody"] != DBNull.Value ? row["responsebody"].ToString() : null;
            entry.CreatedUtc = DateTime.Parse(row["createdutc"].ToString());
            return entry;
        }

        private List<RequestHistory> MapList(DataTable table)
        {
            List<RequestHistory> list = new List<RequestHistory>();
            if (table != null && table.Rows.Count > 0)
            {
                foreach (DataRow row in table.Rows)
                {
                    list.Add(MapFromRow(row));
                }
            }
            return list;
        }

        private bool IsBitTrue(object val)
        {
            if (val == null || val == DBNull.Value) return false;
            string s = val.ToString();
            return s == "1" || s.Equals("True", StringComparison.OrdinalIgnoreCase);
        }
    }
}
