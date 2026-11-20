namespace Less3.Publisher.Manifest
{
    using System.Collections.Generic;

    /// <summary>
    /// Native packages a Linux install needs. A missing library shows up as a crash on first run, so the packages
    /// declare them.
    /// </summary>
    public sealed class LinuxDependencySettings
    {
        /// <summary>
        /// Debian package dependencies. An entry may list alternatives separated by " | ".
        /// </summary>
        public List<string> Deb { get; set; } = new List<string>();

        /// <summary>
        /// Debian packages that are recommended but not required.
        /// </summary>
        public List<string> DebRecommends { get; set; } = new List<string>();

        /// <summary>
        /// RPM package dependencies.
        /// </summary>
        public List<string> Rpm { get; set; } = new List<string>();
    }
}
