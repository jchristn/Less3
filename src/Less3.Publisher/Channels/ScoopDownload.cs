namespace Less3.Publisher.Channels
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// One Scoop download: an archive URL, its SHA-256, and the folder inside it to extract.
    /// </summary>
    public sealed class ScoopDownload
    {
        /// <summary>
        /// Archive URL.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        /// <summary>
        /// Lowercase hex SHA-256.
        /// </summary>
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = "";

        /// <summary>
        /// Top-level folder in the archive.
        /// </summary>
        [JsonPropertyName("extract_dir")]
        public string ExtractDir { get; set; } = "";
    }
}
