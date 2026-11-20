namespace Less3.Dashboard
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// less3-dashboard: starts the Less3 dashboard on the Node.js runtime installed beside it and stops it on Ctrl+C or
    /// a termination signal. Run it with --open to open the dashboard in the default browser once it answers.
    /// </summary>
    public static class Program
    {
        private static readonly CancellationTokenSource _Stopping = new CancellationTokenSource();
        private static Process? _Node = null;
        private static volatile bool _Signaled = false;

        /// <summary>
        /// Runs the launcher.
        /// </summary>
        /// <param name="args">Command-line arguments; see --help.</param>
        /// <returns>The dashboard's exit code, 2 for a usage error, or 1 when the runtime is missing.</returns>
        public static async Task<int> Main(string[] args)
        {
            LauncherOptions options;
            try
            {
                options = LauncherOptions.Parse(args, Environment.GetEnvironmentVariable);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine("less3-dashboard: " + ex.Message);
                return 2;
            }

            if (options.Help)
            {
                PrintUsage();
                return 0;
            }

            if (options.Version)
            {
                Console.WriteLine(VersionText());
                return 0;
            }

            RuntimeLayout? layout = RuntimeLayout.Find(AppContext.BaseDirectory);
            if (layout == null)
            {
                Console.Error.WriteLine("less3-dashboard: the dashboard files were not found beside " + AppContext.BaseDirectory + " (expected app/server.js and node/). Reinstall Less3 Dashboard.");
                return 1;
            }

            ProcessStartInfo start = new ProcessStartInfo(layout.NodeExecutable)
            {
                WorkingDirectory = layout.AppDirectory,
                UseShellExecute = false
            };
            start.ArgumentList.Add("server.js");
            start.Environment["PORT"] = options.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            start.Environment["HOSTNAME"] = options.Host;
            start.Environment["NODE_ENV"] = "production";
            start.Environment["NEXT_TELEMETRY_DISABLED"] = "1";

            using (PosixSignalRegistration interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal))
            using (PosixSignalRegistration terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal))
            using (PosixSignalRegistration quit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, OnSignal))
            {
                Console.WriteLine("Less3 Dashboard " + VersionText() + " at " + options.LocalUrl + " (listening on " + options.Host + "). Press Ctrl+C to stop.");
                Console.WriteLine("Sign in with your Less3 server's URL (default http://127.0.0.1:8000) and its admin API key.");
                try
                {
                    _Node = Process.Start(start);
                }
                catch (Win32Exception ex)
                {
                    Console.Error.WriteLine("less3-dashboard: could not start " + layout.NodeExecutable + ": " + ex.Message);
                    return 1;
                }

                if (_Node == null)
                {
                    Console.Error.WriteLine("less3-dashboard: could not start " + layout.NodeExecutable + ".");
                    return 1;
                }

                Task opening = options.Open ? OpenWhenReadyAsync(options.LocalUrl, _Stopping.Token) : Task.CompletedTask;
                try
                {
                    await _Node.WaitForExitAsync(_Stopping.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    StopNode();
                    await _Node.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                }

                _Stopping.Cancel();
                await opening.ConfigureAwait(false);
                // Stopped by Ctrl+C or a service manager is a normal exit, whatever code the killed runtime reported.
                return _Signaled ? 0 : _Node.ExitCode;
            }
        }

        private static void OnSignal(PosixSignalContext context)
        {
            context.Cancel = true;
            _Signaled = true;
            _Stopping.Cancel();
        }

        private static void StopNode()
        {
            try
            {
                if (_Node != null && !_Node.HasExited) _Node.Kill(true);
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception)
            {
            }
        }

        private static async Task OpenWhenReadyAsync(string url, CancellationToken token)
        {
            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
            {
                DateTime deadline = DateTime.UtcNow.AddSeconds(60);
                while (!token.IsCancellationRequested && DateTime.UtcNow < deadline)
                {
                    try
                    {
                        using (HttpResponseMessage response = await client.GetAsync(url, token).ConfigureAwait(false))
                        {
                            OpenBrowser(url);
                            return;
                        }
                    }
                    catch (HttpRequestException)
                    {
                    }
                    catch (TaskCanceledException) when (!token.IsCancellationRequested)
                    {
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    try
                    {
                        await Task.Delay(500, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) Process.Start("open", url)?.Dispose();
                else Process.Start("xdg-open", url)?.Dispose();
            }
            catch (Win32Exception)
            {
                Console.WriteLine("Open " + url + " in your browser.");
            }
        }

        private static string VersionText()
        {
            string? version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (String.IsNullOrEmpty(version)) return "unknown";
            int plus = version.IndexOf('+', StringComparison.Ordinal);
            return plus < 0 ? version : version.Substring(0, plus);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("less3-dashboard: runs the Less3 dashboard, a web app for managing a Less3 server.");
            Console.WriteLine();
            Console.WriteLine("  less3-dashboard [--host <address>] [--port <port>] [--open]");
            Console.WriteLine();
            Console.WriteLine("  --host <address>  Address to listen on (default 127.0.0.1, or " + LauncherOptions.HostVariable + "); 0.0.0.0 for every interface");
            Console.WriteLine("  --port <port>     Port to listen on (default 3000, or " + LauncherOptions.PortVariable + ")");
            Console.WriteLine("  --open            Open the dashboard in your browser once it is ready");
            Console.WriteLine("  --version         Print the version and exit");
            Console.WriteLine();
            Console.WriteLine("Sign in with your Less3 server's URL (default http://127.0.0.1:8000) and its admin API key.");
        }
    }
}
