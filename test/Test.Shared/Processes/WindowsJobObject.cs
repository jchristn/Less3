namespace Test.Shared.Processes
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A process-wide Windows job object with kill-on-close set. Its handle is never closed explicitly, so the
    /// operating system closes it when the test process ends, however it ends (normal exit, crash, or being
    /// killed by a test runner), and every process assigned to it is terminated. Thread-safe. On other operating
    /// systems every method is a no-op that returns false.
    /// </summary>
    internal static class WindowsJobObject
    {
        #region Private-Members

        private const int _JobObjectExtendedLimitInformationClass = 9;
        private const uint _JobObjectLimitKillOnJobClose = 0x00002000;

        private static readonly object _Lock = new object();
        private static IntPtr _Job = IntPtr.Zero;
        private static bool _Initialized = false;

        #endregion

        #region Internal-Methods

        /// <summary>
        /// Assign a process to the job, so it is terminated when the test process ends.
        /// </summary>
        /// <param name="process">A started process.</param>
        /// <returns>True when the process was assigned.</returns>
        internal static bool TryAssign(Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            IntPtr job = GetJob();
            if (job == IntPtr.Zero) return false;

            try
            {
                return AssignProcessToJobObject(job, process.Handle);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Whether a process is in the job.
        /// </summary>
        /// <param name="process">A running process.</param>
        /// <returns>True when the process is in the job.</returns>
        internal static bool Contains(Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            IntPtr job = GetJob();
            if (job == IntPtr.Zero) return false;

            return IsProcessInJob(process.Handle, job, out bool result) && result;
        }

        #endregion

        #region Private-Methods

        private static IntPtr GetJob()
        {
            if (!OperatingSystem.IsWindows()) return IntPtr.Zero;

            lock (_Lock)
            {
                if (_Initialized) return _Job;
                _Initialized = true;

                IntPtr job = CreateJobObjectW(IntPtr.Zero, null);
                if (job == IntPtr.Zero) return IntPtr.Zero;

                JobObjectExtendedLimitInformation info = new JobObjectExtendedLimitInformation();
                info.BasicLimitInformation.LimitFlags = _JobObjectLimitKillOnJobClose;

                if (!SetInformationJobObject(job, _JobObjectExtendedLimitInformationClass, ref info, (uint)Marshal.SizeOf<JobObjectExtendedLimitInformation>()))
                {
                    CloseHandle(job);
                    return IntPtr.Zero;
                }

                _Job = job;
                return _Job;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateJobObjectW(IntPtr jobAttributes, string? name);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetInformationJobObject(IntPtr job, int infoClass, ref JobObjectExtendedLimitInformation info, uint length);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsProcessInJob(IntPtr process, IntPtr job, [MarshalAs(UnmanagedType.Bool)] out bool result);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);

        #endregion
    }
}
