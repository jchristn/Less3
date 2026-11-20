namespace Test.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Less3.Publisher;
    using Less3.Publisher.Channels;
    using Less3.Publisher.Manifest;
    using SkiaSharp;
    using Xunit;

    /// <summary>
    /// Asset names, release-note extraction, generated artwork, checksums, publish arguments, package-manager
    /// manifests, and option parsing.
    /// </summary>
    public class ToolTests
    {
        /// <summary>
        /// Every asset keeps the version in its name, and Debian and RPM follow their own conventions.
        /// </summary>
        [Fact]
        public void AssetNamesFollowConventions()
        {
            Assert.Equal("less3-4.1.2-win-arm64-setup.exe", AssetNames.WindowsSetup("less3", "4.1.2", RuntimeTarget.Parse("win-arm64")));
            Assert.Equal("less3-4.1.2-osx-arm64.pkg", AssetNames.MacPackage("less3", "4.1.2", RuntimeTarget.Parse("osx-arm64")));
            Assert.Equal("less3-4.1.2-osx-x64.dmg", AssetNames.MacDiskImage("less3", "4.1.2", RuntimeTarget.Parse("osx-x64")));
            Assert.Equal("less3_4.1.2_amd64.deb", AssetNames.Deb("less3", "4.1.2", RuntimeTarget.Parse("linux-x64")));
            Assert.Equal("less3_4.1.2_arm64.deb", AssetNames.Deb("less3", "4.1.2", RuntimeTarget.Parse("linux-arm64")));
            Assert.Equal("less3-4.1.2-1.x86_64.rpm", AssetNames.Rpm("less3", "4.1.2", RuntimeTarget.Parse("linux-x64")));
            Assert.Equal("less3-4.1.2-1.aarch64.rpm", AssetNames.Rpm("less3", "4.1.2", RuntimeTarget.Parse("linux-arm64")));
            Assert.Equal("less3-4.1.2-aarch64.AppImage", AssetNames.AppImage("less3", "4.1.2", RuntimeTarget.Parse("linux-arm64")));
            Assert.Equal("less3-4.1.2-win-x64.zip", AssetNames.Archive("less3", "4.1.2", RuntimeTarget.Parse("win-x64")));
            Assert.Equal("less3-4.1.2-linux-x64.tar.gz", AssetNames.Archive("less3", "4.1.2", RuntimeTarget.Parse("linux-x64")));
            Assert.False(RuntimeTarget.TryParse("linux-musl-x64", out RuntimeTarget? _));
            Assert.False(RuntimeTarget.TryParse("win-x86", out RuntimeTarget? _));
        }

        /// <summary>
        /// Release notes come from exactly one version's section.
        /// </summary>
        [Fact]
        public void ChangelogSection()
        {
            string text = "# Change Log\r\n\r\n## [Unreleased]\r\n\r\n## [4.1.2] - 2026-11-20\r\n\r\n- New thing.\r\n- Other thing.\r\n\r\n## [4.1.1] - 2026-10-03\r\n\r\n- Old thing.\r\n";
            Assert.Equal("- New thing.\n- Other thing.", ChangelogReader.Section(text, "## [{version}]", "4.1.2"));
            Assert.Equal("- Old thing.", ChangelogReader.Section(text, "## [{version}]", "4.1.1"));
            Assert.Equal("", ChangelogReader.Section(text, "## [{version}]", "4.2.0"));
            Assert.Equal("", ChangelogReader.Section(text, "## [{version}]", "Unreleased"));
            Assert.Equal("", ChangelogReader.Section(text, "## [{version}]", "4.1"));
        }

        /// <summary>
        /// Icons, the Windows .ico, and the Inno Setup wizard bitmaps are generated from the logo at the requested
        /// sizes.
        /// </summary>
        [Fact]
        public void ImageAssetsAndIcon()
        {
            string logo = RepositoryPaths.File(RepositoryPaths.Manifest().Project.Logo);
            string directory = Path.Combine(Path.GetTempPath(), "less3-publisher-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string png = Path.Combine(directory, "icon.png");
                ImageAssets.SquarePng(logo, 48, png);
                using (SKBitmap? bitmap = SKBitmap.Decode(png))
                {
                    Assert.NotNull(bitmap);
                    Assert.Equal(48, bitmap!.Width);
                    Assert.Equal(48, bitmap.Height);
                }

                string bmp = Path.Combine(directory, "wizard.bmp");
                ImageAssets.Banner(logo, 165, 314, 0.7, bmp);
                byte[] bytes = File.ReadAllBytes(bmp);
                int rowBytes = (165 * 3 + 3) & ~3;
                Assert.Equal((byte)'B', bytes[0]);
                Assert.Equal((byte)'M', bytes[1]);
                Assert.Equal(165, BitConverter.ToInt32(bytes, 18));
                Assert.Equal(314, BitConverter.ToInt32(bytes, 22));
                Assert.Equal(24, BitConverter.ToInt16(bytes, 28));
                Assert.Equal(54 + rowBytes * 314, bytes.Length);
                Assert.Equal(new byte[] { 255, 255, 255 }, bytes.Skip(54).Take(3));

                string ico = Path.Combine(directory, "setup.ico");
                IcoWriter.Write(logo, ico);
                byte[] icon = File.ReadAllBytes(ico);
                Assert.Equal(0, BitConverter.ToInt16(icon, 0));
                Assert.Equal(1, BitConverter.ToInt16(icon, 2));
                Assert.Equal(IcoWriter.Sizes.Count, BitConverter.ToInt16(icon, 4));
                for (int i = 0; i < IcoWriter.Sizes.Count; i++)
                {
                    int entry = 6 + 16 * i;
                    int size = IcoWriter.Sizes[i];
                    Assert.Equal(size >= 256 ? 0 : size, icon[entry]);
                    int length = BitConverter.ToInt32(icon, entry + 8);
                    int offset = BitConverter.ToInt32(icon, entry + 12);
                    Assert.True(offset + length <= icon.Length, "Icon entry " + size + " points past the end of the file.");
                    Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, icon.Skip(offset).Take(4));
                    using (SKBitmap? image = SKBitmap.Decode(icon.Skip(offset).Take(length).ToArray()))
                    {
                        Assert.Equal(size, image!.Width);
                    }
                }
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// SHA256SUMS lists every file in name order in the format sha256sum -c reads, and never lists itself.
        /// </summary>
        [Fact]
        public void ChecksumsFile()
        {
            string directory = Path.Combine(Path.GetTempPath(), "less3-publisher-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllText(Path.Combine(directory, "b.txt"), "b");
                File.WriteAllText(Path.Combine(directory, "a.txt"), "a");
                string sums = File.ReadAllText(Checksums.WriteSums(directory));
                Assert.Equal("ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb  a.txt\n3e23e8160039594a33894f6564e1b1348bbd7a0088d42c4acb73eeaed59c009d  b.txt\n", sums);
                Checksums.WriteSums(directory);
                Assert.DoesNotContain(Checksums.SumsFileName, File.ReadAllText(Path.Combine(directory, Checksums.SumsFileName)), StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// The payload is a self-contained single file named by the manifest's assembly name, stamped with the version.
        /// </summary>
        [Fact]
        public void PublishArguments()
        {
            Payload payload = new Payload("/repo", RepositoryPaths.Server(), "net10.0", "4.1.2", "/stage");
            List<string> arguments = payload.PublishArguments("/repo/src/Less3/Less3.csproj", RuntimeTarget.Parse("linux-arm64"), "/stage/out");
            foreach (string expected in new[] { "publish", "-r", "linux-arm64", "-f", "net10.0", "-p:Version=4.1.2", "-p:AssemblyName=less3", "-p:PublishSingleFile=true", "-p:IncludeNativeLibrariesForSelfExtract=true", "-p:DebugType=none" })
            {
                Assert.Contains(expected, arguments);
            }

            Assert.Equal("true", arguments[arguments.IndexOf("--self-contained") + 1]);
            Assert.Equal("/stage/out", arguments[arguments.IndexOf("-o") + 1]);
        }

        /// <summary>
        /// The Homebrew formula installs the command from the macOS archives, and the Scoop manifest puts it on PATH.
        /// </summary>
        [Fact]
        public void PackageManagerManifests()
        {
            Assert.Equal("Less3", BrewFormulaChannel.ClassName("less3"));
            Assert.Equal("MyTool", BrewFormulaChannel.ClassName("my-tool"));

            string directory = Path.Combine(Path.GetTempPath(), "less3-publisher-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                PublisherManifest manifest = RepositoryPaths.Manifest();
                ArtifactSettings server = RepositoryPaths.Server();
                foreach (string rid in new[] { "osx-arm64", "osx-x64", "win-x64", "win-arm64" })
                {
                    File.WriteAllText(Path.Combine(directory, AssetNames.Archive("less3", "4.1.2", RuntimeTarget.Parse(rid))), rid);
                }

                ChannelContext brewContext = Context(manifest, server, "brew", "osx-arm64,osx-x64", directory);
                string formula = File.ReadAllText(new BrewFormulaChannel().Build(brewContext).Single());
                foreach (string expected in new[] { "class Less3 < Formula", "version \"4.1.2\"", "license \"MIT\"", "https://github.com/jchristn/Less3/releases/download/v4.1.2/less3-4.1.2-osx-arm64.tar.gz", "https://github.com/jchristn/Less3/releases/download/v4.1.2/less3-4.1.2-osx-x64.tar.gz", "libexec.install Dir[\"*\"]", "bin.install_symlink libexec/\"less3\"", "on_arm do", "on_intel do" })
                {
                    Assert.Contains(expected, formula, StringComparison.Ordinal);
                }

                Assert.Contains("sha256 \"" + Checksums.Sha256(Path.Combine(directory, "less3-4.1.2-osx-arm64.tar.gz")) + "\"", formula, StringComparison.Ordinal);

                ChannelContext scoopContext = Context(manifest, server, "scoop", "win-x64,win-arm64", directory);
                string scoop = File.ReadAllText(new ScoopChannel().Build(scoopContext).Single());
                Assert.Contains("\"bin\": [\n    \"less3.exe\"\n  ]", scoop.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
                Assert.Contains("\"extract_dir\": \"less3-4.1.2-win-arm64\"", scoop, StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// Options and runtime selection parse as documented.
        /// </summary>
        [Fact]
        public void OptionsAndRuntimes()
        {
            PublisherOptions options = PublisherOptions.Parse(new[] { "--channel", "debrpm, appimage", "--channel", "archive,debrpm", "--sums", "--version", "9.9.9" });
            Assert.Equal(new[] { "debrpm", "appimage", "archive" }, options.Channels);
            Assert.True(options.Sums);
            Assert.Equal("9.9.9", options.Version);
            Assert.Throws<ArgumentException>(() => PublisherOptions.Parse(new[] { "--channel" }));
            Assert.Throws<ArgumentException>(() => PublisherOptions.Parse(new[] { "--bogus" }));
            Assert.Throws<ArgumentException>(() => PublisherOptions.Parse(new[] { "--clean" }));
            Assert.True(PublisherOptions.Parse(Array.Empty<string>()).Help);

            PublisherManifest manifest = RepositoryPaths.Manifest();
            Assert.Equal(new[] { "win-x64", "win-arm64" }, Program.ResolveTargets(manifest, manifest.Channels["inno"], new InnoChannel(), null).Select(item => item.Rid));
            Assert.Equal(new[] { "osx-x64", "osx-arm64" }, Program.ResolveTargets(manifest, manifest.Channels["macos"], new MacChannel(), null).Select(item => item.Rid));
            Assert.Equal("linux-arm64", Program.ResolveTargets(manifest, manifest.Channels["debrpm"], new DebRpmChannel(), "linux-arm64").Single().Rid);
            Assert.All(Program.ResolveTargets(manifest, manifest.Channels["archive"], new ArchiveChannel(), null), item => Assert.Equal(RuntimeTarget.HostOs(), item.Os));
            Assert.Equal(ChannelRegistry.InstallerChannelsFor(RuntimeTarget.HostOs()), Program.ExpandChannels(new[] { "installers", "archive" }));
            Assert.IsType<StubChannel>(ChannelRegistry.Resolve("snap"));
            Assert.Null(ChannelRegistry.Resolve("dotnettool"));
        }

        private static ChannelContext Context(PublisherManifest manifest, ArtifactSettings artifact, string channel, string rids, string directory)
        {
            List<RuntimeTarget> targets = rids.Split(',').Select(RuntimeTarget.Parse).ToList();
            Payload payload = new Payload(directory, artifact, "net10.0", "4.1.2", Path.Combine(directory, ".staging"));
            return new ChannelContext(RepositoryPaths.Root(), manifest, manifest.Channels[channel], artifact, "4.1.2", targets, directory, Path.Combine(directory, ".staging", channel), payload);
        }
    }
}
