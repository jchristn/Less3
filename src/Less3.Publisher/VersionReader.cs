namespace Less3.Publisher
{
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Reads the product version from src/Directory.Build.props, the one file that holds it.
    /// </summary>
    public static class VersionReader
    {
        /// <summary>
        /// Path of the version file relative to the repository root.
        /// </summary>
        public const string RelativePath = "src/Directory.Build.props";

        /// <summary>
        /// Reads the version.
        /// </summary>
        /// <param name="repoRoot">Repository root.</param>
        /// <returns>The version, for example "0.2.0".</returns>
        /// <exception cref="PublisherException">The file is missing or has no Version element.</exception>
        public static string Read(string repoRoot)
        {
            string path = Path.Combine(repoRoot, RelativePath);
            if (!File.Exists(path)) throw new PublisherException(RelativePath + " is missing; it holds the version every installer is named from.");
            Match match = Regex.Match(File.ReadAllText(path), "<Version>\\s*([^<\\s]+)\\s*</Version>");
            if (!match.Success) throw new PublisherException(RelativePath + " has no <Version> element.");
            return match.Groups[1].Value;
        }
    }
}
