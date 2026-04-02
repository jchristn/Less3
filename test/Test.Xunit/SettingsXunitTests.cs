namespace Test.Xunit
{
    using System;
    using global::Xunit;
    using Less3.Settings;
    using Less3.Storage;

    /// <summary>
    /// Xunit tests for Less3 settings classes.
    /// </summary>
    public class SettingsXunitTests
    {
        #region SettingsBase

        [Fact]
        public void SettingsBase_DefaultValues_AreCorrect()
        {
            SettingsBase settings = new SettingsBase();
            Assert.True(settings.EnableConsole);
            Assert.True(settings.ValidateSignatures);
            Assert.Null(settings.BaseDomain);
            Assert.Equal("x-api-key", settings.HeaderApiKey);
            Assert.Equal("less3admin", settings.AdminApiKey);
            Assert.Equal("us-west-1", settings.RegionString);
            Assert.NotNull(settings.Database);
            Assert.NotNull(settings.Webserver);
            Assert.NotNull(settings.Storage);
            Assert.NotNull(settings.Logging);
            Assert.NotNull(settings.Debug);
        }

        [Fact]
        public void SettingsBase_HeaderApiKey_NullThrows()
        {
            SettingsBase settings = new SettingsBase();
            Assert.Throws<ArgumentNullException>(() => settings.HeaderApiKey = null);
            Assert.Throws<ArgumentNullException>(() => settings.HeaderApiKey = "");
        }

        [Fact]
        public void SettingsBase_AdminApiKey_NullThrows()
        {
            SettingsBase settings = new SettingsBase();
            Assert.Throws<ArgumentNullException>(() => settings.AdminApiKey = null);
        }

        [Fact]
        public void SettingsBase_RegionString_NullThrows()
        {
            SettingsBase settings = new SettingsBase();
            Assert.Throws<ArgumentNullException>(() => settings.RegionString = null);
        }

        [Fact]
        public void SettingsBase_Webserver_NullThrows()
        {
            SettingsBase settings = new SettingsBase();
            Assert.Throws<ArgumentNullException>(() => settings.Webserver = null);
        }

        [Fact]
        public void SettingsBase_Database_NullThrows()
        {
            SettingsBase settings = new SettingsBase();
            Assert.Throws<ArgumentNullException>(() => settings.Database = null);
        }

        [Fact]
        public void SettingsBase_Storage_NullThrows()
        {
            SettingsBase settings = new SettingsBase();
            Assert.Throws<ArgumentNullException>(() => settings.Storage = null);
        }

        [Fact]
        public void SettingsBase_Logging_NullThrows()
        {
            SettingsBase settings = new SettingsBase();
            Assert.Throws<ArgumentNullException>(() => settings.Logging = null);
        }

        [Fact]
        public void SettingsBase_Debug_NullThrows()
        {
            SettingsBase settings = new SettingsBase();
            Assert.Throws<ArgumentNullException>(() => settings.Debug = null);
        }

        [Fact]
        public void SettingsBase_PropertyAssignment_Works()
        {
            SettingsBase settings = new SettingsBase();
            settings.EnableConsole = false;
            settings.ValidateSignatures = false;
            settings.BaseDomain = ".example.com";
            settings.HeaderApiKey = "custom-key";
            settings.AdminApiKey = "myadminkey";
            settings.RegionString = "ap-southeast-1";

            Assert.False(settings.EnableConsole);
            Assert.False(settings.ValidateSignatures);
            Assert.Equal(".example.com", settings.BaseDomain);
            Assert.Equal("custom-key", settings.HeaderApiKey);
            Assert.Equal("myadminkey", settings.AdminApiKey);
            Assert.Equal("ap-southeast-1", settings.RegionString);
        }

        #endregion

        #region StorageSettings

        [Fact]
        public void StorageSettings_DefaultValues_AreCorrect()
        {
            StorageSettings storage = new StorageSettings();
            Assert.Equal("./temp/", storage.TempDirectory);
            Assert.Equal(StorageDriverType.Disk, storage.StorageType);
            Assert.Equal("./disk/", storage.DiskDirectory);
        }

        [Fact]
        public void StorageSettings_PropertyAssignment_Works()
        {
            StorageSettings storage = new StorageSettings();
            storage.TempDirectory = "/tmp/custom/";
            storage.DiskDirectory = "/data/objects/";
            Assert.Equal("/tmp/custom/", storage.TempDirectory);
            Assert.Equal("/data/objects/", storage.DiskDirectory);
        }

        #endregion

        #region LoggingSettings

        [Fact]
        public void LoggingSettings_DefaultValues_AreCorrect()
        {
            LoggingSettings logging = new LoggingSettings();
            Assert.Equal("127.0.0.1", logging.SyslogServerIp);
            Assert.Equal(514, logging.SyslogServerPort);
            Assert.False(logging.LogHttpRequests);
            Assert.False(logging.LogS3Requests);
            Assert.True(logging.ConsoleLogging);
            Assert.True(logging.DiskLogging);
            Assert.Equal("./logs/", logging.DiskDirectory);
        }

        #endregion

        #region DebugSettings

        [Fact]
        public void DebugSettings_DefaultValues_AreCorrect()
        {
            DebugSettings debug = new DebugSettings();
            Assert.False(debug.Authentication);
            Assert.False(debug.S3Requests);
            Assert.False(debug.Exceptions);
        }

        [Fact]
        public void DebugSettings_PropertyAssignment_Works()
        {
            DebugSettings debug = new DebugSettings();
            debug.Authentication = true;
            debug.S3Requests = true;
            debug.Exceptions = true;
            Assert.True(debug.Authentication);
            Assert.True(debug.S3Requests);
            Assert.True(debug.Exceptions);
        }

        #endregion
    }
}
