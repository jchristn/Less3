namespace Less3.Publisher
{
    using System;

    /// <summary>
    /// An external command could not start or exited with a non-zero status.
    /// </summary>
    public sealed class ProcessFailedException : Exception
    {
        /// <summary>
        /// The command's exit code, or -1 when it could not start.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Creates the exception.
        /// </summary>
        /// <param name="message">What failed.</param>
        /// <param name="exitCode">Exit code, or -1 when the command did not start.</param>
        public ProcessFailedException(string message, int exitCode) : base(message)
        {
            ExitCode = exitCode;
        }
    }
}
