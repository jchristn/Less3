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
        /// Check if a bucket exists by name.
        /// </summary>
        /// <param name="name">Bucket name.</param>
        /// <returns>True if the bucket exists.</returns>
        bool ExistsByName(string name);

        /// <summary>
        /// Retrieve buckets by owner GUID.
        /// </summary>
        /// <param name="ownerGuid">Owner GUID.</param>
        /// <returns>List of buckets.</returns>
        List<Bucket> GetByOwnerGuid(string ownerGuid);

        /// <summary>
        /// Retrieve a bucket by GUID.
        /// </summary>
        /// <param name="guid">Bucket GUID.</param>
        /// <returns>Bucket or null if not found.</returns>
        Bucket GetByGuid(string guid);

        /// <summary>
        /// Retrieve a bucket by name.
        /// </summary>
        /// <param name="name">Bucket name.</param>
        /// <returns>Bucket or null if not found.</returns>
        Bucket GetByName(string name);

        /// <summary>
        /// Insert a new bucket.
        /// </summary>
        /// <param name="bucket">Bucket to insert.</param>
        void Insert(Bucket bucket);

        /// <summary>
        /// Delete a bucket by GUID.
        /// </summary>
        /// <param name="guid">Bucket GUID.</param>
        void DeleteByGuid(string guid);
    }
}
