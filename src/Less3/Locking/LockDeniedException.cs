namespace Less3.Locking
{
    using System;

    /// <summary>
    /// Thrown when a distributed lock cannot be granted, either because it is held by another
    /// holder (fail-fast) or because the wait timeout elapsed.
    /// </summary>
    public class LockDeniedException : Exception
    {
        #region Public-Members

        /// <summary>
        /// The lock key that was requested.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The reason the lock was not granted.
        /// </summary>
        public AcquireResult Result { get; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="key">Lock key.</param>
        /// <param name="result">Denial reason.</param>
        public LockDeniedException(string key, AcquireResult result)
            : base("Lock '" + key + "' could not be acquired: " + result + ".")
        {
            Key = key;
            Result = result;
        }

        /// <summary>
        /// Instantiate with an inner exception.
        /// </summary>
        /// <param name="key">Lock key.</param>
        /// <param name="result">Denial reason.</param>
        /// <param name="innerException">Inner exception.</param>
        public LockDeniedException(string key, AcquireResult result, Exception innerException)
            : base("Lock '" + key + "' could not be acquired: " + result + ".", innerException)
        {
            Key = key;
            Result = result;
        }

        #endregion
    }
}
