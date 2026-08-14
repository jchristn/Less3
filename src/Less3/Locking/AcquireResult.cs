namespace Less3.Locking
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Outcome of a lock acquisition attempt, surfaced on <see cref="LockDeniedException"/> when a
    /// lock could not be granted.
    /// </summary>
    public enum AcquireResult
    {
        /// <summary>
        /// The lock was granted.
        /// </summary>
        [EnumMember(Value = "Granted")]
        Granted,

        /// <summary>
        /// The lock is held by another holder and fail-fast behavior was requested.
        /// </summary>
        [EnumMember(Value = "Denied")]
        Denied,

        /// <summary>
        /// The wait timeout elapsed before the lock became free.
        /// </summary>
        [EnumMember(Value = "Timeout")]
        Timeout,

        /// <summary>
        /// The lock provider reported an error while attempting acquisition.
        /// </summary>
        [EnumMember(Value = "Error")]
        Error
    }
}
