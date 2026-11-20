namespace Less3.Publisher.Channels
{
    /// <summary>
    /// Paths of the scripts fpm embeds in a .deb or .rpm.
    /// </summary>
    public sealed class PackageScripts
    {
        /// <summary>
        /// Runs after the package's files are installed (and after an upgrade).
        /// </summary>
        public string AfterInstall { get; set; } = "";

        /// <summary>
        /// Runs before the package's files are removed (and before an upgrade replaces them).
        /// </summary>
        public string BeforeRemove { get; set; } = "";

        /// <summary>
        /// Runs after the package's files are removed.
        /// </summary>
        public string AfterRemove { get; set; } = "";
    }
}
