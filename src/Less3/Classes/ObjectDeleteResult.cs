namespace Less3.Classes
{
    /// <summary>
    /// Outcome of deleting an object or object version.
    /// </summary>
    public class ObjectDeleteResult
    {
        #region Public-Members

        /// <summary>
        /// True when a version was permanently removed or a delete marker was created. False when the
        /// key or version did not exist; Amazon S3 still reports such a delete as successful.
        /// </summary>
        public bool Found { get; set; } = false;

        /// <summary>
        /// True when the operation created a delete marker, or permanently removed a version that was a delete marker.
        /// </summary>
        public bool DeleteMarker { get; set; } = false;

        /// <summary>
        /// Version ID to report: the version that was removed, or the delete marker that was created.
        /// Null when the bucket has never had versioning enabled.
        /// </summary>
        public string VersionId { get; set; } = null;

        /// <summary>
        /// True when the operation created a new delete marker rather than removing a version.
        /// </summary>
        public bool CreatedDeleteMarker { get; set; } = false;

        #endregion
    }
}
