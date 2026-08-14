namespace Less3.Settings
{
    using System;

    /// <summary>
    /// Distributed lock manager tuning. Every value is in milliseconds unless otherwise noted.
    /// Lease expiry is always evaluated with the database clock, never a node clock, so these
    /// values are safe across nodes with skewed clocks.
    /// </summary>
    public class LockSettings
    {
        #region Public-Members

        /// <summary>
        /// Default lease duration granted on acquire, in milliseconds.
        /// The lease is renewed automatically by the lock manager's heartbeat loop while the
        /// handle is held. Default value is 30000 (30 seconds). Minimum value is 1000.
        /// Choose a value comfortably larger than the longest single locked operation.
        /// </summary>
        public int DefaultLeaseMs
        {
            get { return _DefaultLeaseMs; }
            set
            {
                if (value < 1000) throw new ArgumentOutOfRangeException(nameof(DefaultLeaseMs), "DefaultLeaseMs must be at least 1000.");
                _DefaultLeaseMs = value;
            }
        }

        /// <summary>
        /// Interval at which held leases are renewed, in milliseconds.
        /// Default value is 10000 (10 seconds). Minimum value is 500.
        /// Should be well under <see cref="DefaultLeaseMs"/> so a renewal is attempted several
        /// times before a lease could lapse.
        /// </summary>
        public int HeartbeatIntervalMs
        {
            get { return _HeartbeatIntervalMs; }
            set
            {
                if (value < 500) throw new ArgumentOutOfRangeException(nameof(HeartbeatIntervalMs), "HeartbeatIntervalMs must be at least 500.");
                _HeartbeatIntervalMs = value;
            }
        }

        /// <summary>
        /// Hard ceiling on how long a single holder may keep a lock, in milliseconds, regardless
        /// of heartbeats. Prevents a wedged holder from keeping a key forever.
        /// Default value is 3600000 (1 hour). Minimum value is 1000.
        /// </summary>
        public int MaxHoldMs
        {
            get { return _MaxHoldMs; }
            set
            {
                if (value < 1000) throw new ArgumentOutOfRangeException(nameof(MaxHoldMs), "MaxHoldMs must be at least 1000.");
                _MaxHoldMs = value;
            }
        }

        /// <summary>
        /// Default maximum time to wait for a contended lock when acquiring with Wait behavior,
        /// in milliseconds. Default value is 15000 (15 seconds). Minimum value is 0
        /// (0 means fail immediately if not free).
        /// </summary>
        public int AcquireTimeoutMs
        {
            get { return _AcquireTimeoutMs; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(AcquireTimeoutMs), "AcquireTimeoutMs cannot be negative.");
                _AcquireTimeoutMs = value;
            }
        }

        /// <summary>
        /// Poll interval used while waiting for a contended lock, in milliseconds.
        /// Cross-node waiters wake on this interval; correctness is unaffected because the
        /// acquire transaction is the only authority. Default value is 250. Minimum value is 25.
        /// </summary>
        public int WaiterPollMs
        {
            get { return _WaiterPollMs; }
            set
            {
                if (value < 25) throw new ArgumentOutOfRangeException(nameof(WaiterPollMs), "WaiterPollMs must be at least 25.");
                _WaiterPollMs = value;
            }
        }

        #endregion

        #region Private-Members

        private int _DefaultLeaseMs = 30000;
        private int _HeartbeatIntervalMs = 10000;
        private int _MaxHoldMs = 3600000;
        private int _AcquireTimeoutMs = 15000;
        private int _WaiterPollMs = 250;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with default settings.
        /// </summary>
        public LockSettings()
        {
        }

        #endregion
    }
}
