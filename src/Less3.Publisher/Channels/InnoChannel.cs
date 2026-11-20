namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Windows installers: one Inno Setup .exe per architecture. Runs on Windows with Inno Setup 6. When CI is not
    /// available it can also run on macOS or Linux: set LESS3_ISCC to a command that runs Inno Setup's compiler
    /// under Wine (src/Less3.Publisher/iscc-docker.sh runs it in Docker), and the script's paths are written as Wine Z: paths.
    /// </summary>
    public sealed class InnoChannel : IChannel
    {
        private static readonly int[] _WizardScales = new[] { 100, 125, 150, 175, 200 };

        /// <inheritdoc/>
        public string Id => "inno";

        /// <inheritdoc/>
        public string DisplayName => "Windows installer (Inno Setup)";

        /// <summary>
        /// Environment variable naming an Inno Setup compiler command to use instead of a Windows install.
        /// </summary>
        public const string CompilerVariable = "LESS3_ISCC";

        /// <inheritdoc/>
        public OsFamilyEnum RequiredHost => String.IsNullOrEmpty(Environment.GetEnvironmentVariable(CompilerVariable)) ? OsFamilyEnum.Windows : OsFamilyEnum.Any;

        /// <inheritdoc/>
        public OsFamilyEnum TargetOs => OsFamilyEnum.Windows;

        /// <inheritdoc/>
        public IReadOnlyList<ToolRequirement> Tools => Array.Empty<ToolRequirement>();

        /// <inheritdoc/>
        public IReadOnlyList<string> Build(ChannelContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            string iscc = ResolveIscc();
            List<string> outputs = new List<string>();
            foreach (RuntimeTarget target in context.Targets)
            {
                string payload = context.Payload.Publish(target);
                string stage = context.FreshStaging("inno-" + target.Rid);
                File.Copy(context.LicensePath(), Path.Combine(stage, "LICENSE.txt"), true);
                IcoWriter.Write(context.LogoPath(), Path.Combine(stage, "setup.ico"));
                if (context.Artifact.Launcher != null)
                {
                    File.WriteAllText(Path.Combine(stage, InnoScript.LauncherFile(context.Artifact.Launcher)), LauncherScripts.WindowsCmd(context.Project, context.Artifact));
                }

                InnoScriptInput input = new InnoScriptInput
                {
                    AppId = String.IsNullOrWhiteSpace(context.Artifact.WindowsAppId) ? context.Option("appId", "") : context.Artifact.WindowsAppId,
                    Project = context.Project,
                    Artifact = context.Artifact,
                    Version = context.Version,
                    Target = target,
                    PayloadDir = ScriptPath(payload),
                    OutputDir = ScriptPath(context.OutputDir),
                    OutputBaseName = Path.GetFileNameWithoutExtension(AssetNames.WindowsSetup(context.Project.Name, context.Version, target))
                };

                string logo = context.LogoPath();
                foreach (int scale in _WizardScales)
                {
                    string large = "wizard-" + scale + ".bmp";
                    string small = "wizard-small-" + scale + ".bmp";
                    ImageAssets.Banner(logo, 164 * scale / 100, 314 * scale / 100, 0.7, Path.Combine(stage, large));
                    ImageAssets.Banner(logo, 55 * scale / 100, 55 * scale / 100, 0.9, Path.Combine(stage, small));
                    input.WizardImages.Add(large);
                    input.WizardSmallImages.Add(small);
                }

                string script = Path.Combine(stage, context.Project.Name + "-" + target.Rid + ".iss");
                File.WriteAllText(script, InnoScript.Render(input), new UTF8Encoding(true));

                Console.WriteLine("[inno] compiling " + input.OutputBaseName + ".exe");
                ProcessRunner.Run(iscc, new[] { "/Q", script }, stage);

                string produced = Path.Combine(context.OutputDir, input.OutputBaseName + ".exe");
                if (!File.Exists(produced)) throw new PublisherException("Inno Setup finished but " + produced + " is missing.");
                Signing.SignWindows(produced, context.Manifest.Signing.Windows);
                outputs.Add(produced);
            }

            return outputs;
        }

        /// <summary>
        /// A path as the Inno Setup compiler sees it: unchanged on Windows, and a Wine Z: path elsewhere.
        /// </summary>
        /// <param name="path">Absolute host path.</param>
        /// <returns>The path to write into the script.</returns>
        public static string ScriptPath(string path)
        {
            if (RuntimeTarget.HostOs() == OsFamilyEnum.Windows) return path;
            return WinePath(path);
        }

        /// <summary>
        /// The Wine path of a Unix path: "/a/b" becomes "Z:\a\b".
        /// </summary>
        /// <param name="path">Absolute Unix path.</param>
        /// <returns>Wine path.</returns>
        public static string WinePath(string path)
        {
            return "Z:" + (path ?? "").Replace('/', '\\');
        }

        private static string ResolveIscc()
        {
            string? configured = Environment.GetEnvironmentVariable(CompilerVariable);
            if (!String.IsNullOrEmpty(configured)) return configured;

            string[] candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Inno Setup 6", "ISCC.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Inno Setup 6", "ISCC.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Inno Setup 6", "ISCC.exe")
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }

            if (ProcessRunner.IsAvailable("iscc", "/?")) return "iscc";
            throw new PublisherException("Inno Setup 6 (ISCC.exe) was not found. Install it with: choco install innosetup --no-progress -y  (or winget install JRSoftware.InnoSetup)");
        }
    }
}
