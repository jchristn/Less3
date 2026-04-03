namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Test.Shared.Suites;
    using global::Xunit;

    /// <summary>
    /// Runs the Signature Validation API test suite from Test.Shared via xunit.
    /// This suite manages its own Less3 server with signature validation enabled.
    /// </summary>
    public class SignatureValidationApiSuiteTests
    {
        /// <summary>
        /// Executes all signature validation API tests and asserts every test passes.
        /// </summary>
        [Fact]
        public async Task RunSuite()
        {
            SignatureValidationApiTests suite = new SignatureValidationApiTests();
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }
}
