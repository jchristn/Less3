namespace Test.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Less3.Dashboard;
    using Less3.Publisher;
    using Less3.Publisher.Channels;
    using Less3.Publisher.Manifest;
    using Xunit;

    /// <summary>
    /// The Less3 Dashboard installers: the artifact's own identity, the files rendered for it, the Node.js runtime
    /// download, and the less3-dashboard launcher.
    /// </summary>
    public class DashboardTests
    {
        private static ArtifactSettings Dashboard()
        {
            return RepositoryPaths.Manifest().FindArtifact("dashboard") ?? throw new InvalidOperationException("publisher.json has no dashboard artifact.");
        }

        /// <summary>
        /// The dashboard installs under its own name and identifiers, beside but separate from the server.
        /// </summary>
        [Fact]
        public void DashboardHasItsOwnIdentity()
        {
            PublisherManifest manifest = RepositoryPaths.Manifest();
            ProjectSettings dashboard = manifest.Project.For(Dashboard());
            Assert.Equal("less3-dashboard", dashboard.Name);
            Assert.Equal("Less3 Dashboard", dashboard.DisplayName);
            Assert.Equal("ai.latticetech.less3.dashboard", dashboard.BundleId);
            Assert.Equal("LatticeAI.Less3.Dashboard", dashboard.PackageId);
            Assert.Equal(manifest.Project.Publisher, dashboard.Publisher);
            Assert.Equal(manifest.Project.Logo, dashboard.Logo);
            Assert.NotEqual(manifest.Project.Tagline, dashboard.Tagline);

            ProjectSettings server = manifest.Project.For(RepositoryPaths.Server());
            Assert.Equal("less3", server.Name);
            Assert.Equal(manifest.Project.Tagline, server.Tagline);

            ArtifactSettings artifact = Dashboard();
            Assert.Equal("src/Less3.Dashboard/Less3.Dashboard.csproj", artifact.Csproj);
            Assert.Equal(new[] { "less3-dashboard" }, artifact.PathCommands);
            Assert.NotNull(artifact.NextApp);
            Assert.Equal("dashboard", artifact.NextApp!.Directory);
            Assert.Matches("^22\\.\\d+\\.\\d+$", artifact.NextApp.NodeVersion);
            Assert.Equal("1", artifact.NextApp.BuildEnvironment["LESS3_DASHBOARD_STANDALONE"]);
            Assert.Contains("node_modules/@img", artifact.NextApp.RemoveFromApp);
            Assert.Contains("LESS3_DASHBOARD_STANDALONE", File.ReadAllText(RepositoryPaths.File("dashboard/next.config.mjs")), StringComparison.Ordinal);
        }

        /// <summary>
        /// Two artifacts packaged under one name, or with an inexact Node.js version, are rejected.
        /// </summary>
        [Fact]
        public void ValidationRejectsCollidingArtifacts()
        {
            string json = "{ \"project\": { \"name\": \"x\", \"displayName\": \"X\", \"tagline\": \"T\", \"bundleId\": \"a.b\" }, " +
                "\"build\": { \"framework\": \"net10.0\", \"runtimes\": [\"win-x64\"], \"artifacts\": [ " +
                "{ \"id\": \"one\", \"csproj\": \"a.csproj\", \"mainExecutable\": \"a\", \"executables\": [\"a\"] }, " +
                "{ \"id\": \"two\", \"csproj\": \"b.csproj\", \"mainExecutable\": \"b\", \"executables\": [\"b\"], \"nextApp\": { \"directory\": \"web\", \"nodeVersion\": \"22\" } } ] }, " +
                "\"channels\": { \"inno\": { \"enabled\": true, \"artifacts\": [\"one\", \"two\"] }, \"macos\": { \"enabled\": true, \"artifacts\": [] } } }";
            InvalidDataException ex = Assert.Throws<InvalidDataException>(() => PublisherManifest.Parse(json, "test.json"));
            foreach (string expected in new[] { "would all produce installers named 'x'", "share the identifier 'a.b'", "exact nodeVersion", "Channel 'macos' names no artifact" })
            {
                Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
            }

            ChannelSettings legacy = new ChannelSettings { Artifact = "server" };
            Assert.Equal(new[] { "server" }, legacy.ArtifactIds());
            Assert.Empty(new ChannelSettings().ArtifactIds());
        }

        /// <summary>
        /// The dashboard's launcher opens the browser and does not change directory; its unit runs it as its own
        /// account on 127.0.0.1:3000 with no configuration file to wait for.
        /// </summary>
        [Fact]
        public void DashboardLauncherAndService()
        {
            PublisherManifest manifest = RepositoryPaths.Manifest();
            ArtifactSettings artifact = Dashboard();
            ProjectSettings project = manifest.Project.For(artifact);

            string cmd = LauncherScripts.WindowsCmd(project, artifact);
            Assert.Contains("\"%~dp0less3-dashboard.exe\" --open %*\r\n", cmd, StringComparison.Ordinal);
            Assert.DoesNotContain("cd /d", cmd, StringComparison.Ordinal);
            Assert.DoesNotContain("mkdir", cmd, StringComparison.Ordinal);

            string shell = LauncherScripts.Shell(project, artifact, "/opt/less3-dashboard/less3-dashboard", true);
            Assert.Contains("\"$program\" --open \"$@\"\n", shell, StringComparison.Ordinal);
            Assert.DoesNotContain("cd ", shell, StringComparison.Ordinal);

            string unit = LinuxFiles.ServiceUnit(project, artifact);
            foreach (string expected in new[] { "User=less3-dashboard\n", "WorkingDirectory=/var/lib/less3-dashboard\n", "ExecStart=/opt/less3-dashboard/less3-dashboard\n", "Environment=LESS3_DASHBOARD_HOST=127.0.0.1\n", "Environment=LESS3_DASHBOARD_PORT=3000\n" })
            {
                Assert.Contains(expected, unit, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("ConditionPathExists", unit, StringComparison.Ordinal);
            Assert.True(unit.IndexOf("Environment=", StringComparison.Ordinal) < unit.IndexOf("ExecStart=", StringComparison.Ordinal));

            List<string> windows = InstallerText.WhatGetsInstalled(project, artifact, OsFamilyEnum.Windows);
            Assert.Contains("The less3-dashboard command, added to PATH", windows);
            Assert.Contains(windows, item => item.Contains("opens it in your browser", StringComparison.Ordinal));
            Assert.Contains("Less3 Dashboard.app in Applications, which runs Less3 Dashboard in a Terminal window and opens it in your browser", InstallerText.Welcome(project, artifact, OsFamilyEnum.MacOs), StringComparison.Ordinal);
            Assert.EndsWith(artifact.InstallerNote, InstallerText.Welcome(project, artifact, OsFamilyEnum.Linux), StringComparison.Ordinal);
        }

        /// <summary>
        /// The dashboard's Windows installer is its own program with its own AppId, folder, and Start menu entry.
        /// </summary>
        [Fact]
        public void DashboardInnoScript()
        {
            PublisherManifest manifest = RepositoryPaths.Manifest();
            ArtifactSettings artifact = Dashboard();
            string script = InnoScript.Render(new InnoScriptInput
            {
                AppId = artifact.WindowsAppId,
                Project = manifest.Project.For(artifact),
                Artifact = artifact,
                Version = "4.1.3",
                Target = RuntimeTarget.Parse("win-x64"),
                OutputBaseName = "less3-dashboard-4.1.3-win-x64-setup"
            });
            foreach (string expected in new[]
            {
                "AppId={{" + artifact.WindowsAppId + "}", "AppName=Less3 Dashboard\n", "DefaultDirName={autopf}\\Less3 Dashboard\n", "UninstallDisplayIcon={app}\\less3-dashboard.exe",
                "Name: \"{group}\\Less3 Dashboard\"; Filename: \"{app}\\start-less3-dashboard.cmd\"", "Description: \"Run Less3 Dashboard now\"", "The less3-dashboard command, added to PATH"
            })
            {
                Assert.Contains(expected, script, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("system.json", script, StringComparison.Ordinal);
        }

        /// <summary>
        /// The dashboard payload keeps the launcher's own assembly name, so the executable is less3-dashboard.
        /// </summary>
        [Fact]
        public void DashboardPublishArguments()
        {
            Payload payload = new Payload("/repo", Dashboard(), "net10.0", "4.1.3", "/stage");
            List<string> arguments = payload.PublishArguments("/repo/src/Less3.Dashboard/Less3.Dashboard.csproj", RuntimeTarget.Parse("osx-arm64"), "/stage/out");
            Assert.DoesNotContain(arguments, item => item.StartsWith("-p:AssemblyName=", StringComparison.Ordinal));
            Assert.Contains("-p:PublishSingleFile=true", arguments);
            Assert.Contains("osx-arm64", arguments);
        }

        /// <summary>
        /// Node.js archives are named and verified the way nodejs.org publishes them.
        /// </summary>
        [Fact]
        public void NodeRuntimeNames()
        {
            Assert.Equal("win-arm64", NodeRuntime.Platform(RuntimeTarget.Parse("win-arm64")));
            Assert.Equal("darwin-x64", NodeRuntime.Platform(RuntimeTarget.Parse("osx-x64")));
            Assert.Equal("linux-arm64", NodeRuntime.Platform(RuntimeTarget.Parse("linux-arm64")));
            Assert.Equal("node-v22.23.5-win-x64.zip", NodeRuntime.ArchiveName("22.23.5", RuntimeTarget.Parse("win-x64")));
            Assert.Equal("node-v22.23.5-darwin-arm64.tar.gz", NodeRuntime.ArchiveName("22.23.5", RuntimeTarget.Parse("osx-arm64")));
            Assert.Equal("node-v22.23.5-linux-x64.tar.gz", NodeRuntime.ArchiveName("22.23.5", RuntimeTarget.Parse("linux-x64")));

            string sums = "aaaa  node-v22.23.5-darwin-arm64.tar.gz\nBBBB  node-v22.23.5-linux-x64.tar.gz\ncccc  node-v22.23.5-linux-x64.tar.gz.sig\n";
            Assert.Equal("aaaa", NodeRuntime.ExpectedHash(sums, "node-v22.23.5-darwin-arm64.tar.gz"));
            Assert.Equal("bbbb", NodeRuntime.ExpectedHash(sums, "node-v22.23.5-linux-x64.tar.gz"));
            Assert.Null(NodeRuntime.ExpectedHash(sums, "node-v22.23.5-win-x64.zip"));
            Assert.Null(NodeRuntime.ExpectedHash(sums, "node-v22.23.5-linux"));
        }

        /// <summary>
        /// less3-dashboard reads its address and port from options, then the environment, then defaults.
        /// </summary>
        [Fact]
        public void LauncherOptionsParse()
        {
            Dictionary<string, string> environment = new Dictionary<string, string>();
            LauncherOptions defaults = LauncherOptions.Parse(Array.Empty<string>(), name => environment.TryGetValue(name, out string? value) ? value : null);
            Assert.Equal("127.0.0.1", defaults.Host);
            Assert.Equal(3000, defaults.Port);
            Assert.False(defaults.Open);
            Assert.Equal("http://127.0.0.1:3000/", defaults.LocalUrl);

            environment[LauncherOptions.HostVariable] = "0.0.0.0";
            environment[LauncherOptions.PortVariable] = "4000";
            LauncherOptions fromEnvironment = LauncherOptions.Parse(Array.Empty<string>(), name => environment.TryGetValue(name, out string? value) ? value : null);
            Assert.Equal("0.0.0.0", fromEnvironment.Host);
            Assert.Equal(4000, fromEnvironment.Port);
            Assert.Equal("http://127.0.0.1:4000/", fromEnvironment.LocalUrl);

            LauncherOptions fromArguments = LauncherOptions.Parse(new[] { "--host", "::1", "--port", "5000", "--open" }, name => environment.TryGetValue(name, out string? value) ? value : null);
            Assert.Equal(5000, fromArguments.Port);
            Assert.True(fromArguments.Open);
            Assert.Equal("http://[::1]:5000/", fromArguments.LocalUrl);

            Func<string, string?> none = name => null;
            Assert.Throws<ArgumentException>(() => LauncherOptions.Parse(new[] { "--port", "0" }, none));
            Assert.Throws<ArgumentException>(() => LauncherOptions.Parse(new[] { "--port", "70000" }, none));
            Assert.Throws<ArgumentException>(() => LauncherOptions.Parse(new[] { "--port", "-1" }, none));
            Assert.Throws<ArgumentException>(() => LauncherOptions.Parse(new[] { "--port" }, none));
            Assert.Throws<ArgumentException>(() => LauncherOptions.Parse(new[] { "--bogus" }, none));
            Assert.Throws<ArgumentException>(() => LauncherOptions.Parse(Array.Empty<string>(), name => name == LauncherOptions.PortVariable ? "abc" : null));
            Assert.True(LauncherOptions.Parse(new[] { "--help" }, none).Help);
            Assert.True(LauncherOptions.Parse(new[] { "--version" }, none).Version);
        }

        /// <summary>
        /// less3-dashboard finds app/ and node/ beside itself, or in the Resources folder of its macOS app bundle.
        /// </summary>
        [Fact]
        public void RuntimeLayoutFindsTheBundledApp()
        {
            string root = Path.Combine(Path.GetTempPath(), "less3-dashboard-" + Guid.NewGuid().ToString("N"));
            string node = OperatingSystem.IsWindows() ? "node.exe" : "node";
            try
            {
                string flat = Path.Combine(root, "flat");
                Directory.CreateDirectory(Path.Combine(flat, "app"));
                Directory.CreateDirectory(Path.Combine(flat, "node"));
                Assert.Null(RuntimeLayout.Find(flat));
                File.WriteAllText(Path.Combine(flat, "app", "server.js"), "");
                Assert.Null(RuntimeLayout.Find(flat));
                File.WriteAllText(Path.Combine(flat, "node", node), "");
                RuntimeLayout? found = RuntimeLayout.Find(flat);
                Assert.NotNull(found);
                Assert.Equal(Path.Combine(flat, "app"), found!.AppDirectory);
                Assert.Equal(Path.Combine(flat, "node", node), found.NodeExecutable);

                string contents = Path.Combine(root, "Less3 Dashboard.app", "Contents");
                Directory.CreateDirectory(Path.Combine(contents, "MacOS"));
                Directory.CreateDirectory(Path.Combine(contents, "Resources", "app"));
                Directory.CreateDirectory(Path.Combine(contents, "Resources", "node"));
                File.WriteAllText(Path.Combine(contents, "Resources", "app", "server.js"), "");
                File.WriteAllText(Path.Combine(contents, "Resources", "node", node), "");
                RuntimeLayout? bundled = RuntimeLayout.Find(Path.Combine(contents, "MacOS"));
                Assert.NotNull(bundled);
                Assert.Equal(Path.Combine(contents, "Resources", "app"), bundled!.AppDirectory);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
