namespace Less3.Classes
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Tag entry for a bucket.
    /// </summary>
    public class BucketTag
    {
        #region Public-Members

        /// <summary>
        /// Id.
        /// </summary>
        public string Id { get; set; } = Less3.Helpers.IdGenerator.GenerateBucketTagId();

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// Id of the bucket.
        /// </summary>
        public string BucketId { get; set; } = null;

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
        public BucketTag()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="bucketId">Bucket Id.</param>
        /// <param name="key">Key.</param>
        /// <param name="val">Value.</param>
        public BucketTag(string bucketId, string key, string val)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            BucketId = bucketId;
            Key = key;
            Value = val;
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="id">Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <param name="key">Key.</param>
        /// <param name="val">Value.</param>
        public BucketTag(string id, string bucketId, string key, string val)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Id = id;
            BucketId = bucketId;
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
