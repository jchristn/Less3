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
        /// Database identifier.
        /// </summary>
        [JsonIgnore]
        public int Id { get; set; } = 0;

        /// <summary>
        /// GUID of the object.
        /// </summary>
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// GUID of the bucket.
        /// </summary>
        public string BucketGUID { get; set; } = null;

        /// <summary>
        /// GUID of the owner.
        /// </summary>
        public string OwnerGUID { get; set; } = null;

        /// <summary>
        /// GUID of the author.
        /// </summary>
        public string AuthorGUID { get; set; } = null;

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
