namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Test.Shared.Suites;
    using global::Xunit;

    /// <summary>
    /// Runs the S3Server regression test suite from Test.Shared via xunit.
    /// </summary>
    public class S3ServerRegressionSuiteTests
    {
        /// <summary>
        /// Executes all S3Server regression tests and asserts every test passes.
        /// </summary>
        [Fact]
        public async Task RunSuite()
        {
            S3ServerRegressionTests suite = new S3ServerRegressionTests();
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }
}
