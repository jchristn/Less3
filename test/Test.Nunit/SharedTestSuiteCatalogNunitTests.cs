namespace Test.Nunit
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;

    /// <summary>
    /// NUnit adapter for every standalone suite in the Test.Shared catalog.
    /// </summary>
    [TestFixture]
    public sealed class SharedStandaloneSuiteCatalogNunitTests
    {
        private static IEnumerable Suites()
        {
            return SharedTestSuiteCatalog.StandaloneSuites.Select(suite => new TestCaseData(suite).SetName(suite.Id));
        }

        /// <summary>
        /// Runs a standalone shared suite.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(Suites))]
        public async Task RunSuite(SharedTestSuiteDescriptor suiteDescriptor)
        {
            TestSuite suite = suiteDescriptor.Create();
            List<TestResult> results = await suite.RunAsync().ConfigureAwait(false);
            AssertAllPassed(results);
        }

        private static void AssertAllPassed(List<TestResult> results)
        {
            List<TestResult> failures = results.Where(result => !result.Passed).ToList();
            if (failures.Count > 0)
            {
                string messages = string.Join("\n", failures.Select(failure => "  FAIL " + failure.Name + ": " + failure.Message));
                Assert.Fail(failures.Count + " of " + results.Count + " test(s) failed:\n" + messages);
            }

            Assert.That(results.Count, Is.GreaterThan(0), "Suite produced no test results");
        }
    }

    /// <summary>
    /// NUnit adapter for every live-server suite in the Test.Shared catalog.
    /// </summary>
    [TestFixture]
    public sealed class SharedIntegrationSuiteCatalogNunitTests
    {
        private static IEnumerable Suites()
        {
            return SharedTestSuiteCatalog.IntegrationSuites.Select(suite => new TestCaseData(suite).SetName(suite.Id));
        }

        /// <summary>
        /// Runs a live-server shared suite.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(Suites))]
        public async Task RunSuite(SharedTestSuiteDescriptor suiteDescriptor)
        {
            using Less3TestServer server = new Less3TestServer();
            await server.StartAsync().ConfigureAwait(false);

            TestSuite suite = suiteDescriptor.Create(server);
            List<TestResult> results = await suite.RunAsync().ConfigureAwait(false);
            AssertAllPassed(results);
        }

        private static void AssertAllPassed(List<TestResult> results)
        {
            List<TestResult> failures = results.Where(result => !result.Passed).ToList();
            if (failures.Count > 0)
            {
                string messages = string.Join("\n", failures.Select(failure => "  FAIL " + failure.Name + ": " + failure.Message));
                Assert.Fail(failures.Count + " of " + results.Count + " test(s) failed:\n" + messages);
            }

            Assert.That(results.Count, Is.GreaterThan(0), "Suite produced no test results");
        }
    }
}
