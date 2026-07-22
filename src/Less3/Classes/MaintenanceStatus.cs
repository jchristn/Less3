namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Operational maintenance status and editable runtime settings.
    /// </summary>
    public class MaintenanceStatus
    {
        /// <summary>
        /// Current request history retention in days.
        /// </summary>
        public int RequestHistoryRetentionDays { get; set; } = 30;

        /// <summary>
        /// Current cleanup interval in milliseconds.
        /// </summary>
        public int CleanupIntervalMs { get; set; } = 3600000;

        /// <summary>
        /// Last cleanup run timestamp.
        /// </summary>
        public DateTime? LastCleanupRunUtc { get; set; } = null;

        /// <summary>
        /// Settings that can be changed at runtime.
        /// </summary>
        public List<string> RuntimeEditableSettings { get; set; } = new List<string>();

        /// <summary>
        /// Settings that require restart before taking effect.
        /// </summary>
        public List<string> RestartRequiredSettings { get; set; } = new List<string>();

        /// <summary>
        /// Redacted configuration summary.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// UTC timestamp when the status was generated.
        /// </summary>
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    }
}
