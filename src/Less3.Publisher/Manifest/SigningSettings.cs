namespace Less3.Publisher.Manifest
{
    /// <summary>
    /// Code-signing settings per operating system. Less3 is not code-signed by policy, so every target ships with
    /// IsConfigured false; the wiring exists so a policy change is a secrets change instead of a rewrite.
    /// </summary>
    public sealed class SigningSettings
    {
        /// <summary>
        /// macOS Developer ID signing and notarization.
        /// </summary>
        public SigningTarget MacOs { get; set; } = new SigningTarget();

        /// <summary>
        /// Windows Authenticode signing.
        /// </summary>
        public SigningTarget Windows { get; set; } = new SigningTarget();

        /// <summary>
        /// GPG signing of apt and yum repository metadata.
        /// </summary>
        public SigningTarget Linux { get; set; } = new SigningTarget();
    }
}
