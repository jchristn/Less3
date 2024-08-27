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

namespace Less3.Classes
{
    /// <summary>
    /// Less3 settings.
    /// </summary>
    public class Settings
    {
        #region Public-Members

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
        public string HeaderApiKey { get; set; } = "x-api-key";

        /// <summary>
        /// Admin API key.
        /// </summary>
        public string AdminApiKey { get; set; } = "less3admin";

        /// <summary>
        /// Region string.
        /// </summary>
        public string RegionString { get; set; } = "us-west-1";

        /// <summary>
        /// Database settings.
        /// </summary>
        public DatabaseSettings Database { get; set; } = new DatabaseSettings("./less3.db");

        /// <summary>
        /// Web server settings.
        /// </summary>
        public WebserverSettings Webserver { get; set; } = new WebserverSettings();

        /// <summary>
        /// Storage settings.
        /// </summary>
        public SettingsStorage Storage { get; set; } = new SettingsStorage();

        /// <summary>
        /// Logging settings.
        /// </summary>
        public SettingsLogging Logging { get; set; } = new SettingsLogging();

        /// <summary>
        /// Debugging settings.
        /// </summary>
        public SettingsDebug Debug { get; set; } = new SettingsDebug();

        #endregion

        #region Subordinate-Classes

        /// <summary>
        /// Storage settings.
        /// </summary>
        public class SettingsStorage
        {
            /// <summary>
            /// Temporary storage directory.
            /// </summary>
            public string TempDirectory { get; set; } = "./temp/";

            /// <summary>
            /// Type of storage driver.
            /// </summary>
            public StorageDriverType StorageType { get; set; } = StorageDriverType.Disk;

            /// <summary>
            /// Storage directory for 'Disk' StorageType.
            /// </summary>
            public string DiskDirectory { get; set; } = "./disk/";
        }

        /// <summary>
        /// Logging settings.
        /// </summary>
        public class SettingsLogging
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
        }

        /// <summary>
        /// Debug settings.
        /// </summary>
        public class SettingsDebug
        {
            /// <summary>
            /// Enable or disable debugging of authentication logic.
            /// </summary>
            public bool Authentication { get; set; } = false;

            /// <summary>
            /// Enable or disable debugging of S3 request parsing.
            /// </summary>
            public bool S3Requests { get; set; } = false;

            /// <summary>
            /// Enable or disable debugging of exceptions.
            /// </summary>
            public bool Exceptions { get; set; } = false;
        }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Settings()
        {

        }

        /// <summary>
        /// Instantiate the object from a file.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <returns>Settings.</returns>
        public static Settings FromFile(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            if (!File.Exists(filename)) throw new FileNotFoundException(nameof(filename));

            string contents = Common.ReadTextFile(@filename);
            if (String.IsNullOrEmpty(contents))
            {
                Common.ExitApplication("Settings", "Unable to read contents of " + filename, -1);
                return null;
            }

            Settings ret = null;

            try
            {
                ret = SerializationHelper.DeserializeJson<Settings>(contents);
                if (ret == null)
                {
                    Common.ExitApplication("Settings", "Unable to deserialize " + filename + " (null)", -1);
                    return null;
                }
            }
            catch (Exception)
            {
                Common.ExitApplication("Settings", "Unable to deserialize " + filename + " (exception)", -1);
                return null;
            }

            return ret;
        }

        #endregion

        #region Internal-Methods

        #endregion
    }
}
