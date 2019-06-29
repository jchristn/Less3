using System;
using System.Collections.Generic;
using System.IO;
using SyslogLogging;

namespace Less3.Classes
{
    public class Settings
    {
        #region Public-Members-and-Nested-Classes

        public string ProductName;
        public bool EnableConsole;

        public SettingsFiles Files;
        public SettingsServer Server;  
        public SettingsStorage Storage;  
        public SettingsSyslog Syslog; 

        public class SettingsFiles
        {
            public string Users;
            public string Credentials;
            public string Buckets;
        }

        public class SettingsServer
        {
            public string DnsHostname;
            public int ListenerPort;
            public bool Ssl;

            public string HeaderApiKey;
            public string HeaderEmail;
            public string HeaderPassword;
            public string HeaderToken;
            public string HeaderVersion;

            public string AdminApiKey;
            public int TokenExpirationSec;
            public int FailedRequestsIntervalSec;
            public long MaxObjectSize;
            public int MaxTransferSize;
        }
         
        public class SettingsStorage
        {
            public string TempFiles;
            public string Directory;
        }
          
        public class SettingsSyslog
        {
            public string ServerIp;
            public int ServerPort;
            public string Header;
            public int MinimumLevel;
            public bool LogHttpRequests;
            public bool LogHttpResponses;
            public bool ConsoleLogging;
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
    }
}
