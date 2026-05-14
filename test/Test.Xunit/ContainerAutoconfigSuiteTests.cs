namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Test.Shared.Suites;
    using global::Xunit;

    /// <summary>
    /// Runs the container autoconfig test suite from Test.Shared via xunit.
    /// </summary>
    public class ContainerAutoconfigSuiteTests
    {
        /// <summary>
        /// Executes all container autoconfiguration tests and asserts every test passes.
        /// </summary>
        [Fact]
        public async Task RunSuite()
        {
            ContainerAutoconfigTests suite = new ContainerAutoconfigTests();
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }
}
