using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Watson.ORM.Core;

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
        [Column("id", true, DataTypes.Int, false)]
        public int Id { get; set; }

        /// <summary>
        /// GUID of the object.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; }

        /// <summary>
        /// GUID of the bucket.
        /// </summary>
        [Column("bucketguid", false, DataTypes.Nvarchar, 64, false)]
        public string BucketGUID { get; set; }

        /// <summary>
        /// GUID of the owner.
        /// </summary>
        [Column("ownerguid", false, DataTypes.Nvarchar, 64, false)]
        public string OwnerGUID { get; set; }

        /// <summary>
        /// GUID of the author.
        /// </summary>
        [Column("authorguid", false, DataTypes.Nvarchar, 64, false)]
        public string AuthorGUID { get; set; }

        /// <summary>
        /// Object key.
        /// </summary>
        [Column("key", false, DataTypes.Nvarchar, 256, false)]
        public string Key { get; set; }

        /// <summary>
        /// Content type.
        /// </summary>
        [Column("contenttype", false, DataTypes.Nvarchar, 128, true)]
        public string ContentType { get; set; }

        /// <summary>
        /// Content length.
        /// </summary>
        [Column("contentlength", false, DataTypes.Long, false)]
        public long ContentLength { get; set; }

        /// <summary>
        /// Object version.
        /// </summary>
        [Column("version", false, DataTypes.Long, false)]
        public long Version { get; set; }

        /// <summary>
        /// ETag of the object.
        /// </summary>
        [Column("etag", false, DataTypes.Nvarchar, 64, true)]
        public string Etag { get; set; }

        /// <summary>
        /// Retention type.
        /// </summary>
        [Column("retention", false, DataTypes.Enum, 32, true)]
        public RetentionType Retention { get; set; }

        /// <summary>
        /// BLOB filename.
        /// </summary>
        [Column("blobfilename", false, DataTypes.Nvarchar, 256, false)]
        public string BlobFilename { get; set; }

        /// <summary>
        /// Delete marker.
        /// </summary>
        [Column("deletemarker", false, DataTypes.Boolean, false)]
        public bool DeleteMarker { get; set; }

        /// <summary>
        /// MD5.
        /// </summary>
        [Column("md5", false, DataTypes.Nvarchar, 32, true)]
        public string Md5 { get; set; }

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        [Column("createdutc", false, DataTypes.DateTime, false)]
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Last update timestamp.
        /// </summary>
        [Column("lastupdateutc", false, DataTypes.DateTime, false)]
        public DateTime LastUpdateUtc { get; set; }

        /// <summary>
        /// Last access timestamp.
        /// </summary>
        [Column("lastaccessutc", false, DataTypes.DateTime, false)]
        public DateTime LastAccessUtc { get; set; }

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
        /// Instantiate the object.
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
