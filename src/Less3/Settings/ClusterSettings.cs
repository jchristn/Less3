namespace Less3.Settings
{
    using System;

    /// <summary>
    /// Multi-node cluster configuration. When <see cref="Enabled"/> is false, Less3 runs as a
    /// standalone single node (the default for a native binary run): SQLite is acceptable, the
    /// lock provider is in-process, and no cluster membership is tracked. When enabled, Less3
    /// participates in a scale-out cluster and requires a networked control-plane database
    /// (PostgreSQL) plus shared storage mounted identically on every node.
    /// </summary>
    public class ClusterSettings
    {
        #region Public-Members

        /// <summary>
        /// Enable multi-node cluster behavior. Default value is false. When true, the database
        /// must not be SQLite and the lock provider must not be Local; startup validation enforces
        /// this because a shared SQLite file cannot back multiple writers.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Stable identifier for this node, used in lock holder identifiers, cluster membership,
        /// metrics labels, and request-history attribution. Null or empty means the identifier is
        /// resolved at startup from the machine name plus a short generated suffix.
        /// </summary>
        public string NodeId { get; set; } = null;

        /// <summary>
        /// Distributed lock provider. Default value is <see cref="LockProviderEnum.Local"/>.
        /// Cluster deployments use <see cref="LockProviderEnum.Postgres"/> (the DB is the lock
        /// authority) or, optionally, <see cref="LockProviderEnum.Clutch"/>.
        /// </summary>
        public LockProviderEnum LockProvider { get; set; } = LockProviderEnum.Local;

        /// <summary>
        /// Interval at which this node refreshes its membership row, in milliseconds.
        /// Default value is 10000 (10 seconds). Minimum value is 1000.
        /// </summary>
        public int NodeHeartbeatIntervalMs
        {
            get { return _NodeHeartbeatIntervalMs; }
            set
            {
                if (value < 1000) throw new ArgumentOutOfRangeException(nameof(NodeHeartbeatIntervalMs), "NodeHeartbeatIntervalMs must be at least 1000.");
                _NodeHeartbeatIntervalMs = value;
            }
        }

        /// <summary>
        /// Age after which a node that has not refreshed its membership row is considered stale
        /// (down), in milliseconds. Should be a small multiple of
        /// <see cref="NodeHeartbeatIntervalMs"/>. Default value is 30000 (30 seconds).
        /// Minimum value is 2000.
        /// </summary>
        public int NodeStaleAfterMs
        {
            get { return _NodeStaleAfterMs; }
            set
            {
                if (value < 2000) throw new ArgumentOutOfRangeException(nameof(NodeStaleAfterMs), "NodeStaleAfterMs must be at least 2000.");
                _NodeStaleAfterMs = value;
            }
        }

        /// <summary>
        /// How long a cached bucket client is trusted before it is revalidated against the control
        /// plane, in milliseconds. Only applies in cluster mode (single-node trusts its cache
        /// indefinitely because it is the only writer). Lower values converge faster on cross-node
        /// bucket create/delete/config changes at the cost of more control-plane reads. Default
        /// value is 5000 (5 seconds). Minimum value is 500.
        /// </summary>
        public int BucketClientCacheTtlMs
        {
            get { return _BucketClientCacheTtlMs; }
            set
            {
                if (value < 500) throw new ArgumentOutOfRangeException(nameof(BucketClientCacheTtlMs), "BucketClientCacheTtlMs must be at least 500.");
                _BucketClientCacheTtlMs = value;
            }
        }

        /// <summary>
        /// Lock manager tuning.
        /// </summary>
        public LockSettings Lock
        {
            get { return _Lock; }
            set { _Lock = value ?? throw new ArgumentNullException(nameof(Lock)); }
        }

        /// <summary>
        /// Configuration for the optional Clutch lock provider.
        /// </summary>
        public ClutchSettings Clutch
        {
            get { return _Clutch; }
            set { _Clutch = value ?? throw new ArgumentNullException(nameof(Clutch)); }
        }

        /// <summary>
        /// Bounded authentication/authorization caching.
        /// </summary>
        public AuthCacheSettings AuthCache
        {
            get { return _AuthCache; }
            set { _AuthCache = value ?? throw new ArgumentNullException(nameof(AuthCache)); }
        }

        #endregion

        #region Private-Members

        private int _NodeHeartbeatIntervalMs = 10000;
        private int _NodeStaleAfterMs = 30000;
        private int _BucketClientCacheTtlMs = 5000;
        private LockSettings _Lock = new LockSettings();
        private ClutchSettings _Clutch = new ClutchSettings();
        private AuthCacheSettings _AuthCache = new AuthCacheSettings();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with default (single-node) settings.
        /// </summary>
        public ClusterSettings()
        {
        }

        #endregion
    }
}
