namespace Less3.Classes
{
    /// <summary>
    /// A bucket's versioning state captured once for the duration of an object operation, so every decision
    /// in that operation uses the same state even if the bucket's configuration changes concurrently.
    /// </summary>
    public class BucketVersioningState
    {
        #region Public-Members

        /// <summary>
        /// True when versioning is enabled.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// True when versioning was enabled and is now suspended.
        /// </summary>
        public bool Suspended { get; }

        /// <summary>
        /// True when versioning is enabled or suspended, i.e. the bucket reports version IDs.
        /// </summary>
        public bool Configured
        {
            get { return Enabled || Suspended; }
        }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="enabled">True when versioning is enabled.</param>
        /// <param name="suspended">True when versioning is suspended.</param>
        public BucketVersioningState(bool enabled, bool suspended)
        {
            Enabled = enabled;
            Suspended = !enabled && suspended;
        }

        #endregion
    }
}
