namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;

    /// <summary>
    /// Chocolatey package sources (.nuspec and install script) that download the Inno installers. The workflow runs
    /// choco pack and push on Windows; a new package then waits in Chocolatey's moderation queue.
    /// </summary>
    public sealed class ChocoChannel : IChannel
    {
        /// <inheritdoc/>
        public string Id => "choco";

        /// <inheritdoc/>
        public string DisplayName => "Chocolatey package";

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
            RuntimeTarget x64 = RuntimeTarget.Parse("win-x64");
            string file = AssetNames.WindowsSetup(context.Project.Name, context.Version, x64);
            string sha = Checksums.Sha256(context.RequireAsset(file, "inno"));
            string name = context.Project.Name;
            string directory = ManifestOutput.Directory(context, Id);
            Directory.CreateDirectory(Path.Combine(directory, "tools"));

            string nuspec =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<package xmlns=\"http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd\">\n" +
                "  <metadata>\n" +
                "    <id>" + name + "</id>\n" +
                "    <version>" + context.Version + "</version>\n" +
                "    <title>" + WebUtility.HtmlEncode(context.Project.DisplayName) + "</title>\n" +
                "    <authors>" + WebUtility.HtmlEncode(context.Project.Publisher) + "</authors>\n" +
                "    <projectUrl>" + context.Project.Homepage + "</projectUrl>\n" +
                "    <licenseUrl>" + context.Project.Repo + "/blob/main/" + context.Project.LicenseFile + "</licenseUrl>\n" +
                "    <requireLicenseAcceptance>false</requireLicenseAcceptance>\n" +
                "    <summary>" + WebUtility.HtmlEncode(context.Project.Tagline) + "</summary>\n" +
                "    <description>" + WebUtility.HtmlEncode(context.Project.Description) + "</description>\n" +
                "  </metadata>\n" +
                "  <files>\n    <file src=\"tools\\**\" target=\"tools\" />\n  </files>\n" +
                "</package>\n";
            string nuspecPath = Path.Combine(directory, name + ".nuspec");
            File.WriteAllText(nuspecPath, nuspec);
            File.WriteAllText(Path.Combine(directory, "tools", "chocolateyInstall.ps1"),
                "$ErrorActionPreference = 'Stop'\n" +
                "$packageArgs = @{\n" +
                "  packageName    = '" + name + "'\n" +
                "  fileType       = 'exe'\n" +
                "  url64bit       = '" + context.ReleaseAssetUrl(file) + "'\n" +
                "  checksum64     = '" + sha + "'\n" +
                "  checksumType64 = 'sha256'\n" +
                "  silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'\n" +
                "  validExitCodes = @(0)\n" +
                "}\n" +
                "Install-ChocolateyPackage @packageArgs\n");
            Console.WriteLine("[choco] wrote the package sources in " + directory);
            return new[] { nuspecPath };
        }
    }
}
