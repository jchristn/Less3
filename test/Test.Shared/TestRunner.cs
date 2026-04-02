namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Runs a collection of <see cref="TestSuite"/> instances and prints a summary of results.
    /// </summary>
    public class TestRunner
    {
        #region Private-Members

        private string _Title;
        private List<TestSuite> _Suites = new List<TestSuite>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunner"/> class with the specified title.
        /// </summary>
        /// <param name="title">The title to display when running tests.</param>
        public TestRunner(string title)
        {
            _Title = title ?? throw new ArgumentNullException(nameof(title));
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
        /// Runs all registered test suites and prints a summary to the console.
        /// </summary>
        /// <returns>0 if all tests passed, 1 if any test failed.</returns>
        public async Task<int> RunAllAsync()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"=== {_Title} ===");
            Console.ResetColor();
            Console.WriteLine();

            int totalTests = 0;
            int passedTests = 0;
            int failedTests = 0;

            foreach (TestSuite suite in _Suites)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{suite.Name}]");
                Console.ResetColor();

                List<TestResult> results = await suite.RunAsync().ConfigureAwait(false);

                foreach (TestResult result in results)
                {
                    totalTests++;
                    if (result.Passed)
                    {
                        passedTests++;
                    }
                    else
                    {
                        failedTests++;
                    }
                }

                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Summary ===");
            Console.ResetColor();
            Console.WriteLine($"  Total:  {totalTests}");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Passed: {passedTests}");
            Console.ResetColor();

            if (failedTests > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine($"  Failed: {failedTests}");
            Console.ResetColor();
            Console.WriteLine();

            return failedTests > 0 ? 1 : 0;
        }

        #endregion
    }
}
