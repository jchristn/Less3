namespace Test.Xunit.Fixtures
{
    using global::Xunit;

    /// <summary>
    /// Defines the xunit test collection that shares a single Less3TestServer instance
    /// across all integration test suite classes.
    /// </summary>
    [CollectionDefinition("Integration")]
    public class IntegrationTestCollection : ICollectionFixture<Less3TestServerFixture>
    {
    }
}
