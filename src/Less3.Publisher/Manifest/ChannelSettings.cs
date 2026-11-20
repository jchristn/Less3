namespace Less3.Publisher.Manifest
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Configuration for one packaging channel.
    /// </summary>
    public sealed class ChannelSettings
    {
        /// <summary>
        /// When false the channel refuses to run.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Artifact id the channel packages.
        /// </summary>
        public string Artifact { get; set; } = "";

        /// <summary>
        /// Runtime identifiers to build. Empty means the manifest's runtimes for the channel's operating system.
        /// </summary>
        public List<string> Runtimes { get; set; } = new List<string>();

        /// <summary>
        /// Channel-specific string options.
        /// </summary>
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
