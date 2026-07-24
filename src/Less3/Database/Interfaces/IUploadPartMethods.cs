namespace Less3.Database.Interfaces
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for upload part database methods.
    /// </summary>
    public interface IUploadPartMethods
    {
        /// <summary>
        /// Insert a new upload part.
        /// </summary>
        /// <param name="part">Upload part to insert.</param>
        void Insert(UploadPart part);

        /// <summary>
        /// Retrieve all parts for an upload by upload Id.
        /// </summary>
        /// <param name="uploadId">Upload Id.</param>
        /// <returns>List of upload parts.</returns>
        List<UploadPart> GetByUploadId(string uploadId);

        /// <summary>
        /// Retrieve all parts for an upload by tenant and upload Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="uploadId">Upload Id.</param>
        /// <returns>List of upload parts.</returns>
        List<UploadPart> GetByUploadId(string tenantId, string uploadId);

        /// <summary>
        /// Delete all parts for an upload by upload Id.
        /// </summary>
        /// <param name="uploadId">Upload Id.</param>
        void DeleteByUploadId(string uploadId);

        /// <summary>
        /// Delete all parts for an upload by tenant and upload Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="uploadId">Upload Id.</param>
        void DeleteByUploadId(string tenantId, string uploadId);

        /// <summary>
        /// Delete all stored rows for a specific part number within a multipart upload.
        /// </summary>
        /// <param name="uploadId">Upload Id.</param>
        /// <param name="partNumber">Part number.</param>
        void DeleteByUploadIdAndPartNumber(string uploadId, int partNumber);

        /// <summary>
        /// Delete all stored rows for a specific part number within a tenant multipart upload.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="uploadId">Upload Id.</param>
        /// <param name="partNumber">Part number.</param>
        void DeleteByUploadIdAndPartNumber(string tenantId, string uploadId, int partNumber);
    }
}
