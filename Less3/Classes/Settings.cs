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
        #region Internal-Members-and-Nested-Classes

        public string Version;
        public bool EnableConsole;
        public SettingsFiles Files;
        public SettingsServer Server;
        public SettingsStorage Storage;
        public SettingsLogging Logging;
        public SettingsDebug Debug;

        public class SettingsFiles
        {
            public string ConfigDatabase; 
        }

        public class SettingsServer
        {
            public string DnsHostname;
            public int ListenerPort;
            public bool Ssl;
            public string BaseDomain;
            public string HeaderApiKey;
            public string AdminApiKey;
            public string RegionString;
        }

        public class SettingsStorage
        {
            public string Directory;
            public string TempDirectory;
        }

        public class SettingsLogging
        {
            public string SyslogServerIp;
            public int SyslogServerPort;
            public string Header;
            public int MinimumLevel;
            public bool LogHttpRequests; 
            public bool ConsoleLogging;
            public bool DiskLogging;
            public string DiskDirectory;
        }

        public class SettingsDebug
        {
            public bool DatabaseQueries;
            public bool DatabaseResults;
            public bool Authentication;
            public bool S3Requests;
        }

        #endregion

        #region Constructors-and-Factories

        public Settings()
        {

        }

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

        #region Internal-Methods

        internal void Validate()
        {
            if (Files == null) throw new ArgumentException("System.json parameter 'Files' must not be null.");
            if (Server == null) throw new ArgumentException("System.json parameter 'Server' must not be null.");
            if (Storage == null) throw new ArgumentException("System.json parameter 'Storage' must not be null.");
            if (Logging == null) throw new ArgumentException("System.json parameter 'Syslog' must not be null.");
            if (Debug == null) throw new ArgumentException("System.json parameter 'Debug' must not be null.");

            if (String.IsNullOrEmpty(Files.ConfigDatabase)) throw new ArgumentException("System.json parameter 'Files.ConfigDatabase' must not be null."); 

            if (String.IsNullOrEmpty(Server.DnsHostname)) throw new ArgumentException("System.json parameter 'Server.DnsHostname' must not be null.");
            if (String.IsNullOrEmpty(Server.HeaderApiKey)) throw new ArgumentException("System.json parameter 'Server.HeaderApiKey' must not be null.");
            if (String.IsNullOrEmpty(Server.AdminApiKey)) throw new ArgumentException("System.json parameter 'Server.AdminApiKey' must not be null.");
            if (Server.ListenerPort < 0 || Server.ListenerPort > 65535) throw new ArgumentException("System.json parameter 'Server.ListenerPort' must be within the range 0-65535.");

            IPAddress tempIp;
            if (IPAddress.TryParse(Server.DnsHostname, out tempIp)) throw new ArgumentException("System.json parameter 'Server.DnsHostname' must be a hostname, not an IP address.");

            if (String.IsNullOrEmpty(Storage.Directory)) throw new ArgumentException("System.json parameter 'Storage.Directory' must not be null.");
            if (String.IsNullOrEmpty(Storage.TempDirectory)) throw new ArgumentException("System.json parameter 'Storage.TempDirectory' must not be null.");

            if (String.IsNullOrEmpty(Logging.SyslogServerIp)) throw new ArgumentException("System.json parameter 'Syslog.ServerIp' must not be null.");
            if (Logging.SyslogServerPort < 0 || Logging.SyslogServerPort > 65535) throw new ArgumentException("System.json parameter 'Syslog.ServerPort' must be within the range 0-65535."); 
        }

        #endregion
    }
}
