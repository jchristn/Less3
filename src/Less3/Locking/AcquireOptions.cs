namespace Less3.Locking
{
    using System;

    /// <summary>
    /// Options controlling a single lock acquisition. All values are optional; unset values fall
    /// back to the lock manager's configured defaults.
    /// </summary>
    public class AcquireOptions
    {
        #region Public-Members

        /// <summary>
        /// Behavior when the lock is contended. Default value is <see cref="LockBehavior.FailFast"/>.
        /// </summary>
        public LockBehavior Behavior { get; set; } = LockBehavior.FailFast;

        /// <summary>
        /// Maximum time to wait for a contended lock when <see cref="Behavior"/> is
        /// <see cref="LockBehavior.Wait"/>, in milliseconds. Null uses the configured default.
        /// Ignored for fail-fast acquisition.
        /// </summary>
        public int? TimeoutMs { get; set; } = null;

        /// <summary>
        /// Lease duration to request, in milliseconds. Null uses the configured default. The lease
        /// is renewed automatically while the handle is held.
        /// </summary>
        public int? LeaseMs { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with default (fail-fast) options.
        /// </summary>
        public AcquireOptions()
        {
        }

        /// <summary>
        /// Instantiate wait-behavior options with a timeout.
        /// </summary>
        /// <param name="timeoutMs">Maximum time to wait, in milliseconds.</param>
        public AcquireOptions(int timeoutMs)
        {
            if (timeoutMs < 0) throw new ArgumentOutOfRangeException(nameof(timeoutMs), "timeoutMs cannot be negative.");
            Behavior = LockBehavior.Wait;
            TimeoutMs = timeoutMs;
        }

        #endregion
    }
}
