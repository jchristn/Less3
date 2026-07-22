namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Compact health status exposed by the admin API.
    /// </summary>
    public class AdminHealthStatus
    {
        /// <summary>
        /// Server version.
        /// </summary>
        public string ServerVersion { get; set; } = null;

        /// <summary>
        /// Process uptime in seconds.
        /// </summary>
        public long UptimeSeconds { get; set; } = 0;

        /// <summary>
        /// Configured database type.
        /// </summary>
        public string DatabaseType { get; set; } = null;

        /// <summary>
        /// Indicates if the database can be reached.
        /// </summary>
        public bool DatabaseReachable { get; set; } = false;

        /// <summary>
        /// Configured storage path.
        /// </summary>
        public string StoragePath { get; set; } = null;

        /// <summary>
        /// Indicates if the storage path is writable.
        /// </summary>
        public bool StoragePathWritable { get; set; } = false;

        /// <summary>
        /// Free disk bytes for the storage path drive.
        /// </summary>
        public long FreeDiskBytes { get; set; } = 0;

        /// <summary>
        /// Configured temporary upload path.
        /// </summary>
        public string TempPath { get; set; } = null;

        /// <summary>
        /// Number of files currently in the temporary upload path.
        /// </summary>
        public int TempUploadCount { get; set; } = 0;

        /// <summary>
        /// Request history retention in days.
        /// </summary>
        public int RequestHistoryRetentionDays { get; set; } = 30;

        /// <summary>
        /// Last cleanup run timestamp in UTC, if available.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public DateTime? LastCleanupRunUtc { get; set; } = null;

        /// <summary>
        /// Generation timestamp in UTC.
        /// </summary>
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    }
}
