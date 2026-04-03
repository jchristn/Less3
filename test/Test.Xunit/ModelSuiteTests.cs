namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Test.Shared.Suites;
    using global::Xunit;

    /// <summary>
    /// Runs the Model test suite from Test.Shared via xunit.
    /// </summary>
    public class ModelSuiteTests
    {
        /// <summary>
        /// Executes all model tests and asserts every test passes.
        /// </summary>
        [Fact]
        public async Task RunSuite()
        {
            ModelTests suite = new ModelTests();
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }
}
