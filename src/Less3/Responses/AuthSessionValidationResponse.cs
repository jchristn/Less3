namespace Less3.Responses
{
    using Less3.Classes;

    /// <summary>
    /// Response returned when validating a session token.
    /// </summary>
    public class AuthSessionValidationResponse
    {
        /// <summary>
        /// Whether the token resolves to an active, unexpired session.
        /// </summary>
        public bool Valid { get; set; } = false;

        /// <summary>
        /// Resolved session when valid.
        /// </summary>
        public AuthSession Session { get; set; } = null;

        /// <summary>
        /// Failure reason when invalid.
        /// </summary>
        public string Reason { get; set; } = null;
    }
}
