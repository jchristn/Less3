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
        /// Check if a user exists by GUID.
        /// </summary>
        /// <param name="guid">User GUID.</param>
        /// <returns>True if the user exists.</returns>
        bool ExistsByGuid(string guid);

        /// <summary>
        /// Check if a user exists by email.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <returns>True if the user exists.</returns>
        bool ExistsByEmail(string email);

        /// <summary>
        /// Retrieve a user by GUID.
        /// </summary>
        /// <param name="guid">User GUID.</param>
        /// <returns>User or null if not found.</returns>
        User GetByGuid(string guid);

        /// <summary>
        /// Retrieve a user by name.
        /// </summary>
        /// <param name="name">User name.</param>
        /// <returns>User or null if not found.</returns>
        User GetByName(string name);

        /// <summary>
        /// Retrieve a user by email.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <returns>User or null if not found.</returns>
        User GetByEmail(string email);

        /// <summary>
        /// Insert a new user.
        /// </summary>
        /// <param name="user">User to insert.</param>
        void Insert(User user);

        /// <summary>
        /// Delete a user by GUID.
        /// </summary>
        /// <param name="guid">User GUID.</param>
        void DeleteByGuid(string guid);
    }
}
