namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Test.Shared;
    using Test.Xunit.Fixtures;
    using global::Xunit;

    /// <summary>
    /// xUnit adapter for every standalone suite in the Test.Shared catalog.
    /// </summary>
    public sealed class SharedStandaloneSuiteCatalogTests
    {
        /// <summary>
        /// Standalone shared suites.
        /// </summary>
        public static IEnumerable<object[]> Suites()
        {
            return SharedTestSuiteCatalog.StandaloneSuites.Select(suite => new object[] { suite });
        }

        /// <summary>
        /// Runs a standalone shared suite.
        /// </summary>
        [Theory]
        [MemberData(nameof(Suites))]
        public async Task RunSuite(SharedTestSuiteDescriptor suiteDescriptor)
        {
            TestSuite suite = suiteDescriptor.Create();
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }

    /// <summary>
    /// xUnit adapter for every live-server suite in the Test.Shared catalog.
    /// </summary>
    [Collection("Integration")]
    public sealed class SharedIntegrationSuiteCatalogTests
    {
        private readonly Less3TestServerFixture _Fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedIntegrationSuiteCatalogTests"/> class.
        /// </summary>
        public SharedIntegrationSuiteCatalogTests(Less3TestServerFixture fixture)
        {
            _Fixture = fixture;
        }

        /// <summary>
        /// Live-server shared suites.
        /// </summary>
        public static IEnumerable<object[]> Suites()
        {
            return SharedTestSuiteCatalog.IntegrationSuites.Select(suite => new object[] { suite });
        }

        /// <summary>
        /// Runs a live-server shared suite.
        /// </summary>
        [Theory]
        [MemberData(nameof(Suites))]
        public async Task RunSuite(SharedTestSuiteDescriptor suiteDescriptor)
        {
            TestSuite suite = suiteDescriptor.Create(_Fixture.Server);
            List<TestResult> results = await suite.RunAsync();
            SuiteRunner.AssertAllPassed(results);
        }
    }
}
