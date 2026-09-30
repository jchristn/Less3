namespace Less3.Classes
{
    /// <summary>
    /// Conditional-write headers (If-Match, If-None-Match) evaluated against the current object while the
    /// write lock is held, so the check and the write are atomic.
    /// </summary>
    public class ObjectWritePreconditions
    {
        #region Public-Members

        /// <summary>
        /// Value of the If-Match header, or null. The write proceeds only if the current object's ETag matches.
        /// </summary>
        public string IfMatch { get; set; } = null;

        /// <summary>
        /// Value of the If-None-Match header, or null. Amazon S3 only supports "*", which makes the write
        /// proceed only if the key does not currently exist.
        /// </summary>
        public string IfNoneMatch { get; set; } = null;

        /// <summary>
        /// True when at least one precondition is set.
        /// </summary>
        public bool Any
        {
            get { return !string.IsNullOrEmpty(IfMatch) || !string.IsNullOrEmpty(IfNoneMatch); }
        }

        #endregion
    }
}
