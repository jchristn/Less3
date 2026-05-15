namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Aggregate dashboard statistics exposed by the admin API.
    /// </summary>
    public class DashboardStatistics
    {
        /// <summary>
        /// Number of buckets in the system.
        /// </summary>
        public int BucketCount { get; set; } = 0;

        /// <summary>
        /// Number of stored objects across all buckets.
        /// </summary>
        public long TotalObjectCount { get; set; } = 0;

        /// <summary>
        /// Total number of stored bytes across all buckets.
        /// </summary>
        public long TotalBytes { get; set; } = 0;

        /// <summary>
        /// Per-bucket statistics used to build the aggregate totals.
        /// </summary>
        public List<BucketStatistics> Buckets { get; set; } = new List<BucketStatistics>();

        /// <summary>
        /// Generation timestamp in UTC.
        /// </summary>
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    }
}
