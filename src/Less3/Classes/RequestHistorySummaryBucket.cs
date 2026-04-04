namespace Less3.Classes
{
    using System;

    /// <summary>
    /// A single time bucket within a request history summary, containing success and failure counts for that interval.
    /// </summary>
    public class RequestHistorySummaryBucket
    {
        #region Public-Members

        /// <summary>
        /// UTC timestamp representing the start of this time bucket.
        /// </summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        /// Number of successful requests (status code less than 400) in this time bucket.
        /// </summary>
        public long SuccessCount { get; set; } = 0;

        /// <summary>
        /// Number of failed requests (status code 400 or greater) in this time bucket.
        /// </summary>
        public long FailureCount { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistorySummaryBucket()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
