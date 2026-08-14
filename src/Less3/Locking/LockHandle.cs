namespace Less3.Locking
{
    using System;

    /// <summary>
    /// A granted lock. The handle is the caller's proof of ownership; it carries the fencing token
    /// that a downstream mutation re-checks so a holder whose lease has expired cannot corrupt data.
    /// A handle is single-use: acquire, optionally validate before committing a mutation, then
    /// release.
    /// </summary>
    public class LockHandle
    {
        #region Public-Members

        /// <summary>
        /// The lock key that was acquired.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The mode in which the lock was granted.
        /// </summary>
        public LockMode Mode { get; }

        /// <summary>
        /// Unique identifier for this specific acquisition. Used to release and renew, and to
        /// prove ownership at the point of a guarded mutation.
        /// </summary>
        public string HolderId { get; }

        /// <summary>
        /// Per-key monotonically increasing fencing token. A larger token supersedes a smaller one.
        /// A mutation carrying a stale token (because the lease lapsed and another holder took over)
        /// is rejected. Zero indicates a non-fencing handle (for example a non-blocking read).
        /// </summary>
        public long FencingToken { get; }

        /// <summary>
        /// The lock provider that issued this handle (for example "Local", "Postgres", "Clutch").
        /// </summary>
        public string Provider { get; }

        /// <summary>
        /// Absolute UTC time at which the lease currently expires, as computed by the lock
        /// authority. Updated as the handle is renewed.
        /// </summary>
        public DateTime LeaseExpiresUtc
        {
            get { return _LeaseExpiresUtc; }
            internal set { _LeaseExpiresUtc = value; }
        }

        /// <summary>
        /// True once the lock manager has determined the lease was lost (renewal failed or the key
        /// was taken over). A lost handle must not be used to commit a mutation.
        /// </summary>
        public bool IsLost
        {
            get { return _IsLost; }
            internal set { _IsLost = value; }
        }

        /// <summary>
        /// Opaque provider-specific state carried with the handle (for example a Clutch session
        /// identifier). Not meaningful to callers.
        /// </summary>
        public string ProviderState
        {
            get { return _ProviderState; }
            internal set { _ProviderState = value; }
        }

        #endregion

        #region Private-Members

        private DateTime _LeaseExpiresUtc;
        private bool _IsLost = false;
        private string _ProviderState = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate a lock handle.
        /// </summary>
        /// <param name="key">Lock key.</param>
        /// <param name="mode">Granted mode.</param>
        /// <param name="holderId">Unique holder identifier.</param>
        /// <param name="fencingToken">Per-key monotonic fencing token.</param>
        /// <param name="leaseExpiresUtc">Absolute UTC lease expiry.</param>
        /// <param name="provider">Issuing provider name.</param>
        /// <exception cref="ArgumentNullException">Thrown when key, holderId, or provider is null or empty.</exception>
        public LockHandle(string key, LockMode mode, string holderId, long fencingToken, DateTime leaseExpiresUtc, string provider)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrEmpty(holderId)) throw new ArgumentNullException(nameof(holderId));
            if (String.IsNullOrEmpty(provider)) throw new ArgumentNullException(nameof(provider));

            Key = key;
            Mode = mode;
            HolderId = holderId;
            FencingToken = fencingToken;
            _LeaseExpiresUtc = leaseExpiresUtc;
            Provider = provider;
        }

        #endregion
    }
}
