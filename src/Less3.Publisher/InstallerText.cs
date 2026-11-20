namespace Less3.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Less3.Publisher.Manifest;

    /// <summary>
    /// Text every installer shows: what the product is and what the installer puts on the machine, and the note on
    /// getting past the unsigned-build prompt. Kept in one place so the formats say the same thing.
    /// </summary>
    public static class InstallerText
    {
        /// <summary>
        /// The launcher's data folder as a user on the given operating system writes it, for example "~/Less3" or
        /// "%USERPROFILE%\Less3".
        /// </summary>
        /// <param name="launcher">Launcher settings.</param>
        /// <param name="os">Operating system.</param>
        /// <returns>Folder path.</returns>
        public static string DataFolder(LauncherSettings launcher, OsFamilyEnum os)
        {
            ArgumentNullException.ThrowIfNull(launcher);
            return os == OsFamilyEnum.Windows ? "%USERPROFILE%\\" + launcher.DataFolder : "~/" + launcher.DataFolder;
        }

        /// <summary>
        /// What an installer adds, one item per line, for the given operating system.
        /// </summary>
        /// <param name="project">Project settings.</param>
        /// <param name="artifact">Artifact being installed.</param>
        /// <param name="os">Operating system.</param>
        /// <returns>Items such as "The less3 command, added to PATH".</returns>
        public static List<string> WhatGetsInstalled(ProjectSettings project, ArtifactSettings artifact, OsFamilyEnum os)
        {
            ArgumentNullException.ThrowIfNull(project);
            ArgumentNullException.ThrowIfNull(artifact);
            List<string> items = new List<string>();
            foreach (string command in artifact.PathCommands)
            {
                items.Add(os == OsFamilyEnum.MacOs ? "The " + command + " command in /usr/local/bin" : "The " + command + " command, added to PATH");
            }

            if (artifact.Launcher != null)
            {
                string folder = DataFolder(artifact.Launcher, os);
                switch (os)
                {
                    case OsFamilyEnum.MacOs:
                        items.Add(project.DisplayName + ".app in Applications, which runs " + project.DisplayName + " in a Terminal window in " + folder);
                        break;
                    case OsFamilyEnum.Windows:
                        items.Add(artifact.Launcher.DisplayName + " in the Start menu, which runs " + project.DisplayName + " in a console window in " + folder);
                        break;
                    default:
                        items.Add(artifact.Launcher.DisplayName + " in the application menu, which runs " + project.DisplayName + " in a terminal in " + folder);
                        break;
                }
            }

            if (os == OsFamilyEnum.Linux && artifact.Service != null)
            {
                items.Add("A " + artifact.Service.Name + " systemd service that runs in " + artifact.Service.WorkingDirectory + ", installed but not enabled");
            }

            return items;
        }

        /// <summary>
        /// One sentence on where the server keeps its files, shown after the list of what gets installed.
        /// </summary>
        /// <param name="project">Project settings.</param>
        /// <param name="mainExecutable">The command, for example "less3".</param>
        /// <returns>Plain text.</returns>
        public static string WorkingDirectoryNote(ProjectSettings project, string mainExecutable)
        {
            ArgumentNullException.ThrowIfNull(project);
            return project.DisplayName + " keeps its configuration (system.json), database, and objects in the folder it runs in; the first run asks a few setup questions. Run " + mainExecutable + " from any folder to use that folder instead.";
        }

        /// <summary>
        /// The welcome paragraph: the tagline followed by what gets installed.
        /// </summary>
        /// <param name="project">Project settings.</param>
        /// <param name="artifact">Artifact being installed.</param>
        /// <param name="os">Operating system.</param>
        /// <returns>Plain text, one sentence per item.</returns>
        public static string Welcome(ProjectSettings project, ArtifactSettings artifact, OsFamilyEnum os)
        {
            ArgumentNullException.ThrowIfNull(project);
            List<string> items = WhatGetsInstalled(project, artifact, os);
            return project.Tagline + " This installs: " + String.Join("; ", items.Select(item => LowerFirst(item))) + ".";
        }

        /// <summary>
        /// The note placed beside the app in a macOS disk image, with the steps past Gatekeeper's prompt.
        /// </summary>
        /// <param name="project">Project settings.</param>
        /// <param name="artifact">Artifact.</param>
        /// <returns>Plain text.</returns>
        public static string GatekeeperNote(ProjectSettings project, ArtifactSettings artifact)
        {
            ArgumentNullException.ThrowIfNull(project);
            ArgumentNullException.ThrowIfNull(artifact);
            string name = project.DisplayName;
            return
                "IF MACOS SAYS \"" + name + "\" NOT OPENED\n\n" +
                name + " is free, open-source software and is not code-signed (signing certificates are an ongoing cost\n" +
                "we choose not to pass on). The first time you open it, macOS says:\n\n" +
                "    \"" + name + "\" Not Opened\n" +
                "    Apple could not verify \"" + name + "\" is free of malware that may harm your Mac or\n" +
                "    compromise your privacy.\n\n" +
                "That doesn't mean the download is damaged. To open it anyway:\n\n" +
                "  1. Drag " + name + ".app onto the Applications folder in this window, then open it from there.\n" +
                "  2. When the message appears, click Done. Do not click Move to Trash.\n" +
                "  3. Open System Settings > Privacy & Security and scroll down to Security.\n" +
                "  4. Next to \"" + name + "\" was blocked to protect your Mac, click Open Anyway. The button only\n" +
                "     appears for about an hour after step 2; if it is missing, try to open " + name + " again first.\n" +
                "  5. Click Open Anyway again in the dialog that follows and enter your password. macOS remembers\n" +
                "     the choice, so you only do this once.\n\n" +
                "Control-click > Open no longer gets past this prompt on macOS 15 and later.\n\n" +
                "Or, after step 1, run this in Terminal and open " + name + " normally:\n\n" +
                "    xattr -dr com.apple.quarantine /Applications/" + name + ".app\n\n" +
                "The .pkg installer also puts the " + artifact.MainExecutable + " command on your PATH, which the drag-and-drop copy\n" +
                "does not. Full instructions: " + project.Repo + "/blob/main/INSTALL.md\n";
        }

        private static string LowerFirst(string text)
        {
            if (text.StartsWith("The ", StringComparison.Ordinal)) return "the " + text.Substring(4);
            if (text.StartsWith("A ", StringComparison.Ordinal)) return "a " + text.Substring(2);
            return text;
        }
    }
}
