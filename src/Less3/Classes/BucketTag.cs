using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Watson.ORM.Core;
using Newtonsoft.Json;

namespace Less3.Classes
{
    /// <summary>
    /// Tag entry for a bucket.
    /// </summary>
    [Table("buckettags")]
    public class BucketTag
    {
        #region Public-Members

        /// <summary>
        /// Database identifier.
        /// </summary>
        [JsonIgnore]
        [Column("id", true, DataTypes.Int, false)]
        public int Id { get; set; } = 0;

        /// <summary>
        /// GUID.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// GUID of the bucket.
        /// </summary>
        [Column("bucketguid", false, DataTypes.Nvarchar, 64, false)]
        public string BucketGUID { get; set; } = null;

        /// <summary>
        /// Key.
        /// </summary>
        [Column("tagkey", false, DataTypes.Nvarchar, 256, false)]
        public string Key { get; set; } = null;

        /// <summary>
        /// Value.
        /// </summary>
        [Column("tagvalue", false, DataTypes.Nvarchar, 1024, true)]
        public string Value { get; set; } = null;

        /// <summary>
        /// Timestamp from record creation, in UTC time.
        /// </summary>
        [Column("createdutc", false, DataTypes.DateTime, 6, 6, false)]
        public DateTime CreatedUtc { get; set; } = DateTime.Now.ToUniversalTime();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public BucketTag()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <param name="key">Key.</param>
        /// <param name="val">Value.</param>
        public BucketTag(string bucketGuid, string key, string val)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            BucketGUID = bucketGuid;
            Key = key;
            Value = val;
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <param name="key">Key.</param>
        /// <param name="val">Value.</param>
        public BucketTag(string guid, string bucketGuid, string key, string val)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            GUID = guid;
            BucketGUID = bucketGuid;
            Key = key;
            Value = val;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
