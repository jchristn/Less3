namespace Less3.Database.Interfaces
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for user database methods.
    /// </summary>
    public interface IUserMethods
    {
        /// <summary>
        /// Retrieve all users.
        /// </summary>
        /// <returns>List of users.</returns>
        List<User> GetAll();

        /// <summary>
        /// Retrieve all users for a tenant.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <returns>List of users.</returns>
        List<User> GetAll(string tenantId);

        /// <summary>
        /// Check if a user exists by Id.
        /// </summary>
        /// <param name="id">User Id.</param>
        /// <returns>True if the user exists.</returns>
        bool ExistsById(string id);

        /// <summary>
        /// Check if a user exists by tenant and Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">User Id.</param>
        /// <returns>True if the user exists.</returns>
        bool ExistsById(string tenantId, string id);

        /// <summary>
        /// Check if a user exists by email.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <returns>True if the user exists.</returns>
        bool ExistsByEmail(string email);

        /// <summary>
        /// Check if a user exists by tenant and email.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="email">Email address.</param>
        /// <returns>True if the user exists.</returns>
        bool ExistsByEmail(string tenantId, string email);

        /// <summary>
        /// Retrieve a user by Id.
        /// </summary>
        /// <param name="id">User Id.</param>
        /// <returns>User or null if not found.</returns>
        User GetById(string id);

        /// <summary>
        /// Retrieve a user by tenant and Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">User Id.</param>
        /// <returns>User or null if not found.</returns>
        User GetById(string tenantId, string id);

        /// <summary>
        /// Retrieve a user by name.
        /// </summary>
        /// <param name="name">User name.</param>
        /// <returns>User or null if not found.</returns>
        User GetByName(string name);

        /// <summary>
        /// Retrieve a user by tenant and name.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="name">User name.</param>
        /// <returns>User or null if not found.</returns>
        User GetByName(string tenantId, string name);

        /// <summary>
        /// Retrieve a user by email.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <returns>User or null if not found.</returns>
        User GetByEmail(string email);

        /// <summary>
        /// Retrieve a user by tenant and email.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="email">Email address.</param>
        /// <returns>User or null if not found.</returns>
        User GetByEmail(string tenantId, string email);

        /// <summary>
        /// Insert a new user.
        /// </summary>
        /// <param name="user">User to insert.</param>
        void Insert(User user);

        /// <summary>
        /// Update an existing user.
        /// </summary>
        /// <param name="user">User to update.</param>
        void Update(User user);

        /// <summary>
        /// Delete a user by Id.
        /// </summary>
        /// <param name="id">User Id.</param>
        void DeleteById(string id);

        /// <summary>
        /// Delete a user by tenant and Id.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="id">User Id.</param>
        void DeleteById(string tenantId, string id);
    }
}
