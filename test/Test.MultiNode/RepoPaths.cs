namespace Test.MultiNode
{
    using System;
    using System.IO;

    /// <summary>
    /// Locates key repository paths by walking up from the running assembly until the solution file
    /// is found, so the harness works regardless of the current working directory.
    /// </summary>
    public static class RepoPaths
    {
        /// <summary>
        /// Absolute path to the repository root (the directory whose <c>src</c> subdirectory holds
        /// Less3.sln).
        /// </summary>
        public static string RepoRoot { get; }

        /// <summary>
        /// Absolute path to the <c>src</c> directory (the Docker build context).
        /// </summary>
        public static string SrcDir { get; }

        static RepoPaths()
        {
            string dir = AppContext.BaseDirectory;
            string found = null;

            DirectoryInfo current = new DirectoryInfo(dir);
            while (current != null)
            {
                string candidate = Path.Combine(current.FullName, "src", "Less3.sln");
                if (File.Exists(candidate))
                {
                    found = current.FullName;
                    break;
                }
                current = current.Parent;
            }

            if (found == null) throw new DirectoryNotFoundException("Unable to locate repository root (src/Less3.sln) above " + dir + ".");

            RepoRoot = found;
            SrcDir = Path.Combine(found, "src");
        }
    }
}
