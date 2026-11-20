namespace Less3.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// SHA-256 helpers. The release carries one SHA256SUMS file covering every asset, and package-manager manifests
    /// compute their hashes from the built installers instead of a hand-copied value.
    /// </summary>
    public static class Checksums
    {
        /// <summary>
        /// Name of the aggregate checksum file.
        /// </summary>
        public const string SumsFileName = "SHA256SUMS";

        /// <summary>
        /// Lowercase hex SHA-256 of a file.
        /// </summary>
        /// <param name="path">File to hash.</param>
        /// <returns>The hash.</returns>
        /// <exception cref="FileNotFoundException">The file does not exist.</exception>
        public static string Sha256(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Cannot hash a file that does not exist.", path);
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = SHA256.HashData(stream);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
        }

        /// <summary>
        /// Writes SHA256SUMS in a directory covering every file directly inside it, in name order, in the format
        /// sha256sum -c and shasum -a 256 -c read.
        /// </summary>
        /// <param name="directory">Directory whose files to list.</param>
        /// <returns>Path of the written file.</returns>
        /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
        public static string WriteSums(string directory)
        {
            if (!Directory.Exists(directory)) throw new DirectoryNotFoundException("Directory not found: " + directory);
            IEnumerable<string> files = Directory.GetFiles(directory)
                .Where(path => !String.Equals(Path.GetFileName(path), SumsFileName, StringComparison.Ordinal))
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal);
            StringBuilder builder = new StringBuilder();
            foreach (string file in files) builder.Append(Sha256(file)).Append("  ").Append(Path.GetFileName(file)).Append('\n');
            string output = Path.Combine(directory, SumsFileName);
            File.WriteAllText(output, builder.ToString());
            return output;
        }
    }
}
