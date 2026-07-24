namespace Test.Shared
{
    using System.Collections.Generic;
    using System.Linq;
    using Test.Shared.Suites;

    /// <summary>
    /// Canonical catalog of legacy Test.Shared suites consumed by runner projects.
    /// </summary>
    public static class SharedTestSuiteCatalog
    {
        private static readonly IReadOnlyList<SharedTestSuiteDescriptor> _All = new List<SharedTestSuiteDescriptor>
        {
            Standalone("ModelTests", "Model Tests", _ => new ModelTests()),
            Standalone("SettingsTests", "Settings Tests", _ => new SettingsTests()),
            Standalone("StorageTests", "Storage Tests", _ => new StorageTests()),
            Standalone("S3ServerRegressionTests", "S3 Server Regression Tests", _ => new S3ServerRegressionTests()),
            Standalone("DockerBootstrapTests", "Docker Bootstrap Tests", _ => new DockerBootstrapTests()),
            Standalone("ContainerAutoconfigTests", "Container Autoconfig Tests", _ => new ContainerAutoconfigTests()),
            Standalone("LegacyV2MigrationTests", "Legacy v2 Migration Tests", _ => new LegacyV2MigrationTests()),
            Standalone("SignatureValidationApiTests", "Signature Validation API Tests", _ => new SignatureValidationApiTests()),

            Integration("AdminApiTests", "Admin API Tests", server => new AdminApiTests(server!)),
            Integration("BucketApiTests", "Bucket API Tests", server => new BucketApiTests(server!)),
            Integration("BucketAdvancedApiTests", "Bucket Advanced API Tests", server => new BucketAdvancedApiTests(server!)),
            Integration("ObjectApiTests", "Object API Tests", server => new ObjectApiTests(server!)),
            Integration("ObjectAdvancedApiTests", "Object Advanced API Tests", server => new ObjectAdvancedApiTests(server!)),
            Integration("ObjectMetadataRegressionTests", "Object Metadata Regression Tests", server => new ObjectMetadataRegressionTests(server!)),
            Integration("MultipartApiTests", "Multipart API Tests", server => new MultipartApiTests(server!)),
            Integration("S3ProtocolComplianceTests", "S3 Protocol Compliance Tests", server => new S3ProtocolComplianceTests(server!)),
            Integration("SecurityBoundaryTests", "Security Boundary Tests", server => new SecurityBoundaryTests(server!)),
            Integration("PerformanceRegressionTests", "Performance Regression Tests", server => new PerformanceRegressionTests(server!))
        };

        /// <summary>
        /// All legacy Test.Shared suites.
        /// </summary>
        public static IReadOnlyList<SharedTestSuiteDescriptor> All => _All;

        /// <summary>
        /// Suites that do not require a running server.
        /// </summary>
        public static IReadOnlyList<SharedTestSuiteDescriptor> StandaloneSuites => _All.Where(s => !s.RequiresServer).ToList();

        /// <summary>
        /// Suites that require a running server.
        /// </summary>
        public static IReadOnlyList<SharedTestSuiteDescriptor> IntegrationSuites => _All.Where(s => s.RequiresServer).ToList();

        private static SharedTestSuiteDescriptor Standalone(
            string id,
            string name,
            System.Func<Less3TestServer?, TestSuite> factory)
        {
            return new SharedTestSuiteDescriptor(id, name, false, factory);
        }

        private static SharedTestSuiteDescriptor Integration(
            string id,
            string name,
            System.Func<Less3TestServer?, TestSuite> factory)
        {
            return new SharedTestSuiteDescriptor(id, name, true, factory);
        }
    }
}
