namespace Less3.Database.MySql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Database.MySql.Queries;

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
        public RequestHistory GetById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable result = _Database.ExecuteQuery(RequestHistoryQueries.SelectById(id)).Result;
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
        public void DeleteById(string id)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _Database.ExecuteQuery(RequestHistoryQueries.DeleteById(id), true).Wait();
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
            entry.TenantId = row.Table.Columns.Contains("tenant_id") && row["tenant_id"] != DBNull.Value ? row["tenant_id"].ToString() : "default";
            entry.Id = row["id"] != null && row["id"] != DBNull.Value ? row["id"].ToString() : null;
            entry.HttpMethod = row["httpmethod"] != null && row["httpmethod"] != DBNull.Value ? row["httpmethod"].ToString() : null;
            entry.RequestUrl = row["requesturl"] != null && row["requesturl"] != DBNull.Value ? row["requesturl"].ToString() : null;
            entry.SourceIp = row["sourceip"] != null && row["sourceip"] != DBNull.Value ? row["sourceip"].ToString() : null;
            entry.StatusCode = Convert.ToInt32(row["statuscode"]);
            entry.Success = Convert.ToInt32(row["success"]) != 0;
            entry.DurationMs = Convert.ToInt64(row["durationms"]);
            entry.RequestType = row["requesttype"] != null && row["requesttype"] != DBNull.Value ? row["requesttype"].ToString() : null;
            entry.UserId = row["user_id"] != null && row["user_id"] != DBNull.Value ? row["user_id"].ToString() : null;
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
    }
}
