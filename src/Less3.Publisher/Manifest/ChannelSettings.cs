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
        /// Artifact id the channel packages. Use <see cref="Artifacts"/> to package several.
        /// </summary>
        public string Artifact { get; set; } = "";

        /// <summary>
        /// Artifact ids the channel packages, each into its own installers. Empty means <see cref="Artifact"/>.
        /// </summary>
        public List<string> Artifacts { get; set; } = new List<string>();

        /// <summary>
        /// Runtime identifiers to build. Empty means the manifest's runtimes for the channel's operating system.
        /// </summary>
        public List<string> Runtimes { get; set; } = new List<string>();

        /// <summary>
        /// Channel-specific string options.
        /// </summary>
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The artifact ids the channel packages, in order: <see cref="Artifacts"/>, or <see cref="Artifact"/> alone.
        /// </summary>
        /// <returns>Artifact ids.</returns>
        public List<string> ArtifactIds()
        {
            if (Artifacts.Count > 0) return new List<string>(Artifacts);
            return String.IsNullOrEmpty(Artifact) ? new List<string>() : new List<string> { Artifact };
        }
    }
}
