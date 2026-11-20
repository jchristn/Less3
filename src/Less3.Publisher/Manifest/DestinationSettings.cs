namespace Less3.Publisher.Manifest
{
    /// <summary>
    /// Where releases and package-manager manifests go. Everything lives in the project's own repository.
    /// </summary>
    public sealed class DestinationSettings
    {
        /// <summary>
        /// GitHub repository as owner/name, for example "jchristn/Less3". Release asset URLs are built from it.
        /// </summary>
        public string GithubRepo { get; set; } = "";

        /// <summary>
        /// GitHub Pages URL that hosts the apt and yum repositories.
        /// </summary>
        public string GithubPagesUrl { get; set; } = "";

        /// <summary>
        /// Homebrew tap repository, when the in-repo tap does not work.
        /// </summary>
        public string HomebrewTapRepo { get; set; } = "";

        /// <summary>
        /// Repository whose bucket/ folder holds the Scoop manifest.
        /// </summary>
        public string ScoopBucketRepo { get; set; } = "";

        /// <summary>
        /// The winget community repository.
        /// </summary>
        public string WingetPkgsRepo { get; set; } = "microsoft/winget-pkgs";

        /// <summary>
        /// NuGet feed for the .NET tool.
        /// </summary>
        public string NugetSource { get; set; } = "https://api.nuget.org/v3/index.json";
    }
}
