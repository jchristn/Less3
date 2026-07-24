namespace Less3.Requests
{
    using System;
    using Less3.Settings;

    /// <summary>
    /// Maintenance settings update request.
    /// </summary>
    public class MaintenanceSettingsUpdateRequest
    {
        /// <summary>
        /// Full server configuration to persist to system.json.
        /// </summary>
        public SettingsBase Configuration { get; set; } = null;

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
