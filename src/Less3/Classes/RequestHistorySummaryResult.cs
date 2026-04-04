namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Result containing a time-series summary of request history data over a specified date range and interval.
    /// </summary>
    public class RequestHistorySummaryResult
    {
        #region Public-Members

        /// <summary>
        /// Time-series data containing success and failure counts per time bucket.
        /// </summary>
        public List<RequestHistorySummaryBucket> Data { get; set; } = new List<RequestHistorySummaryBucket>();

        /// <summary>
        /// UTC timestamp for the start of the summary range.
        /// </summary>
        public DateTime StartUtc { get; set; }

        /// <summary>
        /// UTC timestamp for the end of the summary range.
        /// </summary>
        public DateTime EndUtc { get; set; }

        /// <summary>
        /// Time interval used for bucketing (e.g. "hour", "day", "minute").
        /// Default value is "hour".
        /// </summary>
        public string Interval { get; set; } = "hour";

        /// <summary>
        /// Total number of successful requests across all time buckets.
        /// </summary>
        public long TotalSuccess { get; set; } = 0;

        /// <summary>
        /// Total number of failed requests across all time buckets.
        /// </summary>
        public long TotalFailure { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySummaryResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
