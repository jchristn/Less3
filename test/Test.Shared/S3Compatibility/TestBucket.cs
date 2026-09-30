namespace Test.Shared.S3Compatibility
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.S3;

    /// <summary>
    /// A uniquely named bucket on the shared compatibility server, with a client for the default credential.
    /// Disposing releases the client; the bucket is left in place.
    /// </summary>
    public sealed class TestBucket : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Server hosting the bucket.
        /// </summary>
        public Less3TestServer Server { get; }

        /// <summary>
        /// S3 client using the default credential.
        /// </summary>
        public IAmazonS3 Client { get; }

        /// <summary>
        /// Bucket name.
        /// </summary>
        public string Name { get; }

        #endregion

        #region Private-Members

        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        private TestBucket(Less3TestServer server, IAmazonS3 client, string name)
        {
            Server = server;
            Client = client;
            Name = name;
        }

        /// <summary>
        /// Create a bucket on the shared server.
        /// </summary>
        /// <param name="prefix">Bucket name prefix.</param>
        /// <param name="versioned">True to enable versioning.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Test bucket.</returns>
        public static async Task<TestBucket> CreateAsync(string prefix, bool versioned, CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            return await CreateAsync(server, prefix, versioned, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a bucket on a specific server.
        /// </summary>
        /// <param name="server">Server.</param>
        /// <param name="prefix">Bucket name prefix.</param>
        /// <param name="versioned">True to enable versioning.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Test bucket.</returns>
        public static async Task<TestBucket> CreateAsync(Less3TestServer server, string prefix, bool versioned, CancellationToken token)
        {
            IAmazonS3 client = S3CompatibilityContext.Client(server);
            string name = await S3CompatibilityContext.NewBucketAsync(client, prefix, versioned, token).ConfigureAwait(false);
            return new TestBucket(server, client, name);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Release the client.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            Client.Dispose();
            _Disposed = true;
        }

        #endregion
    }
}
