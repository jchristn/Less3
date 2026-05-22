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
        /// Retrieve all parts for an upload by upload GUID.
        /// </summary>
        /// <param name="uploadGuid">Upload GUID.</param>
        /// <returns>List of upload parts.</returns>
        List<UploadPart> GetByUploadGuid(string uploadGuid);

        /// <summary>
        /// Delete all parts for an upload by upload GUID.
        /// </summary>
        /// <param name="uploadGuid">Upload GUID.</param>
        void DeleteByUploadGuid(string uploadGuid);

        /// <summary>
        /// Delete all stored rows for a specific part number within a multipart upload.
        /// </summary>
        /// <param name="uploadGuid">Upload GUID.</param>
        /// <param name="partNumber">Part number.</param>
        void DeleteByUploadGuidAndPartNumber(string uploadGuid, int partNumber);
    }
}
