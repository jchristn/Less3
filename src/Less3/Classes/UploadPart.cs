namespace Less3.Classes
{
    using System;
    using System.Security.Cryptography;
    using System.Text.Json.Serialization;
    using Watson.ORM.Core;

    /// <summary>
    /// Multipart upload part.
    /// </summary>
    [Table("uploadparts")]
    public class UploadPart
    {
        #region Public-Members

        /// <summary>
        /// ID.
        /// </summary>
        [Column("id", true, DataTypes.Int, false)]
        [JsonIgnore]
        public int Id
        {
            get
            {
                return _Id;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(Id));
                _Id = value;
            }
        }

        /// <summary>
        /// GUID.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Bucket GUID.
        /// </summary>
        [Column("bucketguid", false, DataTypes.Nvarchar, 64, false)]
        public string BucketGUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Owner GUID.
        /// </summary>
        [Column("ownerguid", false, DataTypes.Nvarchar, 64, false)]
        public string OwnerGUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Multipart upload GUID.
        /// </summary>
        [Column("uploadguid", false, DataTypes.Nvarchar, 64, false)]
        public string UploadGUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Part number.
        /// </summary>
        [Column("partnumber", false, DataTypes.Int, false)]
        public int PartNumber
        {
            get
            {
                return _PartNumber;
            }
            set
            {
                if (value < 1 || value > 10000) throw new ArgumentOutOfRangeException(nameof(PartNumber));
                _PartNumber = value;
            }
        }

        /// <summary>
        /// Part length.
        /// </summary>
        [Column("partlength", false, DataTypes.Int, false)]
        public int PartLength
        {
            get
            {
                return _PartLength;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(PartLength));
                _PartLength = value;
            }
        }

        /// <summary>
        /// MD5 hash.
        /// </summary>
        [Column("md5", false, DataTypes.Nvarchar, 32, false)]
        public string MD5Hash { get; set; } = string.Empty;

        /// <summary>
        /// SHA1 hash.
        /// </summary>
        [Column("sha1", false, DataTypes.Nvarchar, 40, false)]
        public string Sha1Hash { get; set; } = null;

        /// <summary>
        /// SHA256 hash.
        /// </summary>
        [Column("sha256", false, DataTypes.Nvarchar, 64, false)]
        public string Sha256Hash { get; set; } = null;

        /// <summary>
        /// Last access timestamp.
        /// </summary>
        [Column("lastaccessutc", false, DataTypes.DateTime, 6, false)]
        public DateTime LastAccessUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Created timestamp.
        /// </summary>
        [Column("createdutc", false, DataTypes.DateTime, 6, false)]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private int _Id = 0;
        private int _PartNumber = 1;
        private int _PartLength = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public UploadPart()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}