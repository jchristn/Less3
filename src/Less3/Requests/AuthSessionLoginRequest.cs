namespace Less3.Requests
{
    /// <summary>
    /// Request to create a user auth session.
    /// </summary>
    public class AuthSessionLoginRequest
    {
        /// <summary>
        /// Tenant identifier. Defaults to default.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// User email address.
        /// </summary>
        public string Email { get; set; } = null;

        /// <summary>
        /// User password.
        /// </summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// Session lifetime in minutes.
        /// </summary>
        public int ExpirationMinutes { get; set; } = 480;
    }
}
