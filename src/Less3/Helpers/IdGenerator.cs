namespace Less3.Helpers
{
    using Less3.Classes;

    /// <summary>
    /// Generates Less3 PrettyID identifiers.
    /// </summary>
    public static class IdGenerator
    {
        private static readonly PrettyId.IdGenerator _Generator = new PrettyId.IdGenerator()
        {
            UseCryptographicRng = true
        };

        /// <summary>
        /// Maximum generated identifier length, including the entity prefix.
        /// </summary>
        public static int MaximumLength
        {
            get
            {
                return 32;
            }
        }

        /// <summary>
        /// Generate a tenant identifier.
        /// </summary>
        /// <returns>Tenant identifier.</returns>
        public static string GenerateTenantId()
        {
            return Generate(Constants.IdPrefixes.Tenant);
        }

        /// <summary>
        /// Generate a user identifier.
        /// </summary>
        /// <returns>User identifier.</returns>
        public static string GenerateUserId()
        {
            return Generate(Constants.IdPrefixes.User);
        }

        /// <summary>
        /// Generate a credential identifier.
        /// </summary>
        /// <returns>Credential identifier.</returns>
        public static string GenerateCredentialId()
        {
            return Generate(Constants.IdPrefixes.Credential);
        }

        /// <summary>
        /// Generate a bucket identifier.
        /// </summary>
        /// <returns>Bucket identifier.</returns>
        public static string GenerateBucketId()
        {
            return Generate(Constants.IdPrefixes.Bucket);
        }

        /// <summary>
        /// Generate an object identifier.
        /// </summary>
        /// <returns>Object identifier.</returns>
        public static string GenerateObjectId()
        {
            return Generate(Constants.IdPrefixes.Object);
        }

        /// <summary>
        /// Generate a multipart upload identifier.
        /// </summary>
        /// <returns>Multipart upload identifier.</returns>
        public static string GenerateUploadId()
        {
            return Generate(Constants.IdPrefixes.Upload);
        }

        /// <summary>
        /// Generate a multipart upload part identifier.
        /// </summary>
        /// <returns>Multipart upload part identifier.</returns>
        public static string GenerateUploadPartId()
        {
            return Generate(Constants.IdPrefixes.UploadPart);
        }

        /// <summary>
        /// Generate a bucket tag identifier.
        /// </summary>
        /// <returns>Bucket tag identifier.</returns>
        public static string GenerateBucketTagId()
        {
            return Generate(Constants.IdPrefixes.BucketTag);
        }

        /// <summary>
        /// Generate an object tag identifier.
        /// </summary>
        /// <returns>Object tag identifier.</returns>
        public static string GenerateObjectTagId()
        {
            return Generate(Constants.IdPrefixes.ObjectTag);
        }

        /// <summary>
        /// Generate a bucket ACL identifier.
        /// </summary>
        /// <returns>Bucket ACL identifier.</returns>
        public static string GenerateBucketAclId()
        {
            return Generate(Constants.IdPrefixes.BucketAcl);
        }

        /// <summary>
        /// Generate an object ACL identifier.
        /// </summary>
        /// <returns>Object ACL identifier.</returns>
        public static string GenerateObjectAclId()
        {
            return Generate(Constants.IdPrefixes.ObjectAcl);
        }

        /// <summary>
        /// Generate a role identifier.
        /// </summary>
        /// <returns>Role identifier.</returns>
        public static string GenerateRoleId()
        {
            return Generate(Constants.IdPrefixes.Role);
        }

        /// <summary>
        /// Generate a permission identifier.
        /// </summary>
        /// <returns>Permission identifier.</returns>
        public static string GeneratePermissionId()
        {
            return Generate(Constants.IdPrefixes.Permission);
        }

        /// <summary>
        /// Generate a role assignment identifier.
        /// </summary>
        /// <returns>Role assignment identifier.</returns>
        public static string GenerateAssignmentId()
        {
            return Generate(Constants.IdPrefixes.Assignment);
        }

        /// <summary>
        /// Generate an authentication session identifier.
        /// </summary>
        /// <returns>Authentication session identifier.</returns>
        public static string GenerateSessionId()
        {
            return Generate(Constants.IdPrefixes.Session);
        }

        /// <summary>
        /// Generate an authorization audit identifier.
        /// </summary>
        /// <returns>Authorization audit identifier.</returns>
        public static string GenerateAuthorizationAuditId()
        {
            return Generate(Constants.IdPrefixes.AuthorizationAudit);
        }

        /// <summary>
        /// Generate a request history identifier.
        /// </summary>
        /// <returns>Request history identifier.</returns>
        public static string GenerateRequestHistoryId()
        {
            return Generate(Constants.IdPrefixes.RequestHistory);
        }

        /// <summary>
        /// Generate an access key value.
        /// </summary>
        /// <returns>Access key.</returns>
        public static string GenerateAccessKey()
        {
            return Generate(Constants.IdPrefixes.AccessKey);
        }

        private static string Generate(string prefix)
        {
            return _Generator.GenerateKSortable(prefix, MaximumLength);
        }
    }
}
