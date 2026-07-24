namespace Test.Nunit
{
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// NUnit TestCaseSource runner for Less3 Touchstone descriptors.
    /// </summary>
    [TestFixture]
    public sealed class Less3TouchstoneNunitTests
    {
        private static IEnumerable TestCases()
        {
            return new TouchstoneTestCaseSource(Less3TouchstoneSuites.All);
        }

        /// <summary>
        /// Run one Touchstone descriptor.
        /// </summary>
        /// <param name="testCase">Test case descriptor.</param>
        /// <returns>Task.</returns>
        [Test]
        [TestCaseSource(nameof(TestCases))]
        public async Task RunTest(TestCaseDescriptor testCase)
        {
            await testCase.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
