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
        /// Retrieve all tags for a tenant bucket.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of bucket tags.</returns>
        List<BucketTag> GetByBucketId(string tenantId, string bucketId);

        /// <summary>
        /// Retrieve a bucket tag by Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Bucket tag Id.</param>
        /// <returns>Bucket tag.</returns>
        BucketTag GetById(string tenantId, string id);

        /// <summary>
        /// Check if a bucket tag exists.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Bucket tag Id.</param>
        /// <returns>True if the bucket tag exists.</returns>
        bool ExistsById(string tenantId, string id);

        /// <summary>
        /// Update a bucket tag.
        /// </summary>
        /// <param name="tag">Bucket tag.</param>
        void Update(BucketTag tag);

        /// <summary>
        /// Delete all tags for a bucket.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        void DeleteByBucketId(string bucketId);

        /// <summary>
        /// Delete a bucket tag by Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Bucket tag Id.</param>
        void DeleteById(string tenantId, string id);
    }
}
