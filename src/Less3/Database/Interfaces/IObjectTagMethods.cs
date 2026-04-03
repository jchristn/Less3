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
        /// Retrieve tags for an object by object GUID and bucket GUID.
        /// </summary>
        /// <param name="objectGuid">Object GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>List of object tags.</returns>
        List<ObjectTag> GetByObjectGuid(string objectGuid, string bucketGuid);

        /// <summary>
        /// Delete all tags for an object by object GUID and bucket GUID.
        /// </summary>
        /// <param name="objectGuid">Object GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        void DeleteByObjectGuid(string objectGuid, string bucketGuid);
    }
}
