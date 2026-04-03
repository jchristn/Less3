namespace Less3.Database.MySql.Queries
{
    using System;
    using Less3.Classes;

    internal static class RequestHistoryQueries
    {
        internal static string InsertQuery(RequestHistory entry)
        {
            return "INSERT INTO requesthistory (guid, httpmethod, requesturl, sourceip, statuscode, success, durationms, requesttype, userguid, accesskey, requestcontenttype, requestbodylength, responsecontenttype, responsebodylength, createdutc) VALUES ("
                + "'" + Sanitizer.SanitizeString(entry.GUID) + "', "
                + "'" + Sanitizer.SanitizeString(entry.HttpMethod) + "', "
                + "'" + Sanitizer.SanitizeString(entry.RequestUrl) + "', "
                + "'" + Sanitizer.SanitizeString(entry.SourceIp) + "', "
                + entry.StatusCode + ", "
                + (entry.Success ? 1 : 0) + ", "
                + entry.DurationMs + ", "
                + "'" + Sanitizer.SanitizeString(entry.RequestType) + "', "
                + "'" + Sanitizer.SanitizeString(entry.UserGUID) + "', "
                + "'" + Sanitizer.SanitizeString(entry.AccessKey) + "', "
                + "'" + Sanitizer.SanitizeString(entry.RequestContentType) + "', "
                + entry.RequestBodyLength + ", "
                + "'" + Sanitizer.SanitizeString(entry.ResponseContentType) + "', "
                + entry.ResponseBodyLength + ", "
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

        internal static string SelectInRange(DateTime start, DateTime end)
        {
            return "SELECT * FROM requesthistory WHERE createdutc >= '" + start.ToString(Sanitizer.TimestampFormat) + "' AND createdutc <= '" + end.ToString(Sanitizer.TimestampFormat) + "';";
        }
    }
}
