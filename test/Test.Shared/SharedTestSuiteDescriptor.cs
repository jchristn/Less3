namespace Test.Shared
{
    using System;

    /// <summary>
    /// Describes a Test.Shared suite and how runner projects should instantiate it.
    /// </summary>
    public sealed class SharedTestSuiteDescriptor
    {
        private readonly Func<Less3TestServer?, TestSuite> _Factory;

        /// <summary>
        /// Stable suite identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Display name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Whether the suite requires a running Less3 server.
        /// </summary>
        public bool RequiresServer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedTestSuiteDescriptor"/> class.
        /// </summary>
        public SharedTestSuiteDescriptor(string id, string name, bool requiresServer, Func<Less3TestServer?, TestSuite> factory)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            RequiresServer = requiresServer;
            _Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Creates the suite instance.
        /// </summary>
        public TestSuite Create(Less3TestServer? server = null)
        {
            if (RequiresServer && server == null)
            {
                throw new ArgumentNullException(nameof(server), Id + " requires a running Less3 test server.");
            }

            return _Factory(server);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Id;
        }
    }
}
