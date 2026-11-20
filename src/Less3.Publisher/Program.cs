namespace Less3.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Less3.Publisher.Channels;
    using Less3.Publisher.Manifest;

    /// <summary>
    /// Entry point. Reads publisher.json and runs one or more channels; see src/Less3.Publisher/README.md.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Runs the Publisher.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>0 on success, 2 for a usage or configuration error, otherwise the failing step's exit code.</returns>
        public static int Main(string[] args)
        {
            try
            {
                PublisherOptions options = PublisherOptions.Parse(args);
                if (options.Help)
                {
                    PrintUsage();
                    return 0;
                }

                string configPath = Path.GetFullPath(options.Config);
                PublisherManifest manifest = PublisherManifest.Load(configPath);
                string repoRoot = Path.GetDirectoryName(configPath) ?? Environment.CurrentDirectory;
                string version = options.Version ?? VersionReader.Read(repoRoot);
                if (options.PrintVersion)
                {
                    Console.WriteLine(version);
                    return 0;
                }

                if (options.List)
                {
                    List(manifest);
                    return 0;
                }

                List<string> channels = ExpandChannels(options.Channels);
                if (channels.Count == 0 && !options.Sums)
                {
                    Console.Error.WriteLine("Nothing to do. Pass --channel <id>[,<id>...] (or --channel installers), or --list.");
                    return 2;
                }

                string output = Path.GetFullPath(options.Output ?? Path.Combine(repoRoot, "installers", version));
                Directory.CreateDirectory(output);
                string staging = Path.Combine(output, ".staging");
                Dictionary<string, Payload> payloads = new Dictionary<string, Payload>(StringComparer.OrdinalIgnoreCase);
                List<string> produced = new List<string>();

                foreach (string id in channels)
                {
                    produced.AddRange(RunChannel(id, manifest, repoRoot, version, output, staging, options, payloads));
                }

                if (!options.KeepStaging && Directory.Exists(staging)) Directory.Delete(staging, true);
                if (options.Sums) produced.Add(Checksums.WriteSums(output));

                Console.WriteLine();
                Console.WriteLine("Done. " + produced.Count + " file(s) in " + output + ":");
                foreach (string path in produced) Console.WriteLine("  " + Path.GetFileName(path));
                return 0;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                return 2;
            }
            catch (InvalidDataException ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                return 2;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message + " (" + ex.FileName + ")");
                return 2;
            }
            catch (PublisherException ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                return 1;
            }
            catch (ProcessFailedException ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                return ex.ExitCode > 0 ? ex.ExitCode : 1;
            }
        }

        /// <summary>
        /// Expands "installers" to the host's installer channels and keeps the rest in order.
        /// </summary>
        /// <param name="requested">Requested channel ids.</param>
        /// <returns>Channel ids to run.</returns>
        public static List<string> ExpandChannels(IEnumerable<string> requested)
        {
            ArgumentNullException.ThrowIfNull(requested);
            List<string> result = new List<string>();
            foreach (string id in requested)
            {
                if (String.Equals(id, "installers", StringComparison.OrdinalIgnoreCase)) result.AddRange(ChannelRegistry.InstallerChannelsFor(RuntimeTarget.HostOs()));
                else result.Add(id);
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// The runtimes a channel builds: the --rid override, the channel's own list, or the manifest's runtimes for
        /// the channel's operating system.
        /// </summary>
        /// <param name="manifest">Manifest.</param>
        /// <param name="settings">Channel settings.</param>
        /// <param name="channel">Channel.</param>
        /// <param name="rid">Override, or null.</param>
        /// <returns>Targets.</returns>
        public static List<RuntimeTarget> ResolveTargets(PublisherManifest manifest, ChannelSettings settings, IChannel channel, string? rid)
        {
            ArgumentNullException.ThrowIfNull(manifest);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(channel);
            if (!String.IsNullOrEmpty(rid)) return new List<RuntimeTarget> { RuntimeTarget.Parse(rid) };
            IEnumerable<string> rids = settings.Runtimes.Count > 0 ? settings.Runtimes : manifest.Build.Runtimes;
            OsFamilyEnum os = channel.TargetOs == OsFamilyEnum.Any ? RuntimeTarget.HostOs() : channel.TargetOs;
            return rids.Select(RuntimeTarget.Parse).Where(target => target.Os == os).ToList();
        }

        private static IReadOnlyList<string> RunChannel(string id, PublisherManifest manifest, string repoRoot, string version, string output, string staging, PublisherOptions options, Dictionary<string, Payload> payloads)
        {
            IChannel? channel = ChannelRegistry.Resolve(id);
            if (channel == null) throw new ArgumentException("Unknown channel '" + id + "'. Known channels: " + String.Join(", ", ChannelRegistry.KnownIds) + ".");
            if (!manifest.Channels.TryGetValue(id, out ChannelSettings? settings)) throw new ArgumentException("Channel '" + id + "' is not declared in publisher.json.");
            if (!settings.Enabled) throw new ArgumentException("Channel '" + id + "' is disabled in publisher.json.");

            OsFamilyEnum host = RuntimeTarget.HostOs();
            if (channel.RequiredHost != OsFamilyEnum.Any && channel.RequiredHost != host)
            {
                throw new PublisherException("Channel '" + id + "' must run on " + channel.RequiredHost + ", and this is " + host + ". Run it on the matching runner in .github/workflows/release.yml.");
            }

            List<string> missing = channel.Tools.Where(tool => !ProcessRunner.IsAvailable(tool.Command, tool.Probe)).Select(tool => "  " + tool.Command + ": " + tool.InstallHint).ToList();
            if (missing.Count > 0) throw new PublisherException("Channel '" + id + "' needs tools that are not installed:\n" + String.Join("\n", missing));

            ArtifactSettings artifact = manifest.FindArtifact(settings.Artifact) ?? throw new ArgumentException("Channel '" + id + "' references unknown artifact '" + settings.Artifact + "'.");
            List<RuntimeTarget> targets = ResolveTargets(manifest, settings, channel, options.Rid);
            if (targets.Count == 0) throw new ArgumentException("Channel '" + id + "' has no runtimes to build on this host.");

            if (!payloads.TryGetValue(artifact.Id, out Payload? payload))
            {
                payload = new Payload(repoRoot, artifact, manifest.Build.Framework, version, Path.Combine(staging, "payload"));
                payloads[artifact.Id] = payload;
            }

            string channelStaging = FileSystemUtil.Reset(Path.Combine(staging, id));
            ChannelContext context = new ChannelContext(repoRoot, manifest, settings, artifact, version, targets, output, channelStaging, payload);
            Console.WriteLine();
            Console.WriteLine("== " + channel.DisplayName + " (" + id + ") ==");
            Console.WriteLine("   version  : " + version);
            Console.WriteLine("   runtimes : " + String.Join(", ", targets.Select(item => item.Rid)));
            Console.WriteLine("   output   : " + output);
            return channel.Build(context);
        }

        private static void List(PublisherManifest manifest)
        {
            Console.WriteLine("Channels (installers first):");
            foreach (string id in ChannelRegistry.KnownIds)
            {
                IChannel? channel = ChannelRegistry.Resolve(id);
                string state = manifest.Channels.TryGetValue(id, out ChannelSettings? settings) ? (settings.Enabled ? "enabled" : "disabled") : "not declared";
                string host = channel == null || channel.RequiredHost == OsFamilyEnum.Any ? "any host" : "on " + channel.RequiredHost;
                Console.WriteLine("  " + id.PadRight(11) + state.PadRight(13) + host.PadRight(13) + (channel is StubChannel ? "(not implemented) " : "") + channel?.DisplayName);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Less3 publisher: builds installers and package-manager manifests from publisher.json.");
            Console.WriteLine();
            Console.WriteLine("  less3-publish --channel <id>[,<id>...] [options]");
            Console.WriteLine();
            Console.WriteLine("  --channel <ids>    Channels to run, comma-separated; 'installers' means every installer this host builds");
            Console.WriteLine("  --rid <rid>        Build one runtime instead of the channel's runtimes");
            Console.WriteLine("  --version <v>      Override the version (default: <Version> in src/Directory.Build.props)");
            Console.WriteLine("  --config <path>    publisher.json to read (default: ./publisher.json)");
            Console.WriteLine("  --output <dir>     Output folder (default: installers/<version>)");
            Console.WriteLine("  --sums             Write SHA256SUMS for the output folder");
            Console.WriteLine("  --keep-staging     Keep the .staging scratch folder");
            Console.WriteLine("  --list             List channels");
            Console.WriteLine("  --print-version    Print the version and exit");
        }
    }
}
