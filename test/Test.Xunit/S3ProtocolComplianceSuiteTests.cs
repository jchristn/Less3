namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Test.Shared.Suites;
    using Test.Xunit.Fixtures;
    using global::Xunit;

    /// <summary>
    /// Runs the S3 Protocol Compliance test suite from Test.Shared via xunit.
    /// </summary>
    [Collection("Integration")]
    public class S3ProtocolComplianceSuiteTests
    {
        private Less3TestServerFixture _Fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="S3ProtocolComplianceSuiteTests"/> class.
        /// </summary>
        /// <param name="fixture">The shared test server fixture.</param>
        public S3ProtocolComplianceSuiteTests(Less3TestServerFixture fixture)
        {
            _Fixture = fixture;
        }

        /// <summary>
        /// Executes all S3 protocol compliance tests and asserts every test passes.
        /// </summary>
        [Fact]
        public async Task RunSuite()
        {
            S3ProtocolComplianceTests suite = new S3ProtocolComplianceTests(_Fixture.Server);
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }
}
