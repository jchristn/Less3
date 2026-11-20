namespace Less3.Publisher.Manifest
{
    using System.Collections.Generic;

    /// <summary>
    /// A Next.js app the payload bundles beside the published executable: the standalone server under app/ and a
    /// Node.js runtime under node/. The app is built once per Publisher run and is the same on every runtime; only the
    /// Node.js runtime differs, so the app must not depend on native Node modules (see <see cref="RemoveFromApp"/>).
    /// </summary>
    public sealed class NextAppSettings
    {
        /// <summary>
        /// The app's folder relative to the repository root, for example "dashboard".
        /// </summary>
        public string Directory { get; set; } = "";

        /// <summary>
        /// Exact Node.js version to bundle, for example "22.23.5". Downloads come from https://nodejs.org/dist and are
        /// checked against the release's SHASUMS256.txt.
        /// </summary>
        public string NodeVersion { get; set; } = "";

        /// <summary>
        /// Environment variables set for the build, for example LESS3_DASHBOARD_STANDALONE=1 to select
        /// output: 'standalone' in next.config.mjs.
        /// </summary>
        public Dictionary<string, string> BuildEnvironment { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Paths inside the standalone app to delete, relative to app/, for example the host-specific native image
        /// library node_modules/@img that next build traces but the app does not use.
        /// </summary>
        public List<string> RemoveFromApp { get; set; } = new List<string>();
    }
}
