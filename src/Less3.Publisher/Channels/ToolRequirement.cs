namespace Less3.Publisher.Channels
{
    /// <summary>
    /// An external tool a channel needs and how to install it.
    /// </summary>
    public sealed class ToolRequirement
    {
        /// <summary>
        /// Command name, for example "fpm".
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// Argument that makes the tool exit quickly, for example "--version".
        /// </summary>
        public string Probe { get; }

        /// <summary>
        /// The command that installs the tool.
        /// </summary>
        public string InstallHint { get; }

        /// <summary>
        /// Creates a requirement.
        /// </summary>
        /// <param name="command">Command name.</param>
        /// <param name="probe">Argument that makes the tool exit quickly.</param>
        /// <param name="installHint">How to install it.</param>
        public ToolRequirement(string command, string probe, string installHint)
        {
            Command = command;
            Probe = probe;
            InstallHint = installHint;
        }
    }
}
