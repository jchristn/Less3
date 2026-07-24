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
        /// Check if a tenant object group ACL exists.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="groupName">Group name.</param>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByGroupName(string tenantId, string groupName, string objectId, string bucketId);

        /// <summary>
        /// Check if an object user ACL exists.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByUserId(string userId, string objectId, string bucketId);

        /// <summary>
        /// Check if a tenant object user ACL exists.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>True if the ACL exists.</returns>
        bool ExistsByUserId(string tenantId, string userId, string objectId, string bucketId);

        /// <summary>
        /// Retrieve ACLs for an object by object Id and bucket Id.
        /// </summary>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of object ACLs.</returns>
        List<ObjectAcl> GetByObjectId(string objectId, string bucketId);

        /// <summary>
        /// Retrieve ACLs for an object by tenant, object Id, and bucket Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of object ACLs.</returns>
        List<ObjectAcl> GetByObjectId(string tenantId, string objectId, string bucketId);

        /// <summary>
        /// Retrieve all object ACLs for a bucket.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of object ACLs.</returns>
        List<ObjectAcl> GetByBucketId(string bucketId);

        /// <summary>
        /// Retrieve all object ACLs for a tenant bucket.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <returns>List of object ACLs.</returns>
        List<ObjectAcl> GetByBucketId(string tenantId, string bucketId);

        /// <summary>
        /// Retrieve an object ACL by Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Object ACL Id.</param>
        /// <returns>Object ACL.</returns>
        ObjectAcl GetById(string tenantId, string id);

        /// <summary>
        /// Check if an object ACL exists.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Object ACL Id.</param>
        /// <returns>True if the object ACL exists.</returns>
        bool ExistsById(string tenantId, string id);

        /// <summary>
        /// Insert a new object ACL.
        /// </summary>
        /// <param name="acl">Object ACL to insert.</param>
        void Insert(ObjectAcl acl);

        /// <summary>
        /// Update an object ACL.
        /// </summary>
        /// <param name="acl">Object ACL.</param>
        void Update(ObjectAcl acl);

        /// <summary>
        /// Delete all ACLs for a specific object within a bucket.
        /// </summary>
        /// <param name="objectId">Object Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        void DeleteByObjectIdAndBucketId(string objectId, string bucketId);

        /// <summary>
        /// Delete an object ACL by Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">Object ACL Id.</param>
        void DeleteById(string tenantId, string id);
    }
}
