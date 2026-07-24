namespace Less3.Database.Interfaces
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for bucket database methods.
    /// </summary>
    public interface IBucketMethods
    {
        /// <summary>
        /// Retrieve all buckets.
        /// </summary>
        /// <returns>List of buckets.</returns>
        List<Bucket> GetAll();

        /// <summary>
        /// Retrieve all buckets for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <returns>List of buckets.</returns>
        List<Bucket> GetAll(string tenantId);

        /// <summary>
        /// Check if a bucket exists by name.
        /// </summary>
        /// <param name="name">Bucket name.</param>
        /// <returns>True if the bucket exists.</returns>
        bool ExistsByName(string name);

        /// <summary>
        /// Check if a bucket exists by tenant and name.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="name">Bucket name.</param>
        /// <returns>True if the bucket exists.</returns>
        bool ExistsByName(string tenantId, string name);

        /// <summary>
        /// Retrieve buckets by owner Id.
        /// </summary>
        /// <param name="ownerId">Owner Id.</param>
        /// <returns>List of buckets.</returns>
        List<Bucket> GetByOwnerId(string ownerId);

        /// <summary>
        /// Retrieve buckets by tenant and owner Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="ownerId">Owner Id.</param>
        /// <returns>List of buckets.</returns>
        List<Bucket> GetByOwnerId(string tenantId, string ownerId);

        /// <summary>
        /// Retrieve a bucket by Id.
        /// </summary>
        /// <param name="id">Bucket Id.</param>
        /// <returns>Bucket or null if not found.</returns>
        Bucket GetById(string id);

        /// <summary>
        /// Retrieve a bucket by tenant and Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Bucket Id.</param>
        /// <returns>Bucket or null if not found.</returns>
        Bucket GetById(string tenantId, string id);

        /// <summary>
        /// Retrieve a bucket by name.
        /// </summary>
        /// <param name="name">Bucket name.</param>
        /// <returns>Bucket or null if not found.</returns>
        Bucket GetByName(string name);

        /// <summary>
        /// Retrieve a bucket by tenant and name.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="name">Bucket name.</param>
        /// <returns>Bucket or null if not found.</returns>
        Bucket GetByName(string tenantId, string name);

        /// <summary>
        /// Insert a new bucket.
        /// </summary>
        /// <param name="bucket">Bucket to insert.</param>
        void Insert(Bucket bucket);

        /// <summary>
        /// Update a bucket.
        /// </summary>
        /// <param name="bucket">Bucket to update.</param>
        void Update(Bucket bucket);

        /// <summary>
        /// Delete a bucket by Id.
        /// </summary>
        /// <param name="id">Bucket Id.</param>
        void DeleteById(string id);

        /// <summary>
        /// Delete a bucket by tenant and Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Bucket Id.</param>
        void DeleteById(string tenantId, string id);
    }
}
