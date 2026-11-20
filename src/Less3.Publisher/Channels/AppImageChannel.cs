namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Less3.Publisher.Manifest;

    /// <summary>
    /// Linux AppImage: one distribution-independent file per architecture. Run from a terminal, it runs the server in
    /// the current directory. It never lands on PATH and installs no service; the .deb and .rpm do both. Runs on Linux
    /// with appimagetool.
    /// </summary>
    public sealed class AppImageChannel : IChannel
    {
        /// <inheritdoc/>
        public string Id => "appimage";

        /// <inheritdoc/>
        public string DisplayName => "AppImage";

        /// <inheritdoc/>
        public OsFamilyEnum RequiredHost => OsFamilyEnum.Linux;

        /// <inheritdoc/>
        public OsFamilyEnum TargetOs => OsFamilyEnum.Linux;

        /// <inheritdoc/>
        public IReadOnlyList<ToolRequirement> Tools => new[]
        {
            new ToolRequirement("appimagetool", "--version", "sudo curl -fsSL -o /usr/local/bin/appimagetool https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage && sudo chmod +x /usr/local/bin/appimagetool")
        };

        /// <inheritdoc/>
        public IReadOnlyList<string> Build(ChannelContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ProjectSettings project = context.Project;
            ArtifactSettings artifact = context.Artifact;
            List<string> outputs = new List<string>();
            foreach (RuntimeTarget target in context.Targets)
            {
                string payload = context.Payload.Publish(target);
                string appDir = context.FreshStaging(project.DisplayName + "-" + target.Rid + ".AppDir");
                string bin = Path.Combine(appDir, "usr", "bin");
                FileSystemUtil.CopyTree(payload, bin);
                File.Copy(context.LicensePath(), Path.Combine(appDir, Path.GetFileName(context.LicensePath())), true);

                string entry = LinuxFiles.DesktopEntry(project, project.DisplayName, project.Tagline, artifact.MainExecutable);
                FileSystemUtil.WriteUnixText(Path.Combine(appDir, project.Name + ".desktop"), entry);
                string applications = Path.Combine(appDir, "usr", "share", "applications");
                Directory.CreateDirectory(applications);
                FileSystemUtil.WriteUnixText(Path.Combine(applications, project.Name + ".desktop"), entry);

                string icons = Path.Combine(appDir, "usr", "share", "icons", "hicolor", "256x256", "apps");
                Directory.CreateDirectory(icons);
                string icon = Path.Combine(appDir, project.Name + ".png");
                ImageAssets.SquarePng(context.LogoPath(), 256, icon);
                File.Copy(icon, Path.Combine(icons, project.Name + ".png"), true);
                File.Copy(icon, Path.Combine(appDir, ".DirIcon"), true);

                string appRun = Path.Combine(appDir, "AppRun");
                FileSystemUtil.WriteUnixText(appRun, LinuxFiles.AppRun(artifact));
                ProcessRunner.Run("chmod", new[] { "755", appRun });

                string output = Path.Combine(context.OutputDir, AssetNames.AppImage(project.Name, context.Version, target));
                if (File.Exists(output)) File.Delete(output);

                // appimagetool guesses the architecture from the host, which is wrong for arm64 built on x64, and it
                // needs FUSE, which CI runners lack; ARCH and extract-and-run handle both.
                Dictionary<string, string> environment = new Dictionary<string, string>
                {
                    ["ARCH"] = target.RpmArch,
                    ["APPIMAGE_EXTRACT_AND_RUN"] = "1"
                };
                Console.WriteLine("[appimage] building " + Path.GetFileName(output));
                ProcessRunner.Run("appimagetool", new[] { appDir, output }, context.StagingDir, environment);
                if (!File.Exists(output)) throw new PublisherException("appimagetool finished but " + output + " is missing.");
                outputs.Add(output);
            }

            return outputs;
        }
    }
}
