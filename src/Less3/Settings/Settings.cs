namespace Less3.Settings
{
    using System;
    using Less3.Database;
    using Less3.Storage;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Less3 settings.
    /// </summary>
    public class SettingsBase
    {
        /// <summary>
        /// Enable or disable the console.
        /// </summary>
        public bool EnableConsole { get; set; } = true;

        /// <summary>
        /// Enable or disable signature validation.
        /// </summary>
        public bool ValidateSignatures { get; set; } = true;

        /// <summary>
        /// Base domain, if using virtual hosted-style URLs, e.g. "localhost".
        /// </summary>
        public string BaseDomain { get; set; } = null;

        /// <summary>
        /// API key header for admin API requests.
        /// </summary>
        public string HeaderApiKey
        {
            get => _HeaderApiKey;
            set => _HeaderApiKey = (!String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(HeaderApiKey)));
        }

        /// <summary>
        /// Admin API key.
        /// </summary>
        public string AdminApiKey
        {
            get => _AdminApiKey;
            set => _AdminApiKey = (!String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(AdminApiKey)));
        }

        /// <summary>
        /// Region string.
        /// </summary>
        public string RegionString
        {
            get => _RegionString;
            set => _RegionString = (!String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(RegionString)));
        }

        /// <summary>
        /// Number of days to keep request history before maintenance purges it.
        /// </summary>
        public int RequestHistoryRetentionDays
        {
            get => _RequestHistoryRetentionDays;
            set => _RequestHistoryRetentionDays = value < 1 ? throw new ArgumentOutOfRangeException(nameof(RequestHistoryRetentionDays)) : value;
        }

        /// <summary>
        /// Cleanup timer interval in milliseconds.
        /// </summary>
        public int CleanupIntervalMs
        {
            get => _CleanupIntervalMs;
            set => _CleanupIntervalMs = value < 60000 ? throw new ArgumentOutOfRangeException(nameof(CleanupIntervalMs)) : value;
        }

        /// <summary>
        /// Database settings.
        /// </summary>
        public DatabaseSettings Database
        {
            get => _Database;
            set => _Database = (value != null ? value : throw new ArgumentNullException(nameof(Database)));
        }

        /// <summary>
        /// Web server settings.
        /// </summary>
        public WebserverSettings Webserver
        {
            get => _Webserver;
            set => _Webserver = (value != null ? value : throw new ArgumentNullException(nameof(Webserver)));
        }

        /// <summary>
        /// Storage settings.
        /// </summary>
        public StorageSettings Storage
        {
            get => _Storage;
            set => _Storage = (value != null ? value : throw new ArgumentNullException(nameof(Storage)));
        }

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings Logging
        {
            get => _Logging;
            set => _Logging = (value != null ? value : throw new ArgumentNullException(nameof(Logging)));
        }

        /// <summary>
        /// Debugging settings.
        /// </summary>
        public DebugSettings Debug
        {
            get => _Debug;
            set => _Debug = (value != null ? value : throw new ArgumentNullException(nameof(Debug)));
        }

        /// <summary>
        /// Multi-node cluster settings. When disabled (the default), Less3 runs as a standalone
        /// single node.
        /// </summary>
        public ClusterSettings Cluster
        {
            get => _Cluster;
            set => _Cluster = (value != null ? value : throw new ArgumentNullException(nameof(Cluster)));
        }

        /// <summary>
        /// Observability settings (metrics, traces, logs).
        /// </summary>
        public ObservabilitySettings Observability
        {
            get => _Observability;
            set => _Observability = (value != null ? value : throw new ArgumentNullException(nameof(Observability)));
        }

        private string _HeaderApiKey = "x-api-key";
        private string _AdminApiKey = "less3admin";
        private string _RegionString = "us-west-1";
        private int _RequestHistoryRetentionDays = 30;
        private int _CleanupIntervalMs = 3600000;
        private DatabaseSettings _Database = new DatabaseSettings("./less3.db");
        private WebserverSettings _Webserver = new WebserverSettings();
        private StorageSettings _Storage = new StorageSettings();
        private LoggingSettings _Logging = new LoggingSettings();
        private DebugSettings _Debug = new DebugSettings();
        private ClusterSettings _Cluster = new ClusterSettings();
        private ObservabilitySettings _Observability = new ObservabilitySettings();

        /// <summary>
        /// Instantiate.
        /// </summary>
        public SettingsBase()
        {

        }
    }
}
