namespace Less3.Helpers
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Redacts password, token, credential, and signature material before text is written to logs or request history.
    /// </summary>
    public static class LogSanitizer
    {
        #region Private-Members

        private static readonly Regex _JsonStringSecretRegex = new Regex(
            "(\"(?:Password|PasswordHash|SecretKey|Token|TokenHash|Authorization|XApiKey|AdminApiKey|SessionToken|AccessToken|RefreshToken)\"\\s*:\\s*\")([^\"]*)(\")",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _QuerySecretRegex = new Regex(
            "([?&](?:password|passwordHash|secretKey|token|tokenHash|sessionToken|accessToken|refreshToken|x-api-key|apiKey|adminApiKey|signature|X-Amz-Signature|X-Amz-Credential|X-Amz-Security-Token)=)[^&\\s]*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _HeaderSecretRegex = new Regex(
            "^(\\s*(?:Authorization|Proxy-Authorization|Cookie|Set-Cookie|x-api-key|x-less3-session-token|x-session-token|x-token|x-secret-key|x-amz-security-token|[\\w-]*(?:api-key|token|secret|password)[\\w-]*)\\s*:\\s*)(.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex _AuthorizationCredentialRegex = new Regex(
            "((?:Credential|Signature|SignedHeaders)=)([^,\\s]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion

        #region Public-Methods

        /// <summary>
        /// Redacts sensitive values from free-form request, response, exception, and diagnostic text.
        /// </summary>
        /// <param name="value">Input text.</param>
        /// <returns>Sanitized text.</returns>
        public static string Redact(string value)
        {
            if (String.IsNullOrEmpty(value)) return value;

            string redacted = _JsonStringSecretRegex.Replace(value, "$1[redacted]$3");
            redacted = _QuerySecretRegex.Replace(redacted, "$1[redacted]");
            redacted = _HeaderSecretRegex.Replace(redacted, "$1[redacted]");
            redacted = _AuthorizationCredentialRegex.Replace(redacted, "$1[redacted]");
            return redacted;
        }

        #endregion
    }
}
