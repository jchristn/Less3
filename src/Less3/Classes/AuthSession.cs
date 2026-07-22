namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;
    using Less3.Helpers;

    /// <summary>
    /// Revocable authenticated session bound to a tenant and principal.
    /// </summary>
    public class AuthSession
    {
        #region Public-Members

        /// <summary>
        /// Session identifier.
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
        /// Principal identifier.
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
        /// Session token hash.
        /// </summary>
        [JsonIgnore]
        public string TokenHash
        {
            get
            {
                return _TokenHash;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(TokenHash));
                _TokenHash = value;
            }
        }

        /// <summary>
        /// Whether this session is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// UTC time the session was created.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC time the session expires.
        /// </summary>
        public DateTime ExpirationUtc { get; set; } = DateTime.UtcNow.AddHours(8);

        /// <summary>
        /// UTC time the session was revoked.
        /// </summary>
        public DateTime? RevokedUtc { get; set; } = null;

        /// <summary>
        /// Last observed source IP address.
        /// </summary>
        public string SourceIp { get; set; } = null;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateSessionId();
        private string _TenantId = String.Empty;
        private string _PrincipalType = "User";
        private string _PrincipalId = String.Empty;
        private string _TokenHash = String.Empty;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the auth session.
        /// </summary>
        public AuthSession()
        {

        }

        #endregion
    }
}
