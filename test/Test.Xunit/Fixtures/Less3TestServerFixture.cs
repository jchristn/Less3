namespace Test.Xunit.Fixtures
{
    using System.Threading.Tasks;
    using global::Xunit;
    using Test.Shared;

    /// <summary>
    /// Xunit fixture that manages a shared Less3 test server for integration test suites.
    /// </summary>
    public class Less3TestServerFixture : IAsyncLifetime
    {
        /// <summary>
        /// The running Less3 test server instance.
        /// </summary>
        public Less3TestServer Server { get; private set; } = null!;

        /// <summary>
        /// Starts the Less3 test server.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            Server = new Less3TestServer();
            await Server.StartAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Stops and disposes the Less3 test server.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task DisposeAsync()
        {
            Server?.Dispose();
            return Task.CompletedTask;
        }
    }
}
