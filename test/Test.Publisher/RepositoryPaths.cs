namespace Test.Publisher
{
    using System;
    using System.IO;

    using Less3.Publisher.Manifest;

    /// <summary>
    /// Locates the repository and loads the committed publisher.json for the tests.
    /// </summary>
    public static class RepositoryPaths
    {
        /// <summary>
        /// The repository root: the folder holding publisher.json and src/Less3.sln.
        /// </summary>
        /// <returns>Absolute path.</returns>
        /// <exception cref="DirectoryNotFoundException">The tests are not running inside the repository.</exception>
        public static string Root()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (System.IO.File.Exists(Path.Combine(directory.FullName, "src", "Less3.sln")) && System.IO.File.Exists(Path.Combine(directory.FullName, "publisher.json"))) return directory.FullName;
                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Unable to locate the repository root containing src/Less3.sln and publisher.json.");
        }

        /// <summary>
        /// A path under the repository root.
        /// </summary>
        /// <param name="relative">Relative path with forward slashes.</param>
        /// <returns>Absolute path.</returns>
        public static string File(string relative)
        {
            return Path.Combine(Root(), relative.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// The committed manifest.
        /// </summary>
        /// <returns>Manifest.</returns>
        public static PublisherManifest Manifest()
        {
            return PublisherManifest.Load(File("publisher.json"));
        }

        /// <summary>
        /// The server artifact from the committed manifest.
        /// </summary>
        /// <returns>Artifact.</returns>
        /// <exception cref="InvalidOperationException">The manifest has no server artifact.</exception>
        public static ArtifactSettings Server()
        {
            return Manifest().FindArtifact("server") ?? throw new InvalidOperationException("publisher.json has no server artifact.");
        }
    }
}
