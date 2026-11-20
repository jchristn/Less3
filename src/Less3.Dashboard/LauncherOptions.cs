namespace Less3.Dashboard
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Command-line options for less3-dashboard. Each option falls back to an environment variable, so a service
    /// manager can configure the dashboard without changing its command line.
    /// </summary>
    public sealed class LauncherOptions
    {
        /// <summary>
        /// Environment variable holding the default listen address.
        /// </summary>
        public const string HostVariable = "LESS3_DASHBOARD_HOST";

        /// <summary>
        /// Environment variable holding the default listen port.
        /// </summary>
        public const string PortVariable = "LESS3_DASHBOARD_PORT";

        private int _Port = 3000;

        /// <summary>
        /// Address the dashboard listens on. Default 127.0.0.1, so only this machine can reach it; use 0.0.0.0 to
        /// listen on every interface.
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// Port the dashboard listens on. Default 3000. Minimum 1, maximum 65535.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is outside 1 to 65535.</exception>
        public int Port
        {
            get { return _Port; }
            set
            {
                if (value < 1 || value > 65535) throw new ArgumentOutOfRangeException(nameof(value), value, "The port must be between 1 and 65535.");
                _Port = value;
            }
        }

        /// <summary>
        /// True to open the dashboard in the default browser once it answers. Default false.
        /// </summary>
        public bool Open { get; set; } = false;

        /// <summary>
        /// True to print the version and exit.
        /// </summary>
        public bool Version { get; set; } = false;

        /// <summary>
        /// True to print usage and exit.
        /// </summary>
        public bool Help { get; set; } = false;

        /// <summary>
        /// The URL a browser on this machine uses to reach the dashboard. A wildcard listen address is reached
        /// through 127.0.0.1.
        /// </summary>
        public string LocalUrl
        {
            get
            {
                string host = Host == "0.0.0.0" || Host == "::" || Host == "*" ? "127.0.0.1" : Host;
                if (host.Contains(':', StringComparison.Ordinal) && !host.StartsWith("[", StringComparison.Ordinal)) host = "[" + host + "]";
                return "http://" + host + ":" + Port.ToString(CultureInfo.InvariantCulture) + "/";
            }
        }

        /// <summary>
        /// Parses arguments, starting from the environment variables' values.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <param name="environment">Reads an environment variable; null when it is not set.</param>
        /// <returns>Options.</returns>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        /// <exception cref="ArgumentException">An option is unknown, is missing its value, or has an invalid value.</exception>
        public static LauncherOptions Parse(IReadOnlyList<string> args, Func<string, string?> environment)
        {
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(environment);
            LauncherOptions options = new LauncherOptions();
            string? host = environment(HostVariable);
            if (!String.IsNullOrWhiteSpace(host)) options.Host = host.Trim();
            string? port = environment(PortVariable);
            if (!String.IsNullOrWhiteSpace(port)) options.Port = ParsePort(port, PortVariable);

            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "--host":
                        options.Host = Value(args, ref i, arg);
                        break;
                    case "--port":
                        options.Port = ParsePort(Value(args, ref i, arg), arg);
                        break;
                    case "--open":
                        options.Open = true;
                        break;
                    case "--version":
                        options.Version = true;
                        break;
                    case "--help":
                    case "-h":
                    case "-?":
                        options.Help = true;
                        break;
                    default:
                        throw new ArgumentException("Unknown option '" + arg + "'. Run less3-dashboard --help.");
                }
            }

            return options;
        }

        private static string Value(IReadOnlyList<string> args, ref int index, string name)
        {
            if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException(name + " needs a value.");
            index++;
            return args[index];
        }

        private static int ParsePort(string value, string source)
        {
            if (!Int32.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int port) || port < 1 || port > 65535) throw new ArgumentException(source + " must be a port number from 1 to 65535, not '" + value + "'.");
            return port;
        }
    }
}
