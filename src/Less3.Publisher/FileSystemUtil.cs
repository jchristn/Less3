namespace Less3.Publisher
{
    using System.IO;

    /// <summary>
    /// File system helpers shared by channels.
    /// </summary>
    public static class FileSystemUtil
    {
        /// <summary>
        /// Copies a directory tree, preserving Unix file modes, and replaces files that already exist.
        /// </summary>
        /// <param name="source">Directory to copy.</param>
        /// <param name="destination">Destination directory; created when missing.</param>
        /// <exception cref="DirectoryNotFoundException">The source does not exist.</exception>
        public static void CopyTree(string source, string destination)
        {
            if (!Directory.Exists(source)) throw new DirectoryNotFoundException("Directory not found: " + source);
            Directory.CreateDirectory(destination);
            foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
            }

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)), true);
            }
        }

        /// <summary>
        /// Deletes a directory when it exists and creates it empty.
        /// </summary>
        /// <param name="path">Directory.</param>
        /// <returns>The path.</returns>
        public static string Reset(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Total size in bytes of every file under a directory.
        /// </summary>
        /// <param name="path">Directory.</param>
        /// <returns>Size in bytes, or 0 when the directory does not exist.</returns>
        public static long Size(string path)
        {
            if (!Directory.Exists(path)) return 0;
            // Symbolic links are skipped, not followed: a disk image's Applications link would otherwise count the
            // whole /Applications folder.
            EnumerationOptions options = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = true };
            long total = 0;
            foreach (string file in Directory.GetFiles(path, "*", options))
            {
                FileInfo info = new FileInfo(file);
                if (info.LinkTarget == null) total += info.Length;
            }

            return total;
        }

        /// <summary>
        /// Writes text with Unix line endings, which shell scripts, plists, and desktop entries need even when the
        /// Publisher runs on Windows.
        /// </summary>
        /// <param name="path">File to write.</param>
        /// <param name="text">Content.</param>
        public static void WriteUnixText(string path, string text)
        {
            File.WriteAllText(path, text.Replace("\r\n", "\n", System.StringComparison.Ordinal));
        }
    }
}
