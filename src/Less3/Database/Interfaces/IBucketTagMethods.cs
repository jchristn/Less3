namespace Less3.Database.Interfaces
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for bucket tag database methods.
    /// </summary>
    public interface IBucketTagMethods
    {
        /// <summary>
        /// Insert a new bucket tag.
        /// </summary>
        /// <param name="tag">Bucket tag to insert.</param>
        void Insert(BucketTag tag);

        /// <summary>
        /// Retrieve all tags for a bucket.
        /// </summary>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>List of bucket tags.</returns>
        List<BucketTag> GetByBucketGuid(string bucketGuid);

        /// <summary>
        /// Delete all tags for a bucket.
        /// </summary>
        /// <param name="bucketGuid">Bucket GUID.</param>
        void DeleteByBucketGuid(string bucketGuid);
    }
}
