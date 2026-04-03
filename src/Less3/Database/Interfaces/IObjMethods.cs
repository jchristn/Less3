namespace Less3.Database.Interfaces
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for object database methods.
    /// </summary>
    public interface IObjMethods
    {
        /// <summary>
        /// Insert a new object.
        /// </summary>
        /// <param name="obj">Object to insert.</param>
        void Insert(Obj obj);

        /// <summary>
        /// Retrieve the latest version of an object by key within a bucket.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>Object or null if not found.</returns>
        Obj GetLatestByKey(string key, string bucketGuid);

        /// <summary>
        /// Retrieve an object by key and version within a bucket.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="version">Object version.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>Object or null if not found.</returns>
        Obj GetByKeyAndVersion(string key, long version, string bucketGuid);

        /// <summary>
        /// Retrieve an object by GUID within a bucket.
        /// </summary>
        /// <param name="guid">Object GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>Object or null if not found.</returns>
        Obj GetByGuid(string guid, string bucketGuid);

        /// <summary>
        /// Get the latest version number for a given key within a bucket.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>Latest version number, or 0 if none found.</returns>
        long GetLatestVersion(string key, string bucketGuid);

        /// <summary>
        /// Update an existing object record.
        /// </summary>
        /// <param name="obj">Object to update.</param>
        void Update(Obj obj);

        /// <summary>
        /// Delete an object record.
        /// </summary>
        /// <param name="obj">Object to delete.</param>
        void Delete(Obj obj);

        /// <summary>
        /// Enumerate objects in a bucket with pagination, optional prefix filter, and optional delete marker exclusion.
        /// Results are ordered by ID ascending.
        /// </summary>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <param name="startIndex">Minimum ID to start from.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <param name="excludeDeleteMarkers">Whether to exclude objects with delete markers.</param>
        /// <param name="prefix">Optional key prefix filter.</param>
        /// <returns>List of matching objects.</returns>
        List<Obj> Enumerate(string bucketGuid, int startIndex, int maxResults, bool excludeDeleteMarkers, string prefix);

        /// <summary>
        /// Get object count and total bytes for a bucket.
        /// </summary>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>Bucket statistics.</returns>
        BucketStatistics GetStatistics(string bucketGuid);
    }
}
