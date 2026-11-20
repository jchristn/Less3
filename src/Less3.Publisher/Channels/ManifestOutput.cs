namespace Less3.Publisher.Channels
{
    using System;
    using System.IO;

    /// <summary>
    /// Locations package-manager manifests are written to.
    /// </summary>
    public static class ManifestOutput
    {
        /// <summary>
        /// The folder for one channel's manifests: output/manifests/&lt;channel&gt;.
        /// </summary>
        /// <param name="context">Channel context.</param>
        /// <param name="channel">Channel id.</param>
        /// <returns>Absolute path; created when missing.</returns>
        public static string Directory(ChannelContext context, string channel)
        {
            ArgumentNullException.ThrowIfNull(context);
            string path = Path.Combine(context.OutputDir, "manifests", channel);
            System.IO.Directory.CreateDirectory(path);
            return path;
        }
    }
}
