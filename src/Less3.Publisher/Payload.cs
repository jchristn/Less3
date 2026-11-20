namespace Less3.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Less3.Publisher.Manifest;

    /// <summary>
    /// Publishes an artifact for one runtime with a self-contained dotnet publish, plus, for an artifact with a
    /// Next.js app, the standalone app under app/ and the target's Node.js runtime under node/. Every channel starts
    /// from this payload, and each runtime is published once per Publisher run.
    /// </summary>
    public sealed class Payload
    {
        private readonly string _RepoRoot;
        private readonly ArtifactSettings _Artifact;
        private readonly string _Framework;
        private readonly string _Version;
        private readonly string _Root;
        private readonly Dictionary<string, string> _Published = new Dictionary<string, string>(StringComparer.Ordinal);
        private string? _NextBuild = null;

        /// <summary>
        /// Creates a payload builder.
        /// </summary>
        /// <param name="repoRoot">Repository root.</param>
        /// <param name="artifact">Artifact to publish.</param>
        /// <param name="framework">Target framework.</param>
        /// <param name="version">Version to stamp into the binaries.</param>
        /// <param name="root">Directory under which per-runtime publish folders are created.</param>
        /// <exception cref="ArgumentNullException">A required argument is null.</exception>
        public Payload(string repoRoot, ArtifactSettings artifact, string framework, string version, string root)
        {
            ArgumentNullException.ThrowIfNull(artifact);
            _RepoRoot = repoRoot ?? throw new ArgumentNullException(nameof(repoRoot));
            _Artifact = artifact;
            _Framework = framework ?? throw new ArgumentNullException(nameof(framework));
            _Version = version ?? throw new ArgumentNullException(nameof(version));
            _Root = root ?? throw new ArgumentNullException(nameof(root));
        }

        /// <summary>
        /// The dotnet publish arguments for a runtime.
        /// </summary>
        /// <param name="csproj">Absolute project path.</param>
        /// <param name="target">Runtime target.</param>
        /// <param name="output">Publish folder.</param>
        /// <returns>Arguments, one per item.</returns>
        public List<string> PublishArguments(string csproj, RuntimeTarget target, string output)
        {
            ArgumentNullException.ThrowIfNull(target);
            List<string> arguments = new List<string>
            {
                "publish", csproj,
                "-c", "Release",
                "-r", target.Rid,
                "-f", _Framework,
                "--self-contained", "true",
                "-p:Version=" + _Version,
                "-p:DebugType=none",
                "-p:GenerateDocumentationFile=false",
                "--nologo",
                "-o", output
            };
            if (!String.IsNullOrEmpty(_Artifact.AssemblyName)) arguments.Add("-p:AssemblyName=" + _Artifact.AssemblyName);
            if (_Artifact.SingleFile)
            {
                arguments.Add("-p:PublishSingleFile=true");
                arguments.Add("-p:IncludeNativeLibrariesForSelfExtract=true");
            }

            return arguments;
        }

        /// <summary>
        /// Publishes the artifact for a runtime, or returns the folder already published in this run.
        /// </summary>
        /// <param name="target">Runtime target.</param>
        /// <returns>The publish folder.</returns>
        /// <exception cref="PublisherException">The project is missing or the output lacks an expected executable.</exception>
        /// <exception cref="ProcessFailedException">dotnet publish failed.</exception>
        public string Publish(RuntimeTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (_Published.TryGetValue(target.Rid, out string? existing)) return existing;

            string csproj = Path.GetFullPath(Path.Combine(_RepoRoot, _Artifact.Csproj));
            if (!File.Exists(csproj)) throw new PublisherException("Artifact '" + _Artifact.Id + "' project not found at " + csproj + ".");

            string output = FileSystemUtil.Reset(Path.Combine(_Root, _Artifact.Id + "-" + target.Rid));
            List<string> arguments = PublishArguments(csproj, target, output);

            Console.WriteLine("[payload] publishing " + _Artifact.Id + " for " + target.Rid);
            ProcessRunner.Run("dotnet", arguments, _RepoRoot);

            foreach (string relative in _Artifact.RemoveFromPayload)
            {
                string path = Path.Combine(output, relative);
                if (File.Exists(path)) File.Delete(path);
                else if (Directory.Exists(path)) Directory.Delete(path, true);
            }

            if (_Artifact.NextApp != null) AddNextApp(_Artifact.NextApp, target, output);

            foreach (string executable in _Artifact.Executables)
            {
                string path = Path.Combine(output, executable + target.ExecutableExtension);
                if (!File.Exists(path)) throw new PublisherException("The " + target.Rid + " publish of " + _Artifact.Csproj + " has no " + executable + target.ExecutableExtension + ". Check build.artifacts[].executables and assemblyName in publisher.json.");
            }

            _Published[target.Rid] = output;
            return output;
        }

        /// <summary>
        /// Builds the artifact's Next.js app in standalone mode, once per run, and returns the folder holding the
        /// complete app: server.js, its traced node_modules, .next/static, and public.
        /// </summary>
        /// <returns>The assembled app folder.</returns>
        /// <exception cref="PublisherException">The artifact has no Next.js app, or the build produced no standalone
        /// server.</exception>
        /// <exception cref="ProcessFailedException">npm failed.</exception>
        public string BuildNextApp()
        {
            if (_NextBuild != null) return _NextBuild;
            NextAppSettings next = _Artifact.NextApp ?? throw new PublisherException("Artifact '" + _Artifact.Id + "' has no nextApp.");
            string project = Path.GetFullPath(Path.Combine(_RepoRoot, next.Directory));
            if (!File.Exists(Path.Combine(project, "package.json"))) throw new PublisherException("No package.json in " + project + ". Check build.artifacts[].nextApp.directory in publisher.json.");

            Console.WriteLine("[nextapp] building " + next.Directory + " (standalone)");
            if (!Directory.Exists(Path.Combine(project, "node_modules"))) Npm(project, new[] { "ci", "--no-audit", "--no-fund" }, null);
            string dotNext = Path.Combine(project, ".next");
            if (Directory.Exists(dotNext)) Directory.Delete(dotNext, true);
            Npm(project, new[] { "run", "build" }, next.BuildEnvironment);

            string standalone = Path.Combine(dotNext, "standalone");
            if (!File.Exists(Path.Combine(standalone, "server.js"))) throw new PublisherException("next build produced no " + Path.Combine(standalone, "server.js") + ". The build environment must select output: 'standalone' in next.config.mjs.");

            string app = FileSystemUtil.Reset(Path.Combine(_Root, _Artifact.Id + "-nextapp"));
            FileSystemUtil.CopyTree(standalone, app);
            FileSystemUtil.CopyTree(Path.Combine(dotNext, "static"), Path.Combine(app, ".next", "static"));
            if (Directory.Exists(Path.Combine(project, "public"))) FileSystemUtil.CopyTree(Path.Combine(project, "public"), Path.Combine(app, "public"));
            foreach (string relative in next.RemoveFromApp)
            {
                string path = Path.Combine(app, relative);
                if (Directory.Exists(path)) Directory.Delete(path, true);
                else if (File.Exists(path)) File.Delete(path);
            }

            string[] native = Directory.GetFiles(app, "*.node", SearchOption.AllDirectories);
            if (native.Length > 0) throw new PublisherException("The standalone app still contains native Node modules, which only work on the build host: " + String.Join(", ", native.Select(item => Path.GetRelativePath(app, item))) + ". Add them to nextApp.removeFromApp, or stop using them.");

            _NextBuild = app;
            return app;
        }

        private void AddNextApp(NextAppSettings next, RuntimeTarget target, string output)
        {
            FileSystemUtil.CopyTree(BuildNextApp(), Path.Combine(output, "app"));
            NodeRuntime.Install(next.NodeVersion, target, Path.Combine(_Root, "node-cache"), Path.Combine(output, "node"));
        }

        private static void Npm(string directory, IEnumerable<string> arguments, IDictionary<string, string>? environment)
        {
            if (RuntimeTarget.HostOs() == OsFamilyEnum.Windows)
            {
                List<string> wrapped = new List<string> { "/d", "/s", "/c", "npm" };
                wrapped.AddRange(arguments);
                ProcessRunner.Run("cmd.exe", wrapped, directory, environment);
            }
            else
            {
                ProcessRunner.Run("npm", arguments, directory, environment);
            }
        }
    }
}
