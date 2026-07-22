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
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of bucket tags.</returns>
        List<BucketTag> GetByBucketId(string bucketId);

        /// <summary>
        /// Delete all tags for a bucket.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        void DeleteByBucketId(string bucketId);
    }
}
