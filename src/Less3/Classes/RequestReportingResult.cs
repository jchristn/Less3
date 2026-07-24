namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Lightweight request reporting summary for dashboard and admin diagnostics.
    /// </summary>
    public class RequestReportingResult
    {
        /// <summary>
        /// Tenant scope used for the report.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// UTC start timestamp used for the report.
        /// </summary>
        public DateTime StartUtc { get; set; } = DateTime.UtcNow.AddHours(-1);

        /// <summary>
        /// UTC end timestamp used for the report.
        /// </summary>
        public DateTime EndUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total request count in the report range.
        /// </summary>
        public long RequestCount { get; set; } = 0;

        /// <summary>
        /// Successful request count in the report range.
        /// </summary>
        public long SuccessCount { get; set; } = 0;

        /// <summary>
        /// Failed request count in the report range.
        /// </summary>
        public long FailureCount { get; set; } = 0;

        /// <summary>
        /// Average requests per minute in the report range.
        /// </summary>
        public double RequestsPerMinute { get; set; } = 0;

        /// <summary>
        /// Fraction of failed requests.
        /// </summary>
        public double FailureRate { get; set; } = 0;

        /// <summary>
        /// P50 latency in milliseconds.
        /// </summary>
        public double P50LatencyMs { get; set; } = 0;

        /// <summary>
        /// P95 latency in milliseconds.
        /// </summary>
        public double P95LatencyMs { get; set; } = 0;

        /// <summary>
        /// Buckets with the most stored bytes.
        /// </summary>
        public List<RequestReportingTopItem> TopBucketsByBytes { get; set; } = new List<RequestReportingTopItem>();

        /// <summary>
        /// Buckets with the most requests.
        /// </summary>
        public List<RequestReportingTopItem> TopBucketsByRequestCount { get; set; } = new List<RequestReportingTopItem>();

        /// <summary>
        /// Failed request types ordered by frequency.
        /// </summary>
        public List<RequestReportingTopItem> TopFailedRequestTypes { get; set; } = new List<RequestReportingTopItem>();

        /// <summary>
        /// Access keys ordered by request count.
        /// </summary>
        public List<RequestReportingTopItem> TopAccessKeys { get; set; } = new List<RequestReportingTopItem>();

        /// <summary>
        /// UTC timestamp when the report was generated.
        /// </summary>
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    }
}
