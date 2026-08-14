namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Aggregate health of a Less3 deployment, suitable for a dashboard or an orchestrator probe.
    /// </summary>
    public class ClusterHealthResult
    {
        /// <summary>
        /// True when the deployment is running as a multi-node cluster.
        /// </summary>
        public bool ClusterEnabled { get; set; } = false;

        /// <summary>
        /// Configured lock provider (Local, Postgres, or Clutch).
        /// </summary>
        public string LockProvider { get; set; } = null;

        /// <summary>
        /// Identifier of the node that answered this request.
        /// </summary>
        public string SelfNodeId { get; set; } = null;

        /// <summary>
        /// Total number of known nodes.
        /// </summary>
        public int TotalNodes { get; set; } = 0;

        /// <summary>
        /// Number of nodes considered healthy.
        /// </summary>
        public int HealthyNodes { get; set; } = 0;

        /// <summary>
        /// Per-node membership detail.
        /// </summary>
        public List<ClusterNodeInfo> Nodes { get; set; } = new List<ClusterNodeInfo>();

        /// <summary>
        /// UTC time this result was generated.
        /// </summary>
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ClusterHealthResult()
        {
        }
    }
}
