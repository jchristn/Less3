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
        /// <summary>
        /// The name of the bucket.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// GUID of the bucket.
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// The number of objects in the bucket including all versions.
        /// </summary>
        public long Objects = 0;

        /// <summary>
        /// The number of bytes for all objects in the bucket.
        /// </summary>
        public long Bytes = 0;

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public BucketStatistics()
        {

        }
    }
}
