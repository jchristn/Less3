namespace Less3.Classes
{
    using System;
    using Less3.Helpers;

    /// <summary>
    /// Assignment of a role to a user or credential within a tenant.
    /// </summary>
    public class RoleAssignment
    {
        #region Public-Members

        /// <summary>
        /// Assignment identifier.
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
        /// Tenant identifier.
        /// </summary>
        public string TenantId
        {
            get
            {
                return _TenantId;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(TenantId));
                _TenantId = value;
            }
        }

        /// <summary>
        /// Role identifier.
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
        /// Principal type, such as User or Credential.
        /// </summary>
        public string PrincipalType
        {
            get
            {
                return _PrincipalType;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(PrincipalType));
                _PrincipalType = value;
            }
        }

        /// <summary>
        /// User or credential identifier receiving the role.
        /// </summary>
        public string PrincipalId
        {
            get
            {
                return _PrincipalId;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(PrincipalId));
                _PrincipalId = value;
            }
        }

        /// <summary>
        /// Optional resource type for resource-scoped assignments.
        /// </summary>
        public string ResourceType { get; set; } = null;

        /// <summary>
        /// Optional resource identifier for resource-scoped assignments.
        /// </summary>
        public string ResourceId { get; set; } = null;

        /// <summary>
        /// Whether this assignment is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateAssignmentId();
        private string _TenantId = String.Empty;
        private string _RoleId = String.Empty;
        private string _PrincipalType = "User";
        private string _PrincipalId = String.Empty;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the role assignment.
        /// </summary>
        public RoleAssignment()
        {

        }

        #endregion
    }
}
