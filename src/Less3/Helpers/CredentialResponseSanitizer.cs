namespace Less3.Helpers
{
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Builds credential response objects without leaking secret keys except when explicitly allowed.
    /// </summary>
    internal static class CredentialResponseSanitizer
    {
        /// <summary>
        /// Create a response-safe credential object.
        /// </summary>
        /// <param name="credential">Credential.</param>
        /// <param name="includeSecret">Whether to include the secret key.</param>
        /// <returns>Response-safe credential.</returns>
        internal static Credential ForResponse(Credential credential, bool includeSecret)
        {
            if (credential == null) return null;

            return new Credential
            {
                Id = credential.Id,
                TenantId = credential.TenantId,
                UserId = credential.UserId,
                Description = credential.Description,
                AccessKey = credential.AccessKey,
                SecretKey = includeSecret ? credential.SecretKey : null,
                IsBase64 = credential.IsBase64,
                Active = credential.Active,
                LastUsedUtc = credential.LastUsedUtc,
                LastFailedUtc = credential.LastFailedUtc,
                CreatedUtc = credential.CreatedUtc
            };
        }

        /// <summary>
        /// Create response-safe credential objects.
        /// </summary>
        /// <param name="credentials">Credentials.</param>
        /// <param name="includeSecret">Whether to include secret keys.</param>
        /// <returns>Response-safe credentials.</returns>
        internal static List<Credential> ForResponse(List<Credential> credentials, bool includeSecret)
        {
            List<Credential> results = new List<Credential>();
            if (credentials == null) return results;

            foreach (Credential credential in credentials)
            {
                results.Add(ForResponse(credential, includeSecret));
            }

            return results;
        }
    }
}
