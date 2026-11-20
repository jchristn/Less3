namespace Less3.Publisher.Channels
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A Scoop manifest's downloads per architecture. An architecture with no build is left out.
    /// </summary>
    public sealed class ScoopArchitectures
    {
        /// <summary>
        /// x64 download, or null.
        /// </summary>
        [JsonPropertyName("64bit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ScoopDownload? X64 { get; set; } = null;

        /// <summary>
        /// ARM64 download, or null.
        /// </summary>
        [JsonPropertyName("arm64")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ScoopDownload? Arm64 { get; set; } = null;
    }
}
