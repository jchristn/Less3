using System;
using System.Collections.Generic;
using System.Text;

namespace Less3.Classes
{
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
        /// GUID of the bucket.
        /// </summary>
        public string GUID { get; set; } = Guid.NewGuid().ToString();

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
        /// <param name="guid">GUID.</param>
        /// <param name="objects">Number of objects.</param>
        /// <param name="bytes">Number of bytes.</param>
        public BucketStatistics(string name, string guid, long objects, long bytes)
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
