namespace Less3.Requests
{
    using System;

    /// <summary>
    /// Runtime maintenance settings update request.
    /// </summary>
    public class MaintenanceSettingsUpdateRequest
    {
        /// <summary>
        /// Request history retention in days.
        /// </summary>
        public int? RequestHistoryRetentionDays { get; set; } = null;

        /// <summary>
        /// Cleanup interval in milliseconds.
        /// </summary>
        public int? CleanupIntervalMs { get; set; } = null;

        /// <summary>
        /// Optional explicit cutoff for purge operations.
        /// </summary>
        public DateTime? OlderThanUtc { get; set; } = null;
    }
}
