namespace Test.Xunit
{
    using System.Threading;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using global::Xunit;

    /// <summary>
    /// xUnit theory-style runner for Less3 Touchstone descriptors.
    /// </summary>
    public sealed class Less3TouchstoneTheoryTests
    {
        /// <summary>
        /// Touchstone test cases exposed as xUnit theory data.
        /// </summary>
        /// <returns>Theory data.</returns>
        public static TheoryData<TestCaseDescriptor> TestCases()
        {
            TheoryData<TestCaseDescriptor> data = new TheoryData<TestCaseDescriptor>();

            foreach (TestSuiteDescriptor suite in Less3TouchstoneSuites.All)
            {
                foreach (TestCaseDescriptor testCase in suite.Cases)
                {
                    if (!testCase.Skip)
                    {
                        data.Add(testCase);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Run one Touchstone descriptor.
        /// </summary>
        /// <param name="testCase">Test case descriptor.</param>
        /// <returns>Task.</returns>
        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None);
        }
    }
}
