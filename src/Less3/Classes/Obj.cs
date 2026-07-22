namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Object stored in Less3.
    /// </summary>
    public class Obj
    {
        #region Public-Members

        /// <summary>
        /// Id of the object.
        /// </summary>
        public string Id { get; set; } = Less3.Helpers.IdGenerator.GenerateObjectId();

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
        /// Content type.
        /// </summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Content length.
        /// </summary>
        public long ContentLength { get; set; } = 0;

        /// <summary>
        /// Object version.
        /// </summary>
        public long Version { get; set; } = 1;

        /// <summary>
        /// ETag of the object.
        /// </summary>
        public string Etag { get; set; } = null;

        /// <summary>
        /// Retention type.
        /// </summary>
        public RetentionType Retention { get; set; } = RetentionType.NONE;

        /// <summary>
        /// BLOB filename.
        /// </summary>
        public string BlobFilename { get; set; } = null;

        /// <summary>
        /// Indicates if the object is a folder, i.e. ends with '/' and has a content length of 0.
        /// </summary>
        public bool IsFolder { get; set; } = false;

        /// <summary>
        /// Delete marker.
        /// </summary>
        public bool DeleteMarker { get; set; } = false;

        /// <summary>
        /// MD5.
        /// </summary>
        public string Md5 { get; set; } = null;

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// Last update timestamp.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// Last access timestamp.
        /// </summary>
        public DateTime LastAccessUtc { get; set; } = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// User-defined metadata stored as JSON.
        /// </summary>
        public string Metadata { get; set; } = null;

        /// <summary>
        /// Object expiration timestamp.
        /// </summary>
        public DateTime? ExpirationUtc = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Obj()
        {

        }
         
        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
