namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Test.Shared.Suites;
    using global::Xunit;

    /// <summary>
    /// Runs the Settings test suite from Test.Shared via xunit.
    /// </summary>
    public class SettingsSuiteTests
    {
        /// <summary>
        /// Executes all settings tests and asserts every test passes.
        /// </summary>
        [Fact]
        public async Task RunSuite()
        {
            SettingsTests suite = new SettingsTests();
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }
}
