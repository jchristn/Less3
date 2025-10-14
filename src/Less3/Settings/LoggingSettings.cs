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
    /// Logging settings.
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// IP address or hostname of the syslog server.
        /// </summary>
        public string SyslogServerIp { get; set; } = "127.0.0.1";

        /// <summary>
        /// Syslog server port number.
        /// </summary>
        public int SyslogServerPort { get; set; } = 514;

        /// <summary>
        /// Minimum log level severity.
        /// </summary>
        public Severity MinimumLevel { get; set; } = Severity.Info;

        /// <summary>
        /// Enable or disable logging of HTTP requests.
        /// </summary>
        public bool LogHttpRequests { get; set; } = false;

        /// <summary>
        /// Enable or disable logging of S3 requests.
        /// </summary>
        public bool LogS3Requests { get; set; } = false;

        /// <summary>
        /// Enable or disable logging of exceptions.
        /// </summary>
        public bool LogExceptions { get; set; } = false;

        /// <summary>
        /// Enable or disable logging of signature validation.
        /// </summary>
        public bool LogSignatureValidation { get; set; } = false;

        /// <summary>
        /// Enable or disable logging to the console.
        /// </summary>
        public bool ConsoleLogging { get; set; } = true;

        /// <summary>
        /// Enable or disable logging to disk.
        /// </summary>
        public bool DiskLogging { get; set; } = true;

        /// <summary>
        /// Directory on disk to write log files.
        /// </summary>
        public string DiskDirectory { get; set; } = "./logs/";

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings()
        {

        }
    }
}