namespace Less3.Classes
{
    using System.Collections.Generic;

    /// <summary>
    /// Maintenance settings metadata shared by the admin maintenance API.
    /// </summary>
    internal static class MaintenanceSettingsMetadata
    {
        /// <summary>
        /// Placeholder returned for secret settings.
        /// </summary>
        internal const string RedactedValue = "[redacted]";

        /// <summary>
        /// Settings paths applied to the running process when saved.
        /// </summary>
        internal static List<string> RuntimeEditableSettings()
        {
            return new List<string>
            {
                "HeaderApiKey",
                "AdminApiKey",
                "RegionString",
                "RequestHistoryRetentionDays",
                "CleanupIntervalMs",
                "Logging.LogHttpRequests",
                "Logging.LogExceptions",
                "Debug.Authentication",
                "Debug.S3Requests",
                "Debug.Exceptions"
            };
        }

        /// <summary>
        /// Settings paths that require a restart before they affect initialized components.
        /// </summary>
        internal static List<string> RestartRequiredSettings()
        {
            return new List<string>
            {
                "EnableConsole",
                "ValidateSignatures",
                "BaseDomain",
                "Database",
                "Webserver",
                "Storage",
                "Logging.SyslogServerIp",
                "Logging.SyslogServerPort",
                "Logging.MinimumLevel",
                "Logging.LogS3Requests",
                "Logging.LogSignatureValidation",
                "Logging.ConsoleLogging",
                "Logging.DiskLogging",
                "Logging.DiskDirectory"
            };
        }
    }
}
