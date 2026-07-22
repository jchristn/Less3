namespace Less3.Database.Interfaces
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for object ACL database methods.
    /// </summary>
    public interface IObjectAclMethods
    {
        /// <summary>
        /// Check if an object group ACL exists.
        /// </summary>
        /// <param name="groupName">Group name.</param>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByGroupName(string groupName, string objectId, string bucketId);

        /// <summary>
        /// Check if an object user ACL exists.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByUserId(string userId, string objectId, string bucketId);

        /// <summary>
        /// Retrieve ACLs for an object by object Id and bucket Id.
        /// </summary>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of object ACLs.</returns>
        List<ObjectAcl> GetByObjectId(string objectId, string bucketId);

        /// <summary>
        /// Retrieve all object ACLs for a bucket.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of object ACLs.</returns>
        List<ObjectAcl> GetByBucketId(string bucketId);

        /// <summary>
        /// Insert a new object ACL.
        /// </summary>
        /// <param name="acl">Object ACL to insert.</param>
        void Insert(ObjectAcl acl);

        /// <summary>
        /// Delete all ACLs for a specific object within a bucket.
        /// </summary>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        void DeleteByObjectIdAndBucketId(string objectId, string bucketId);
    }
}
