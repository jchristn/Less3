namespace Test.Shared.Processes
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Native JOBOBJECT_BASIC_LIMIT_INFORMATION (Windows).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct JobObjectBasicLimitInformation
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }
}
