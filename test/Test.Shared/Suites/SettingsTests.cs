namespace Test.Shared.Suites
{
    using System;
    using System.Threading.Tasks;
    using Less3.Settings;
    using Less3.Storage;

    /// <summary>
    /// Tests for Less3 settings classes: SettingsBase, StorageSettings, LoggingSettings, DebugSettings.
    /// </summary>
    public class SettingsTests : TestSuite
    {
        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Settings Tests";

        /// <summary>
        /// Runs all settings tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            #region SettingsBase

            await RunTest("SettingsBase_DefaultValues", () =>
            {
                SettingsBase settings = new SettingsBase();
                AssertTrue(settings.EnableConsole);
                AssertTrue(settings.ValidateSignatures);
                AssertNull(settings.BaseDomain);
                AssertEqual("x-api-key", settings.HeaderApiKey);
                AssertEqual("less3admin", settings.AdminApiKey);
                AssertEqual("us-west-1", settings.RegionString);
                AssertNotNull(settings.Database);
                AssertNotNull(settings.Webserver);
                AssertNotNull(settings.Storage);
                AssertNotNull(settings.Logging);
                AssertNotNull(settings.Debug);
            });

            await RunTest("SettingsBase_HeaderApiKey_NullThrows", () =>
            {
                SettingsBase settings = new SettingsBase();
                AssertThrows<ArgumentNullException>(() =>
                {
                    settings.HeaderApiKey = null;
                });
                AssertThrows<ArgumentNullException>(() =>
                {
                    settings.HeaderApiKey = "";
                });
            });

            await RunTest("SettingsBase_AdminApiKey_NullThrows", () =>
            {
                SettingsBase settings = new SettingsBase();
                AssertThrows<ArgumentNullException>(() =>
                {
                    settings.AdminApiKey = null;
                });
            });

            await RunTest("SettingsBase_RegionString_NullThrows", () =>
            {
                SettingsBase settings = new SettingsBase();
                AssertThrows<ArgumentNullException>(() =>
                {
                    settings.RegionString = null;
                });
            });

            await RunTest("SettingsBase_Webserver_NullThrows", () =>
            {
                SettingsBase settings = new SettingsBase();
                AssertThrows<ArgumentNullException>(() =>
                {
                    settings.Webserver = null;
                });
            });

            await RunTest("SettingsBase_Database_NullThrows", () =>
            {
                SettingsBase settings = new SettingsBase();
                AssertThrows<ArgumentNullException>(() =>
                {
                    settings.Database = null;
                });
            });

            await RunTest("SettingsBase_Storage_NullThrows", () =>
            {
                SettingsBase settings = new SettingsBase();
                AssertThrows<ArgumentNullException>(() =>
                {
                    settings.Storage = null;
                });
            });

            await RunTest("SettingsBase_Logging_NullThrows", () =>
            {
                SettingsBase settings = new SettingsBase();
                AssertThrows<ArgumentNullException>(() =>
                {
                    settings.Logging = null;
                });
            });

            await RunTest("SettingsBase_Debug_NullThrows", () =>
            {
                SettingsBase settings = new SettingsBase();
                AssertThrows<ArgumentNullException>(() =>
                {
                    settings.Debug = null;
                });
            });

            await RunTest("SettingsBase_PropertyAssignment", () =>
            {
                SettingsBase settings = new SettingsBase();
                settings.EnableConsole = false;
                settings.ValidateSignatures = false;
                settings.BaseDomain = ".example.com";
                settings.HeaderApiKey = "custom-key";
                settings.AdminApiKey = "myadminkey";
                settings.RegionString = "ap-southeast-1";

                AssertFalse(settings.EnableConsole);
                AssertFalse(settings.ValidateSignatures);
                AssertEqual(".example.com", settings.BaseDomain);
                AssertEqual("custom-key", settings.HeaderApiKey);
                AssertEqual("myadminkey", settings.AdminApiKey);
                AssertEqual("ap-southeast-1", settings.RegionString);
            });

            #endregion

            #region StorageSettings

            await RunTest("StorageSettings_DefaultValues", () =>
            {
                StorageSettings storage = new StorageSettings();
                AssertEqual("./temp/", storage.TempDirectory);
                AssertEqual(StorageDriverType.Disk, storage.StorageType);
                AssertEqual("./disk/", storage.DiskDirectory);
            });

            await RunTest("StorageSettings_PropertyAssignment", () =>
            {
                StorageSettings storage = new StorageSettings();
                storage.TempDirectory = "/tmp/custom/";
                storage.DiskDirectory = "/data/objects/";
                AssertEqual("/tmp/custom/", storage.TempDirectory);
                AssertEqual("/data/objects/", storage.DiskDirectory);
            });

            #endregion

            #region LoggingSettings

            await RunTest("LoggingSettings_DefaultValues", () =>
            {
                LoggingSettings logging = new LoggingSettings();
                AssertEqual("127.0.0.1", logging.SyslogServerIp);
                AssertEqual(514, logging.SyslogServerPort);
                AssertFalse(logging.LogHttpRequests);
                AssertFalse(logging.LogS3Requests);
                AssertFalse(logging.LogExceptions);
                AssertFalse(logging.LogSignatureValidation);
                AssertTrue(logging.ConsoleLogging);
                AssertTrue(logging.DiskLogging);
                AssertEqual("./logs/", logging.DiskDirectory);
            });

            #endregion

            #region DebugSettings

            await RunTest("DebugSettings_DefaultValues", () =>
            {
                DebugSettings debug = new DebugSettings();
                AssertFalse(debug.Authentication);
                AssertFalse(debug.S3Requests);
                AssertFalse(debug.Exceptions);
            });

            await RunTest("DebugSettings_PropertyAssignment", () =>
            {
                DebugSettings debug = new DebugSettings();
                debug.Authentication = true;
                debug.S3Requests = true;
                debug.Exceptions = true;
                AssertTrue(debug.Authentication);
                AssertTrue(debug.S3Requests);
                AssertTrue(debug.Exceptions);
            });

            #endregion
        }
    }
}
