namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Less3.Publisher.Manifest;

    /// <summary>
    /// Linux .deb and .rpm packages, both built with fpm from one install tree: the payload under /opt, the PATH
    /// commands linked into /usr/bin, the application menu entry with icons, the systemd unit (installed, not enabled),
    /// and the license. Runs on Linux.
    /// </summary>
    public sealed class DebRpmChannel : IChannel
    {
        /// <inheritdoc/>
        public string Id => "debrpm";

        /// <inheritdoc/>
        public string DisplayName => "Debian and RPM packages";

        /// <inheritdoc/>
        public OsFamilyEnum RequiredHost => OsFamilyEnum.Linux;

        /// <inheritdoc/>
        public OsFamilyEnum TargetOs => OsFamilyEnum.Linux;

        /// <inheritdoc/>
        public IReadOnlyList<ToolRequirement> Tools => new[]
        {
            new ToolRequirement("fpm", "--version", "sudo apt-get install -y ruby ruby-dev rpm && sudo gem install --no-document fpm"),
            new ToolRequirement("rpmbuild", "--version", "sudo apt-get install -y rpm")
        };

        /// <inheritdoc/>
        public IReadOnlyList<string> Build(ChannelContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            List<string> outputs = new List<string>();
            foreach (RuntimeTarget target in context.Targets)
            {
                string root = BuildTree(context, target);
                string scripts = context.FreshStaging("scripts-" + target.Rid);
                PackageScripts paths = new PackageScripts
                {
                    AfterInstall = Path.Combine(scripts, "after-install.sh"),
                    BeforeRemove = Path.Combine(scripts, "before-remove.sh"),
                    AfterRemove = Path.Combine(scripts, "after-remove.sh")
                };
                FileSystemUtil.WriteUnixText(paths.AfterInstall, LinuxFiles.AfterInstall(context.Artifact));
                FileSystemUtil.WriteUnixText(paths.BeforeRemove, LinuxFiles.BeforeRemove(context.Artifact));
                FileSystemUtil.WriteUnixText(paths.AfterRemove, LinuxFiles.AfterRemove(context.Artifact));

                outputs.Add(RunFpm(context, "deb", target.DebArch, root, paths, Path.Combine(context.OutputDir, AssetNames.Deb(context.Project.Name, context.Version, target))));
                outputs.Add(RunFpm(context, "rpm", target.RpmArch, root, paths, Path.Combine(context.OutputDir, AssetNames.Rpm(context.Project.Name, context.Version, target))));
            }

            return outputs;
        }

        /// <summary>
        /// Lays out the install tree for a target.
        /// </summary>
        /// <param name="context">Channel context.</param>
        /// <param name="target">Linux target.</param>
        /// <returns>The tree's root folder.</returns>
        public static string BuildTree(ChannelContext context, RuntimeTarget target)
        {
            ArgumentNullException.ThrowIfNull(context);
            ProjectSettings project = context.Project;
            ArtifactSettings artifact = context.Artifact;
            string payload = context.Payload.Publish(target);
            string root = context.FreshStaging("pkgroot-" + target.Rid);
            string installDir = LinuxFiles.InstallDir(project);
            string opt = Path.Combine(root, installDir.TrimStart('/'));
            FileSystemUtil.CopyTree(payload, opt);

            string bin = Path.Combine(root, "usr", "bin");
            Directory.CreateDirectory(bin);
            foreach (string command in artifact.PathCommands) File.CreateSymbolicLink(Path.Combine(bin, command), installDir + "/" + command);

            if (artifact.Launcher != null)
            {
                string launcher = Path.Combine(opt, artifact.Launcher.FileName + ".sh");
                FileSystemUtil.WriteUnixText(launcher, LauncherScripts.Shell(project, artifact, installDir + "/" + artifact.MainExecutable, true));
                ProcessRunner.Run("chmod", new[] { "755", launcher });

                string applications = Path.Combine(root, "usr", "share", "applications");
                Directory.CreateDirectory(applications);
                FileSystemUtil.WriteUnixText(Path.Combine(applications, project.BundleId + ".desktop"), LinuxFiles.DesktopEntry(project, artifact.Launcher.DisplayName, artifact.Launcher.Comment, installDir + "/" + artifact.Launcher.FileName + ".sh"));
            }

            if (artifact.Service != null)
            {
                string unit = Path.Combine(root, LinuxFiles.UnitPath(artifact.Service).TrimStart('/'));
                Directory.CreateDirectory(Path.GetDirectoryName(unit)!);
                FileSystemUtil.WriteUnixText(unit, LinuxFiles.ServiceUnit(project, artifact));
            }

            string logo = context.LogoPath();
            foreach (int size in LinuxFiles.IconSizes)
            {
                string icons = Path.Combine(root, "usr", "share", "icons", "hicolor", size + "x" + size, "apps");
                Directory.CreateDirectory(icons);
                ImageAssets.SquarePng(logo, size, Path.Combine(icons, project.Name + ".png"));
            }

            string license = context.LicensePath();
            string doc = Path.Combine(root, "usr", "share", "doc", project.Name);
            string licenses = Path.Combine(root, "usr", "share", "licenses", project.Name);
            Directory.CreateDirectory(doc);
            Directory.CreateDirectory(licenses);
            File.Copy(license, Path.Combine(doc, "copyright"), true);
            File.Copy(license, Path.Combine(licenses, Path.GetFileName(license)), true);
            File.Copy(license, Path.Combine(opt, Path.GetFileName(license)), true);
            return root;
        }

        private static string RunFpm(ChannelContext context, string type, string arch, string root, PackageScripts scripts, string output)
        {
            ProjectSettings project = context.Project;
            LinuxDependencySettings dependencies = context.Artifact.LinuxDependencies;
            if (File.Exists(output)) File.Delete(output);
            List<string> arguments = new List<string>
            {
                "-s", "dir", "-t", type,
                "-n", project.Name,
                "-v", context.Version,
                "--iteration", "1",
                "-a", arch,
                "--license", project.License,
                "--vendor", project.Publisher,
                "--maintainer", project.Maintainer,
                "--url", project.Homepage,
                "--description", project.Tagline + "\n" + project.Description,
                "--category", "net",
                "--after-install", scripts.AfterInstall,
                "--before-remove", scripts.BeforeRemove,
                "--after-remove", scripts.AfterRemove,
                "--package", output,
                "-C", root
            };

            foreach (string dependency in type == "deb" ? dependencies.Deb : dependencies.Rpm)
            {
                arguments.Add("-d");
                arguments.Add(dependency);
            }

            if (type == "deb")
            {
                foreach (string recommended in dependencies.DebRecommends)
                {
                    arguments.Add("--deb-recommends");
                    arguments.Add(recommended);
                }
            }
            else
            {
                arguments.Add("--rpm-os");
                arguments.Add("linux");
            }

            foreach (string top in Directory.GetDirectories(root)) arguments.Add(Path.GetFileName(top));
            Console.WriteLine("[debrpm] building " + Path.GetFileName(output));
            ProcessRunner.Run("fpm", arguments, root);
            if (!File.Exists(output)) throw new PublisherException("fpm finished but " + output + " is missing.");
            return output;
        }
    }
}
