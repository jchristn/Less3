namespace Test.Shared.Processes
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Native JOBOBJECT_EXTENDED_LIMIT_INFORMATION (Windows).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct JobObjectExtendedLimitInformation
    {
        public JobObjectBasicLimitInformation BasicLimitInformation;
        public IoCounters IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }
}
