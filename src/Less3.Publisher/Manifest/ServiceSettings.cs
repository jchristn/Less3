namespace Less3.Publisher.Manifest
{
    using System.Collections.Generic;

    /// <summary>
    /// A systemd system service for a server, installed by the .deb and .rpm but not enabled, so installing a package
    /// never starts a server or opens a port by itself.
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

        /// <summary>
        /// Arguments appended to ExecStart, or empty.
        /// </summary>
        public string Arguments { get; set; } = "";

        /// <summary>
        /// Environment variables the unit sets, as NAME=value. Empty for none.
        /// </summary>
        public List<string> Environment { get; set; } = new List<string>();
    }
}
