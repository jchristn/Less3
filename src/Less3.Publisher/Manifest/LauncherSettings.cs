namespace Less3.Publisher.Manifest
{
    /// <summary>
    /// A menu entry for a console server. It opens a terminal window and runs the main executable there with
    /// <see cref="Arguments"/>. With a <see cref="DataFolder"/>, it first creates that per-user folder and changes into
    /// it, so the configuration and data the server writes to its working directory land somewhere the user can find,
    /// never in the install folder.
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
        /// %USERPROFILE%\Less3 on Windows). Empty runs the program without changing directory.
        /// </summary>
        public string DataFolder { get; set; } = "";

        /// <summary>
        /// Arguments passed to the main executable, for example "--open". Empty for none.
        /// </summary>
        public string Arguments { get; set; } = "";
    }
}
