namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;

    /// <summary>
    /// Scoop manifest for this repository's bucket/ folder, referencing the portable .zip archives (Scoop extracts
    /// archives; it does not run installers).
    /// </summary>
    public sealed class ScoopChannel : IChannel
    {
        /// <inheritdoc/>
        public string Id => "scoop";

        /// <inheritdoc/>
        public string DisplayName => "Scoop manifest";

        /// <inheritdoc/>
        public OsFamilyEnum RequiredHost => OsFamilyEnum.Any;

        /// <inheritdoc/>
        public OsFamilyEnum TargetOs => OsFamilyEnum.Windows;

        /// <inheritdoc/>
        public IReadOnlyList<ToolRequirement> Tools => Array.Empty<ToolRequirement>();

        /// <inheritdoc/>
        public IReadOnlyList<string> Build(ChannelContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ScoopManifest manifest = new ScoopManifest
            {
                Version = context.Version,
                Description = context.Project.Tagline,
                Homepage = context.Project.Homepage,
                License = context.Project.License
            };
            foreach (RuntimeTarget target in context.Targets)
            {
                string file = AssetNames.Archive(context.Project.Name, context.Version, target);
                ScoopDownload download = new ScoopDownload
                {
                    Url = context.ReleaseAssetUrl(file),
                    Hash = Checksums.Sha256(context.RequireAsset(file, "archive")),
                    ExtractDir = AssetNames.ArchiveFolder(context.Project.Name, context.Version, target)
                };
                if (target.IsArm64) manifest.Architecture.Arm64 = download;
                else manifest.Architecture.X64 = download;
            }

            foreach (string command in context.Artifact.PathCommands) manifest.Bin.Add(command + ".exe");
            string name = context.Project.Name;
            string output = Path.Combine(ManifestOutput.Directory(context, Id), name + ".json");
            File.WriteAllText(output, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            Console.WriteLine("[scoop] wrote " + output + " (commit it to bucket/ in " + context.Manifest.Destinations.ScoopBucketRepo + ")");
            return new[] { output };
        }
    }
}
