namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Less3.Publisher.Manifest;

    /// <summary>
    /// Renders the Linux package files: the application menu entry, the systemd unit, the package scripts, and the
    /// AppImage entry point.
    /// </summary>
    public static class LinuxFiles
    {
        /// <summary>
        /// Icon sizes installed under /usr/share/icons/hicolor.
        /// </summary>
        public static IReadOnlyList<int> IconSizes { get; } = new[] { 48, 64, 128, 256, 512 };

        /// <summary>
        /// Where the packages install the payload, for example "/opt/less3".
        /// </summary>
        /// <param name="project">Project settings.</param>
        /// <returns>Absolute path.</returns>
        public static string InstallDir(ProjectSettings project)
        {
            ArgumentNullException.ThrowIfNull(project);
            return "/opt/" + project.Name;
        }

        /// <summary>
        /// Where the packages install the systemd unit, for example "/usr/lib/systemd/system/less3.service".
        /// </summary>
        /// <param name="service">Service settings.</param>
        /// <returns>Absolute path.</returns>
        public static string UnitPath(ServiceSettings service)
        {
            ArgumentNullException.ThrowIfNull(service);
            return "/usr/lib/systemd/system/" + service.Name + ".service";
        }

        /// <summary>
        /// A desktop entry for the application menu. The program runs in a terminal.
        /// </summary>
        /// <param name="project">Project settings.</param>
        /// <param name="displayName">Name shown in the menu.</param>
        /// <param name="comment">One-line description; empty uses the tagline.</param>
        /// <param name="exec">Exec value, for example "/opt/less3/start-less3.sh".</param>
        /// <returns>Desktop entry text.</returns>
        public static string DesktopEntry(ProjectSettings project, string displayName, string comment, string exec)
        {
            ArgumentNullException.ThrowIfNull(project);
            StringBuilder s = new StringBuilder();
            s.Append("[Desktop Entry]\n");
            s.Append("Type=Application\n");
            s.Append("Name=").Append(displayName).Append('\n');
            s.Append("Comment=").Append(String.IsNullOrEmpty(comment) ? project.Tagline : comment).Append('\n');
            s.Append("Exec=").Append(exec).Append('\n');
            s.Append("Icon=").Append(project.Name).Append('\n');
            s.Append("Terminal=true\n");
            s.Append("Categories=Network;Utility;\n");
            return s.ToString();
        }

        /// <summary>
        /// The systemd unit. It runs as the service account in the working directory, and its
        /// ConditionPathExists keeps it from starting before the server has been configured there.
        /// </summary>
        /// <param name="project">Project settings.</param>
        /// <param name="artifact">Artifact with a service.</param>
        /// <returns>Unit file text.</returns>
        /// <exception cref="ArgumentException">The artifact has no service.</exception>
        public static string ServiceUnit(ProjectSettings project, ArtifactSettings artifact)
        {
            ArgumentNullException.ThrowIfNull(project);
            ArgumentNullException.ThrowIfNull(artifact);
            ServiceSettings service = artifact.Service ?? throw new ArgumentException("The artifact has no service.", nameof(artifact));
            StringBuilder s = new StringBuilder();
            s.Append("# Installed by the ").Append(project.DisplayName).Append(" package. Configure the server once, then enable it; see\n");
            s.Append("# ").Append(project.Repo).Append("/blob/main/INSTALL.md\n");
            s.Append("[Unit]\n");
            s.Append("Description=").Append(String.IsNullOrEmpty(service.Description) ? project.DisplayName : service.Description).Append('\n');
            s.Append("Documentation=").Append(project.Repo).Append("/blob/main/INSTALL.md\n");
            s.Append("After=network-online.target\n");
            s.Append("Wants=network-online.target\n");
            if (!String.IsNullOrEmpty(service.RequiredFile)) s.Append("ConditionPathExists=").Append(service.WorkingDirectory.TrimEnd('/')).Append('/').Append(service.RequiredFile).Append('\n');
            s.Append('\n');
            s.Append("[Service]\n");
            s.Append("Type=simple\n");
            s.Append("User=").Append(service.User).Append('\n');
            s.Append("Group=").Append(service.User).Append('\n');
            s.Append("WorkingDirectory=").Append(service.WorkingDirectory).Append('\n');
            foreach (string variable in service.Environment) s.Append("Environment=").Append(variable).Append('\n');
            s.Append("ExecStart=").Append(InstallDir(project)).Append('/').Append(artifact.MainExecutable);
            if (!String.IsNullOrEmpty(service.Arguments)) s.Append(' ').Append(service.Arguments);
            s.Append('\n');
            s.Append("StandardInput=null\n");
            s.Append("Restart=on-failure\n");
            s.Append("RestartSec=5\n");
            s.Append('\n');
            s.Append("[Install]\n");
            s.Append("WantedBy=multi-user.target\n");
            return s.ToString();
        }

        /// <summary>
        /// The post-install script: creates the service account and its working directory, refreshes the icon cache
        /// and the menu database, reloads systemd, and restarts the service if it was already running (an upgrade).
        /// It never enables or starts a service that was not running.
        /// </summary>
        /// <param name="artifact">Artifact.</param>
        /// <returns>Shell script.</returns>
        public static string AfterInstall(ArtifactSettings artifact)
        {
            ArgumentNullException.ThrowIfNull(artifact);
            StringBuilder s = new StringBuilder("#!/bin/sh\n");
            ServiceSettings? service = artifact.Service;
            if (service != null)
            {
                string user = service.User;
                string home = service.WorkingDirectory;
                s.Append("if ! getent group ").Append(user).Append(" >/dev/null 2>&1; then\n");
                s.Append("  groupadd --system ").Append(user).Append(" >/dev/null 2>&1 || true\n");
                s.Append("fi\n");
                s.Append("if ! getent passwd ").Append(user).Append(" >/dev/null 2>&1; then\n");
                s.Append("  useradd --system --gid ").Append(user).Append(" --home-dir ").Append(home).Append(" --no-create-home --shell /usr/sbin/nologin --comment \"").Append(service.Description).Append("\" ").Append(user).Append(" >/dev/null 2>&1 || true\n");
                s.Append("fi\n");
                s.Append("if [ ! -d ").Append(home).Append(" ]; then\n");
                s.Append("  mkdir -p ").Append(home).Append('\n');
                s.Append("  chown ").Append(user).Append(':').Append(user).Append(' ').Append(home).Append(" 2>/dev/null || true\n");
                s.Append("  chmod 750 ").Append(home).Append('\n');
                s.Append("fi\n");
            }

            s.Append("gtk-update-icon-cache -q -t /usr/share/icons/hicolor 2>/dev/null || true\n");
            s.Append("update-desktop-database -q /usr/share/applications 2>/dev/null || true\n");
            if (service != null)
            {
                s.Append("if [ -d /run/systemd/system ]; then\n");
                s.Append("  systemctl daemon-reload >/dev/null 2>&1 || true\n");
                s.Append("  systemctl try-restart ").Append(service.Name).Append(".service >/dev/null 2>&1 || true\n");
                s.Append("fi\n");
            }

            s.Append("exit 0\n");
            return s.ToString();
        }

        /// <summary>
        /// The pre-remove script: on removal (not upgrade) stops and disables the service. The service account and
        /// the working directory, which holds the user's data, are left alone.
        /// </summary>
        /// <param name="artifact">Artifact.</param>
        /// <returns>Shell script.</returns>
        public static string BeforeRemove(ArtifactSettings artifact)
        {
            ArgumentNullException.ThrowIfNull(artifact);
            StringBuilder s = new StringBuilder("#!/bin/sh\n");
            if (artifact.Service != null)
            {
                s.Append("case \"$1\" in\n");
                s.Append("  remove|purge|0)\n");
                s.Append("    if [ -d /run/systemd/system ]; then systemctl disable --now ").Append(artifact.Service.Name).Append(".service >/dev/null 2>&1 || true; fi\n");
                s.Append("    ;;\n");
                s.Append("esac\n");
            }

            s.Append("exit 0\n");
            return s.ToString();
        }

        /// <summary>
        /// The post-remove script: reloads systemd so a removed unit disappears.
        /// </summary>
        /// <param name="artifact">Artifact.</param>
        /// <returns>Shell script.</returns>
        public static string AfterRemove(ArtifactSettings artifact)
        {
            ArgumentNullException.ThrowIfNull(artifact);
            StringBuilder s = new StringBuilder("#!/bin/sh\n");
            if (artifact.Service != null) s.Append("if [ -d /run/systemd/system ]; then systemctl daemon-reload >/dev/null 2>&1 || true; fi\n");
            s.Append("exit 0\n");
            return s.ToString();
        }

        /// <summary>
        /// The AppImage entry point: runs the main executable in the current directory with the given arguments.
        /// </summary>
        /// <param name="artifact">Artifact.</param>
        /// <returns>Shell script.</returns>
        public static string AppRun(ArtifactSettings artifact)
        {
            ArgumentNullException.ThrowIfNull(artifact);
            return
                "#!/bin/sh\n" +
                "HERE=\"$(dirname \"$(readlink -f \"$0\")\")\"\n" +
                "exec \"$HERE/usr/bin/" + artifact.MainExecutable + "\" \"$@\"\n";
        }
    }
}
