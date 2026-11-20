namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// macOS installers: for each architecture an ad-hoc-signed app bundle wrapped in a .pkg (the recommended
    /// installer: it shows the license and puts the command on PATH) and a .dmg for
    /// drag-to-Applications installs. Runs on macOS.
    /// </summary>
    public sealed class MacChannel : IChannel
    {
        private static readonly int[] _IconSizes = new[] { 16, 32, 128, 256, 512 };

        /// <inheritdoc/>
        public string Id => "macos";

        /// <inheritdoc/>
        public string DisplayName => "macOS installer package and disk image";

        /// <inheritdoc/>
        public OsFamilyEnum RequiredHost => OsFamilyEnum.MacOs;

        /// <inheritdoc/>
        public OsFamilyEnum TargetOs => OsFamilyEnum.MacOs;

        /// <inheritdoc/>
        public IReadOnlyList<ToolRequirement> Tools => new[]
        {
            new ToolRequirement("pkgbuild", "--help", "xcode-select --install"),
            new ToolRequirement("hdiutil", "help", "xcode-select --install")
        };

        /// <inheritdoc/>
        public IReadOnlyList<string> Build(ChannelContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            List<string> outputs = new List<string>();
            foreach (RuntimeTarget target in context.Targets)
            {
                string payload = context.Payload.Publish(target);
                string bundleStage = context.FreshStaging("bundle-" + target.Rid);
                string app = BuildBundle(context, target, payload, bundleStage);
                outputs.Add(BuildPackage(context, target, app));
                outputs.Add(BuildDiskImage(context, target, app));
            }

            return outputs;
        }

        /// <summary>
        /// Assembles and ad-hoc signs the app bundle for a target.
        /// </summary>
        /// <param name="context">Channel context.</param>
        /// <param name="target">macOS target.</param>
        /// <param name="payload">Published payload folder.</param>
        /// <param name="stage">Folder to build the bundle in.</param>
        /// <returns>The bundle path.</returns>
        public static string BuildBundle(ChannelContext context, RuntimeTarget target, string payload, string stage)
        {
            ArgumentNullException.ThrowIfNull(context);
            string app = Path.Combine(stage, context.Project.DisplayName + ".app");
            string contents = Path.Combine(app, "Contents");
            string macOs = Path.Combine(contents, "MacOS");
            string resources = Path.Combine(contents, "Resources");
            Directory.CreateDirectory(resources);
            Directory.CreateDirectory(macOs);
            // Contents/MacOS holds only the executables; payload folders (a bundled web app and its Node.js runtime)
            // go in Contents/Resources, where codesign seals them as resources and the launcher looks for them.
            foreach (string file in Directory.GetFiles(payload)) File.Copy(file, Path.Combine(macOs, Path.GetFileName(file)), true);
            foreach (string directory in Directory.GetDirectories(payload)) FileSystemUtil.CopyTree(directory, Path.Combine(resources, Path.GetFileName(directory)));

            FileSystemUtil.WriteUnixText(Path.Combine(contents, "Info.plist"), MacBundleFiles.InfoPlist(context.Project, context.Artifact, context.Version));
            File.WriteAllText(Path.Combine(contents, "PkgInfo"), "APPL????");
            File.Copy(context.LicensePath(), Path.Combine(resources, Path.GetFileName(context.LicensePath())), true);
            BuildIcon(context, target, Path.Combine(resources, "AppIcon.icns"));
            if (context.Artifact.Launcher != null)
            {
                string main = Path.Combine(macOs, context.Artifact.Launcher.FileName);
                string command = Path.Combine(resources, context.Artifact.Launcher.FileName + ".command");
                FileSystemUtil.WriteUnixText(main, LauncherScripts.MacBundleMain(context.Artifact));
                FileSystemUtil.WriteUnixText(command, LauncherScripts.Shell(context.Project, context.Artifact, "$(cd \"$(dirname \"$0\")/../MacOS\" && pwd)/" + context.Artifact.MainExecutable, false));
                ProcessRunner.Run("chmod", new[] { "755", main, command });
            }

            string uninstall = Path.Combine(resources, "uninstall.sh");
            FileSystemUtil.WriteUnixText(uninstall, MacBundleFiles.UninstallScript(context.Project, context.Artifact));
            ProcessRunner.Run("chmod", new[] { "755", uninstall });
            ProcessRunner.Run("plutil", new[] { "-lint", Path.Combine(contents, "Info.plist") });

            Signing.SignMac(app, context.Manifest.Signing.MacOs);
            return app;
        }

        private static void BuildIcon(ChannelContext context, RuntimeTarget target, string icns)
        {
            string iconset = context.FreshStaging("AppIcon-" + target.Rid + ".iconset");
            string logo = context.LogoPath();
            foreach (int size in _IconSizes)
            {
                ImageAssets.SquarePng(logo, size, Path.Combine(iconset, "icon_" + size + "x" + size + ".png"));
                ImageAssets.SquarePng(logo, size * 2, Path.Combine(iconset, "icon_" + size + "x" + size + "@2x.png"));
            }

            ProcessRunner.Run("iconutil", new[] { "--convert", "icns", iconset, "--output", icns });
        }

        private static string BuildPackage(ChannelContext context, RuntimeTarget target, string app)
        {
            string root = context.FreshStaging("pkgroot-" + target.Rid);
            string applications = Path.Combine(root, "Applications");
            Directory.CreateDirectory(applications);
            ProcessRunner.Run("ditto", new[] { app, Path.Combine(applications, Path.GetFileName(app)) });

            string scripts = context.FreshStaging("pkgscripts-" + target.Rid);
            WriteScript(Path.Combine(scripts, "postinstall"), MacBundleFiles.PostinstallScript(context.Project, context.Artifact));

            // Installer.app installs a relocatable bundle wherever another copy with the same identifier already
            // lives (for example ~/Applications from a source install), so the component is marked non-relocatable.
            string componentDir = context.FreshStaging("component-" + target.Rid);
            string componentPlist = Path.Combine(componentDir, "component.plist");
            ProcessRunner.Run("pkgbuild", new[] { "--analyze", "--root", root, componentPlist });
            ProcessRunner.Run("/usr/libexec/PlistBuddy", new[] { "-c", "Set :0:BundleIsRelocatable false", componentPlist });

            string component = Path.Combine(componentDir, "component.pkg");
            ProcessRunner.Run("pkgbuild", new[]
            {
                "--root", root,
                "--component-plist", componentPlist,
                "--identifier", MacBundleFiles.PackageId(context.Project),
                "--version", context.Version,
                "--scripts", scripts,
                "--install-location", "/",
                component
            });
            File.Delete(componentPlist);

            string resources = context.FreshStaging("pkgresources-" + target.Rid);
            File.Copy(context.LicensePath(), Path.Combine(resources, "LICENSE.txt"), true);
            ImageAssets.SquarePng(context.LogoPath(), 160, Path.Combine(resources, "background.png"));
            File.WriteAllText(Path.Combine(resources, "welcome.html"), MacBundleFiles.WelcomeHtml(context.Project, context.Artifact, context.Version));
            string distribution = Path.Combine(context.StagingDir, "distribution-" + target.Rid + ".xml");
            File.WriteAllText(distribution, MacBundleFiles.DistributionXml(context.Project, context.Version, target, "component.pkg"));

            string package = Path.Combine(context.OutputDir, AssetNames.MacPackage(context.Project.Name, context.Version, target));
            if (File.Exists(package)) File.Delete(package);
            ProcessRunner.Run("productbuild", new[] { "--distribution", distribution, "--package-path", componentDir, "--resources", resources, package });
            Signing.NotarizeMac(package, context.Manifest.Signing.MacOs);
            return package;
        }

        private static string BuildDiskImage(ChannelContext context, RuntimeTarget target, string app)
        {
            string stage = context.FreshStaging("dmg-" + target.Rid);
            ProcessRunner.Run("ditto", new[] { app, Path.Combine(stage, Path.GetFileName(app)) });
            ProcessRunner.Run("ln", new[] { "-s", "/Applications", Path.Combine(stage, "Applications") });
            if (!context.Manifest.Signing.MacOs.IsConfigured)
            {
                File.WriteAllText(Path.Combine(stage, "If " + context.Project.DisplayName + " won't open.txt"), InstallerText.GatekeeperNote(context.Project, context.Artifact));
            }

            // hdiutil underestimates -srcfolder sizes and fails with "No space left on device" for large bundles:
            // size it explicitly at the payload plus 25%, plus 64 MB. UDZO compression keeps the shipped file small.
            long megabytes = FileSystemUtil.Size(stage) * 125 / 100 / (1024 * 1024) + 64;
            string image = Path.Combine(context.OutputDir, AssetNames.MacDiskImage(context.Project.Name, context.Version, target));
            if (File.Exists(image)) File.Delete(image);
            ProcessRunner.Run("hdiutil", new[]
            {
                "create", "-volname", context.Project.DisplayName, "-srcfolder", stage,
                "-size", megabytes + "m", "-fs", "HFS+", "-format", "UDZO", "-ov", image
            });
            Signing.NotarizeMac(image, context.Manifest.Signing.MacOs);
            return image;
        }

        private static void WriteScript(string path, string text)
        {
            FileSystemUtil.WriteUnixText(path, text);
            ProcessRunner.Run("chmod", new[] { "755", path });
        }
    }
}
