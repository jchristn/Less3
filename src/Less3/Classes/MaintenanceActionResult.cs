namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Result returned from maintenance actions.
    /// </summary>
    public class MaintenanceActionResult
    {
        /// <summary>
        /// Whether the maintenance action completed.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Human-readable action name.
        /// </summary>
        public string Action { get; set; } = null;

        /// <summary>
        /// Number of request history entries purged.
        /// </summary>
        public int PurgedRequestHistoryCount { get; set; } = 0;

        /// <summary>
        /// Number of expired multipart uploads cleaned.
        /// </summary>
        public int ExpiredUploadCount { get; set; } = 0;

        /// <summary>
        /// Number of temporary files deleted.
        /// </summary>
        public int DeletedTempFileCount { get; set; } = 0;

        /// <summary>
        /// Number of object metadata rows inspected.
        /// </summary>
        public int ObjectRowCount { get; set; } = 0;

        /// <summary>
        /// Number of object blob files missing on disk.
        /// </summary>
        public int MissingBlobFileCount { get; set; } = 0;

        /// <summary>
        /// Missing blob file identifiers.
        /// </summary>
        public List<string> MissingBlobFiles { get; set; } = new List<string>();

        /// <summary>
        /// UTC cutoff used by purge operations.
        /// </summary>
        public DateTime? CutoffUtc { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the action completed.
        /// </summary>
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    }
}
