namespace Less3.Publisher.Manifest
{
    /// <summary>
    /// A systemd system service for a server, installed by the .deb and .rpm but not enabled: the server needs its
    /// configuration written once (interactively) before the service can start it.
    /// </summary>
    public sealed class ServiceSettings
    {
        /// <summary>
        /// Unit name without ".service", for example "less3".
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Description line of the unit.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// System account the service runs as. The packages create it (with no login shell) when it is missing.
        /// </summary>
        public string User { get; set; } = "";

        /// <summary>
        /// Working directory, which holds the configuration file, database, and data. The packages create it,
        /// owned by <see cref="User"/>, and never delete it.
        /// </summary>
        public string WorkingDirectory { get; set; } = "";

        /// <summary>
        /// File in the working directory that must exist before the service starts, for example "system.json".
        /// The unit's ConditionPathExists keeps an unconfigured server from starting. Empty for no condition.
        /// </summary>
        public string RequiredFile { get; set; } = "";
    }
}
