namespace Less3.Classes
{
    using System;
    using Less3.Helpers;

    /// <summary>
    /// Tenant record. Tenants are the top-level ownership boundary in Less3.
    /// </summary>
    public class Tenant
    {
        #region Public-Members

        /// <summary>
        /// Tenant identifier.
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
        /// Optional parent tenant identifier for future hierarchical tenant support.
        /// </summary>
        public string ParentId { get; set; } = null;

        /// <summary>
        /// Human-readable tenant name.
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
        /// Whether this tenant can authenticate and access tenant-owned resources.
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

        private string _Id = IdGenerator.GenerateTenantId();
        private string _Name = "New tenant";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the tenant.
        /// </summary>
        public Tenant()
        {

        }

        /// <summary>
        /// Instantiate the tenant.
        /// </summary>
        /// <param name="id">Tenant identifier.</param>
        /// <param name="name">Tenant name.</param>
        public Tenant(string id, string name)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            Id = id;
            Name = name;
        }

        #endregion
    }
}
