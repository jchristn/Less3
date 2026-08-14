namespace Less3.Settings
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Distributed lock provider selection for multi-node coordination.
    /// </summary>
    public enum LockProviderEnum
    {
        /// <summary>
        /// In-process locking for single-node deployments. Fencing tokens are an in-memory
        /// per-key monotonic counter. This is the default and is the only supported provider
        /// when the database is SQLite.
        /// </summary>
        [EnumMember(Value = "Local")]
        Local,

        /// <summary>
        /// Native PostgreSQL-backed distributed lock manager. The database is the single
        /// authority for lock state; acquisition is serialized per key and protected by
        /// monotonic fencing tokens. This is the default in cluster mode.
        /// </summary>
        [EnumMember(Value = "Postgres")]
        Postgres,

        /// <summary>
        /// Optional Clutch-backed distributed lock manager. Clutch shares the same PostgreSQL
        /// database via "bring your own database", so the database remains authoritative.
        /// Opt-in only.
        /// </summary>
        [EnumMember(Value = "Clutch")]
        Clutch
    }
}
