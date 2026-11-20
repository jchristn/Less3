namespace Less3.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Parsed command-line options.
    /// </summary>
    public sealed class PublisherOptions
    {
        /// <summary>
        /// Channels to run, in order. "installers" expands to every installer channel the host can build.
        /// </summary>
        public List<string> Channels { get; set; } = new List<string>();

        /// <summary>
        /// A single runtime identifier to build instead of the channel's runtimes, or null.
        /// </summary>
        public string? Rid { get; set; } = null;

        /// <summary>
        /// Version override, or null to read src/Directory.Build.props.
        /// </summary>
        public string? Version { get; set; } = null;

        /// <summary>
        /// Path to publisher.json. Default ./publisher.json.
        /// </summary>
        public string Config { get; set; } = "publisher.json";

        /// <summary>
        /// Output directory, or null for installers/&lt;version&gt; under the repository root.
        /// </summary>
        public string? Output { get; set; } = null;

        /// <summary>
        /// True to write SHA256SUMS in the output directory after the channels run.
        /// </summary>
        public bool Sums { get; set; } = false;

        /// <summary>
        /// True to keep the .staging scratch folder.
        /// </summary>
        public bool KeepStaging { get; set; } = false;

        /// <summary>
        /// True to list channels and exit.
        /// </summary>
        public bool List { get; set; } = false;

        /// <summary>
        /// True to print the version and exit.
        /// </summary>
        public bool PrintVersion { get; set; } = false;

        /// <summary>
        /// True to print help and exit.
        /// </summary>
        public bool Help { get; set; } = false;

        /// <summary>
        /// Parses arguments.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Options.</returns>
        /// <exception cref="ArgumentException">An option is unknown or missing its value.</exception>
        public static PublisherOptions Parse(IReadOnlyList<string> args)
        {
            ArgumentNullException.ThrowIfNull(args);
            PublisherOptions options = new PublisherOptions();
            if (args.Count == 0) options.Help = true;
            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "--channel":
                        options.Channels.AddRange(Value(args, ref i, arg).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        break;
                    case "--rid": options.Rid = Value(args, ref i, arg); break;
                    case "--version": options.Version = Value(args, ref i, arg); break;
                    case "--config": options.Config = Value(args, ref i, arg); break;
                    case "--output": options.Output = Value(args, ref i, arg); break;
                    case "--sums": options.Sums = true; break;
                    case "--keep-staging": options.KeepStaging = true; break;
                    case "--list": options.List = true; break;
                    case "--print-version": options.PrintVersion = true; break;
                    case "--help":
                    case "-h": options.Help = true; break;
                    default: throw new ArgumentException("Unknown option '" + arg + "'. Run with --help.");
                }
            }

            options.Channels = options.Channels.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return options;
        }

        private static string Value(IReadOnlyList<string> args, ref int index, string name)
        {
            if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException(name + " needs a value.");
            index++;
            return args[index];
        }
    }
}
