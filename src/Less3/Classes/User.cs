namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// User object.
    /// </summary>
    public class User
    {
        #region Public-Members

        /// <summary>
        /// Id.
        /// </summary>
        public string Id { get; set; } = Less3.Helpers.IdGenerator.GenerateUserId();

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// Name of the user.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Email address of the user.
        /// </summary>
        public string Email { get; set; } = null;

        /// <summary>
        /// Password hash for REST/dashboard authentication.
        /// </summary>
        public string PasswordHash { get; set; } = null;

        /// <summary>
        /// Whether the user is a global administrator.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Whether the user is an administrator within the user's tenant.
        /// </summary>
        public bool IsTenantAdmin { get; set; } = false;

        /// <summary>
        /// Whether the user is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Timestamp from record creation, in UTC time.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.Now.ToUniversalTime();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public User()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="email">Email.</param>
        public User(string name, string email)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            Id = Less3.Helpers.IdGenerator.GenerateUserId();
            Name = name;
            Email = email;
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="id">Id.</param>
        /// <param name="name">Name.</param>
        /// <param name="email">Email.</param>
        public User(string id, string name, string email)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            Id = id;
            Name = name;
            Email = email;
        }
         
        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
