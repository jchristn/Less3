namespace Less3.Publisher.Manifest
{
    /// <summary>
    /// A menu entry for a console server. It opens a terminal window in a per-user data folder (created on first use)
    /// and runs the main executable there, so the configuration and data it creates in its working directory land
    /// somewhere the user can find, never in the install folder.
    /// </summary>
    public sealed class LauncherSettings
    {
        /// <summary>
        /// Name shown in the Start menu and the application menu, for example "Less3".
        /// </summary>
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// One-line description for the menu entry. Empty uses the project tagline.
        /// </summary>
        public string Comment { get; set; } = "";

        /// <summary>
        /// Base file name of the launcher script, without extension, for example "start-less3". Each format adds
        /// its own extension (.cmd, .sh, .command).
        /// </summary>
        public string FileName { get; set; } = "";

        /// <summary>
        /// Folder in the user's home directory the launcher runs in, for example "Less3" for ~/Less3 (or
        /// %USERPROFILE%\Less3 on Windows).
        /// </summary>
        public string DataFolder { get; set; } = "";
    }
}
