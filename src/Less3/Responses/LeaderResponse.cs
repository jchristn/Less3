namespace Less3.Responses
{
    /// <summary>
    /// Response describing the node currently holding the cluster cleanup-leader lease.
    /// </summary>
    public class LeaderResponse
    {
        /// <summary>
        /// Identifier of the leader node, or null when no node currently holds the lease.
        /// </summary>
        public string LeaderNodeId { get; set; } = null;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public LeaderResponse()
        {
        }
    }
}
