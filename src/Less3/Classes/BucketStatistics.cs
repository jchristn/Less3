namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Bucket statistics.
    /// </summary>
    public class BucketStatistics
    {
        #region Public-Members

        /// <summary>
        /// The name of the bucket.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Id of the bucket.
        /// </summary>
        public string Id { get; set; } = Less3.Helpers.IdGenerator.GenerateBucketId();

        /// <summary>
        /// The number of objects in the bucket including all versions.
        /// </summary>
        public long Objects = 0;

        /// <summary>
        /// The number of bytes for all objects in the bucket.
        /// </summary>
        public long Bytes = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public BucketStatistics()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="id">Id.</param>
        /// <param name="objects">Number of objects.</param>
        /// <param name="bytes">Number of bytes.</param>
        public BucketStatistics(string name, string id, long objects, long bytes)
        {
            Name = name;
            Id = id;
            Objects = objects;
            Bytes = bytes;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
