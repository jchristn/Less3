using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using SyslogLogging;

namespace Less3.Classes
{
    /// <summary>
    /// Less3 settings.
    /// </summary>
    public class Settings
    {
        #region Public-Members-and-Nested-Classes

        /// <summary>
        /// Enable or disable the console.
        /// </summary>
        public bool EnableConsole;

        /// <summary>
        /// Files settings.
        /// </summary>
        public SettingsFiles Files;

        /// <summary>
        /// Server settings.
        /// </summary>
        public SettingsServer Server;  

        /// <summary>
        /// Storage settings.
        /// </summary>
        public SettingsStorage Storage;  

        /// <summary>
        /// Logging settings.
        /// </summary>
        public SettingsSyslog Syslog;

        /// <summary>
        /// Debug settings.
        /// </summary>
        public SettingsDebug Debug;

        /// <summary>
        /// Files settings.
        /// </summary>
        public class SettingsFiles
        {
            /// <summary>
            /// File containing configuration database.
            /// </summary>
            public string ConfigDatabase; 
        }

        /// <summary>
        /// Server settings.
        /// </summary>
        public class SettingsServer
        {
            /// <summary>
            /// Hostname on which to listen for HTTP requests.  NOTE: this must NOT be an IP address.  Further, the incoming HTTP HOST header must match this value.
            /// </summary>
            public string DnsHostname;

            /// <summary>
            /// TCP port.
            /// </summary>
            public int ListenerPort;

            /// <summary>
            /// Enable or disable SSL.
            /// </summary>
            public bool Ssl;

            /// <summary>
            /// Header to use when setting admin API key.
            /// </summary>
            public string HeaderApiKey;
             
            /// <summary>
            /// Admin API key.
            /// </summary>
            public string AdminApiKey;
        }
         
        /// <summary>
        /// Storage settings.
        /// </summary>
        public class SettingsStorage
        {
            /// <summary>
            /// Base directory for buckets.
            /// </summary>
            public string Directory;
        }
          
        /// <summary>
        /// Syslog settings.
        /// </summary>
        public class SettingsSyslog
        {
            /// <summary>
            /// Syslog server IP address.
            /// </summary>
            public string ServerIp;

            /// <summary>
            /// Server port number.
            /// </summary>
            public int ServerPort;

            /// <summary>
            /// Header to prepend to each syslog message.
            /// </summary>
            public string Header;

            /// <summary>
            /// Minimum level required to send a syslog message.
            /// </summary>
            public int MinimumLevel;

            /// <summary>
            /// Enable logging of HTTP requests.
            /// </summary>
            public bool LogHttpRequests;

            /// <summary>
            /// Enable logging of HTTP responses.
            /// </summary>
            public bool LogHttpResponses;

            /// <summary>
            /// Enable or disable console logging.
            /// </summary>
            public bool ConsoleLogging;
        }
          
        /// <summary>
        /// Debug settings.
        /// </summary>
        public class SettingsDebug
        {
            /// <summary>
            /// Enable or disable database debugging.
            /// </summary>
            public bool Database;

            /// <summary>
            /// Enable or disable authentication.
            /// </summary>
            public bool Authentication;
        }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Settings()
        {

        }

        /// <summary>
        /// Load settings from a file.
        /// </summary>
        /// <param name="filename">Path and filename.</param>
        /// <returns>Settings.</returns>
        public static Settings FromFile(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            if (!Common.FileExists(filename)) throw new FileNotFoundException(nameof(filename));

            string contents = Common.ReadTextFile(@filename);
            if (String.IsNullOrEmpty(contents))
            {
                Common.ExitApplication("Settings", "Unable to read contents of " + filename, -1);
                return null;
            }

            Settings ret = null;

            try
            {
                ret = Common.DeserializeJson<Settings>(contents);
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

        #region Public-Methods

        /// <summary>
        /// Validate settings.
        /// </summary>
        public void Validate()
        {
            if (Files == null) throw new ArgumentException("System.json parameter 'Files' must not be null.");
            if (Server == null) throw new ArgumentException("System.json parameter 'Server' must not be null.");
            if (Storage == null) throw new ArgumentException("System.json parameter 'Storage' must not be null.");
            if (Syslog == null) throw new ArgumentException("System.json parameter 'Syslog' must not be null.");
            if (Debug == null) throw new ArgumentException("System.json parameter 'Debug' must not be null.");

            if (String.IsNullOrEmpty(Files.ConfigDatabase)) throw new ArgumentException("System.json parameter 'Files.ConfigDatabase' must not be null."); 

            if (String.IsNullOrEmpty(Server.DnsHostname)) throw new ArgumentException("System.json parameter 'Server.DnsHostname' must not be null.");
            if (String.IsNullOrEmpty(Server.HeaderApiKey)) throw new ArgumentException("System.json parameter 'Server.HeaderApiKey' must not be null.");
            if (String.IsNullOrEmpty(Server.AdminApiKey)) throw new ArgumentException("System.json parameter 'Server.AdminApiKey' must not be null.");
            if (Server.ListenerPort < 0 || Server.ListenerPort > 65535) throw new ArgumentException("System.json parameter 'Server.ListenerPort' must be within the range 0-65535.");

            IPAddress tempIp;
            if (IPAddress.TryParse(Server.DnsHostname, out tempIp)) throw new ArgumentException("System.json parameter 'Server.DnsHostname' must be a hostname, not an IP address.");

            if (String.IsNullOrEmpty(Storage.Directory)) throw new ArgumentException("System.json parameter 'Storage.Directory' must not be null.");

            if (String.IsNullOrEmpty(Syslog.ServerIp)) throw new ArgumentException("System.json parameter 'Syslog.ServerIp' must not be null.");
            if (Syslog.ServerPort < 0 || Syslog.ServerPort > 65535) throw new ArgumentException("System.json parameter 'Syslog.ServerPort' must be within the range 0-65535."); 
        }

        #endregion
    }
}
