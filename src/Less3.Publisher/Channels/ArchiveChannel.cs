namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;

    /// <summary>
    /// Portable archives of the bare self-contained payload, for scripts, CI jobs, and anyone who won't run an
    /// installer: a .zip on Windows and a .tar.gz on macOS and Linux. Builds the host operating system's runtimes, so
    /// macOS executables can be ad-hoc signed.
    /// </summary>
    public sealed class ArchiveChannel : IChannel
    {
        /// <inheritdoc/>
        public string Id => "archive";

        /// <inheritdoc/>
        public string DisplayName => "Portable archives";

        /// <inheritdoc/>
        public OsFamilyEnum RequiredHost => OsFamilyEnum.Any;

        /// <inheritdoc/>
        public OsFamilyEnum TargetOs => OsFamilyEnum.Any;

        /// <inheritdoc/>
        public IReadOnlyList<ToolRequirement> Tools => Array.Empty<ToolRequirement>();

        /// <inheritdoc/>
        public IReadOnlyList<string> Build(ChannelContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            List<string> outputs = new List<string>();
            foreach (RuntimeTarget target in context.Targets)
            {
                if (target.Os == OsFamilyEnum.MacOs && RuntimeTarget.HostOs() != OsFamilyEnum.MacOs) throw new PublisherException("macOS archives must be built on macOS so their executables can be ad-hoc signed.");
                string payload = context.Payload.Publish(target);
                string folderName = AssetNames.ArchiveFolder(context.Project.Name, context.Version, target);
                string stage = context.FreshStaging("archive-" + target.Rid);
                string folder = Path.Combine(stage, folderName);
                FileSystemUtil.CopyTree(payload, folder);
                File.Copy(context.LicensePath(), Path.Combine(folder, Path.GetFileName(context.LicensePath())), true);

                if (target.Os == OsFamilyEnum.MacOs)
                {
                    foreach (string executable in context.Artifact.Executables) Signing.SignMac(Path.Combine(folder, executable), context.Manifest.Signing.MacOs);
                }

                string output = Path.Combine(context.OutputDir, AssetNames.Archive(context.Project.Name, context.Version, target));
                if (File.Exists(output)) File.Delete(output);
                Console.WriteLine("[archive] building " + Path.GetFileName(output));
                if (target.Os == OsFamilyEnum.Windows) ZipFile.CreateFromDirectory(folder, output, CompressionLevel.Optimal, true);
                else ProcessRunner.Run("tar", new[] { "-czf", output, "-C", stage, folderName });
                outputs.Add(output);
            }

            return outputs;
        }
    }
}
