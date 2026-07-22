namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.XunitAdapter;
    using global::Xunit;

    /// <summary>
    /// xUnit fact-style runner for Less3 Touchstone descriptors.
    /// </summary>
    public sealed class Less3TouchstoneFactTests : TouchstoneFactBase
    {
        /// <summary>
        /// Touchstone suites consumed by this runner.
        /// </summary>
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get
            {
                return Less3TouchstoneSuites.All;
            }
        }

        /// <summary>
        /// Run all Touchstone descriptors.
        /// </summary>
        /// <returns>Task.</returns>
        [Fact]
        public async Task RunAll()
        {
            await RunAllAsync();
        }
    }
}
