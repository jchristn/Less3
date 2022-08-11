using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Watson.ORM.Core;
using Newtonsoft.Json;

namespace Less3.Classes
{
    /// <summary>
    /// Object stored in Less3.
    /// </summary>
    [Table("objects")]
    public class Obj
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
        /// Content type.
        /// </summary>
        [Column("contenttype", false, DataTypes.Nvarchar, 128, true)]
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Content length.
        /// </summary>
        [Column("contentlength", false, DataTypes.Long, false)]
        public long ContentLength { get; set; } = 0;

        /// <summary>
        /// Object version.
        /// </summary>
        [Column("version", false, DataTypes.Long, false)]
        public long Version { get; set; } = 1;

        /// <summary>
        /// ETag of the object.
        /// </summary>
        [Column("etag", false, DataTypes.Nvarchar, 64, true)]
        public string Etag { get; set; } = null;

        /// <summary>
        /// Retention type.
        /// </summary>
        [Column("retention", false, DataTypes.Enum, 32, true)]
        public RetentionType Retention { get; set; } = RetentionType.NONE;

        /// <summary>
        /// BLOB filename.
        /// </summary>
        [Column("blobfilename", false, DataTypes.Nvarchar, 256, false)]
        public string BlobFilename { get; set; } = null;

        /// <summary>
        /// Indicates if the object is a folder, i.e. ends with '/' and has a content length of 0.
        /// </summary>
        [Column("isfolder", false, DataTypes.Boolean, false)]
        public bool IsFolder { get; set; } = false;

        /// <summary>
        /// Delete marker.
        /// </summary>
        [Column("deletemarker", false, DataTypes.Boolean, false)]
        public bool DeleteMarker { get; set; } = false;

        /// <summary>
        /// MD5.
        /// </summary>
        [Column("md5", false, DataTypes.Nvarchar, 32, true)]
        public string Md5 { get; set; } = null;

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        [Column("createdutc", false, DataTypes.DateTime, false)]
        public DateTime CreatedUtc { get; set; } = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// Last update timestamp.
        /// </summary>
        [Column("lastupdateutc", false, DataTypes.DateTime, false)]
        public DateTime LastUpdateUtc { get; set; } = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// Last access timestamp.
        /// </summary>
        [Column("lastaccessutc", false, DataTypes.DateTime, false)]
        public DateTime LastAccessUtc { get; set; } = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// Object expiration timestamp.
        /// </summary>
        [Column("expirationutc", false, DataTypes.DateTime, true)]
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
