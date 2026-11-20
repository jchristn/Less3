namespace Test.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Less3.Publisher;
    using Less3.Publisher.Channels;
    using Less3.Publisher.Manifest;
    using Xunit;

    /// <summary>
    /// The committed publisher.json, its validation, the single version source, and the release workflow's asset list.
    /// </summary>
    public class ManifestTests
    {
        /// <summary>
        /// publisher.json loads, validates, covers six runtimes, declares every installer channel, and uses the
        /// Lattice AI identifiers.
        /// </summary>
        [Fact]
        public void ManifestIsValid()
        {
            PublisherManifest manifest = RepositoryPaths.Manifest();
            Assert.Empty(manifest.Validate());
            Assert.Equal(new[] { "linux-arm64", "linux-x64", "osx-arm64", "osx-x64", "win-arm64", "win-x64" }, manifest.Build.Runtimes.OrderBy(item => item, StringComparer.Ordinal));
            foreach (string id in new[] { "inno", "macos", "debrpm", "appimage", "archive" })
            {
                Assert.True(manifest.Channels.TryGetValue(id, out ChannelSettings? channel) && channel.Enabled, "Installer channel " + id + " must be declared and enabled.");
            }

            foreach (string id in manifest.Channels.Keys) Assert.NotNull(ChannelRegistry.Resolve(id));
            Assert.False(manifest.Signing.MacOs.IsConfigured || manifest.Signing.Windows.IsConfigured || manifest.Signing.Linux.IsConfigured, "Less3 is not code-signed by policy.");

            Assert.Equal("ai.latticetech.less3", manifest.Project.BundleId);
            Assert.Equal("Lattice AI, LLC", manifest.Project.Publisher);
            Assert.Equal("https://latticetech.ai", manifest.Project.PublisherUrl);
            Assert.False(String.IsNullOrWhiteSpace(manifest.Project.Tagline));
            Assert.True(Guid.TryParse(manifest.Channels["inno"].Options["appId"], out Guid _), "The Inno Setup AppId must be a fixed GUID so upgrades replace in place.");

            ArtifactSettings server = RepositoryPaths.Server();
            Assert.Equal("less3", server.AssemblyName);
            Assert.Equal(new[] { "less3" }, server.PathCommands);
            Assert.NotNull(server.Launcher);
            Assert.NotNull(server.Service);
            Assert.Equal("/var/lib/less3", server.Service!.WorkingDirectory);
            Assert.Contains(server.LinuxDependencies.Deb, item => item.StartsWith("libicu", StringComparison.Ordinal));
            Assert.Contains("libicu", server.LinuxDependencies.Rpm);
            Assert.True(File.Exists(RepositoryPaths.File(manifest.Project.LicenseFile)), "The license the installers show must exist.");
        }

        /// <summary>
        /// The logo every format's artwork is generated from is a square PNG of at least 1024 pixels.
        /// </summary>
        [Fact]
        public void LogoIsLargeAndSquare()
        {
            string logo = RepositoryPaths.File(RepositoryPaths.Manifest().Project.Logo);
            byte[] header = new byte[24];
            using (FileStream stream = File.OpenRead(logo)) stream.ReadExactly(header, 0, header.Length);
            Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, header.Take(4));
            int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            Assert.Equal(width, height);
            Assert.True(width >= 1024, "The logo must be at least 1024x1024; it is " + width + ".");
        }

        /// <summary>
        /// Validation names every kind of mistake instead of failing deep inside a channel.
        /// </summary>
        [Fact]
        public void ValidationCatchesMistakes()
        {
            string json = "{ \"project\": { \"name\": \"x\", \"displayName\": \"X\", \"tagline\": \"T\", \"bundleId\": \"a.b\" }, " +
                "\"build\": { \"framework\": \"net10.0\", \"runtimes\": [\"win-x64\", \"amiga-68k\"], \"artifacts\": [ { \"id\": \"a\", \"csproj\": \"a.csproj\", \"mainExecutable\": \"a\", \"executables\": [\"a\"], \"pathCommands\": [\"b\"], " +
                "\"launcher\": { \"displayName\": \"A\", \"fileName\": \"A\", \"dataFolder\": \"A\" }, \"service\": { \"name\": \"a\", \"user\": \"a\", \"workingDirectory\": \"relative\" } } ] }, " +
                "\"channels\": { \"inno\": { \"enabled\": true, \"artifact\": \"missing\" } } }";
            InvalidDataException ex = Assert.Throws<InvalidDataException>(() => PublisherManifest.Parse(json, "test.json"));
            foreach (string expected in new[] { "amiga-68k", "'b' on PATH", "collides with an executable", "absolute workingDirectory", "unknown artifact 'missing'" })
            {
                Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
            }

            Assert.Throws<InvalidDataException>(() => PublisherManifest.Parse("{ not json", "bad.json"));
        }

        /// <summary>
        /// src/Directory.Build.props holds the version, no project sets its own, and CHANGELOG.md has its section.
        /// </summary>
        [Fact]
        public void VersionHasOneSource()
        {
            string root = RepositoryPaths.Root();
            string version = VersionReader.Read(root);
            Assert.Matches(new Regex("^\\d+\\.\\d+\\.\\d+(-[0-9A-Za-z.]+)?$"), version);
            foreach (string csproj in Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories))
            {
                Assert.False(File.ReadAllText(csproj).Contains("<Version>", StringComparison.Ordinal), Path.GetFileName(csproj) + " sets its own <Version>; only src/Directory.Build.props may.");
            }

            PublisherManifest manifest = RepositoryPaths.Manifest();
            string section = ChangelogReader.ReadSection(Path.Combine(root, manifest.ReleaseNotes.Source), manifest.ReleaseNotes.Heading, version);
            Assert.False(section.Length == 0, "CHANGELOG.md has no non-empty '## [" + version + "]' section; the release job refuses to publish without one.");
            Assert.Contains("## [Unreleased]", File.ReadAllText(Path.Combine(root, "CHANGELOG.md")), StringComparison.Ordinal);
        }

        /// <summary>
        /// The release job's list of expected assets is exactly what the installer channels produce for six runtimes,
        /// and the release notes template has the placeholders the job fills in.
        /// </summary>
        [Fact]
        public void ReleaseWorkflowExpectsEveryAsset()
        {
            const string version = "9.8.7";
            List<string> produced = new List<string>();
            foreach (string rid in RepositoryPaths.Manifest().Build.Runtimes)
            {
                RuntimeTarget target = RuntimeTarget.Parse(rid);
                produced.Add(AssetNames.Archive("less3", version, target));
                switch (target.Os)
                {
                    case OsFamilyEnum.Windows:
                        produced.Add(AssetNames.WindowsSetup("less3", version, target));
                        break;
                    case OsFamilyEnum.MacOs:
                        produced.Add(AssetNames.MacPackage("less3", version, target));
                        produced.Add(AssetNames.MacDiskImage("less3", version, target));
                        break;
                    default:
                        produced.Add(AssetNames.Deb("less3", version, target));
                        produced.Add(AssetNames.Rpm("less3", version, target));
                        produced.Add(AssetNames.AppImage("less3", version, target));
                        break;
                }
            }

            string workflow = File.ReadAllText(RepositoryPaths.File(".github/workflows/release.yml"));
            Match block = Regex.Match(workflow, "expected=\"([^\"]*)\"");
            Assert.True(block.Success, "release.yml has no expected=\"...\" asset list.");
            List<string> expected = block.Groups[1].Value.Replace("${VERSION}", version, StringComparison.Ordinal).Replace("$VERSION", version, StringComparison.Ordinal)
                .Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Assert.Equal(produced.OrderBy(item => item, StringComparer.Ordinal), expected.OrderBy(item => item, StringComparer.Ordinal));
            Assert.Contains("if [ \"$count\" != " + produced.Count + " ]", workflow, StringComparison.Ordinal);
            Assert.Contains("mkdir -p release-assets", workflow, StringComparison.Ordinal);
            Assert.Contains("files: release-assets/*", workflow, StringComparison.Ordinal);
            Assert.False(Directory.Exists(RepositoryPaths.File("release-assets")), "The release job stages into release-assets/, so the repository must not have a folder by that name.");

            string notes = File.ReadAllText(RepositoryPaths.File(".github/release-install-notes.md"));
            Assert.StartsWith("## Installing", notes, StringComparison.Ordinal);
            foreach (string file in new[] { "less3-{version}-win-x64-setup.exe", "less3-{version}-osx-arm64.pkg", "less3_{version}_amd64.deb", "less3-{version}-1.x86_64.rpm", "less3-{version}-x86_64.AppImage" })
            {
                Assert.Contains(file, notes, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Scripts that run on macOS and Linux keep LF line endings on every checkout.
        /// </summary>
        [Fact]
        public void ScriptsKeepUnixLineEndings()
        {
            string attributes = File.ReadAllText(RepositoryPaths.File(".gitattributes"));
            Assert.Contains("*.sh text eol=lf", attributes, StringComparison.Ordinal);
            Assert.DoesNotContain("\r", File.ReadAllText(RepositoryPaths.File("build-installers.sh")), StringComparison.Ordinal);
        }
    }
}
