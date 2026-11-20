namespace Less3.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.Formats.Tar;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Http;

    /// <summary>
    /// Downloads the official Node.js runtime for a target from https://nodejs.org/dist, checks it against the
    /// release's SHASUMS256.txt, and extracts the node executable and its license. Downloads are cached per run.
    /// </summary>
    public static class NodeRuntime
    {
        /// <summary>
        /// Base URL of the Node.js releases.
        /// </summary>
        public const string DistUrl = "https://nodejs.org/dist";

        private static readonly HttpClient _Http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

        /// <summary>
        /// The Node.js platform name for a target: win-x64, darwin-arm64, linux-x64, and so on.
        /// </summary>
        /// <param name="target">Runtime target.</param>
        /// <returns>Platform name.</returns>
        public static string Platform(RuntimeTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);
            string os = target.Os == OsFamilyEnum.Windows ? "win" : target.Os == OsFamilyEnum.MacOs ? "darwin" : "linux";
            return os + "-" + (target.IsArm64 ? "arm64" : "x64");
        }

        /// <summary>
        /// The release archive's file name: node-v22.23.5-win-x64.zip, or a .tar.gz for macOS and Linux.
        /// </summary>
        /// <param name="version">Exact Node.js version without the v.</param>
        /// <param name="target">Runtime target.</param>
        /// <returns>File name.</returns>
        public static string ArchiveName(string version, RuntimeTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);
            return "node-v" + version + "-" + Platform(target) + (target.Os == OsFamilyEnum.Windows ? ".zip" : ".tar.gz");
        }

        /// <summary>
        /// Finds a file's SHA-256 in a SHASUMS256.txt listing.
        /// </summary>
        /// <param name="sums">SHASUMS256.txt text.</param>
        /// <param name="fileName">File name.</param>
        /// <returns>Lowercase hex hash, or null when the file is not listed.</returns>
        public static string? ExpectedHash(string sums, string fileName)
        {
            foreach (string line in (sums ?? "").Split('\n'))
            {
                string[] parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && String.Equals(parts[1].TrimStart('*'), fileName, StringComparison.Ordinal)) return parts[0].ToLowerInvariant();
            }

            return null;
        }

        /// <summary>
        /// Puts the node executable (node.exe on Windows) and LICENSE into a folder.
        /// </summary>
        /// <param name="version">Exact Node.js version without the v, for example "22.23.5".</param>
        /// <param name="target">Runtime target.</param>
        /// <param name="cacheDirectory">Folder that keeps downloads for the rest of the run.</param>
        /// <param name="destination">Folder to receive node and LICENSE; created when missing.</param>
        /// <exception cref="PublisherException">The download fails, does not match its published checksum, or lacks
        /// the executable.</exception>
        public static void Install(string version, RuntimeTarget target, string cacheDirectory, string destination)
        {
            ArgumentNullException.ThrowIfNull(target);
            Directory.CreateDirectory(cacheDirectory);
            Directory.CreateDirectory(destination);
            string name = ArchiveName(version, target);
            string archive = Path.Combine(cacheDirectory, name);
            string baseUrl = DistUrl + "/v" + version + "/";
            if (!File.Exists(archive))
            {
                string sums = Download(baseUrl + "SHASUMS256.txt", null);
                string? expected = ExpectedHash(sums, name);
                if (expected == null) throw new PublisherException("Node.js v" + version + " has no " + name + " in its SHASUMS256.txt. Check build.artifacts[].nextApp.nodeVersion in publisher.json.");
                string partial = archive + ".part";
                Download(baseUrl + name, partial);
                string actual = Checksums.Sha256(partial);
                if (!String.Equals(actual, expected, StringComparison.Ordinal))
                {
                    File.Delete(partial);
                    throw new PublisherException(name + " does not match its published SHA-256 (expected " + expected + ", got " + actual + ").");
                }

                File.Move(partial, archive, true);
                Console.WriteLine("[node] downloaded and verified " + name);
            }

            string executable = target.Os == OsFamilyEnum.Windows ? "node.exe" : "node";
            string top = "node-v" + version + "-" + Platform(target) + "/";
            Dictionary<string, string> wanted = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [top + (target.Os == OsFamilyEnum.Windows ? "node.exe" : "bin/node")] = executable,
                [top + "LICENSE"] = "LICENSE"
            };

            if (target.Os == OsFamilyEnum.Windows) ExtractZip(archive, wanted, destination);
            else ExtractTarGz(archive, wanted, destination);
            foreach (string file in wanted.Values)
            {
                if (!File.Exists(Path.Combine(destination, file))) throw new PublisherException(name + " has no " + file + ".");
            }
        }

        private static string Download(string url, string? path)
        {
            try
            {
                using (HttpResponseMessage response = _Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
                {
                    if (!response.IsSuccessStatusCode) throw new PublisherException("Downloading " + url + " failed: HTTP " + (int)response.StatusCode + ".");
                    if (path == null) return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    using (Stream source = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (FileStream target = File.Create(path))
                    {
                        source.CopyTo(target);
                    }

                    return path;
                }
            }
            catch (HttpRequestException ex)
            {
                throw new PublisherException("Downloading " + url + " failed: " + ex.Message);
            }
        }

        private static void ExtractZip(string archive, Dictionary<string, string> wanted, string destination)
        {
            using (ZipArchive zip = ZipFile.OpenRead(archive))
            {
                foreach (ZipArchiveEntry entry in zip.Entries.Where(item => wanted.ContainsKey(item.FullName)))
                {
                    entry.ExtractToFile(Path.Combine(destination, wanted[entry.FullName]), true);
                }
            }
        }

        private static void ExtractTarGz(string archive, Dictionary<string, string> wanted, string destination)
        {
            using (FileStream file = File.OpenRead(archive))
            using (GZipStream gzip = new GZipStream(file, CompressionMode.Decompress))
            using (TarReader reader = new TarReader(gzip))
            {
                TarEntry? entry;
                while ((entry = reader.GetNextEntry()) != null)
                {
                    if (entry.EntryType != TarEntryType.RegularFile || !wanted.TryGetValue(entry.Name, out string? name)) continue;
                    string path = Path.Combine(destination, name);
                    entry.ExtractToFile(path, true);
                    if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, entry.Mode | UnixFileMode.UserExecute | UnixFileMode.UserRead);
                }
            }
        }
    }
}
