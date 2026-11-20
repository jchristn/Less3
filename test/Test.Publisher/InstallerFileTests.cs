namespace Test.Publisher
{
    using System;
    using System.Collections.Generic;

    using Less3.Publisher;
    using Less3.Publisher.Channels;
    using Less3.Publisher.Manifest;
    using Xunit;

    /// <summary>
    /// The files each installer format is built from: the Inno Setup script, the macOS bundle and package files, the
    /// Linux package files, and the launcher scripts.
    /// </summary>
    public class InstallerFileTests
    {
        /// <summary>
        /// The Windows installer shows the logo, tagline, and license, adds and removes the PATH entry, and adds a
        /// Start menu entry that runs the launcher with the server's icon.
        /// </summary>
        [Fact]
        public void InnoScript()
        {
            PublisherManifest manifest = RepositoryPaths.Manifest();
            InnoScriptInput input = new InnoScriptInput
            {
                AppId = manifest.Channels["inno"].Options["appId"],
                Project = manifest.Project,
                Artifact = RepositoryPaths.Server(),
                Version = "4.1.2",
                Target = RuntimeTarget.Parse("win-arm64"),
                PayloadDir = "C:\\stage\\payload",
                OutputDir = "C:\\out",
                OutputBaseName = "less3-4.1.2-win-arm64-setup",
                WizardImages = new List<string> { "wizard-100.bmp", "wizard-200.bmp" },
                WizardSmallImages = new List<string> { "wizard-small-100.bmp" }
            };
            string script = Less3.Publisher.Channels.InnoScript.Render(input);
            foreach (string expected in new[]
            {
                "AppId={{" + input.AppId + "}", "AppName=Less3\n", "AppPublisher=Lattice AI, LLC\n", "AppPublisherURL=https://latticetech.ai\n",
                "LicenseFile=LICENSE.txt", "SetupIconFile=setup.ico", "WizardImageFile=wizard-100.bmp,wizard-200.bmp", "WizardSmallImageFile=wizard-small-100.bmp",
                "ArchitecturesAllowed=arm64", "ChangesEnvironment=yes", "UninstallDisplayIcon={app}\\less3.exe", "WelcomeLabel2=" + manifest.Project.Tagline,
                "The less3 command, added to PATH", "%%USERPROFILE%%\\Less3", "ValueData: \"{olddata};{app}\"; Check: NeedsAddPath",
                "Source: \"start-less3.cmd\"; DestDir: \"{app}\"", "Name: \"{group}\\Less3\"; Filename: \"{app}\\start-less3.cmd\"", "IconFilename: \"{app}\\less3.exe\"",
                "runasoriginaluser shellexec unchecked", "RemovePath(ExpandConstant('{app}'))", "OutputBaseFilename=less3-4.1.2-win-arm64-setup"
            })
            {
                Assert.Contains(expected, script, StringComparison.Ordinal);
            }

            string x64 = Less3.Publisher.Channels.InnoScript.Render(new InnoScriptInput { AppId = "A", Project = manifest.Project, Artifact = RepositoryPaths.Server(), Target = RuntimeTarget.Parse("win-x64") });
            Assert.Contains("ArchitecturesAllowed=x64compatible", x64, StringComparison.Ordinal);
            Assert.Throws<ArgumentException>(() => Less3.Publisher.Channels.InnoScript.Render(new InnoScriptInput { Project = manifest.Project, Artifact = RepositoryPaths.Server() }));
            Assert.Equal("4.1.2.0", Less3.Publisher.Channels.InnoScript.NumericVersion("4.1.2"));
            Assert.Equal("1.4.0.0", Less3.Publisher.Channels.InnoScript.NumericVersion("1.4.0-alpha.2"));
            Assert.Equal("100%% sure", Less3.Publisher.Channels.InnoScript.MessageText("100% sure"));
            Assert.Equal("Z:\\Users\\me\\installers\\4.1.2\\.staging", InnoChannel.WinePath("/Users/me/installers/4.1.2/.staging"));
        }

        /// <summary>
        /// The .pkg shows a welcome page, license, and background, and links less3 into a /usr/local/bin it creates;
        /// the app opens the launcher, and the uninstall script keeps the user's data.
        /// </summary>
        [Fact]
        public void MacPackageFiles()
        {
            PublisherManifest manifest = RepositoryPaths.Manifest();
            ArtifactSettings server = RepositoryPaths.Server();
            string distribution = MacBundleFiles.DistributionXml(manifest.Project, "4.1.2", RuntimeTarget.Parse("osx-x64"), "component.pkg");
            foreach (string expected in new[] { "<title>Less3</title>", "<welcome file=\"welcome.html\"", "<license file=\"LICENSE.txt\"", "<background file=\"background.png\"", "<background-darkAqua", "alignment=\"bottomleft\" scaling=\"none\"", "hostArchitectures=\"x86_64,arm64\"", "ai.latticetech.less3.pkg" })
            {
                Assert.Contains(expected, distribution, StringComparison.Ordinal);
            }

            Assert.Contains("hostArchitectures=\"arm64\"", MacBundleFiles.DistributionXml(manifest.Project, "4.1.2", RuntimeTarget.Parse("osx-arm64"), "component.pkg"), StringComparison.Ordinal);

            string postinstall = MacBundleFiles.PostinstallScript(manifest.Project, server);
            Assert.True(postinstall.IndexOf("/bin/mkdir -p /usr/local/bin", StringComparison.Ordinal) < postinstall.IndexOf("ln -sf", StringComparison.Ordinal), "postinstall must create /usr/local/bin before linking into it.");
            Assert.Contains("/bin/ln -sf \"/Applications/Less3.app/Contents/MacOS/less3\" /usr/local/bin/less3", postinstall, StringComparison.Ordinal);

            string info = MacBundleFiles.InfoPlist(manifest.Project, server, "4.1.2");
            Assert.Contains("<key>CFBundleExecutable</key>\n  <string>start-less3</string>", info, StringComparison.Ordinal);
            Assert.Contains("<key>CFBundleIdentifier</key>\n  <string>ai.latticetech.less3</string>", info, StringComparison.Ordinal);
            Assert.Contains("<key>CFBundleDisplayName</key>\n  <string>Less3</string>", info, StringComparison.Ordinal);
            Assert.Contains("<key>CFBundleIconFile</key>\n  <string>AppIcon</string>", info, StringComparison.Ordinal);

            string uninstall = MacBundleFiles.UninstallScript(manifest.Project, server);
            Assert.Contains("rm -rf \"/Applications/Less3.app\"", uninstall, StringComparison.Ordinal);
            Assert.Contains("pkgutil --forget ai.latticetech.less3.pkg", uninstall, StringComparison.Ordinal);
            Assert.Contains("readlink /usr/local/bin/less3", uninstall, StringComparison.Ordinal);
            Assert.DoesNotContain("rm -rf \"$HOME", uninstall, StringComparison.Ordinal);

            ProjectSettings tricky = new ProjectSettings { DisplayName = "A & B", Tagline = "<b>bold</b>", BundleId = "x.y" };
            string welcome = MacBundleFiles.WelcomeHtml(tricky, server, "1.0.0");
            Assert.Contains("A &amp; B", welcome, StringComparison.Ordinal);
            Assert.Contains("&lt;b&gt;bold&lt;/b&gt;", welcome, StringComparison.Ordinal);
            Assert.Contains("system.json", MacBundleFiles.WelcomeHtml(manifest.Project, server, "4.1.2"), StringComparison.Ordinal);

            string note = InstallerText.GatekeeperNote(manifest.Project, server);
            Assert.Contains("Open Anyway", note, StringComparison.Ordinal);
            Assert.Contains("xattr -dr com.apple.quarantine /Applications/Less3.app", note, StringComparison.Ordinal);
            Assert.Contains("Control-click > Open no longer", note, StringComparison.Ordinal);
        }

        /// <summary>
        /// The menu entry runs the launcher in a terminal; the systemd unit runs the server as its account in its
        /// working folder only once configured; the package scripts create the account and never enable the service.
        /// </summary>
        [Fact]
        public void LinuxPackageFiles()
        {
            PublisherManifest manifest = RepositoryPaths.Manifest();
            ArtifactSettings server = RepositoryPaths.Server();
            Assert.Equal("/opt/less3", LinuxFiles.InstallDir(manifest.Project));
            Assert.Equal("/usr/lib/systemd/system/less3.service", LinuxFiles.UnitPath(server.Service!));

            string entry = LinuxFiles.DesktopEntry(manifest.Project, "Less3", "", "/opt/less3/start-less3.sh");
            foreach (string expected in new[] { "Name=Less3\n", "Exec=/opt/less3/start-less3.sh\n", "Terminal=true\n", "Icon=less3\n", "Comment=" + manifest.Project.Tagline + "\n" })
            {
                Assert.Contains(expected, entry, StringComparison.Ordinal);
            }

            string unit = LinuxFiles.ServiceUnit(manifest.Project, server);
            foreach (string expected in new[] { "User=less3\n", "Group=less3\n", "WorkingDirectory=/var/lib/less3\n", "ExecStart=/opt/less3/less3\n", "ConditionPathExists=/var/lib/less3/system.json\n", "StandardInput=null\n", "WantedBy=multi-user.target\n", "After=network-online.target\n" })
            {
                Assert.Contains(expected, unit, StringComparison.Ordinal);
            }

            string afterInstall = LinuxFiles.AfterInstall(server);
            Assert.True(afterInstall.IndexOf("groupadd --system less3", StringComparison.Ordinal) < afterInstall.IndexOf("useradd --system --gid less3", StringComparison.Ordinal), "The group must exist before the account that uses it.");
            Assert.Contains("--shell /usr/sbin/nologin", afterInstall, StringComparison.Ordinal);
            Assert.Contains("chown less3:less3 /var/lib/less3", afterInstall, StringComparison.Ordinal);
            Assert.Contains("systemctl try-restart less3.service", afterInstall, StringComparison.Ordinal);
            Assert.DoesNotContain("systemctl enable", afterInstall, StringComparison.Ordinal);
            Assert.DoesNotContain("systemctl start", afterInstall, StringComparison.Ordinal);
            Assert.EndsWith("exit 0\n", afterInstall, StringComparison.Ordinal);

            string beforeRemove = LinuxFiles.BeforeRemove(server);
            Assert.Contains("remove|purge|0)", beforeRemove, StringComparison.Ordinal);
            Assert.Contains("systemctl disable --now less3.service", beforeRemove, StringComparison.Ordinal);
            Assert.DoesNotContain("rm ", beforeRemove, StringComparison.Ordinal);
            Assert.DoesNotContain("userdel", beforeRemove, StringComparison.Ordinal);
            Assert.Contains("daemon-reload", LinuxFiles.AfterRemove(server), StringComparison.Ordinal);

            Assert.Equal("#!/bin/sh\nHERE=\"$(dirname \"$(readlink -f \"$0\")\")\"\nexec \"$HERE/usr/bin/less3\" \"$@\"\n", LinuxFiles.AppRun(server));

            ArtifactSettings plain = new ArtifactSettings { Id = "x", MainExecutable = "x" };
            Assert.DoesNotContain("systemctl", LinuxFiles.AfterInstall(plain), StringComparison.Ordinal);
            Assert.Equal("#!/bin/sh\nexit 0\n", LinuxFiles.BeforeRemove(plain));
        }

        /// <summary>
        /// Every launcher creates the per-user data folder, changes into it, and runs the server there.
        /// </summary>
        [Fact]
        public void LauncherScripts()
        {
            PublisherManifest manifest = RepositoryPaths.Manifest();
            ArtifactSettings server = RepositoryPaths.Server();

            string cmd = Less3.Publisher.Channels.LauncherScripts.WindowsCmd(manifest.Project, server);
            Assert.DoesNotContain("\n", cmd.Replace("\r\n", "", StringComparison.Ordinal), StringComparison.Ordinal);
            Assert.Contains("if not exist \"%USERPROFILE%\\Less3\" mkdir \"%USERPROFILE%\\Less3\"\r\n", cmd, StringComparison.Ordinal);
            Assert.Contains("cd /d \"%USERPROFILE%\\Less3\"\r\n", cmd, StringComparison.Ordinal);
            Assert.Contains("\"%~dp0less3.exe\" %*\r\n", cmd, StringComparison.Ordinal);
            Assert.EndsWith("pause\r\n", cmd, StringComparison.Ordinal);

            string linux = Less3.Publisher.Channels.LauncherScripts.Shell(manifest.Project, server, "/opt/less3/less3", true);
            Assert.StartsWith("#!/bin/sh\n", linux, StringComparison.Ordinal);
            Assert.True(linux.IndexOf("program=\"/opt/less3/less3\"", StringComparison.Ordinal) < linux.IndexOf("cd \"$HOME/Less3\"", StringComparison.Ordinal), "The executable path must be set before changing directory.");
            Assert.Contains("mkdir -p \"$HOME/Less3\"", linux, StringComparison.Ordinal);
            Assert.Contains("read -r _", linux, StringComparison.Ordinal);
            Assert.DoesNotContain("exec ", linux, StringComparison.Ordinal);

            string mac = Less3.Publisher.Channels.LauncherScripts.Shell(manifest.Project, server, "$(cd \"$(dirname \"$0\")/../MacOS\" && pwd)/less3", false);
            Assert.Contains("exec \"$program\" \"$@\"", mac, StringComparison.Ordinal);
            Assert.Contains("open -a Terminal \"$(cd \"$(dirname \"$0\")/../Resources\" && pwd)/start-less3.command\"", Less3.Publisher.Channels.LauncherScripts.MacBundleMain(server), StringComparison.Ordinal);

            Assert.Throws<ArgumentException>(() => Less3.Publisher.Channels.LauncherScripts.WindowsCmd(manifest.Project, new ArtifactSettings()));
        }

        /// <summary>
        /// The welcome text lists what each installer adds, per operating system.
        /// </summary>
        [Fact]
        public void WhatGetsInstalled()
        {
            PublisherManifest manifest = RepositoryPaths.Manifest();
            ArtifactSettings server = RepositoryPaths.Server();
            Assert.Contains("The less3 command in /usr/local/bin", InstallerText.WhatGetsInstalled(manifest.Project, server, OsFamilyEnum.MacOs));
            Assert.Contains(InstallerText.WhatGetsInstalled(manifest.Project, server, OsFamilyEnum.MacOs), item => item.StartsWith("Less3.app in Applications", StringComparison.Ordinal));
            Assert.Contains(InstallerText.WhatGetsInstalled(manifest.Project, server, OsFamilyEnum.Linux), item => item.Contains("systemd service", StringComparison.Ordinal) && item.Contains("not enabled", StringComparison.Ordinal));
            Assert.DoesNotContain(InstallerText.WhatGetsInstalled(manifest.Project, server, OsFamilyEnum.Windows), item => item.Contains("systemd", StringComparison.Ordinal));
            Assert.StartsWith(manifest.Project.Tagline + " This installs: the less3 command, added to PATH;", InstallerText.Welcome(manifest.Project, server, OsFamilyEnum.Windows), StringComparison.Ordinal);
        }
    }
}
