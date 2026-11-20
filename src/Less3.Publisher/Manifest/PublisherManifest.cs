namespace Less3.Publisher.Manifest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using Less3.Publisher;

    /// <summary>
    /// The parsed publisher.json: what Less3 ships, how each installer format packages it, and where releases go.
    /// A release replays this manifest against a version; bumping the version never edits it.
    /// </summary>
    public sealed class PublisherManifest
    {
        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// Schema version of the manifest. Currently 1.
        /// </summary>
        public int SchemaVersion { get; set; } = 1;

        /// <summary>
        /// Product identity, artwork, and text shown by every installer.
        /// </summary>
        public ProjectSettings Project { get; set; } = new ProjectSettings();

        /// <summary>
        /// What to build: the target framework, runtime identifiers, and artifacts.
        /// </summary>
        public BuildSettings Build { get; set; } = new BuildSettings();

        /// <summary>
        /// Where releases and package-manager manifests are published.
        /// </summary>
        public DestinationSettings Destinations { get; set; } = new DestinationSettings();

        /// <summary>
        /// Packaging channels keyed by id (inno, macos, debrpm, appimage, archive, and the package managers).
        /// </summary>
        public Dictionary<string, ChannelSettings> Channels { get; set; } = new Dictionary<string, ChannelSettings>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Code-signing settings per operating system. Every target is off by policy; see INSTALLERS.md.
        /// </summary>
        public SigningSettings Signing { get; set; } = new SigningSettings();

        /// <summary>
        /// Where release notes come from.
        /// </summary>
        public ReleaseNotesSettings ReleaseNotes { get; set; } = new ReleaseNotesSettings();

        /// <summary>
        /// Loads and validates a manifest from disk.
        /// </summary>
        /// <param name="path">Path to publisher.json.</param>
        /// <returns>The manifest.</returns>
        /// <exception cref="ArgumentNullException">The path is null or empty.</exception>
        /// <exception cref="FileNotFoundException">The file does not exist.</exception>
        /// <exception cref="InvalidDataException">The file is not a valid manifest.</exception>
        public static PublisherManifest Load(string path)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException("publisher.json not found.", path);
            return Parse(File.ReadAllText(path), path);
        }

        /// <summary>
        /// Parses and validates manifest JSON.
        /// </summary>
        /// <param name="json">Manifest JSON.</param>
        /// <param name="source">Where the JSON came from, for error messages.</param>
        /// <returns>The manifest.</returns>
        /// <exception cref="InvalidDataException">The JSON is not a valid manifest.</exception>
        public static PublisherManifest Parse(string json, string source)
        {
            PublisherManifest? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<PublisherManifest>(json, _JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(source + " is not valid JSON: " + ex.Message, ex);
            }

            if (manifest == null) throw new InvalidDataException(source + " is empty.");
            manifest.Channels = new Dictionary<string, ChannelSettings>(manifest.Channels, StringComparer.OrdinalIgnoreCase);
            List<string> problems = manifest.Validate();
            if (problems.Count > 0) throw new InvalidDataException(source + " is not valid: " + String.Join(" ", problems));
            return manifest;
        }

        /// <summary>
        /// Returns the artifact with the given id, or null when none is declared.
        /// </summary>
        /// <param name="id">Artifact id.</param>
        /// <returns>The artifact, or null.</returns>
        public ArtifactSettings? FindArtifact(string id)
        {
            return Build.Artifacts.FirstOrDefault(item => String.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Lists what is wrong with the manifest. An empty list means it is usable.
        /// </summary>
        /// <returns>Problems, one sentence each.</returns>
        public List<string> Validate()
        {
            List<string> problems = new List<string>();
            if (String.IsNullOrWhiteSpace(Project.Name)) problems.Add("project.name is required.");
            if (String.IsNullOrWhiteSpace(Project.DisplayName)) problems.Add("project.displayName is required.");
            if (String.IsNullOrWhiteSpace(Project.Tagline)) problems.Add("project.tagline is required; every installer's welcome page shows it.");
            if (String.IsNullOrWhiteSpace(Project.BundleId)) problems.Add("project.bundleId is required.");
            if (String.IsNullOrWhiteSpace(Build.Framework)) problems.Add("build.framework is required.");
            if (Build.Runtimes.Count == 0) problems.Add("build.runtimes must list at least one runtime identifier.");
            foreach (string rid in Build.Runtimes)
            {
                if (!RuntimeTarget.TryParse(rid, out RuntimeTarget? _)) problems.Add("build.runtimes has an unsupported runtime identifier: " + rid + ".");
            }

            foreach (ArtifactSettings artifact in Build.Artifacts)
            {
                if (String.IsNullOrWhiteSpace(artifact.Id) || String.IsNullOrWhiteSpace(artifact.Csproj)) problems.Add("Every artifact needs an id and a csproj.");
                if (String.IsNullOrWhiteSpace(artifact.MainExecutable)) problems.Add("Artifact '" + artifact.Id + "' needs a mainExecutable.");
                foreach (string command in artifact.PathCommands)
                {
                    if (!artifact.Executables.Contains(command, StringComparer.Ordinal)) problems.Add("Artifact '" + artifact.Id + "' puts '" + command + "' on PATH but does not list it in executables.");
                }

                if (!String.IsNullOrWhiteSpace(artifact.MainExecutable) && !artifact.Executables.Contains(artifact.MainExecutable, StringComparer.Ordinal)) problems.Add("Artifact '" + artifact.Id + "' runs '" + artifact.MainExecutable + "' but does not list it in executables.");
                if (artifact.Launcher != null)
                {
                    if (String.IsNullOrWhiteSpace(artifact.Launcher.DisplayName) || String.IsNullOrWhiteSpace(artifact.Launcher.FileName)) problems.Add("Artifact '" + artifact.Id + "' launcher needs a displayName and a fileName.");
                    if (artifact.Executables.Contains(artifact.Launcher.FileName, StringComparer.OrdinalIgnoreCase)) problems.Add("Artifact '" + artifact.Id + "' launcher fileName '" + artifact.Launcher.FileName + "' collides with an executable; macOS file names ignore case.");
                }

                if (artifact.Service != null)
                {
                    if (String.IsNullOrWhiteSpace(artifact.Service.Name) || String.IsNullOrWhiteSpace(artifact.Service.User) || !artifact.Service.WorkingDirectory.StartsWith("/", StringComparison.Ordinal)) problems.Add("Artifact '" + artifact.Id + "' service needs a name, a user, and an absolute workingDirectory.");
                }

                if (artifact.NextApp != null)
                {
                    if (String.IsNullOrWhiteSpace(artifact.NextApp.Directory)) problems.Add("Artifact '" + artifact.Id + "' nextApp needs a directory.");
                    if (!System.Text.RegularExpressions.Regex.IsMatch(artifact.NextApp.NodeVersion ?? "", "^\\d+\\.\\d+\\.\\d+$")) problems.Add("Artifact '" + artifact.Id + "' nextApp needs an exact nodeVersion such as 22.23.5.");
                }
            }

            List<ArtifactSettings> packaged = Channels.Values.Where(item => item.Enabled).SelectMany(item => item.ArtifactIds()).Distinct(StringComparer.OrdinalIgnoreCase).Select(FindArtifact).Where(item => item != null).Select(item => item!).ToList();
            foreach (IGrouping<string, ArtifactSettings> group in packaged.GroupBy(item => Project.For(item).Name, StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                problems.Add("Artifacts " + String.Join(", ", group.Select(item => "'" + item.Id + "'")) + " would all produce installers named '" + group.Key + "'; give each its own name.");
            }

            foreach (IGrouping<string, ArtifactSettings> group in packaged.GroupBy(item => Project.For(item).BundleId, StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                problems.Add("Artifacts " + String.Join(", ", group.Select(item => "'" + item.Id + "'")) + " share the identifier '" + group.Key + "'; give each its own bundleId.");
            }

            foreach (KeyValuePair<string, ChannelSettings> channel in Channels)
            {
                List<string> ids = channel.Value.ArtifactIds();
                if (ids.Count == 0) problems.Add("Channel '" + channel.Key + "' names no artifact.");
                foreach (string id in ids)
                {
                    if (FindArtifact(id) == null) problems.Add("Channel '" + channel.Key + "' references unknown artifact '" + id + "'.");
                }
            }

            return problems;
        }
    }
}
