namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Multipart upload.
    /// </summary>
    public class Upload
    {
        #region Public-Members

        /// <summary>
        /// Id of the object.
        /// </summary>
        public string Id { get; set; } = Less3.Helpers.IdGenerator.GenerateUploadId();

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// Id of the bucket.
        /// </summary>
        public string BucketId { get; set; } = null;

        /// <summary>
        /// Id of the owner.
        /// </summary>
        public string OwnerId { get; set; } = null;

        /// <summary>
        /// Id of the author.
        /// </summary>
        public string AuthorId { get; set; } = null;

        /// <summary>
        /// Object key.
        /// </summary>
        public string Key { get; set; } = null;

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// Last access timestamp.
        /// </summary>
        public DateTime LastAccessUtc { get; set; } = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// Expiration UTC.
        /// </summary>
        public DateTime ExpirationUtc { get; set; } = DateTime.UtcNow.AddSeconds(60 * 60 * 24 * 7); // seven days

        /// <summary>
        /// Content type.
        /// </summary>
        public string ContentType { get; set; } = null;

        /// <summary>
        /// Custom metadata stored as JSON.
        /// </summary>
        public string Metadata { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Upload()
        {

        }
         
        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
