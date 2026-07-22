namespace Less3.Requests
{
    /// <summary>
    /// Request containing a bearer session token.
    /// </summary>
    public class AuthSessionTokenRequest
    {
        /// <summary>
        /// Raw bearer token.
        /// </summary>
        public string Token { get; set; } = null;
    }
}
