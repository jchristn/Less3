namespace Less3.Classes
{
    using System;

    /// <summary>
    /// Membership and health information for a single node in a Less3 cluster.
    /// </summary>
    public class ClusterNodeInfo
    {
        /// <summary>
        /// Stable node identifier.
        /// </summary>
        public string NodeId { get; set; } = null;

        /// <summary>
        /// Machine hostname reported by the node.
        /// </summary>
        public string Hostname { get; set; } = null;

        /// <summary>
        /// Less3 version the node is running.
        /// </summary>
        public string Version { get; set; } = null;

        /// <summary>
        /// UTC time the node started.
        /// </summary>
        public DateTime? StartedUtc { get; set; } = null;

        /// <summary>
        /// UTC time the node last refreshed its membership row.
        /// </summary>
        public DateTime? LastSeenUtc { get; set; } = null;

        /// <summary>
        /// True when the node has refreshed its membership within the staleness window.
        /// </summary>
        public bool Healthy { get; set; } = false;

        /// <summary>
        /// True when this row represents the node serving the current request.
        /// </summary>
        public bool IsSelf { get; set; } = false;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterNodeInfo()
        {
        }
    }
}
