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
        /// Delete all tags for an object by object Id and bucket Id.
        /// </summary>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        void DeleteByObjectId(string objectId, string bucketId);
    }
}
