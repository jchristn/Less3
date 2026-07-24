namespace Less3.Classes
{
    using System;
    using Less3.Helpers;

    /// <summary>
    /// Permission rule used by tenant-scoped RBAC.
    /// </summary>
    public class Permission
    {
        #region Public-Members

        /// <summary>
        /// Permission identifier.
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
        /// Tenant identifier. Built-in permissions may use null to indicate global visibility.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Role identifier this permission belongs to.
        /// </summary>
        public string RoleId
        {
            get
            {
                return _RoleId;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(RoleId));
                _RoleId = value;
            }
        }

        /// <summary>
        /// Resource type, such as Tenant, Bucket, Object, User, Credential, Role, Permission, or All.
        /// </summary>
        public string ResourceType
        {
            get
            {
                return _ResourceType;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(ResourceType));
                _ResourceType = value;
            }
        }

        /// <summary>
        /// Operation, such as Create, Read, Update, Delete, Admin, or All.
        /// </summary>
        public string Operation
        {
            get
            {
                return _Operation;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(Operation));
                _Operation = value;
            }
        }

        /// <summary>
        /// Whether this permission permits access. False is an explicit deny.
        /// </summary>
        public bool Permit { get; set; } = true;

        /// <summary>
        /// Whether this permission is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GeneratePermissionId();
        private string _RoleId = String.Empty;
        private string _ResourceType = "All";
        private string _Operation = "All";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the permission.
        /// </summary>
        public Permission()
        {

        }

        #endregion
    }
}
