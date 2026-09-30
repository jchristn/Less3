namespace Test.Shared.S3Compatibility
{
    /// <summary>
    /// A user and credential created for a test.
    /// </summary>
    public class TestPrincipal
    {
        #region Public-Members

        /// <summary>
        /// User id.
        /// </summary>
        public string UserId { get; set; } = "";

        /// <summary>
        /// Credential id.
        /// </summary>
        public string CredentialId { get; set; } = "";

        /// <summary>
        /// User email address.
        /// </summary>
        public string Email { get; set; } = "";

        /// <summary>
        /// Access key.
        /// </summary>
        public string AccessKey { get; set; } = "";

        /// <summary>
        /// Secret key.
        /// </summary>
        public string SecretKey { get; set; } = "";

        #endregion
    }
}
