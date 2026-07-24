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
        /// Retrieve an upload by Id.
        /// </summary>
        /// <param name="id">Upload Id.</param>
        /// <returns>Upload or null if not found.</returns>
        Upload GetById(string id);

        /// <summary>
        /// Retrieve an upload by tenant and Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Upload Id.</param>
        /// <returns>Upload or null if not found.</returns>
        Upload GetById(string tenantId, string id);

        /// <summary>
        /// Retrieve all uploads.
        /// </summary>
        /// <returns>List of uploads.</returns>
        List<Upload> GetAll();

        /// <summary>
        /// Retrieve all uploads for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <returns>List of uploads.</returns>
        List<Upload> GetAll(string tenantId);

        /// <summary>
        /// Retrieve uploads by bucket Id.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of uploads.</returns>
        List<Upload> GetByBucketId(string bucketId);

        /// <summary>
        /// Retrieve uploads by tenant and bucket Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of uploads.</returns>
        List<Upload> GetByBucketId(string tenantId, string bucketId);

        /// <summary>
        /// Insert a new upload.
        /// </summary>
        /// <param name="upload">Upload to insert.</param>
        void Insert(Upload upload);

        /// <summary>
        /// Delete an upload by Id.
        /// </summary>
        /// <param name="id">Upload Id.</param>
        void DeleteById(string id);

        /// <summary>
        /// Delete an upload by tenant and Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Upload Id.</param>
        void DeleteById(string tenantId, string id);
    }
}
