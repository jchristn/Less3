namespace Less3.Locking
{
    using System;

    /// <summary>
    /// Thrown when an operation attempts to use a lock handle whose lease has been lost — because a
    /// renewal failed or another holder took the key over after the lease lapsed. A caller that
    /// sees this must abandon the in-flight mutation; committing it would risk data corruption.
    /// </summary>
    public class LockLostException : Exception
    {
        #region Public-Members

        /// <summary>
        /// The lock key whose lease was lost.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The holder identifier that lost the lease.
        /// </summary>
        public string HolderId { get; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="key">Lock key.</param>
        /// <param name="holderId">Holder identifier.</param>
        public LockLostException(string key, string holderId)
            : base("Lock '" + key + "' lease was lost by holder '" + holderId + "'; the in-flight operation must be abandoned.")
        {
            Key = key;
            HolderId = holderId;
        }

        #endregion
    }
}
