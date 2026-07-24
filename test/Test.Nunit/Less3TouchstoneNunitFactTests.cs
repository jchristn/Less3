namespace Test.Nunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// NUnit fact-style runner for Less3 Touchstone descriptors.
    /// </summary>
    [TestFixture]
    public sealed class Less3TouchstoneNunitFactTests : TouchstoneNunitBase
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
        [Test]
        public async Task RunAll()
        {
            await RunAllAsync().ConfigureAwait(false);
        }
    }
}
