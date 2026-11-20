namespace Less3.Dashboard
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Where the installed dashboard and its Node.js runtime live: an app folder holding server.js and a node folder
    /// holding the node executable. Installers put both beside the launcher, except in a macOS app bundle, where they
    /// are in Contents/Resources.
    /// </summary>
    public sealed class RuntimeLayout
    {
        /// <summary>
        /// Folder holding server.js, the Next.js standalone server.
        /// </summary>
        public string AppDirectory { get; }

        /// <summary>
        /// The node executable.
        /// </summary>
        public string NodeExecutable { get; }

        private RuntimeLayout(string appDirectory, string nodeExecutable)
        {
            AppDirectory = appDirectory;
            NodeExecutable = nodeExecutable;
        }

        /// <summary>
        /// The folders searched for app/ and node/, in order: the launcher's own folder, then the Resources folder of
        /// the macOS app bundle it is in.
        /// </summary>
        /// <param name="baseDirectory">The launcher's folder.</param>
        /// <returns>Absolute folders.</returns>
        /// <exception cref="ArgumentNullException">The folder is null or empty.</exception>
        public static IReadOnlyList<string> Candidates(string baseDirectory)
        {
            if (String.IsNullOrEmpty(baseDirectory)) throw new ArgumentNullException(nameof(baseDirectory));
            string full = Path.GetFullPath(baseDirectory);
            return new[] { full, Path.GetFullPath(Path.Combine(full, "..", "Resources")) };
        }

        /// <summary>
        /// Finds the dashboard and its runtime.
        /// </summary>
        /// <param name="baseDirectory">The launcher's folder.</param>
        /// <returns>The layout, or null when no candidate folder holds both app/server.js and node/node.</returns>
        public static RuntimeLayout? Find(string baseDirectory)
        {
            string node = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "node.exe" : "node";
            foreach (string candidate in Candidates(baseDirectory))
            {
                string app = Path.Combine(candidate, "app");
                string executable = Path.Combine(candidate, "node", node);
                if (File.Exists(Path.Combine(app, "server.js")) && File.Exists(executable)) return new RuntimeLayout(app, executable);
            }

            return null;
        }
    }
}
