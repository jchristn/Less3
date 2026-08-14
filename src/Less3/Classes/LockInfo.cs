namespace Less3.Classes
{
    using System;

    /// <summary>
    /// A currently-held distributed lock, for operational visibility. Read-only; observing locks
    /// never affects them.
    /// </summary>
    public class LockInfo
    {
        /// <summary>
        /// Lock key.
        /// </summary>
        public string LockKey { get; set; } = null;

        /// <summary>
        /// Mode in which the lock is held.
        /// </summary>
        public string Mode { get; set; } = null;

        /// <summary>
        /// Current holder identifier.
        /// </summary>
        public string HolderId { get; set; } = null;

        /// <summary>
        /// Current per-key fencing token.
        /// </summary>
        public long FencingToken { get; set; } = 0;

        /// <summary>
        /// Node that holds the lock.
        /// </summary>
        public string NodeId { get; set; } = null;

        /// <summary>
        /// UTC time the lock was acquired.
        /// </summary>
        public DateTime? AcquiredUtc { get; set; } = null;

        /// <summary>
        /// UTC time the lease expires.
        /// </summary>
        public DateTime? LeaseExpiresUtc { get; set; } = null;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public LockInfo()
        {
        }
    }
}
