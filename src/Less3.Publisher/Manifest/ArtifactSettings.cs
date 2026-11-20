namespace Less3.Publisher.Manifest
{
    using System.Collections.Generic;

    /// <summary>
    /// One published payload and how it is installed: which executables it contains, which command goes on PATH,
    /// how a desktop user opens it, and the system service that runs it on Linux.
    /// </summary>
    public sealed class ArtifactSettings
    {
        /// <summary>
        /// Short id that channels reference, for example "server".
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Lowercase name for this artifact's files and packages, for example "less3-dashboard". Empty uses
        /// project.name. Each artifact with its own installers needs its own name.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Product name for this artifact's installers, for example "Less3 Dashboard". Empty uses project.displayName.
        /// </summary>
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// Reverse-DNS identifier for this artifact, for example "ai.latticetech.less3.dashboard". Empty uses
        /// project.bundleId.
        /// </summary>
        public string BundleId { get; set; } = "";

        /// <summary>
        /// Package identifier for winget and similar feeds. Empty uses project.packageId.
        /// </summary>
        public string PackageId { get; set; } = "";

        /// <summary>
        /// One sentence saying what this artifact is, for its installers' welcome pages. Empty uses project.tagline.
        /// </summary>
        public string Tagline { get; set; } = "";

        /// <summary>
        /// Paragraph for this artifact's package metadata. Empty uses project.description.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Stable Inno Setup AppId GUID for this artifact's Windows installer. Keep it constant so upgrades replace the
        /// installed copy in place. Empty uses the inno channel's appId option.
        /// </summary>
        public string WindowsAppId { get; set; } = "";

        /// <summary>
        /// A sentence the installers' welcome pages add after the list of what gets installed, or empty.
        /// </summary>
        public string InstallerNote { get; set; } = "";

        /// <summary>
        /// Project to publish, relative to the repository root.
        /// </summary>
        public string Csproj { get; set; } = "";

        /// <summary>
        /// True (the default) to publish one self-contained, single-file executable per runtime.
        /// </summary>
        public bool SingleFile { get; set; } = true;

        /// <summary>
        /// Assembly name to publish under, which names the executable, for example "less3". Empty keeps the
        /// project's own assembly name.
        /// </summary>
        public string AssemblyName { get; set; } = "";

        /// <summary>
        /// Executable (without extension) that runs the product, for example "less3".
        /// </summary>
        public string MainExecutable { get; set; } = "";

        /// <summary>
        /// Every executable (without extension) the payload must contain. Publishing fails when one is missing.
        /// </summary>
        public List<string> Executables { get; set; } = new List<string>();

        /// <summary>
        /// Executables that installers put on PATH.
        /// </summary>
        public List<string> PathCommands { get; set; } = new List<string>();

        /// <summary>
        /// Files the publish copies beside the executable that the installers leave out, relative to the publish
        /// folder (for example developer scripts).
        /// </summary>
        public List<string> RemoveFromPayload { get; set; } = new List<string>();

        /// <summary>
        /// A Next.js web app built in standalone mode and bundled, with a Node.js runtime, beside the published
        /// executable (which starts it), or null for none.
        /// </summary>
        public NextAppSettings? NextApp { get; set; } = null;

        /// <summary>
        /// The Start menu, application menu, and macOS app entry that opens the product in a terminal window, or
        /// null for none.
        /// </summary>
        public LauncherSettings? Launcher { get; set; } = null;

        /// <summary>
        /// The systemd system service the Linux packages install, or null for none.
        /// </summary>
        public ServiceSettings? Service { get; set; } = null;

        /// <summary>
        /// Native libraries the payload needs on Linux, by package format.
        /// </summary>
        public LinuxDependencySettings LinuxDependencies { get; set; } = new LinuxDependencySettings();
    }
}
