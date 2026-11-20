namespace Less3.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Extracts one version's section from a changelog.
    /// </summary>
    public static class ChangelogReader
    {
        /// <summary>
        /// Returns the lines between the version's heading and the next heading of the same level, trimmed.
        /// </summary>
        /// <param name="text">Changelog text.</param>
        /// <param name="heading">Heading pattern with {version}, for example "## [{version}]".</param>
        /// <param name="version">Version to find.</param>
        /// <returns>The section body, or an empty string when the version has no section or the section is empty.</returns>
        public static string Section(string text, string heading, string version)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(heading)) return "";
            string start = heading.Replace("{version}", version, StringComparison.Ordinal);
            string level = heading.Substring(0, heading.IndexOf(' ', StringComparison.Ordinal) + 1);
            List<string> captured = new List<string>();
            bool inSection = false;
            foreach (string raw in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                if (!inSection)
                {
                    if (raw.StartsWith(start, StringComparison.Ordinal)) inSection = true;
                    continue;
                }

                if (raw.StartsWith(level, StringComparison.Ordinal)) break;
                captured.Add(raw);
            }

            return String.Join("\n", captured).Trim();
        }

        /// <summary>
        /// Reads a changelog file and returns one version's section.
        /// </summary>
        /// <param name="path">Changelog path.</param>
        /// <param name="heading">Heading pattern with {version}.</param>
        /// <param name="version">Version to find.</param>
        /// <returns>The section body, or an empty string.</returns>
        public static string ReadSection(string path, string heading, string version)
        {
            return File.Exists(path) ? Section(File.ReadAllText(path), heading, version) : "";
        }
    }
}
