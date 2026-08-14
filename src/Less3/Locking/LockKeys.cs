namespace Less3.Locking
{
    using System;

    /// <summary>
    /// Builds the canonical string keys used for distributed locks so every code path locks the
    /// same key for the same resource. Keys are namespaced by resource type.
    /// </summary>
    public static class LockKeys
    {
        /// <summary>
        /// Lock key for a specific object within a bucket. Serializes the read-modify-write on an
        /// object's version chain (create, overwrite, delete).
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="bucketName">Bucket name.</param>
        /// <param name="objectKey">Object key.</param>
        /// <returns>Lock key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null or empty.</exception>
        public static string Object(string tenantId, string bucketName, string objectKey)
        {
            if (String.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrEmpty(objectKey)) throw new ArgumentNullException(nameof(objectKey));
            return "obj:" + (tenantId ?? "-") + ":" + bucketName + ":" + objectKey;
        }

        /// <summary>
        /// Lock key for a multipart upload's lifecycle (complete/abort). Guards assembly of the
        /// final object from its parts.
        /// </summary>
        /// <param name="uploadId">Upload identifier.</param>
        /// <returns>Lock key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when uploadId is null or empty.</exception>
        public static string Upload(string uploadId)
        {
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            return "mpu:" + uploadId;
        }

        /// <summary>
        /// Lock key for a bucket-level mutation (create/delete/config change).
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="bucketName">Bucket name.</param>
        /// <returns>Lock key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when bucketName is null or empty.</exception>
        public static string Bucket(string tenantId, string bucketName)
        {
            if (String.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            return "bucket:" + (tenantId ?? "-") + ":" + bucketName;
        }

        /// <summary>
        /// Well-known lease key elected among nodes to run temp/part cleanup exactly once per
        /// cluster.
        /// </summary>
        public const string ClusterCleanup = "cluster:cleanup";

        /// <summary>
        /// Well-known lease key elected among nodes to run leader-only maintenance (for example
        /// request-history purge).
        /// </summary>
        public const string ClusterMaintenance = "cluster:maintenance";
    }
}
