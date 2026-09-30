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
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>Object or null if not found.</returns>
        Obj GetLatestByKey(string key, string bucketId);

        /// <summary>
        /// Retrieve an object by key and version within a bucket.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="version">Object version.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>Object or null if not found.</returns>
        Obj GetByKeyAndVersion(string key, long version, string bucketId);

        /// <summary>
        /// Retrieve an object by Id within a bucket.
        /// </summary>
        /// <param name="id">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>Object or null if not found.</returns>
        Obj GetById(string id, string bucketId);

        /// <summary>
        /// Get the latest version number for a given key within a bucket.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>Latest version number, or 0 if none found.</returns>
        long GetLatestVersion(string key, string bucketId);

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
        /// <param name="bucketId">Bucket Id.</param>
        /// <param name="startIndex">Minimum ID to start from.</param>
        /// <param name="maxResults">Maximum number of results to return.</param>
        /// <param name="excludeDeleteMarkers">Whether to exclude objects with delete markers.</param>
        /// <param name="prefix">Optional key prefix filter.</param>
        /// <returns>List of matching objects.</returns>
        List<Obj> Enumerate(string bucketId, int startIndex, int maxResults, bool excludeDeleteMarkers, string prefix);

        /// <summary>
        /// Retrieve the null version of a key within a bucket, i.e. the row written while versioning
        /// was not enabled.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>Null-version row, or null if the key has no null version.</returns>
        Obj GetNullVersion(string key, string bucketId);

        /// <summary>
        /// Enumerate the latest version of each key in a bucket, in ascending binary (ordinal) key order,
        /// excluding keys whose latest version is a delete marker. Supports keyset pagination.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        /// <param name="prefix">Optional key prefix; matched exactly and case-sensitively. Null or empty matches all keys.</param>
        /// <param name="afterKey">Optional exclusive lower bound; only keys that sort after this key are returned.</param>
        /// <param name="maxResults">Maximum number of rows to return. Minimum value is 1.</param>
        /// <returns>List of latest-version rows.</returns>
        List<Obj> EnumerateLatest(string bucketId, string prefix, string afterKey, int maxResults);

        /// <summary>
        /// Enumerate every version and delete marker in a bucket, ordered by key ascending (binary) and
        /// version descending. Supports keyset pagination.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        /// <param name="prefix">Optional key prefix; matched exactly and case-sensitively. Null or empty matches all keys.</param>
        /// <param name="afterKey">Optional exclusive lower bound on key.</param>
        /// <param name="afterVersion">Optional version within afterKey; when set, versions of afterKey older than this one are also returned.</param>
        /// <param name="maxResults">Maximum number of rows to return. Minimum value is 1.</param>
        /// <returns>List of rows.</returns>
        List<Obj> EnumerateVersions(string bucketId, string prefix, string afterKey, long? afterVersion, int maxResults);

        /// <summary>
        /// Get object count and total bytes for a bucket.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>Bucket statistics.</returns>
        BucketStatistics GetStatistics(string bucketId);
    }
}
