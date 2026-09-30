namespace Test.Shared.Processes
{
    /// <summary>
    /// Whether a process recorded by id and start time is still the same running process.
    /// </summary>
    public enum ProcessIdentityState
    {
        /// <summary>
        /// A process with the recorded id and start time is running.
        /// </summary>
        Running,

        /// <summary>
        /// No process with the recorded id is running, or the id now belongs to a different process.
        /// </summary>
        NotRunning,

        /// <summary>
        /// The process could not be inspected (for example, access was denied).
        /// </summary>
        Unknown
    }
}
