namespace Less3.Publisher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Less3.Publisher.Manifest;

    /// <summary>
    /// Publishes an artifact for one runtime with a self-contained dotnet publish. Every channel starts from this
    /// payload, and each runtime is published once per Publisher run.
    /// </summary>
    public sealed class Payload
    {
        private readonly string _RepoRoot;
        private readonly ArtifactSettings _Artifact;
        private readonly string _Framework;
        private readonly string _Version;
        private readonly string _Root;
        private readonly Dictionary<string, string> _Published = new Dictionary<string, string>(StringComparer.Ordinal);

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

            foreach (string executable in _Artifact.Executables)
            {
                string path = Path.Combine(output, executable + target.ExecutableExtension);
                if (!File.Exists(path)) throw new PublisherException("The " + target.Rid + " publish of " + _Artifact.Csproj + " has no " + executable + target.ExecutableExtension + ". Check build.artifacts[].executables and assemblyName in publisher.json.");
            }

            _Published[target.Rid] = output;
            return output;
        }
    }
}
