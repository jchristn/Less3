namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Maps channel ids to implementations.
    /// </summary>
    public static class ChannelRegistry
    {
        /// <summary>
        /// Every channel id, installers first in the order a host builds them.
        /// </summary>
        public static IReadOnlyList<string> KnownIds { get; } = new[]
        {
            "inno", "macos", "debrpm", "appimage", "archive",
            "winget", "choco", "scoop", "brew", "aptyum", "snap", "flatpak"
        };

        /// <summary>
        /// Returns the implementation for a channel id, or null for an unknown id.
        /// </summary>
        /// <param name="id">Channel id.</param>
        /// <returns>The channel, or null.</returns>
        public static IChannel? Resolve(string id)
        {
            switch ((id ?? "").ToLowerInvariant())
            {
                case "inno": return new InnoChannel();
                case "macos": return new MacChannel();
                case "debrpm": return new DebRpmChannel();
                case "appimage": return new AppImageChannel();
                case "archive": return new ArchiveChannel();
                case "winget": return new WingetChannel();
                case "choco": return new ChocoChannel();
                case "scoop": return new ScoopChannel();
                case "brew": return new BrewFormulaChannel();
                case "aptyum": return new AptYumChannel();
                case "snap": return new StubChannel("snap", "Snap package", "a strict-confinement snapcraft.yaml that dumps the Linux payload and exposes less3 as an app and a daemon, with network-bind and home access.");
                case "flatpak": return new StubChannel("flatpak", "Flatpak package", "a flatpak-builder manifest on org.freedesktop.Platform that installs the Linux payload, with network and home access.");
                default: return null;
            }
        }

        /// <summary>
        /// The channels a host operating system builds installers with, in order.
        /// </summary>
        /// <param name="os">Host operating system.</param>
        /// <returns>Channel ids.</returns>
        public static IReadOnlyList<string> InstallerChannelsFor(OsFamilyEnum os)
        {
            switch (os)
            {
                case OsFamilyEnum.Windows: return new[] { "inno", "archive" };
                case OsFamilyEnum.MacOs: return new[] { "macos", "archive" };
                case OsFamilyEnum.Linux: return new[] { "debrpm", "appimage", "archive" };
                default: return Array.Empty<string>();
            }
        }
    }
}
