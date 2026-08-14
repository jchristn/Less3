namespace Less3.Locking
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Access mode requested when acquiring a distributed lock. Modes map onto S3 object access
    /// semantics. Because object blobs are immutable (each write produces a new object identifier),
    /// reads do not contend with writes and <see cref="Read"/> is non-blocking; the exclusivity
    /// that protects data integrity is between writers and deleters.
    /// </summary>
    public enum LockMode
    {
        /// <summary>
        /// Shared, non-mutating access. Non-blocking against the immutable-blob model; provided for
        /// API completeness and future use.
        /// </summary>
        [EnumMember(Value = "Read")]
        Read,

        /// <summary>
        /// Exclusive access for a mutating writer. Only one holder at a time for a given key.
        /// </summary>
        [EnumMember(Value = "Write")]
        Write,

        /// <summary>
        /// Fully exclusive access for a deleter. Behaves exclusively like <see cref="Write"/>.
        /// </summary>
        [EnumMember(Value = "Delete")]
        Delete
    }
}
