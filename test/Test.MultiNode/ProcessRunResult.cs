namespace Test.MultiNode
{
    /// <summary>
    /// Result of running an external process.
    /// </summary>
    public sealed class ProcessRunResult
    {
        /// <summary>Process exit code.</summary>
        public int ExitCode { get; set; }

        /// <summary>Combined standard output and standard error.</summary>
        public string Output { get; set; }
    }
}
