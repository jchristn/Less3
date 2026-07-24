namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Credential.
    /// </summary>
    public class Credential
    {
        #region Public-Members

        /// <summary>
        /// Id of the credential.
        /// </summary>
        public string Id { get; set; } = Less3.Helpers.IdGenerator.GenerateCredentialId();

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// User Id.
        /// </summary>
        public string UserId { get; set; } = Less3.Helpers.IdGenerator.GenerateUserId();

        /// <summary>
        /// Description.
        /// </summary>
        public string Description { get; set; } = null;

        /// <summary>
        /// Access key.
        /// </summary>
        public string AccessKey { get; set; } = null;

        /// <summary>
        /// Secret key.
        /// </summary>
        public string SecretKey { get; set; } = null;

        /// <summary>
        /// Indicates if the secret key is base64 encoded.
        /// </summary>
        public bool IsBase64 { get; set; } = false;

        /// <summary>
        /// Whether the credential is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// UTC timestamp of the last successful use.
        /// </summary>
        public DateTime? LastUsedUtc { get; set; } = null;

        /// <summary>
        /// UTC timestamp of the last failed use.
        /// </summary>
        public DateTime? LastFailedUtc { get; set; } = null;

        /// <summary>
        /// Timestamp from record creation, in UTC time.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.Now.ToUniversalTime();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Credential()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="description">Description.</param>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param>
        /// <param name="isBase64">Is base64 encoded.</param>
        public Credential(string userId, string description, string accessKey, string secretKey, bool isBase64)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));

            Id = Less3.Helpers.IdGenerator.GenerateCredentialId();
            UserId = userId;
            Description = description;
            AccessKey = accessKey;
            SecretKey = secretKey;
            IsBase64 = isBase64;
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="id">Id.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="description">Description.</param>
        /// <param name="accessKey">Access key.</param>
        /// <param name="secretKey">Secret key.</param>
        /// <param name="isBase64">Is base64 encoded.</param>
        public Credential(string id, string userId, string description, string accessKey, string secretKey, bool isBase64)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            if (String.IsNullOrEmpty(secretKey)) throw new ArgumentNullException(nameof(secretKey));

            Id = id;
            UserId = userId;
            Description = description;
            AccessKey = accessKey;
            SecretKey = secretKey;
            IsBase64 = isBase64;
        }
         
        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
