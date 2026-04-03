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
        /// <param name="objectGuid">Object GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByGroupName(string groupName, string objectGuid, string bucketGuid);

        /// <summary>
        /// Check if an object user ACL exists.
        /// </summary>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="objectGuid">Object GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByUserGuid(string userGuid, string objectGuid, string bucketGuid);

        /// <summary>
        /// Retrieve ACLs for an object by object GUID and bucket GUID.
        /// </summary>
        /// <param name="objectGuid">Object GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>List of object ACLs.</returns>
        List<ObjectAcl> GetByObjectGuid(string objectGuid, string bucketGuid);

        /// <summary>
        /// Retrieve all object ACLs for a bucket.
        /// </summary>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>List of object ACLs.</returns>
        List<ObjectAcl> GetByBucketGuid(string bucketGuid);

        /// <summary>
        /// Insert a new object ACL.
        /// </summary>
        /// <param name="acl">Object ACL to insert.</param>
        void Insert(ObjectAcl acl);

        /// <summary>
        /// Delete all ACLs for a specific object within a bucket.
        /// </summary>
        /// <param name="objectGuid">Object GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        void DeleteByObjectGuidAndBucketGuid(string objectGuid, string bucketGuid);
    }
}
