namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// apt and yum repository trees built from the .deb and .rpm files, ready to publish on GitHub Pages. apt and dnf
    /// reject unsigned repositories, so the metadata is GPG-signed with a self-generated key when Linux signing is
    /// configured. Runs on Linux.
    /// </summary>
    public sealed class AptYumChannel : IChannel
    {
        /// <inheritdoc/>
        public string Id => "aptyum";

        /// <inheritdoc/>
        public string DisplayName => "apt and yum repositories";

        /// <inheritdoc/>
        public OsFamilyEnum RequiredHost => OsFamilyEnum.Linux;

        /// <inheritdoc/>
        public OsFamilyEnum TargetOs => OsFamilyEnum.Linux;

        /// <inheritdoc/>
        public IReadOnlyList<ToolRequirement> Tools => new[]
        {
            new ToolRequirement("dpkg-scanpackages", "--version", "sudo apt-get install -y dpkg-dev"),
            new ToolRequirement("apt-ftparchive", "--version", "sudo apt-get install -y apt-utils"),
            new ToolRequirement("createrepo_c", "--version", "sudo apt-get install -y createrepo-c"),
            new ToolRequirement("gpg", "--version", "sudo apt-get install -y gnupg")
        };

        /// <inheritdoc/>
        public IReadOnlyList<string> Build(ChannelContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            string[] debs = Directory.GetFiles(context.OutputDir, "*.deb");
            string[] rpms = Directory.GetFiles(context.OutputDir, "*.rpm");
            if (debs.Length == 0 && rpms.Length == 0) throw new PublisherException("No .deb or .rpm files in " + context.OutputDir + ". Run the 'debrpm' channel first.");
            bool sign = context.Manifest.Signing.Linux.IsConfigured;
            if (!sign) Console.WriteLine("[aptyum] Linux signing is not configured: the repository metadata is unsigned, and apt and dnf will refuse it until a GPG key is set up.");

            string repo = FileSystemUtil.Reset(Path.Combine(context.OutputDir, "repo"));
            string apt = Path.Combine(repo, "deb");
            Directory.CreateDirectory(Path.Combine(apt, "pool"));
            foreach (string deb in debs) File.Copy(deb, Path.Combine(apt, "pool", Path.GetFileName(deb)), true);
            Shell("dpkg-scanpackages --multiversion pool > Packages && gzip -kf Packages && apt-ftparchive release . > Release", apt);
            if (sign) Shell("gpg --batch --yes --clearsign -o InRelease Release && gpg --batch --yes -abs -o Release.gpg Release", apt);

            string yum = Path.Combine(repo, "rpm");
            Directory.CreateDirectory(yum);
            foreach (string rpm in rpms) File.Copy(rpm, Path.Combine(yum, Path.GetFileName(rpm)), true);
            ProcessRunner.Run("createrepo_c", new[] { yum });
            if (sign)
            {
                ProcessRunner.Run("gpg", new[] { "--batch", "--yes", "--detach-sign", "--armor", Path.Combine(yum, "repodata", "repomd.xml") });
                Shell("gpg --armor --export > " + context.Project.Name + "-archive-keyring.asc", repo);
            }

            return new[] { apt, yum };
        }

        private static void Shell(string command, string directory)
        {
            ProcessRunner.Run("/bin/sh", new[] { "-c", command }, directory);
        }
    }
}
