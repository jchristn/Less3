namespace Less3.Publisher.Manifest
{
    /// <summary>
    /// Where release notes come from.
    /// </summary>
    public sealed class ReleaseNotesSettings
    {
        /// <summary>
        /// File holding release notes, relative to the repository root. Default CHANGELOG.md.
        /// </summary>
        public string Source { get; set; } = "CHANGELOG.md";

        /// <summary>
        /// Heading that opens a version's section, with {version} as the placeholder. Default "## [{version}]".
        /// </summary>
        public string Heading { get; set; } = "## [{version}]";
    }
}
