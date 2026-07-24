namespace Less3.Database.Interfaces
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for object tag database methods.
    /// </summary>
    public interface IObjectTagMethods
    {
        /// <summary>
        /// Insert a new object tag.
        /// </summary>
        /// <param name="tag">Object tag to insert.</param>
        void Insert(ObjectTag tag);

        /// <summary>
        /// Retrieve tags for an object by object Id and bucket Id.
        /// </summary>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of object tags.</returns>
        List<ObjectTag> GetByObjectId(string objectId, string bucketId);

        /// <summary>
        /// Retrieve tags for an object by tenant, object Id, and bucket Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of object tags.</returns>
        List<ObjectTag> GetByObjectId(string tenantId, string objectId, string bucketId);

        /// <summary>
        /// Retrieve an object tag by Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Object tag Id.</param>
        /// <returns>Object tag.</returns>
        ObjectTag GetById(string tenantId, string id);

        /// <summary>
        /// Check if an object tag exists.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Object tag Id.</param>
        /// <returns>True if the object tag exists.</returns>
        bool ExistsById(string tenantId, string id);

        /// <summary>
        /// Update an object tag.
        /// </summary>
        /// <param name="tag">Object tag.</param>
        void Update(ObjectTag tag);

        /// <summary>
        /// Delete all tags for an object by object Id and bucket Id.
        /// </summary>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        void DeleteByObjectId(string objectId, string bucketId);

        /// <summary>
        /// Delete an object tag by Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Object tag Id.</param>
        void DeleteById(string tenantId, string id);
    }
}
