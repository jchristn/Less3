namespace Less3.Settings
{
    using System;

    /// <summary>
    /// Bounded caching of authentication and authorization results. Caching credentials and
    /// authorization decisions avoids a database round trip on every request, but a stale entry
    /// must never outlive a revocation by more than the configured TTL. Entries are also dropped
    /// immediately when the credential/role/session epoch advances after any mutation.
    /// </summary>
    public class AuthCacheSettings
    {
        #region Public-Members

        /// <summary>
        /// Enable or disable auth caching. Default value is false, meaning every request resolves
        /// credentials and authorization live against the database — the strictly safest posture,
        /// with no possibility of a revoked credential lingering. Enabling it trades a bounded
        /// staleness window (<see cref="TtlMs"/>) for fewer control-plane reads.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Maximum lifetime of a cached authentication/authorization entry, in milliseconds.
        /// A revoked credential, deleted session, or changed role stops working within this
        /// window even absent an explicit epoch bump. Default value is 15000 (15 seconds).
        /// Minimum value is 1000. Maximum value is 300000 (5 minutes).
        /// </summary>
        public int TtlMs
        {
            get { return _TtlMs; }
            set
            {
                if (value < 1000 || value > 300000)
                    throw new ArgumentOutOfRangeException(nameof(TtlMs), "TtlMs must be between 1000 and 300000.");
                _TtlMs = value;
            }
        }

        /// <summary>
        /// Maximum number of entries retained in the auth cache before least-recently-used
        /// eviction. Default value is 10000. Minimum value is 100.
        /// </summary>
        public int MaxEntries
        {
            get { return _MaxEntries; }
            set
            {
                if (value < 100) throw new ArgumentOutOfRangeException(nameof(MaxEntries), "MaxEntries must be at least 100.");
                _MaxEntries = value;
            }
        }

        #endregion

        #region Private-Members

        private int _TtlMs = 15000;
        private int _MaxEntries = 10000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with default settings.
        /// </summary>
        public AuthCacheSettings()
        {
        }

        #endregion
    }
}
