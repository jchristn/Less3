namespace Less3.Classes
{
    using System;
    using Less3.Helpers;

    /// <summary>
    /// Role used by tenant-scoped RBAC.
    /// </summary>
    public class Role
    {
        #region Public-Members

        /// <summary>
        /// Role identifier.
        /// </summary>
        public string Id
        {
            get
            {
                return _Id;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(Id));
                _Id = value;
            }
        }

        /// <summary>
        /// Tenant identifier. Built-in roles may use null to indicate global visibility.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Role name.
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(Name));
                _Name = value;
            }
        }

        /// <summary>
        /// Role description.
        /// </summary>
        public string Description { get; set; } = null;

        /// <summary>
        /// Whether the role is built in and protected from tenant-side mutation.
        /// </summary>
        public bool IsBuiltIn { get; set; } = false;

        /// <summary>
        /// Whether assignments to this role inherit to child resources.
        /// </summary>
        public bool InheritsToChildren { get; set; } = true;

        /// <summary>
        /// Whether this role is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC last update timestamp.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateRoleId();
        private string _Name = "New role";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the role.
        /// </summary>
        public Role()
        {

        }

        #endregion
    }
}
