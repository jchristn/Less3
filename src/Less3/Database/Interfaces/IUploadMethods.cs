namespace Less3.Database.Interfaces
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for upload database methods.
    /// </summary>
    public interface IUploadMethods
    {
        /// <summary>
        /// Retrieve an upload by GUID.
        /// </summary>
        /// <param name="guid">Upload GUID.</param>
        /// <returns>Upload or null if not found.</returns>
        Upload GetByGuid(string guid);

        /// <summary>
        /// Retrieve all uploads.
        /// </summary>
        /// <returns>List of uploads.</returns>
        List<Upload> GetAll();

        /// <summary>
        /// Retrieve uploads by bucket GUID.
        /// </summary>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>List of uploads.</returns>
        List<Upload> GetByBucketGuid(string bucketGuid);

        /// <summary>
        /// Insert a new upload.
        /// </summary>
        /// <param name="upload">Upload to insert.</param>
        void Insert(Upload upload);

        /// <summary>
        /// Delete an upload by GUID.
        /// </summary>
        /// <param name="guid">Upload GUID.</param>
        void DeleteByGuid(string guid);
    }
}
