namespace Less3.Classes
{
    using System;
    using System.IO;
    using Less3.Database;
    using Less3.Settings;

    /// <summary>
    /// Creates the default runtime configuration used for container bootstrap.
    /// </summary>
    internal static class ContainerBootstrapSettingsFactory
    {
        internal static SettingsBase CreateDefaults()
        {
            SettingsBase settings = new SettingsBase();
            settings.EnableConsole = false;
            settings.ValidateSignatures = false;
            settings.Database = new DatabaseSettings("./db/less3.db");
            settings.Webserver.Hostname = "*";
            settings.Webserver.Port = ReadPortFromEnvironment();
            settings.Storage.TempDirectory = "./temp/";
            settings.Storage.DiskDirectory = "./disk/";
            settings.Logging.ConsoleLogging = true;
            settings.Logging.DiskLogging = true;
            settings.Logging.DiskDirectory = "./logs/";
            return settings;
        }

        internal static void EnsureDirectories(SettingsBase settings)
        {
            string databaseDirectory = Path.GetDirectoryName(settings.Database.Filename);
            if (!string.IsNullOrEmpty(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }

            Directory.CreateDirectory(settings.Storage.DiskDirectory);
            Directory.CreateDirectory(settings.Storage.TempDirectory);
            Directory.CreateDirectory(settings.Logging.DiskDirectory);
        }

        private static int ReadPortFromEnvironment()
        {
            string portValue = Environment.GetEnvironmentVariable("LESS3_PORT");
            if (Int32.TryParse(portValue, out int port) && port > 0 && port <= 65535)
            {
                return port;
            }

            return 8000;
        }
    }
}
