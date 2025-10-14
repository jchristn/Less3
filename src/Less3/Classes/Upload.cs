namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Watson.ORM.Core;

    /// <summary>
    /// Multipart upload.
    /// </summary>
    [Table("uploads")]
    public class Upload
    {
        #region Public-Members

        /// <summary>
        /// Database identifier.
        /// </summary>
        [JsonIgnore]
        [Column("id", true, DataTypes.Int, false)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// GUID of the object.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// GUID of the bucket.
        /// </summary>
        [Column("bucketguid", false, DataTypes.Nvarchar, 64, false)]
        public string BucketGUID { get; set; } = null;

        /// <summary>
        /// GUID of the owner.
        /// </summary>
        [Column("ownerguid", false, DataTypes.Nvarchar, 64, false)]
        public string OwnerGUID { get; set; } = null;

        /// <summary>
        /// GUID of the author.
        /// </summary>
        [Column("authorguid", false, DataTypes.Nvarchar, 64, false)]
        public string AuthorGUID { get; set; } = null;

        /// <summary>
        /// Object key.
        /// </summary>
        [Column("key", false, DataTypes.Nvarchar, 256, false)]
        public string Key { get; set; } = null;

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        [Column("createdutc", false, DataTypes.DateTime, false)]
        public DateTime CreatedUtc { get; set; } = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// Last access timestamp.
        /// </summary>
        [Column("lastaccessutc", false, DataTypes.DateTime, false)]
        public DateTime LastAccessUtc { get; set; } = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// Expiration UTC.
        /// </summary>
        [Column("expirationutc", false, DataTypes.DateTime, 6, false)]
        public DateTime ExpirationUtc { get; set; } = DateTime.UtcNow.AddSeconds(60 * 60 * 24 * 7); // seven days

        /// <summary>
        /// Content type.
        /// </summary>
        [Column("contenttype", false, DataTypes.Nvarchar, 256, true)]
        public string ContentType { get; set; } = null;

        /// <summary>
        /// Custom metadata stored as JSON.
        /// </summary>
        [Column("metadata", false, DataTypes.Nvarchar, 4096, true)]
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
