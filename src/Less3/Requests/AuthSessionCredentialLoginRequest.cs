namespace Less3.Requests
{
    /// <summary>
    /// Credential-based session login request.
    /// </summary>
    public class AuthSessionCredentialLoginRequest
    {
        /// <summary>
        /// Credential access key.
        /// </summary>
        public string AccessKey { get; set; } = null;

        /// <summary>
        /// Credential secret key.
        /// </summary>
        public string SecretKey { get; set; } = null;

        /// <summary>
        /// Session expiration in minutes.
        /// </summary>
        public int ExpirationMinutes { get; set; } = 60;
    }
}
