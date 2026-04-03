namespace Less3.Database.Interfaces
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for credential database methods.
    /// </summary>
    public interface ICredentialMethods
    {
        /// <summary>
        /// Retrieve all credentials.
        /// </summary>
        /// <returns>List of credentials.</returns>
        List<Credential> GetAll();

        /// <summary>
        /// Check if a credential exists by GUID.
        /// </summary>
        /// <param name="guid">Credential GUID.</param>
        /// <returns>True if the credential exists.</returns>
        bool ExistsByGuid(string guid);

        /// <summary>
        /// Retrieve a credential by GUID.
        /// </summary>
        /// <param name="guid">Credential GUID.</param>
        /// <returns>Credential or null if not found.</returns>
        Credential GetByGuid(string guid);

        /// <summary>
        /// Retrieve credentials by user GUID.
        /// </summary>
        /// <param name="userGuid">User GUID.</param>
        /// <returns>List of credentials.</returns>
        List<Credential> GetByUserGuid(string userGuid);

        /// <summary>
        /// Retrieve a credential by access key.
        /// </summary>
        /// <param name="accessKey">Access key.</param>
        /// <returns>Credential or null if not found.</returns>
        Credential GetByAccessKey(string accessKey);

        /// <summary>
        /// Insert a new credential.
        /// </summary>
        /// <param name="credential">Credential to insert.</param>
        void Insert(Credential credential);

        /// <summary>
        /// Delete a credential by GUID.
        /// </summary>
        /// <param name="guid">Credential GUID.</param>
        void DeleteByGuid(string guid);
    }
}
