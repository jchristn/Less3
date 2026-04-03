namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Test.Shared.Suites;
    using Test.Xunit.Fixtures;
    using global::Xunit;

    /// <summary>
    /// Runs the Bucket API test suite from Test.Shared via xunit.
    /// </summary>
    [Collection("Integration")]
    public class BucketApiSuiteTests
    {
        private Less3TestServerFixture _Fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketApiSuiteTests"/> class.
        /// </summary>
        /// <param name="fixture">The shared test server fixture.</param>
        public BucketApiSuiteTests(Less3TestServerFixture fixture)
        {
            _Fixture = fixture;
        }

        /// <summary>
        /// Executes all bucket API tests and asserts every test passes.
        /// </summary>
        [Fact]
        public async Task RunSuite()
        {
            BucketApiTests suite = new BucketApiTests(_Fixture.Server);
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }
}
