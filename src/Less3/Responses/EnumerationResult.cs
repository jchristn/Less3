namespace Less3.Responses
{
    using System.Collections.Generic;

    /// <summary>
    /// Standard response shape for server-side enumeration results.
    /// </summary>
    /// <typeparam name="T">Type of item being enumerated.</typeparam>
    public class EnumerationResult<T>
    {
        #region Public-Members

        /// <summary>
        /// Records returned for the current page.
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Total matching record count when available.
        /// </summary>
        public long? Total { get; set; } = null;

        /// <summary>
        /// Number of records requested.
        /// </summary>
        public int Limit { get; set; } = 100;

        /// <summary>
        /// Zero-based offset used for this page.
        /// </summary>
        public int Offset { get; set; } = 0;

        /// <summary>
        /// Continuation token to use for the next page.
        /// </summary>
        public string NextContinuationToken { get; set; } = null;

        /// <summary>
        /// Whether another page is available.
        /// </summary>
        public bool HasMore { get; set; } = false;

        #endregion
    }
}
