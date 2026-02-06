namespace Less3.Settings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using SyslogLogging;
    using DatabaseWrapper.Core;
    using Less3.Storage;
    using S3ServerLibrary;
    using Watson.ORM.Core;
    using WatsonWebserver.Core;
    using Less3;

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
        /// Enable or disable use of the TCP server, with its own implementation, as opposed to the HTTP server.
        /// This should always be 'false' unless you encounter client incompatibility due to invalid HTTP headers.
        /// </summary>
        public bool UseTcpServer { get; set; } = false;

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

        private string _HeaderApiKey = "x-api-key";
        private string _AdminApiKey = "less3admin";
        private string _RegionString = "us-west-1";
        private DatabaseSettings _Database = new DatabaseSettings("./less3.db");
        private WebserverSettings _Webserver = new WebserverSettings();
        private StorageSettings _Storage = new StorageSettings();
        private LoggingSettings _Logging = new LoggingSettings();
        private DebugSettings _Debug = new DebugSettings();

        /// <summary>
        /// Instantiate.
        /// </summary>
        public SettingsBase()
        {

        }
    }
}
