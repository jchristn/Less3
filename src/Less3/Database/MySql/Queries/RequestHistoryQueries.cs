namespace Less3.Database.MySql.Queries
{
    using System;
    using Less3.Classes;

    internal static class RequestHistoryQueries
    {
        internal static string InsertQuery(RequestHistory entry)
        {
            return "INSERT INTO requesthistory (tenant_id, id, httpmethod, requesturl, sourceip, statuscode, success, durationms, requesttype, user_id, accesskey, requestcontenttype, requestbodylength, responsecontenttype, responsebodylength, requestbody, responsebody, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(entry.TenantId) + "', "
                + "'" + Sanitizer.SanitizeString(entry.Id) + "', "
                + "'" + Sanitizer.SanitizeString(entry.HttpMethod) + "', "
                + "'" + Sanitizer.SanitizeString(entry.RequestUrl) + "', "
                + "'" + Sanitizer.SanitizeString(entry.SourceIp) + "', "
                + entry.StatusCode + ", "
                + (entry.Success ? 1 : 0) + ", "
                + entry.DurationMs + ", "
                + "'" + Sanitizer.SanitizeString(entry.RequestType) + "', "
                + "'" + Sanitizer.SanitizeString(entry.UserId) + "', "
                + "'" + Sanitizer.SanitizeString(entry.AccessKey) + "', "
                + "'" + Sanitizer.SanitizeString(entry.RequestContentType) + "', "
                + entry.RequestBodyLength + ", "
                + "'" + Sanitizer.SanitizeString(entry.ResponseContentType) + "', "
                + entry.ResponseBodyLength + ", "
                + "'" + Sanitizer.SanitizeString(entry.RequestBody) + "', "
                + "'" + Sanitizer.SanitizeString(entry.ResponseBody) + "', "
                + "'" + entry.CreatedUtc.ToString(Sanitizer.TimestampFormat) + "'"
                + ");";
        }

        internal static string SelectAll()
        {
            return "SELECT * FROM requesthistory;";
        }

        internal static string SelectById(string id)
        {
            return "SELECT * FROM requesthistory WHERE id = '" + Sanitizer.SanitizeString(id) + "' LIMIT 1;";
        }

        internal static string DeleteById(string id)
        {
            return "DELETE FROM requesthistory WHERE id = '" + Sanitizer.SanitizeString(id) + "';";
        }

        internal static string DeleteOlderThan(DateTime cutoff)
        {
            return "DELETE FROM requesthistory WHERE createdutc < '" + cutoff.ToString(Sanitizer.TimestampFormat) + "';";
        }

        internal static string SelectInRange(DateTime start, DateTime end)
        {
            return "SELECT * FROM requesthistory WHERE createdutc >= '" + start.ToString(Sanitizer.TimestampFormat) + "' AND createdutc <= '" + end.ToString(Sanitizer.TimestampFormat) + "';";
        }
    }
}
