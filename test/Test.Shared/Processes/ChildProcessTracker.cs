namespace Test.Shared.Processes
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Makes sure Less3 test servers never outlive the test process that started them. Thread-safe.
    /// <list type="bullet">
    /// <item>On Windows every server joins a kill-on-close job object, so the operating system terminates it when the
    /// test process ends for any reason, including a test runner killing its host.</item>
    /// <item>On every platform, servers still registered when the process exits normally or on Ctrl+C are killed,
    /// covering servers owned by abandoned (timed-out) tests.</item>
    /// <item>Each server's working directory holds a <see cref="ServerOwnerRecord"/>. The first server started by a
    /// process sweeps the temp directory and kills servers whose owner is gone, so a hard-killed run on Linux or
    /// macOS is cleaned up by the next one.</item>
    /// </list>
    /// </summary>
    public static class ChildProcessTracker
    {
        #region Public-Members

        /// <summary>
        /// Prefix of test server working directories under the system temp directory. Default is "less3-test-".
        /// </summary>
        public static string DirectoryPrefix { get; } = "less3-test-";

        /// <summary>
        /// How long to wait for a killed process to exit, in milliseconds. Default is 5000. Minimum is 0.
        /// </summary>
        public static int KillWaitMs
        {
            get { return _KillWaitMs; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Must be zero or greater.");
                _KillWaitMs = value;
            }
        }

        #endregion

        #region Private-Members

        private static readonly object _Lock = new object();
        private static readonly Dictionary<Process, string> _Tracked = new Dictionary<Process, string>();
        private static int _Swept = 0;
        private static int _KillWaitMs = 5000;

        #endregion

        #region Constructors-and-Factories

        static ChildProcessTracker()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => KillAll();
            Console.CancelKeyPress += (sender, e) => KillAll();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Track a started server process: assign it to the job object (Windows), write its owner record, and kill
        /// it at process exit unless it is untracked first.
        /// </summary>
        /// <param name="process">The started server process.</param>
        /// <param name="workingDirectory">The server's working directory, removed if the server is killed at exit.</param>
        /// <exception cref="ArgumentNullException">Thrown when process or workingDirectory is null.</exception>
        public static void Track(Process process, string workingDirectory)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (String.IsNullOrEmpty(workingDirectory)) throw new ArgumentNullException(nameof(workingDirectory));

            WindowsJobObject.TryAssign(process);

            lock (_Lock)
            {
                _Tracked[process] = workingDirectory;
            }

            try
            {
                ServerOwnerRecord record = new ServerOwnerRecord();
                using (Process self = Process.GetCurrentProcess())
                {
                    record.OwnerProcessId = self.Id;
                    record.OwnerStartUtc = self.StartTime.ToUniversalTime();
                }

                record.ServerProcessId = process.Id;
                record.ServerStartUtc = process.StartTime.ToUniversalTime();
                record.Write(workingDirectory);
            }
            catch (InvalidOperationException)
            {
                // The server already exited; there is nothing left to sweep.
            }
            catch (IOException)
            {
                // Without a record a later sweep cannot find this server, but the job object and exit hooks still apply.
            }
        }

        /// <summary>
        /// Stop tracking a process, typically because its owner stopped it.
        /// </summary>
        /// <param name="process">The server process.</param>
        public static void Untrack(Process process)
        {
            if (process == null) return;

            lock (_Lock)
            {
                _Tracked.Remove(process);
            }
        }

        /// <summary>
        /// Whether a process is contained in this process's kill-on-close job object. Always false on operating
        /// systems other than Windows.
        /// </summary>
        /// <param name="process">A running process.</param>
        /// <returns>True when the process will be terminated by the operating system when this process ends.</returns>
        public static bool IsInKillOnCloseJob(Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            return WindowsJobObject.Contains(process);
        }

        /// <summary>
        /// Sweep the system temp directory once per process. Called before the first server starts.
        /// </summary>
        public static void EnsureSwept()
        {
            if (Interlocked.Exchange(ref _Swept, 1) == 1) return;
            SweepOrphans(Path.GetTempPath());
        }

        /// <summary>
        /// Kill servers recorded under a directory whose owning test process is no longer running, and remove
        /// their working directories. Directories without an owner record, directories whose owner is still
        /// running, and processes that cannot be inspected are left alone. A server is killed only when both
        /// its process id and its start time match the record, so a reused process id is never killed.
        /// </summary>
        /// <param name="rootDirectory">Directory containing server working directories.</param>
        /// <returns>Number of server processes killed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when rootDirectory is null or empty.</exception>
        public static int SweepOrphans(string rootDirectory)
        {
            if (String.IsNullOrEmpty(rootDirectory)) throw new ArgumentNullException(nameof(rootDirectory));
            if (!Directory.Exists(rootDirectory)) return 0;

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(rootDirectory, DirectoryPrefix + "*");
            }
            catch (IOException)
            {
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }

            int killed = 0;
            foreach (string directory in directories)
            {
                ServerOwnerRecord? record = ServerOwnerRecord.TryRead(directory);
                if (record == null) continue;
                if (Identify(record.OwnerProcessId, record.OwnerStartUtc) != ProcessIdentityState.NotRunning) continue;

                ProcessIdentityState server = Identify(record.ServerProcessId, record.ServerStartUtc);
                if (server == ProcessIdentityState.Unknown) continue;

                if (server == ProcessIdentityState.Running && Kill(record.ServerProcessId)) killed++;
                DeleteDirectory(directory);
            }

            return killed;
        }

        /// <summary>
        /// Determine whether the process recorded by id and start time is still running.
        /// </summary>
        /// <param name="processId">Process id.</param>
        /// <param name="startUtc">Recorded start time (UTC).</param>
        /// <returns>The identity state.</returns>
        public static ProcessIdentityState Identify(int processId, DateTime startUtc)
        {
            if (processId <= 0) return ProcessIdentityState.NotRunning;

            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    if (process.HasExited) return ProcessIdentityState.NotRunning;
                    double drift = Math.Abs((process.StartTime.ToUniversalTime() - startUtc).TotalSeconds);
                    return drift < 1 ? ProcessIdentityState.Running : ProcessIdentityState.NotRunning;
                }
            }
            catch (ArgumentException)
            {
                return ProcessIdentityState.NotRunning;
            }
            catch (InvalidOperationException)
            {
                return ProcessIdentityState.NotRunning;
            }
            catch (Win32Exception)
            {
                return ProcessIdentityState.Unknown;
            }
            catch (NotSupportedException)
            {
                return ProcessIdentityState.Unknown;
            }
        }

        #endregion

        #region Private-Methods

        private static void KillAll()
        {
            List<KeyValuePair<Process, string>> tracked;
            lock (_Lock)
            {
                tracked = _Tracked.ToList();
                _Tracked.Clear();
            }

            foreach (KeyValuePair<Process, string> entry in tracked)
            {
                try
                {
                    if (!entry.Key.HasExited)
                    {
                        entry.Key.Kill(true);
                        entry.Key.WaitForExit(_KillWaitMs);
                    }
                }
                catch (InvalidOperationException)
                {
                }
                catch (Win32Exception)
                {
                }

                DeleteDirectory(entry.Value);
            }
        }

        private static bool Kill(int processId)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    process.Kill(true);
                    process.WaitForExit(_KillWaitMs);
                    return true;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        private static void DeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        #endregion
    }
}
