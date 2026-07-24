namespace Less3.Responses
{
    using Less3.Classes;

    /// <summary>
    /// Response returned after successful session creation.
    /// </summary>
    public class AuthSessionLoginResponse
    {
        /// <summary>
        /// Created session without the raw token.
        /// </summary>
        public AuthSession Session { get; set; } = null;

        /// <summary>
        /// Raw bearer token. Shown once.
        /// </summary>
        public string Token { get; set; } = null;
    }
}
