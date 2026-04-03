namespace Less3.Database.Interfaces
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for bucket ACL database methods.
    /// </summary>
    public interface IBucketAclMethods
    {
        /// <summary>
        /// Check if a bucket group ACL exists.
        /// </summary>
        /// <param name="groupName">Group name.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByGroupName(string groupName, string bucketGuid);

        /// <summary>
        /// Check if a bucket user ACL exists.
        /// </summary>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByUserGuid(string userGuid, string bucketGuid);

        /// <summary>
        /// Retrieve all ACLs for a bucket.
        /// </summary>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <returns>List of bucket ACLs.</returns>
        List<BucketAcl> GetByBucketGuid(string bucketGuid);

        /// <summary>
        /// Insert a new bucket ACL.
        /// </summary>
        /// <param name="acl">Bucket ACL to insert.</param>
        void Insert(BucketAcl acl);

        /// <summary>
        /// Delete all ACLs for a bucket.
        /// </summary>
        /// <param name="bucketGuid">Bucket GUID.</param>
        void DeleteByBucketGuid(string bucketGuid);
    }
}
