namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request history entry tracking an API call made to Less3.
    /// </summary>
    public class RequestHistory
    {
        #region Public-Members

        /// <summary>
        /// Database identifier.
        /// </summary>
        [JsonIgnore]
        public int Id { get; set; } = 0;

        /// <summary>
        /// GUID of the request history entry.
        /// </summary>
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// HTTP method string (e.g. GET, PUT, DELETE).
        /// </summary>
        public string HttpMethod { get; set; } = null;

        /// <summary>
        /// Full request URL.
        /// </summary>
        public string RequestUrl { get; set; } = null;

        /// <summary>
        /// Client IP address.
        /// </summary>
        public string SourceIp { get; set; } = null;

        /// <summary>
        /// HTTP response status code.
        /// </summary>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// True if the response status code is less than 400, indicating a successful request.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Request processing duration in milliseconds.
        /// </summary>
        public long DurationMs { get; set; } = 0;

        /// <summary>
        /// S3 request type string (e.g. ListBuckets, GetObject, PutObject).
        /// </summary>
        public string RequestType { get; set; } = null;

        /// <summary>
        /// GUID of the authenticated user, or null if unauthenticated.
        /// </summary>
        public string UserGUID { get; set; } = null;

        /// <summary>
        /// Credential access key used for the request, or null if none provided.
        /// </summary>
        public string AccessKey { get; set; } = null;

        /// <summary>
        /// Content type of the request body.
        /// </summary>
        public string RequestContentType { get; set; } = null;

        /// <summary>
        /// Length of the request body in bytes.
        /// </summary>
        public long RequestBodyLength { get; set; } = 0;

        /// <summary>
        /// Content type of the response body.
        /// </summary>
        public string ResponseContentType { get; set; } = null;

        /// <summary>
        /// Length of the response body in bytes.
        /// </summary>
        public long ResponseBodyLength { get; set; } = 0;

        /// <summary>
        /// Creation timestamp in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.Now.ToUniversalTime();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistory()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
