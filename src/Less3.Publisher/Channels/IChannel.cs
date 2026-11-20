namespace Less3.Publisher.Channels
{
    using System.Collections.Generic;

    /// <summary>
    /// One packaging channel. A channel takes the published payload and produces installer files, or reads built
    /// installers and produces package-manager manifests.
    /// </summary>
    public interface IChannel
    {
        /// <summary>
        /// Channel id as publisher.json names it, for example "inno".
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Name for logs.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The operating system the channel must run on, or Any. The Publisher refuses to run a channel elsewhere,
        /// with a message naming the right runner, instead of failing deep inside a missing tool.
        /// </summary>
        OsFamilyEnum RequiredHost { get; }

        /// <summary>
        /// The operating system whose runtimes the channel builds when the manifest lists none for it. Any means the
        /// host's own operating system.
        /// </summary>
        OsFamilyEnum TargetOs { get; }

        /// <summary>
        /// Tools the channel needs, each with the command that installs it. Checked before anything is built.
        /// </summary>
        IReadOnlyList<ToolRequirement> Tools { get; }

        /// <summary>
        /// Builds the channel's output.
        /// </summary>
        /// <param name="context">Everything the channel needs for this run.</param>
        /// <returns>Absolute paths of the files produced.</returns>
        IReadOnlyList<string> Build(ChannelContext context);
    }
}
