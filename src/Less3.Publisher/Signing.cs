namespace Less3.Publisher
{
    using System;
    using Less3.Publisher.Manifest;

    /// <summary>
    /// Code-signing steps. Less3 is not code-signed by policy, so signing targets ship unconfigured: Windows files stay
    /// unsigned and macOS bundles are ad-hoc signed, which Apple silicon needs to run them at all and which keeps
    /// Gatekeeper from calling a downloaded app damaged. The configured branches exist so a policy change is a
    /// secrets change.
    /// </summary>
    public static class Signing
    {
        /// <summary>
        /// Signs a macOS bundle or executable, then verifies the signature.
        /// </summary>
        /// <param name="path">Bundle or executable.</param>
        /// <param name="target">macOS signing settings.</param>
        /// <exception cref="ProcessFailedException">codesign failed.</exception>
        /// <exception cref="PublisherException">Signing is configured but its secret is missing.</exception>
        public static void SignMac(string path, SigningTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (target.IsConfigured)
            {
                ProcessRunner.Run("codesign", new[] { "--force", "--deep", "--options", "runtime", "--timestamp", "--sign", Secret(target.VaultRef, "macOS signing identity"), path });
            }
            else
            {
                Console.WriteLine("[sign] macOS signing is not configured (by policy): ad-hoc signing " + path);
                ProcessRunner.Run("codesign", new[] { "--force", "--deep", "--sign", "-", path });
            }

            ProcessRunner.Run("codesign", new[] { "--verify", "--deep", "--strict", "--verbose=2", path });
        }

        /// <summary>
        /// Notarizes and staples a macOS installer when notarization is configured; otherwise does nothing.
        /// </summary>
        /// <param name="path">.pkg or .dmg.</param>
        /// <param name="target">macOS signing settings.</param>
        /// <exception cref="ProcessFailedException">notarytool or stapler failed.</exception>
        /// <exception cref="PublisherException">Notarization is configured but its secret is missing.</exception>
        public static void NotarizeMac(string path, SigningTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (!target.IsConfigured || !target.Notarize) return;
            ProcessRunner.Run("xcrun", new[] { "notarytool", "submit", path, "--keychain-profile", Secret(target.NotarizeProfileRef, "notarytool profile"), "--wait" });
            ProcessRunner.Run("xcrun", new[] { "stapler", "staple", path });
        }

        /// <summary>
        /// Authenticode-signs a Windows file when Windows signing is configured; otherwise leaves it unsigned.
        /// </summary>
        /// <param name="path">File to sign.</param>
        /// <param name="target">Windows signing settings.</param>
        /// <exception cref="ProcessFailedException">signtool failed.</exception>
        /// <exception cref="PublisherException">Signing is configured but a secret is missing.</exception>
        public static void SignWindows(string path, SigningTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (!target.IsConfigured)
            {
                Console.WriteLine("[sign] Windows signing is not configured (by policy): " + path + " stays unsigned.");
                return;
            }

            ProcessRunner.Run("signtool", new[] { "sign", "/f", Secret(target.VaultRef, "certificate path"), "/p", Secret(target.PasswordRef, "certificate password"), "/fd", "sha256", "/tr", "http://timestamp.digicert.com", "/td", "sha256", path });
        }

        private static string Secret(string name, string what)
        {
            if (String.IsNullOrEmpty(name)) throw new PublisherException("Signing is configured but publisher.json names no environment variable for the " + what + ".");
            string? value = Environment.GetEnvironmentVariable(name);
            if (String.IsNullOrEmpty(value)) throw new PublisherException("Signing is configured but environment variable " + name + " (" + what + ") is empty. Map the secret into it in the workflow.");
            return value;
        }
    }
}
