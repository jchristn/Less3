namespace Less3.Database.Interfaces
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for credentials database methods.
    /// </summary>
    public interface ICredentialMethods
    {
        /// <summary>
        /// Retrieve all credentials.
        /// </summary>
        /// <returns>List of credentials.</returns>
        List<Credential> GetAll();

        /// <summary>
        /// Retrieve all credentials for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <returns>List of credentials.</returns>
        List<Credential> GetAll(string tenantId);

        /// <summary>
        /// Check if a credentials exists by Id.
        /// </summary>
        /// <param name="id">credentials Id.</param>
        /// <returns>True if the credentials exists.</returns>
        bool ExistsById(string id);

        /// <summary>
        /// Check if credentials exists by tenant and Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">credentials Id.</param>
        /// <returns>True if the credentials exists.</returns>
        bool ExistsById(string tenantId, string id);

        /// <summary>
        /// Retrieve a credentials by Id.
        /// </summary>
        /// <param name="id">credentials Id.</param>
        /// <returns>credentials or null if not found.</returns>
        Credential GetById(string id);

        /// <summary>
        /// Retrieve credentials by tenant and Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">credentials Id.</param>
        /// <returns>credentials or null if not found.</returns>
        Credential GetById(string tenantId, string id);

        /// <summary>
        /// Retrieve credentials by user Id.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <returns>List of credentials.</returns>
        List<Credential> GetByUserId(string userId);

        /// <summary>
        /// Retrieve credentials by tenant and user Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="userId">User Id.</param>
        /// <returns>List of credentials.</returns>
        List<Credential> GetByUserId(string tenantId, string userId);

        /// <summary>
        /// Retrieve a credentials by access key.
        /// </summary>
        /// <param name="accessKey">Access key.</param>
        /// <returns>credentials or null if not found.</returns>
        Credential GetByAccessKey(string accessKey);

        /// <summary>
        /// Insert a new credentials.
        /// </summary>
        /// <param name="credentials">credentials to insert.</param>
        void Insert(Credential credentials);

        /// <summary>
        /// Update an existing credentials.
        /// </summary>
        /// <param name="credentials">credentials to update.</param>
        void Update(Credential credentials);

        /// <summary>
        /// Delete a credentials by Id.
        /// </summary>
        /// <param name="id">credentials Id.</param>
        void DeleteById(string id);

        /// <summary>
        /// Delete credentials by tenant and Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">credentials Id.</param>
        void DeleteById(string tenantId, string id);
    }
}
