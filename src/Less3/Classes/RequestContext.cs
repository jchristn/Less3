namespace Less3.Classes
{
    using System.Collections.Generic;

    /// <summary>
    /// Typed request context produced by authentication and consumed by authorization.
    /// </summary>
    public class RequestContext
    {
        #region Public-Members

        /// <summary>
        /// Whether the request authenticated successfully.
        /// </summary>
        public bool IsAuthenticated { get; set; } = false;

        /// <summary>
        /// Tenant identifier resolved for the request.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// User identifier resolved for the request.
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// Credential identifier resolved for the request.
        /// </summary>
        public string CredentialId { get; set; } = null;

        /// <summary>
        /// Auth session identifier resolved for the request.
        /// </summary>
        public string SessionId { get; set; } = null;

        /// <summary>
        /// Principal display name or email.
        /// </summary>
        public string PrincipalName { get; set; } = null;

        /// <summary>
        /// Whether the principal is a global administrator.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Whether the principal is a tenant administrator.
        /// </summary>
        public bool IsTenantAdmin { get; set; } = false;

        /// <summary>
        /// Scope strings attached by authentication or authorization.
        /// </summary>
        public List<string> Scopes { get; set; } = new List<string>();

        #endregion
    }
}
