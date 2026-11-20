namespace Less3.Publisher.Channels
{
    using System.Collections.Generic;
    using Less3.Publisher.Manifest;

    /// <summary>
    /// Values an Inno Setup script is rendered from, for one architecture.
    /// </summary>
    public sealed class InnoScriptInput
    {
        /// <summary>
        /// Stable upgrade GUID without braces. Keep it constant so a new version replaces the old in place.
        /// </summary>
        public string AppId { get; set; } = "";

        /// <summary>
        /// Project settings.
        /// </summary>
        public ProjectSettings Project { get; set; } = new ProjectSettings();

        /// <summary>
        /// Artifact being installed.
        /// </summary>
        public ArtifactSettings Artifact { get; set; } = new ArtifactSettings();

        /// <summary>
        /// Version.
        /// </summary>
        public string Version { get; set; } = "";

        /// <summary>
        /// Runtime target.
        /// </summary>
        public RuntimeTarget Target { get; set; } = RuntimeTarget.Parse("win-x64");

        /// <summary>
        /// Folder holding the published payload.
        /// </summary>
        public string PayloadDir { get; set; } = "";

        /// <summary>
        /// Folder that receives the setup executable.
        /// </summary>
        public string OutputDir { get; set; } = "";

        /// <summary>
        /// Setup file name without .exe.
        /// </summary>
        public string OutputBaseName { get; set; } = "";

        /// <summary>
        /// License file name beside the script.
        /// </summary>
        public string LicenseFile { get; set; } = "LICENSE.txt";

        /// <summary>
        /// Setup icon file name beside the script.
        /// </summary>
        public string SetupIcon { get; set; } = "setup.ico";

        /// <summary>
        /// Wizard image file names beside the script, smallest first (164x314 at 100% scaling).
        /// </summary>
        public List<string> WizardImages { get; set; } = new List<string>();

        /// <summary>
        /// Small wizard image file names beside the script, smallest first (55x55 at 100% scaling).
        /// </summary>
        public List<string> WizardSmallImages { get; set; } = new List<string>();
    }
}
