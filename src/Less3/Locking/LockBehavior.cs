namespace Less3.Locking
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Behavior when a requested lock is currently held by someone else.
    /// </summary>
    public enum LockBehavior
    {
        /// <summary>
        /// Return immediately with a denial if the lock cannot be granted. This is the default and
        /// the correct choice for request-scoped object operations, which should fail fast rather
        /// than queue.
        /// </summary>
        [EnumMember(Value = "FailFast")]
        FailFast,

        /// <summary>
        /// Wait up to the configured timeout, retrying on a poll interval, before giving up.
        /// </summary>
        [EnumMember(Value = "Wait")]
        Wait
    }
}
