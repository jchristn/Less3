namespace Less3.Publisher.Channels
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A channel that is declared but not built yet. Running it fails with what implementing it involves, instead of
    /// silently producing nothing.
    /// </summary>
    public sealed class StubChannel : IChannel
    {
        /// <inheritdoc/>
        public string Id { get; }

        /// <inheritdoc/>
        public string DisplayName { get; }

        /// <inheritdoc/>
        public OsFamilyEnum RequiredHost => OsFamilyEnum.Any;

        /// <inheritdoc/>
        public OsFamilyEnum TargetOs => OsFamilyEnum.Linux;

        /// <inheritdoc/>
        public IReadOnlyList<ToolRequirement> Tools => Array.Empty<ToolRequirement>();

        /// <summary>
        /// What implementing the channel involves.
        /// </summary>
        public string Plan { get; }

        /// <summary>
        /// Creates a stub.
        /// </summary>
        /// <param name="id">Channel id.</param>
        /// <param name="displayName">Display name.</param>
        /// <param name="plan">What implementing it involves.</param>
        public StubChannel(string id, string displayName, string plan)
        {
            Id = id;
            DisplayName = displayName;
            Plan = plan;
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> Build(ChannelContext context)
        {
            throw new PublisherException("Channel '" + Id + "' (" + DisplayName + ") is declared but not implemented. Planned approach: " + Plan);
        }
    }
}
