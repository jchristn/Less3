namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Tag entry for an object.
    /// </summary>
    public class ObjectTag
    {
        #region Public-Members

        /// <summary>
        /// Database identifier.
        /// </summary>
        [JsonIgnore]
        public int Id { get; set; } = 0;

        /// <summary>
        /// GUID.
        /// </summary>
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// GUID of the bucket.
        /// </summary>
        public string BucketGUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// GUID of the object.
        /// </summary>
        public string ObjectGUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Key.
        /// </summary>
        public string Key { get; set; } = null;

        /// <summary>
        /// Value.
        /// </summary>
        public string Value { get; set; } = null;

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
        public ObjectTag()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <param name="objectGuid">Object GUID.</param>
        /// <param name="key">Key.</param>
        /// <param name="val">Value.</param>
        public ObjectTag(string bucketGuid, string objectGuid, string key, string val)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            BucketGUID = bucketGuid;
            ObjectGUID = objectGuid;
            Key = key;
            Value = val;
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="bucketGuid">Bucket GUID.</param>
        /// <param name="objectGuid">Object GUID.</param>
        /// <param name="key">Key.</param>
        /// <param name="val">Value.</param>
        public ObjectTag(string guid, string bucketGuid, string objectGuid, string key, string val)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            if (String.IsNullOrEmpty(objectGuid)) throw new ArgumentNullException(nameof(objectGuid));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            GUID = guid;
            BucketGUID = bucketGuid;
            ObjectGUID = objectGuid;
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
