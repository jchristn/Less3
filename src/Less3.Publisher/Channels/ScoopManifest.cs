namespace Less3.Publisher.Channels
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A Scoop app manifest (bucket/&lt;name&gt;.json).
    /// </summary>
    public sealed class ScoopManifest
    {
        /// <summary>
        /// Version.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        /// <summary>
        /// One-line description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        /// <summary>
        /// Home page.
        /// </summary>
        [JsonPropertyName("homepage")]
        public string Homepage { get; set; } = "";

        /// <summary>
        /// SPDX license identifier.
        /// </summary>
        [JsonPropertyName("license")]
        public string License { get; set; } = "";

        /// <summary>
        /// Download per architecture.
        /// </summary>
        [JsonPropertyName("architecture")]
        public ScoopArchitectures Architecture { get; set; } = new ScoopArchitectures();

        /// <summary>
        /// Executables Scoop puts on PATH.
        /// </summary>
        [JsonPropertyName("bin")]
        public List<string> Bin { get; set; } = new List<string>();
    }
}
