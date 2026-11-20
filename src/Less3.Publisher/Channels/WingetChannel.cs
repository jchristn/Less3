namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// winget manifests (version, installer, and default locale) for microsoft/winget-pkgs, referencing the Inno
    /// installers by URL and SHA-256. Submission ends at a pull request a human merges.
    /// </summary>
    public sealed class WingetChannel : IChannel
    {
        /// <inheritdoc/>
        public string Id => "winget";

        /// <inheritdoc/>
        public string DisplayName => "winget manifests";

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
            string id = context.Option("identifier", context.Project.PackageId);
            string directory = ManifestOutput.Directory(context, Id);
            StringBuilder installers = new StringBuilder();
            foreach (RuntimeTarget target in context.Targets)
            {
                string file = AssetNames.WindowsSetup(context.Project.Name, context.Version, target);
                string sha = Checksums.Sha256(context.RequireAsset(file, "inno")).ToUpperInvariant();
                installers.Append("  - Architecture: ").Append(target.IsArm64 ? "arm64" : "x64").Append('\n');
                installers.Append("    InstallerUrl: ").Append(context.ReleaseAssetUrl(file)).Append('\n');
                installers.Append("    InstallerSha256: ").Append(sha).Append('\n');
            }

            string header = "PackageIdentifier: " + id + "\nPackageVersion: " + context.Version + "\n";
            File.WriteAllText(Path.Combine(directory, id + ".yaml"), header + "DefaultLocale: en-US\nManifestType: version\nManifestVersion: 1.6.0\n");
            File.WriteAllText(Path.Combine(directory, id + ".installer.yaml"), header + "InstallerType: inno\nScope: machine\nInstallModes:\n  - interactive\n  - silent\n  - silentWithProgress\nInstallers:\n" + installers + "ManifestType: installer\nManifestVersion: 1.6.0\n");
            File.WriteAllText(Path.Combine(directory, id + ".locale.en-US.yaml"), header +
                "PackageLocale: en-US\n" +
                "Publisher: " + context.Project.Publisher + "\n" +
                "PackageName: " + context.Project.DisplayName + "\n" +
                "PackageUrl: " + context.Project.Homepage + "\n" +
                "License: " + context.Project.License + "\n" +
                "ShortDescription: " + context.Project.Tagline + "\n" +
                "Description: " + context.Project.Description + "\n" +
                "ManifestType: defaultLocale\nManifestVersion: 1.6.0\n");
            Console.WriteLine("[winget] wrote manifests in " + directory + " (submit to " + context.Manifest.Destinations.WingetPkgsRepo + ")");
            return new[] { directory };
        }
    }
}
