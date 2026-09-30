namespace Test.Shared.S3Compatibility
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;

    /// <summary>
    /// A captured raw HTTP response.
    /// </summary>
    public class RawResponse
    {
        #region Public-Members

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        /// <summary>
        /// Response and content headers, keyed case-insensitively.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Response body.
        /// </summary>
        public byte[] Body { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Response body decoded as UTF-8.
        /// </summary>
        public string Text
        {
            get { return Encoding.UTF8.GetString(Body); }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Value of a header, or null.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <returns>Value or null.</returns>
        public string? Header(string name)
        {
            return Headers.TryGetValue(name, out string? value) ? value : null;
        }

        #endregion
    }
}
