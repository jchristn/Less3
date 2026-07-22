namespace Less3.Classes
{
    using System;
    using Less3.Helpers;

    /// <summary>
    /// Audit record for authorization decisions and sensitive administrative operations.
    /// </summary>
    public class AuthorizationAudit
    {
        #region Public-Members

        /// <summary>
        /// Audit record identifier.
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
        /// Tenant identifier when the request resolved to a tenant.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// User identifier when the principal is a user.
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// Credential identifier when the principal is a credential.
        /// </summary>
        public string CredentialId { get; set; } = null;

        /// <summary>
        /// Resource type being evaluated.
        /// </summary>
        public string ResourceType { get; set; } = null;

        /// <summary>
        /// Resource identifier when known.
        /// </summary>
        public string ResourceId { get; set; } = null;

        /// <summary>
        /// Operation being evaluated.
        /// </summary>
        public string Operation { get; set; } = null;

        /// <summary>
        /// Whether the operation was permitted.
        /// </summary>
        public bool Permitted { get; set; } = false;

        /// <summary>
        /// Reason recorded by the authorization layer.
        /// </summary>
        public string Reason { get; set; } = null;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateAuthorizationAuditId();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the authorization audit record.
        /// </summary>
        public AuthorizationAudit()
        {

        }

        #endregion
    }
}
