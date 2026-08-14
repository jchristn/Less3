namespace Less3.Storage
{
    using System;
    using Less3.Settings;

    /// <summary>
    /// Builds the on-disk paths used to stage multipart upload parts and single-object write
    /// temporaries. Both the object handler and the cleanup manager use these helpers so the two
    /// can never disagree about where a part lives — in a cluster the parts directory is shared
    /// storage and any node must resolve the identical path.
    /// </summary>
    public static class MultipartPaths
    {
        /// <summary>
        /// Directory in which multipart parts are staged (shared storage in cluster mode). Always
        /// ends with a trailing slash and is created if it does not exist.
        /// </summary>
        /// <param name="storage">Storage settings.</param>
        /// <returns>Parts directory path.</returns>
        /// <exception cref="ArgumentNullException">Thrown when storage is null.</exception>
        public static string PartsDirectory(StorageSettings storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            string dir = storage.GetEffectivePartsDirectory();
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Full path of the file staging a specific multipart part.
        /// </summary>
        /// <param name="storage">Storage settings.</param>
        /// <param name="bucketId">Bucket identifier.</param>
        /// <param name="uploadId">Upload identifier.</param>
        /// <param name="partNumber">Part number (1 or greater).</param>
        /// <returns>Full part file path.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when partNumber is less than 1.</exception>
        public static string PartFilePath(StorageSettings storage, string bucketId, string uploadId, int partNumber)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            if (String.IsNullOrEmpty(uploadId)) throw new ArgumentNullException(nameof(uploadId));
            if (partNumber < 1) throw new ArgumentOutOfRangeException(nameof(partNumber));

            return PartsDirectory(storage) + bucketId + "-upload-" + uploadId + "-part-" + partNumber;
        }
    }
}
