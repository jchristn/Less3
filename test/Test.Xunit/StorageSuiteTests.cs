namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Test.Shared.Suites;
    using global::Xunit;

    /// <summary>
    /// Runs the Storage test suite from Test.Shared via xunit.
    /// </summary>
    public class StorageSuiteTests
    {
        /// <summary>
        /// Executes all storage tests and asserts every test passes.
        /// </summary>
        [Fact]
        public async Task RunSuite()
        {
            StorageTests suite = new StorageTests();
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }
}
