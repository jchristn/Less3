namespace Less3.Publisher.Manifest
{
    /// <summary>
    /// Signing settings for one operating system. Values name environment variables that CI fills from secrets.
    /// </summary>
    public sealed class SigningTarget
    {
        /// <summary>
        /// Environment variable holding the signing identity: a certificate path on Windows, an identity name on macOS,
        /// or a key id for GPG.
        /// </summary>
        public string VaultRef { get; set; } = "";

        /// <summary>
        /// Environment variable holding the identity's password, if any.
        /// </summary>
        public string PasswordRef { get; set; } = "";

        /// <summary>
        /// Environment variable holding the notarytool keychain profile (macOS).
        /// </summary>
        public string NotarizeProfileRef { get; set; } = "";

        /// <summary>
        /// True to notarize and staple after signing (macOS).
        /// </summary>
        public bool Notarize { get; set; } = false;

        /// <summary>
        /// False (the default) builds unsigned. On macOS unsigned builds are still ad-hoc signed.
        /// </summary>
        public bool IsConfigured { get; set; } = false;
    }
}
