namespace Test.Shared.Suites
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Touchstone.Core;

    /// <summary>
    /// Runs one Touchstone <see cref="TestSuiteDescriptor"/> under the shared <see cref="TestSuite"/> harness, so
    /// the xUnit, NUnit and legacy runners exercise the same cases as the Touchstone runner. The cases manage
    /// their own servers (for example through <see cref="S3Compatibility.S3CompatibilityContext"/>) and never use
    /// the harness server, because a harness may dispose its server while other runners in the same process
    /// still run cases.
    /// </summary>
    public class DescriptorSuiteTests : TestSuite
    {
        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => _Suite.DisplayName;

        #endregion

        #region Private-Members

        private readonly TestSuiteDescriptor _Suite;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="suite">The suite to run.</param>
        /// <exception cref="ArgumentNullException">Thrown when suite is null.</exception>
        public DescriptorSuiteTests(TestSuiteDescriptor suite)
        {
            _Suite = suite ?? throw new ArgumentNullException(nameof(suite));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs every case in the suite.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            foreach (TestCaseDescriptor testCase in _Suite.Cases)
            {
                if (testCase.Skip) continue;
                await RunTest(testCase.CaseId, async () => await testCase.ExecuteAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        #endregion
    }
}
