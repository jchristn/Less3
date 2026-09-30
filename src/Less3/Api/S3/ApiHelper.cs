namespace Less3.Api.S3
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using S3ServerLibrary;
    using S3ServerLibrary.S3Objects;
    using Less3.Classes;

    internal static class ApiHelper
    {
        internal static RequestMetadata GetRequestMetadata(S3Context ctx)
        {
            if (ctx == null) return null;
            if (ctx.Metadata == null) return null;
            return (RequestMetadata)(ctx.Metadata);
        }

        /// <summary>
        /// Verify the Content-MD5 header, when present, against the request body. S3Server has already read
        /// the body for XML requests, so it is taken from the request's cached bytes.
        /// </summary>
        /// <exception cref="S3Exception">InvalidDigest when the header is not a base64 MD5, BadDigest when it does not match.</exception>
        internal static void ValidateContentMd5(S3Context ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            string contentMd5 = ctx.Http.Request.Headers?["content-md5"];
            if (String.IsNullOrEmpty(contentMd5)) return;

            byte[] supplied = null;
            try { supplied = Convert.FromBase64String(contentMd5.Trim()); }
            catch (FormatException) { }

            if (supplied == null || supplied.Length != 16) throw new S3Exception(new Error(ErrorCode.InvalidDigest));

            byte[] body = ctx.Request.DataAsBytes ?? Array.Empty<byte>();
            if (!CryptographicOperations.FixedTimeEquals(supplied, MD5.HashData(body)))
                throw new S3Exception(new Error(ErrorCode.BadDigest));
        }

        internal static string AmazonTimestamp(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffz");
        }
    }
}
