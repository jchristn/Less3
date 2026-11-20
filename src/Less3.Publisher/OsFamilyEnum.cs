namespace Less3.Publisher
{
    /// <summary>
    /// Operating system families. Channels name the one they must run on, and runtime identifiers belong to one.
    /// </summary>
    public enum OsFamilyEnum
    {
        /// <summary>
        /// Any operating system (manifest generators).
        /// </summary>
        Any,

        /// <summary>
        /// Windows.
        /// </summary>
        Windows,

        /// <summary>
        /// macOS.
        /// </summary>
        MacOs,

        /// <summary>
        /// Linux.
        /// </summary>
        Linux
    }
}
