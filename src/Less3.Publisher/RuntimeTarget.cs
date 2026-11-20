namespace Less3.Publisher
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A runtime identifier (win-x64, osx-arm64, linux-x64, ...) with the names each packaging format uses for its
    /// architecture.
    /// </summary>
    public sealed class RuntimeTarget
    {
        /// <summary>
        /// The runtime identifier, for example "linux-arm64".
        /// </summary>
        public string Rid { get; }

        /// <summary>
        /// Operating system family.
        /// </summary>
        public OsFamilyEnum Os { get; }

        /// <summary>
        /// True for an ARM64 target.
        /// </summary>
        public bool IsArm64 { get; }

        /// <summary>
        /// Debian architecture: amd64 or arm64.
        /// </summary>
        public string DebArch => IsArm64 ? "arm64" : "amd64";

        /// <summary>
        /// RPM and AppImage architecture: x86_64 or aarch64.
        /// </summary>
        public string RpmArch => IsArm64 ? "aarch64" : "x86_64";

        /// <summary>
        /// Inno Setup architecture identifier: arm64 or x64compatible.
        /// </summary>
        public string InnoArch => IsArm64 ? "arm64" : "x64compatible";

        /// <summary>
        /// macOS Installer host architectures. The Intel package also installs on Apple silicon, where it runs under
        /// Rosetta.
        /// </summary>
        public string MacHostArchitectures => IsArm64 ? "arm64" : "x86_64,arm64";

        /// <summary>
        /// Executable file extension: ".exe" on Windows, otherwise empty.
        /// </summary>
        public string ExecutableExtension => Os == OsFamilyEnum.Windows ? ".exe" : "";

        private RuntimeTarget(string rid, OsFamilyEnum os, bool isArm64)
        {
            Rid = rid;
            Os = os;
            IsArm64 = isArm64;
        }

        /// <summary>
        /// Parses a supported runtime identifier.
        /// </summary>
        /// <param name="rid">Runtime identifier.</param>
        /// <param name="target">The parsed target, or null when the identifier is not supported.</param>
        /// <returns>True when the identifier is supported.</returns>
        public static bool TryParse(string rid, [NotNullWhen(true)] out RuntimeTarget? target)
        {
            target = null;
            if (String.IsNullOrWhiteSpace(rid)) return false;
            string[] parts = rid.Split('-');
            if (parts.Length != 2) return false;

            OsFamilyEnum os;
            switch (parts[0])
            {
                case "win": os = OsFamilyEnum.Windows; break;
                case "osx": os = OsFamilyEnum.MacOs; break;
                case "linux": os = OsFamilyEnum.Linux; break;
                default: return false;
            }

            if (parts[1] != "x64" && parts[1] != "arm64") return false;
            target = new RuntimeTarget(rid, os, parts[1] == "arm64");
            return true;
        }

        /// <summary>
        /// Parses a supported runtime identifier.
        /// </summary>
        /// <param name="rid">Runtime identifier.</param>
        /// <returns>The target.</returns>
        /// <exception cref="ArgumentException">The identifier is not one of the six supported runtimes.</exception>
        public static RuntimeTarget Parse(string rid)
        {
            if (!TryParse(rid, out RuntimeTarget? target)) throw new ArgumentException("Unsupported runtime identifier '" + rid + "'. Use win-x64, win-arm64, osx-x64, osx-arm64, linux-x64, or linux-arm64.", nameof(rid));
            return target;
        }

        /// <summary>
        /// The operating system family the Publisher is running on.
        /// </summary>
        /// <returns>The host's family, or Any when it is none of the three.</returns>
        public static OsFamilyEnum HostOs()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return OsFamilyEnum.Windows;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return OsFamilyEnum.MacOs;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return OsFamilyEnum.Linux;
            return OsFamilyEnum.Any;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Rid;
        }
    }
}
