namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Test.Shared.Suites;
    using Test.Xunit.Fixtures;
    using global::Xunit;

    /// <summary>
    /// Runs the Admin API test suite from Test.Shared via xunit.
    /// </summary>
    [Collection("Integration")]
    public class AdminApiSuiteTests
    {
        private Less3TestServerFixture _Fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminApiSuiteTests"/> class.
        /// </summary>
        /// <param name="fixture">The shared test server fixture.</param>
        public AdminApiSuiteTests(Less3TestServerFixture fixture)
        {
            _Fixture = fixture;
        }

        /// <summary>
        /// Executes all admin API tests and asserts every test passes.
        /// </summary>
        [Fact]
        public async Task RunSuite()
        {
            AdminApiTests suite = new AdminApiTests(_Fixture.Server);
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }
}
