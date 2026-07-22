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
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByGroupName(string groupName, string bucketId);

        /// <summary>
        /// Check if a bucket user ACL exists.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByUserId(string userId, string bucketId);

        /// <summary>
        /// Retrieve all ACLs for a bucket.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of bucket ACLs.</returns>
        List<BucketAcl> GetByBucketId(string bucketId);

        /// <summary>
        /// Insert a new bucket ACL.
        /// </summary>
        /// <param name="acl">Bucket ACL to insert.</param>
        void Insert(BucketAcl acl);

        /// <summary>
        /// Delete all ACLs for a bucket.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        void DeleteByBucketId(string bucketId);
    }
}
