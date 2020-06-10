using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using S3ServerInterface;
using Less3.Classes;

namespace Less3.Api.S3
{
    internal static class ApiHelper
    {
        internal static RequestMetadata GetRequestMetadata(S3Request req)
        {
            if (req == null) return null;
            if (req.UserMetadata == null) return null;
            if (!req.UserMetadata.ContainsKey("RequestMetadata")) return null;
            return (RequestMetadata)(req.UserMetadata["RequestMetadata"]);
        } 

        internal static async Task SendSerializedResponse<T>(S3Request req, S3Response resp, T obj)
        {
            resp.StatusCode = 200;
            resp.ContentType = "application/xml";
            await resp.Send(Common.SerializeXml<T>(obj, false));
        }
         
        internal static string AmazonTimestamp(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffz");
        } 
    }
}
