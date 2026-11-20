namespace Less3.Publisher.Manifest
{
    using System.Collections.Generic;

    /// <summary>
    /// Build inputs: the target framework, the runtime identifiers, and the artifacts.
    /// </summary>
    public sealed class BuildSettings
    {
        /// <summary>
        /// Target framework moniker to publish, for example "net10.0".
        /// </summary>
        public string Framework { get; set; } = "";

        /// <summary>
        /// Runtime identifiers to build. A channel builds the ones for its operating system unless it lists its own.
        /// </summary>
        public List<string> Runtimes { get; set; } = new List<string>();

        /// <summary>
        /// Buildable artifacts.
        /// </summary>
        public List<ArtifactSettings> Artifacts { get; set; } = new List<ArtifactSettings>();
    }
}
