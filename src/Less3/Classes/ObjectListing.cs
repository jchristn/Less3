namespace Less3.Classes
{
    using System.Collections.Generic;

    /// <summary>
    /// One page of a ListObjects or ListObjectVersions result, in the order Amazon S3 returns it.
    /// </summary>
    public class ObjectListing
    {
        #region Public-Members

        /// <summary>
        /// Object rows (latest versions for ListObjects; every version and delete marker for ListObjectVersions).
        /// </summary>
        public List<Obj> Objects { get; set; } = new List<Obj>();

        /// <summary>
        /// Common prefixes rolled up by the delimiter, in ascending order.
        /// </summary>
        public List<string> CommonPrefixes { get; set; } = new List<string>();

        /// <summary>
        /// Object IDs, from Objects, that are the latest version of their key. Only populated for version listings.
        /// </summary>
        public HashSet<string> LatestObjectIds { get; set; } = new HashSet<string>();

        /// <summary>
        /// True when more results are available.
        /// </summary>
        public bool IsTruncated { get; set; } = false;

        /// <summary>
        /// When truncated, the last key or common prefix returned; the next page starts after it.
        /// </summary>
        public string NextMarker { get; set; } = null;

        /// <summary>
        /// When truncated and the last item returned is a version, that version's ID. Only used for version listings.
        /// </summary>
        public string NextVersionIdMarker { get; set; } = null;

        #endregion
    }
}
