namespace Less3.Publisher.Manifest
{
    /// <summary>
    /// Product identity, artwork, and text. Every installer that has a UI shows the display name, the logo, the tagline,
    /// and the license, so they are kept here once instead of in each format.
    /// </summary>
    public sealed class ProjectSettings
    {
        /// <summary>
        /// Lowercase name used in file names and package names, for example "less3".
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Product name as users know it, for example "Less3". Used in window titles, menus, and installed-app lists.
        /// </summary>
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// Package identifier for winget and similar feeds, for example "LatticeAI.Less3".
        /// </summary>
        public string PackageId { get; set; } = "";

        /// <summary>
        /// Reverse-DNS identifier, for example "ai.latticetech.less3". The macOS bundle and .pkg derive their
        /// identifiers from it.
        /// </summary>
        public string BundleId { get; set; } = "";

        /// <summary>
        /// One sentence saying what the product is. Shown on every installer's welcome page.
        /// </summary>
        public string Tagline { get; set; } = "";

        /// <summary>
        /// A longer paragraph for package metadata (deb, rpm, winget, Homebrew).
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// SPDX license identifier, for example "MIT".
        /// </summary>
        public string License { get; set; } = "";

        /// <summary>
        /// License file relative to the repository root. Default LICENSE.md.
        /// </summary>
        public string LicenseFile { get; set; } = "LICENSE.md";

        /// <summary>
        /// Square PNG logo (at least 1024x1024, transparent background) relative to the repository root. Every
        /// format's icons, including the Windows .ico and the macOS .icns, are generated from it.
        /// </summary>
        public string Logo { get; set; } = "assets/logo.png";

        /// <summary>
        /// Publisher name shown in installed-app lists and package metadata, for example "Lattice AI, LLC".
        /// </summary>
        public string Publisher { get; set; } = "";

        /// <summary>
        /// Publisher URL shown in installed-app lists, for example "https://latticetech.ai".
        /// </summary>
        public string PublisherUrl { get; set; } = "";

        /// <summary>
        /// Maintainer field for deb and rpm metadata, as "Name &lt;email&gt;".
        /// </summary>
        public string Maintainer { get; set; } = "";

        /// <summary>
        /// Source repository URL.
        /// </summary>
        public string Repo { get; set; } = "";

        /// <summary>
        /// Project home page URL.
        /// </summary>
        public string Homepage { get; set; } = "";
    }
}
