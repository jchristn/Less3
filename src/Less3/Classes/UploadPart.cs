namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Multipart upload part.
    /// </summary>
    public class UploadPart
    {
        #region Public-Members

        /// <summary>
        /// ID.
        /// </summary>
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
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Bucket GUID.
        /// </summary>
        public string BucketGUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Owner GUID.
        /// </summary>
        public string OwnerGUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Multipart upload GUID.
        /// </summary>
        public string UploadGUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Part number.
        /// </summary>
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
        public string MD5Hash { get; set; } = string.Empty;

        /// <summary>
        /// SHA1 hash.
        /// </summary>
        public string Sha1Hash { get; set; } = null;

        /// <summary>
        /// SHA256 hash.
        /// </summary>
        public string Sha256Hash { get; set; } = null;

        /// <summary>
        /// Last access timestamp.
        /// </summary>
        public DateTime LastAccessUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Created timestamp.
        /// </summary>
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