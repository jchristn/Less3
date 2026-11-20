namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Less3.Publisher.Manifest;

    /// <summary>
    /// Everything a channel needs for one run.
    /// </summary>
    public sealed class ChannelContext
    {
        /// <summary>
        /// Repository root (the folder holding publisher.json).
        /// </summary>
        public string RepoRoot { get; }

        /// <summary>
        /// The manifest.
        /// </summary>
        public PublisherManifest Manifest { get; }

        /// <summary>
        /// This channel's settings.
        /// </summary>
        public ChannelSettings Settings { get; }

        /// <summary>
        /// The artifact this channel packages.
        /// </summary>
        public ArtifactSettings Artifact { get; }

        /// <summary>
        /// Version being released, for example "0.2.0".
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Runtimes to build.
        /// </summary>
        public IReadOnlyList<RuntimeTarget> Targets { get; }

        /// <summary>
        /// Directory that receives finished files.
        /// </summary>
        public string OutputDir { get; }

        /// <summary>
        /// Scratch directory for this channel.
        /// </summary>
        public string StagingDir { get; }

        /// <summary>
        /// Publishes the payload, shared by every channel in a run.
        /// </summary>
        public Payload Payload { get; }

        /// <summary>
        /// Project settings, a shortcut for Manifest.Project.
        /// </summary>
        public ProjectSettings Project => Manifest.Project;

        /// <summary>
        /// Creates a context.
        /// </summary>
        /// <param name="repoRoot">Repository root.</param>
        /// <param name="manifest">Manifest.</param>
        /// <param name="settings">Channel settings.</param>
        /// <param name="artifact">Artifact.</param>
        /// <param name="version">Version.</param>
        /// <param name="targets">Runtimes to build.</param>
        /// <param name="outputDir">Output directory.</param>
        /// <param name="stagingDir">Scratch directory.</param>
        /// <param name="payload">Payload publisher.</param>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        public ChannelContext(string repoRoot, PublisherManifest manifest, ChannelSettings settings, ArtifactSettings artifact, string version, IReadOnlyList<RuntimeTarget> targets, string outputDir, string stagingDir, Payload payload)
        {
            RepoRoot = repoRoot ?? throw new ArgumentNullException(nameof(repoRoot));
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Targets = targets ?? throw new ArgumentNullException(nameof(targets));
            OutputDir = outputDir ?? throw new ArgumentNullException(nameof(outputDir));
            StagingDir = stagingDir ?? throw new ArgumentNullException(nameof(stagingDir));
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }

        /// <summary>
        /// Absolute path of a repository-relative path.
        /// </summary>
        /// <param name="relative">Path relative to the repository root.</param>
        /// <returns>Absolute path.</returns>
        public string RepoPath(string relative)
        {
            return Path.GetFullPath(Path.Combine(RepoRoot, relative));
        }

        /// <summary>
        /// The license file.
        /// </summary>
        /// <returns>Absolute path.</returns>
        /// <exception cref="PublisherException">The license file is missing; every installer shows it.</exception>
        public string LicensePath()
        {
            string path = RepoPath(Project.LicenseFile);
            if (!File.Exists(path)) throw new PublisherException("License file " + Project.LicenseFile + " not found; every installer shows the license. Set project.licenseFile in publisher.json.");
            return path;
        }

        /// <summary>
        /// The square PNG logo.
        /// </summary>
        /// <returns>Absolute path.</returns>
        /// <exception cref="PublisherException">The logo is missing.</exception>
        public string LogoPath()
        {
            string path = RepoPath(Project.Logo);
            if (!File.Exists(path)) throw new PublisherException("Logo " + Project.Logo + " not found. Installers must show the product logo, not packager default artwork.");
            return path;
        }

        /// <summary>
        /// A channel option, or a default.
        /// </summary>
        /// <param name="key">Option name.</param>
        /// <param name="fallback">Default.</param>
        /// <returns>The value.</returns>
        public string Option(string key, string fallback)
        {
            return Settings.Options.TryGetValue(key, out string? value) && !String.IsNullOrEmpty(value) ? value : fallback;
        }

        /// <summary>
        /// A scratch subdirectory, emptied first.
        /// </summary>
        /// <param name="name">Subdirectory name.</param>
        /// <returns>Absolute path.</returns>
        public string FreshStaging(string name)
        {
            return FileSystemUtil.Reset(Path.Combine(StagingDir, name));
        }

        /// <summary>
        /// Download URL a release asset will have, given tags named v plus the version.
        /// </summary>
        /// <param name="fileName">Asset file name.</param>
        /// <returns>URL.</returns>
        public string ReleaseAssetUrl(string fileName)
        {
            return "https://github.com/" + Manifest.Destinations.GithubRepo + "/releases/download/v" + Version + "/" + fileName;
        }

        /// <summary>
        /// Finds a file another channel already built in the output directory.
        /// </summary>
        /// <param name="fileName">Exact file name.</param>
        /// <param name="producer">Channel that builds it, for the error message.</param>
        /// <returns>Absolute path.</returns>
        /// <exception cref="PublisherException">The file is not there.</exception>
        public string RequireAsset(string fileName, string producer)
        {
            string? hit = Directory.GetFiles(OutputDir, fileName, SearchOption.AllDirectories).FirstOrDefault();
            if (hit == null) throw new PublisherException("Expected " + fileName + " in " + OutputDir + ". Run the '" + producer + "' channel first (in CI, download its artifacts into the output directory).");
            return hit;
        }

        /// <summary>
        /// Moves a finished file into the output directory, replacing an older copy.
        /// </summary>
        /// <param name="file">File to place.</param>
        /// <returns>Its path in the output directory.</returns>
        public string Deliver(string file)
        {
            string destination = Path.Combine(OutputDir, Path.GetFileName(file));
            if (String.Equals(Path.GetFullPath(file), Path.GetFullPath(destination), StringComparison.Ordinal)) return destination;
            File.Move(file, destination, true);
            return destination;
        }
    }
}
