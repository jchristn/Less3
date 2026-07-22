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
        /// Check if a tenant bucket group ACL exists.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="groupName">Group name.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByGroupName(string tenantId, string groupName, string bucketId);

        /// <summary>
        /// Check if a bucket user ACL exists.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByUserId(string userId, string bucketId);

        /// <summary>
        /// Check if a tenant bucket user ACL exists.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByUserId(string tenantId, string userId, string bucketId);

        /// <summary>
        /// Retrieve all ACLs for a bucket.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of bucket ACLs.</returns>
        List<BucketAcl> GetByBucketId(string bucketId);

        /// <summary>
        /// Retrieve all ACLs for a tenant bucket.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of bucket ACLs.</returns>
        List<BucketAcl> GetByBucketId(string tenantId, string bucketId);

        /// <summary>
        /// Retrieve a bucket ACL by Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Bucket ACL Id.</param>
        /// <returns>Bucket ACL.</returns>
        BucketAcl GetById(string tenantId, string id);

        /// <summary>
        /// Check if a bucket ACL exists.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Bucket ACL Id.</param>
        /// <returns>True if the bucket ACL exists.</returns>
        bool ExistsById(string tenantId, string id);

        /// <summary>
        /// Insert a new bucket ACL.
        /// </summary>
        /// <param name="acl">Bucket ACL to insert.</param>
        void Insert(BucketAcl acl);

        /// <summary>
        /// Update a bucket ACL.
        /// </summary>
        /// <param name="acl">Bucket ACL.</param>
        void Update(BucketAcl acl);

        /// <summary>
        /// Delete all ACLs for a bucket.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        void DeleteByBucketId(string bucketId);

        /// <summary>
        /// Delete a bucket ACL by Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Bucket ACL Id.</param>
        void DeleteById(string tenantId, string id);
    }
}
