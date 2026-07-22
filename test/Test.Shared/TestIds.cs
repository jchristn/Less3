namespace Test.Shared
{
    using System;
    using System.IO;
    using Less3.Helpers;

    /// <summary>
    /// Test identifier helpers that avoid ID generation.
    /// </summary>
    public static class TestIds
    {
        /// <summary>
        /// Generate a tenant identifier.
        /// </summary>
        /// <returns>Tenant identifier.</returns>
        public static string Tenant()
        {
            return IdGenerator.GenerateTenantId();
        }

        /// <summary>
        /// Generate a user identifier.
        /// </summary>
        /// <returns>User identifier.</returns>
        public static string User()
        {
            return IdGenerator.GenerateUserId();
        }

        /// <summary>
        /// Generate a credential identifier.
        /// </summary>
        /// <returns>Credential identifier.</returns>
        public static string Credential()
        {
            return IdGenerator.GenerateCredentialId();
        }

        /// <summary>
        /// Generate a bucket identifier.
        /// </summary>
        /// <returns>Bucket identifier.</returns>
        public static string Bucket()
        {
            return IdGenerator.GenerateBucketId();
        }

        /// <summary>
        /// Generate an object identifier.
        /// </summary>
        /// <returns>Object identifier.</returns>
        public static string Object()
        {
            return IdGenerator.GenerateObjectId();
        }

        /// <summary>
        /// Generate a bucket tag identifier.
        /// </summary>
        /// <returns>Bucket tag identifier.</returns>
        public static string BucketTag()
        {
            return IdGenerator.GenerateBucketTagId();
        }

        /// <summary>
        /// Generate an object tag identifier.
        /// </summary>
        /// <returns>Object tag identifier.</returns>
        public static string ObjectTag()
        {
            return IdGenerator.GenerateObjectTagId();
        }

        /// <summary>
        /// Generate a role identifier.
        /// </summary>
        /// <returns>Role identifier.</returns>
        public static string Role()
        {
            return IdGenerator.GenerateRoleId();
        }

        /// <summary>
        /// Generate a permission identifier.
        /// </summary>
        /// <returns>Permission identifier.</returns>
        public static string Permission()
        {
            return IdGenerator.GeneratePermissionId();
        }

        /// <summary>
        /// Generate a role assignment identifier.
        /// </summary>
        /// <returns>Role assignment identifier.</returns>
        public static string Assignment()
        {
            return IdGenerator.GenerateAssignmentId();
        }

        /// <summary>
        /// Generate an auth session identifier.
        /// </summary>
        /// <returns>Auth session identifier.</returns>
        public static string Session()
        {
            return IdGenerator.GenerateSessionId();
        }

        /// <summary>
        /// Generate an authorization audit identifier.
        /// </summary>
        /// <returns>Authorization audit identifier.</returns>
        public static string AuthorizationAudit()
        {
            return IdGenerator.GenerateAuthorizationAuditId();
        }

        /// <summary>
        /// Generate a request history identifier.
        /// </summary>
        /// <returns>Request history identifier.</returns>
        public static string RequestHistory()
        {
            return IdGenerator.GenerateRequestHistoryId();
        }

        /// <summary>
        /// Generate a short lowercase token safe for bucket names and paths.
        /// </summary>
        /// <returns>Lowercase token.</returns>
        public static string Suffix()
        {
            return Path.GetRandomFileName().Replace(".", "", StringComparison.Ordinal).ToLowerInvariant();
        }

        /// <summary>
        /// Generate a token with a caller-supplied prefix.
        /// </summary>
        /// <param name="prefix">Token prefix.</param>
        /// <returns>Prefixed token.</returns>
        public static string Token(string prefix)
        {
            if (String.IsNullOrWhiteSpace(prefix)) throw new ArgumentNullException(nameof(prefix));
            return prefix + "-" + Suffix();
        }
    }
}
