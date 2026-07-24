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
        /// Id.
        /// </summary>
        public string Id { get; set; } = Less3.Helpers.IdGenerator.GenerateObjectTagId();

        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// Id of the bucket.
        /// </summary>
        public string BucketId { get; set; } = Less3.Helpers.IdGenerator.GenerateBucketId();

        /// <summary>
        /// Id of the object.
        /// </summary>
        public string ObjectId { get; set; } = Less3.Helpers.IdGenerator.GenerateObjectId();

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
        /// <param name="bucketId">Bucket Id.</param>
        /// <param name="objectId">Object Id.</param>
        /// <param name="key">Key.</param>
        /// <param name="val">Value.</param>
        public ObjectTag(string bucketId, string objectId, string key, string val)
        {
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            BucketId = bucketId;
            ObjectId = objectId;
            Key = key;
            Value = val;
        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="id">Id.</param>
        /// <param name="bucketId">Bucket Id.</param>
        /// <param name="objectId">Object Id.</param>
        /// <param name="key">Key.</param>
        /// <param name="val">Value.</param>
        public ObjectTag(string id, string bucketId, string objectId, string key, string val)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrEmpty(bucketId)) throw new ArgumentNullException(nameof(bucketId));
            if (String.IsNullOrEmpty(objectId)) throw new ArgumentNullException(nameof(objectId));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            Id = id;
            BucketId = bucketId;
            ObjectId = objectId;
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
