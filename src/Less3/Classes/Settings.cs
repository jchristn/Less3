using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using SyslogLogging;
using DatabaseWrapper.Core;
using S3ServerLibrary;
using Watson.ORM.Core;
using Less3.Storage;

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
        /// Database settings.
        /// </summary>
        public DatabaseSettings Database { get; set; } = new DatabaseSettings("./less3.db");

        /// <summary>
        /// Web server settings.
        /// </summary>
        public SettingsServer Server { get; set; } = new SettingsServer();

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
        /// Web server settings.
        /// </summary>
        public class SettingsServer
        {
            /// <summary>
            /// Hostname on which to listen.
            /// </summary>
            public string DnsHostname { get; set; } = "localhost";

            /// <summary>
            /// TCP port on which to listen.
            /// </summary>
            public int ListenerPort { get; set; } = 8000;

            /// <summary>
            /// Enable or disable SSL.
            /// </summary>
            public bool Ssl { get; set; } = false;

            /// <summary>
            /// Base domain.  
            /// </summary>
            public string BaseDomain { get; set; } = null;

            /// <summary>
            /// Header to use for the admin API key.
            /// </summary>
            public string HeaderApiKey { get; set; } = "x-api-key";

            /// <summary>
            /// Admin API key.
            /// </summary>
            public string AdminApiKey { get; set; } = "less3admin";

            /// <summary>
            /// AWS region string to use for location requests.
            /// </summary>
            public string RegionString { get; set; } = "us-west-1";
        }

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

        internal void Validate()
        {
            if (Database == null) throw new ArgumentException("system.json parameter 'Database' must not be null.");
            if (Server == null) throw new ArgumentException("system.json parameter 'Server' must not be null.");
            if (Storage == null) throw new ArgumentException("system.json parameter 'Storage' must not be null.");
            if (Logging == null) throw new ArgumentException("system.json parameter 'Syslog' must not be null.");
            if (Debug == null) throw new ArgumentException("system.json parameter 'Debug' must not be null.");

            if (String.IsNullOrEmpty(Server.DnsHostname)) throw new ArgumentException("system.json parameter 'Server.DnsHostname' must not be null.");
            if (String.IsNullOrEmpty(Server.HeaderApiKey)) throw new ArgumentException("system.json parameter 'Server.HeaderApiKey' must not be null.");
            if (String.IsNullOrEmpty(Server.AdminApiKey)) throw new ArgumentException("system.json parameter 'Server.AdminApiKey' must not be null.");
            if (Server.ListenerPort < 0 || Server.ListenerPort > 65535) throw new ArgumentException("system.json parameter 'Server.ListenerPort' must be within the range 0-65535.");

            IPAddress tempIp;
            if (IPAddress.TryParse(Server.DnsHostname, out tempIp)) throw new ArgumentException("system.json parameter 'Server.DnsHostname' must be a hostname, not an IP address.");

            if (String.IsNullOrEmpty(Storage.DiskDirectory)) throw new ArgumentException("system.json parameter 'Storage.DiskDirectory' must not be null.");
            if (String.IsNullOrEmpty(Storage.TempDirectory)) throw new ArgumentException("system.json parameter 'Storage.TempDirectory' must not be null.");

            if (String.IsNullOrEmpty(Logging.SyslogServerIp)) throw new ArgumentException("system.json parameter 'Syslog.ServerIp' must not be null.");
            if (Logging.SyslogServerPort < 0 || Logging.SyslogServerPort > 65535) throw new ArgumentException("system.json parameter 'Syslog.ServerPort' must be within the range 0-65535."); 
        }

        #endregion
    }
}
