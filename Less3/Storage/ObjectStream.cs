using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Less3.Storage
{
    /// <summary>
    /// Stream of data for an object.
    /// </summary>
    public class ObjectStream
    {
        #region Public-Members

        /// <summary>
        /// Object key.
        /// </summary>
        public string Key { get; set; }
        
        /// <summary>
        /// Content length.
        /// </summary>
        public long ContentLength { get; set; }

        /// <summary>
        /// Stream containing data.
        /// </summary>
        public Stream Data { get; set; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="contentLength">Content length.</param>
        /// <param name="data">Stream containing data.</param>
        public ObjectStream(string key, long contentLength, Stream data)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (contentLength < 0) throw new ArgumentException("Content length must be zero or greater.");
            if (data == null) throw new ArgumentNullException(nameof(data));

            Key = key;
            ContentLength = contentLength;
            Data = data;
        }

        #endregion
    }
}
