namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Test.Shared.Suites;
    using global::Xunit;

    /// <summary>
    /// Runs the Docker bootstrap test suite from Test.Shared via xunit.
    /// </summary>
    public class DockerBootstrapSuiteTests
    {
        /// <summary>
        /// Executes all Docker bootstrap tests and asserts every test passes.
        /// </summary>
        [Fact]
        public async Task RunSuite()
        {
            DockerBootstrapTests suite = new DockerBootstrapTests();
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }
}
