namespace Test.Automated
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Cli;

    /// <summary>
    /// Entry point for the Less3 automated test runner.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point. Runs Touchstone descriptors by default, or the legacy harness with --legacy.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>0 if all tests passed, 1 if any test failed.</returns>
        public static async Task<int> Main(string[] args)
        {
            if (args != null && args.Contains("--legacy"))
            {
                return await RunLegacyAsync().ConfigureAwait(false);
            }

            string? resultsPath = null;

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--results" && i + 1 < args.Length)
                    {
                        resultsPath = args[i + 1];
                        break;
                    }
                }
            }

            return await ConsoleRunner.RunAsync(
                Less3TouchstoneSuites.All,
                resultsPath: resultsPath).ConfigureAwait(false);
        }

        private static async Task<int> RunLegacyAsync()
        {
            TestRunner runner = new TestRunner("Less3 Automated Tests");

            foreach (SharedTestSuiteDescriptor suite in SharedTestSuiteCatalog.StandaloneSuites)
            {
                runner.AddSuite(suite.Create());
            }

            // Integration test suites (require running Less3 server)
            Less3TestServer server = new Less3TestServer();

            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Starting Less3 test server on port {server.Port}...");
                Console.ResetColor();

                await server.StartAsync().ConfigureAwait(false);

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Less3 test server ready at {server.BaseUrl}");
                Console.ResetColor();

                foreach (SharedTestSuiteDescriptor suite in SharedTestSuiteCatalog.IntegrationSuites)
                {
                    runner.AddSuite(suite.Create(server));
                }

                int exitCode = await runner.RunAllAsync().ConfigureAwait(false);
                return exitCode;
            }
            finally
            {
                server.Dispose();
            }
        }
    }
}
