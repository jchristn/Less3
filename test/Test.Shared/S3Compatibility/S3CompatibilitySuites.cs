namespace Test.Shared.S3Compatibility
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Touchstone.Core;

    /// <summary>
    /// Catalog of the S3 compatibility suites: behavior checked against Amazon S3 semantics, in both the
    /// positive and negative direction.
    /// </summary>
    public static class S3CompatibilitySuites
    {
        #region Public-Members

        /// <summary>
        /// All S3 compatibility suites.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get
            {
                return new List<TestSuiteDescriptor>
                {
                    DeleteObjectsCompatibilityCases.Suite(),
                    VersioningCompatibilityCases.Suite(),
                    ListingCompatibilityCases.Suite(),
                    ObjectCompatibilityCases.Suite(),
                    CopyObjectCompatibilityCases.Suite(),
                    SecurityCompatibilityCases.Suite(),
                    BucketMultipartCompatibilityCases.Suite(),
                    ProtocolCompatibilityCases.Suite()
                };
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build a test case descriptor.
        /// </summary>
        /// <param name="suiteId">Suite id.</param>
        /// <param name="caseId">Case id, unique within the suite.</param>
        /// <param name="displayName">Display name.</param>
        /// <param name="executeAsync">Test body.</param>
        /// <returns>Case descriptor.</returns>
        public static TestCaseDescriptor Case(string suiteId, string caseId, string displayName, Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(
                suiteId: suiteId,
                caseId: suiteId + "_" + caseId,
                displayName: displayName,
                executeAsync: executeAsync);
        }

        #endregion
    }
}
