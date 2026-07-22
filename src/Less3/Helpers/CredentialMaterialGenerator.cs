namespace Less3.Helpers
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Generates access-key and secret-key material for credentials.
    /// </summary>
    internal static class CredentialMaterialGenerator
    {
        /// <summary>
        /// Generate an access key.
        /// </summary>
        /// <returns>Access key.</returns>
        internal static string GenerateAccessKey()
        {
            return IdGenerator.GenerateAccessKey();
        }

        /// <summary>
        /// Generate a secret key.
        /// </summary>
        /// <returns>Secret key.</returns>
        internal static string GenerateSecretKey()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
        }

    }
}
