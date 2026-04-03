namespace Less3.Storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Stream of data for an object.
    /// Implements IDisposable to ensure the underlying data stream is properly released.
    /// </summary>
    public class ObjectStream : IDisposable
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

        #region Private-Members

        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
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

        #region Public-Methods

        /// <summary>
        /// Dispose of the object stream and release the underlying data stream.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Dispose of the object stream.
        /// </summary>
        /// <param name="disposing">Disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                if (Data != null)
                {
                    Data.Dispose();
                    Data = null;
                }
            }

            _Disposed = true;
        }

        #endregion
    }
}
