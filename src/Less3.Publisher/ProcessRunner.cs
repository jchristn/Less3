namespace Less3.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// Runs packaging tools (dotnet, iscc, pkgbuild, fpm, ...) and streams their output to the console.
    /// </summary>
    public static class ProcessRunner
    {
        /// <summary>
        /// Runs a command and waits for it to finish.
        /// </summary>
        /// <param name="fileName">Executable, resolved through PATH.</param>
        /// <param name="arguments">Arguments, one per item; no shell quoting is applied.</param>
        /// <param name="workingDirectory">Working directory, or null for the current directory.</param>
        /// <param name="environment">Extra environment variables, or null.</param>
        /// <exception cref="ProcessFailedException">The command could not start or exited non-zero.</exception>
        public static void Run(string fileName, IEnumerable<string> arguments, string? workingDirectory = null, IDictionary<string, string>? environment = null)
        {
            RunInternal(fileName, arguments, workingDirectory, environment, null);
        }

        /// <summary>
        /// Runs a command and returns its standard output.
        /// </summary>
        /// <param name="fileName">Executable, resolved through PATH.</param>
        /// <param name="arguments">Arguments, one per item.</param>
        /// <param name="workingDirectory">Working directory, or null for the current directory.</param>
        /// <returns>Standard output.</returns>
        /// <exception cref="ProcessFailedException">The command could not start or exited non-zero.</exception>
        public static string Capture(string fileName, IEnumerable<string> arguments, string? workingDirectory = null)
        {
            StringBuilder output = new StringBuilder();
            RunInternal(fileName, arguments, workingDirectory, null, output);
            return output.ToString();
        }

        /// <summary>
        /// True when a command can be started from PATH.
        /// </summary>
        /// <param name="fileName">Executable name.</param>
        /// <param name="probeArgument">An argument that makes the tool exit quickly, for example "--version".</param>
        /// <returns>True when the command started. Its exit code is ignored, since some tools exit non-zero for help.</returns>
        public static bool IsAvailable(string fileName, string probeArgument)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(fileName) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                info.ArgumentList.Add(probeArgument);
                using (Process? process = Process.Start(info))
                {
                    if (process == null) return false;
                    process.StandardOutput.ReadToEnd();
                    process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return true;
                }
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        private static void RunInternal(string fileName, IEnumerable<string> arguments, string? workingDirectory, IDictionary<string, string>? environment, StringBuilder? capture)
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
            };

            StringBuilder printable = new StringBuilder(fileName);
            foreach (string argument in arguments)
            {
                info.ArgumentList.Add(argument);
                printable.Append(' ').Append(Quote(argument));
            }

            if (environment != null)
            {
                foreach (KeyValuePair<string, string> pair in environment) info.Environment[pair.Key] = pair.Value;
            }

            Console.WriteLine("  $ " + printable);
            using (Process process = new Process { StartInfo = info })
            {
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data == null) return;
                    if (capture != null) capture.AppendLine(args.Data);
                    else Console.WriteLine("    " + args.Data);
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null) Console.Error.WriteLine("    " + args.Data);
                };

                try
                {
                    process.Start();
                }
                catch (Win32Exception ex)
                {
                    throw new ProcessFailedException("Could not start '" + fileName + "': " + ex.Message + ". Is it installed and on PATH?", -1);
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new ProcessFailedException("'" + printable + "' exited with code " + process.ExitCode + ".", process.ExitCode);
            }
        }

        private static string Quote(string argument)
        {
            return argument.Length == 0 || argument.IndexOfAny(new[] { ' ', '"', '\'' }) >= 0 ? "\"" + argument.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"" : argument;
        }
    }
}
