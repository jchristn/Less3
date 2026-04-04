namespace Less3.Database.PostgreSql.Queries
{
    using System;
    using Less3.Classes;

    internal static class RequestHistoryQueries
    {
        internal static string InsertQuery(RequestHistory entry)
        {
            return "INSERT INTO requesthistory (guid, httpmethod, requesturl, sourceip, statuscode, success, durationms, requesttype, userguid, accesskey, requestcontenttype, requestbodylength, responsecontenttype, responsebodylength, requestbody, responsebody, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(entry.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(entry.HttpMethod) + "', "
                + "'" + Sanitizer.SanitizeString(entry.RequestUrl) + "', "
                + "'" + Sanitizer.SanitizeString(entry.SourceIp) + "', "
                + entry.StatusCode + ", "
                + (entry.Success ? "TRUE" : "FALSE") + ", "
                + entry.DurationMs + ", "
                + "'" + Sanitizer.SanitizeString(entry.RequestType) + "', "
                + "'" + Sanitizer.SanitizeString(entry.UserGUID) + "', "
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

        internal static string SelectByGuid(string guid)
        {
            return "SELECT * FROM requesthistory WHERE guid = '" + Sanitizer.SanitizeString(guid) + "' LIMIT 1;";
        }

        internal static string DeleteByGuid(string guid)
        {
            return "DELETE FROM requesthistory WHERE guid = '" + Sanitizer.SanitizeString(guid) + "';";
        }

        internal static string DeleteOlderThan(DateTime cutoff)
        {
            return "DELETE FROM requesthistory WHERE createdutc < '" + cutoff.ToString(Sanitizer.TimestampFormat) + "';";
        }

        internal static string SelectInRange(DateTime startUtc, DateTime endUtc)
        {
            return "SELECT * FROM requesthistory WHERE createdutc >= '" + startUtc.ToString(Sanitizer.TimestampFormat)
                + "' AND createdutc <= '" + endUtc.ToString(Sanitizer.TimestampFormat) + "';";
        }
    }
}
