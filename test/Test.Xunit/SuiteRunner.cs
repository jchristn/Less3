namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Linq;
    using Test.Shared;
    using global::Xunit;

    /// <summary>
    /// Helper that bridges Test.Shared suite results to xunit assertions.
    /// </summary>
    public static class SuiteRunner
    {
        /// <summary>
        /// Asserts that all test results in the list are marked as passed.
        /// Fails with details of each individual failure if any tests did not pass.
        /// </summary>
        /// <param name="results">The list of test results from a suite run.</param>
        public static void AssertAllPassed(List<TestResult> results)
        {
            List<TestResult> failures = results.Where(r => !r.Passed).ToList();

            if (failures.Count > 0)
            {
                string messages = string.Join("\n", failures.Select(f => $"  FAIL {f.Name}: {f.Message}"));
                Assert.Fail($"{failures.Count} of {results.Count} test(s) failed:\n{messages}");
            }

            Assert.True(results.Count > 0, "Suite produced no test results");
        }
    }
}
