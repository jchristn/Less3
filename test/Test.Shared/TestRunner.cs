namespace Test.Shared
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Runs a collection of <see cref="TestSuite"/> instances and returns an exit code.
    /// </summary>
    public class TestRunner
    {
        #region Private-Members

        private List<TestSuite> _Suites = new List<TestSuite>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunner"/> class with the specified title.
        /// </summary>
        /// <param name="title">The title to display when running tests.</param>
        public TestRunner(string title)
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Adds a test suite to the runner.
        /// </summary>
        /// <param name="suite">The test suite to add.</param>
        public void AddSuite(TestSuite suite)
        {
            if (suite == null) throw new ArgumentNullException(nameof(suite));
            _Suites.Add(suite);
        }

        /// <summary>
        /// Runs all registered test suites.
        /// </summary>
        /// <returns>0 if all tests passed, 1 if any test failed.</returns>
        public async Task<int> RunAllAsync()
        {
            int failedTests = 0;

            foreach (TestSuite suite in _Suites)
            {
                List<TestResult> results = await suite.RunAsync().ConfigureAwait(false);

                foreach (TestResult result in results)
                {
                    if (!result.Passed)
                    {
                        failedTests++;
                    }
                }
            }

            return failedTests > 0 ? 1 : 0;
        }

        #endregion
    }
}
