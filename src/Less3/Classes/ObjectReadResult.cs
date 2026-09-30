namespace Less3.Classes
{
    using System.IO;

    /// <summary>
    /// An open read of an object: the row actually being served, and a stream over the requested bytes.
    /// The stream holds the key's shared read lock until it is disposed.
    /// </summary>
    public class ObjectReadResult
    {
        #region Public-Members

        /// <summary>
        /// The row being served. When the request named no version this is the latest version at the time
        /// the read lock was acquired, which can be newer than the row resolved before the lock.
        /// </summary>
        public Obj Object { get; set; } = null;

        /// <summary>
        /// Stream over the selected bytes. The caller must dispose it.
        /// </summary>
        public Stream Data { get; set; } = null;

        /// <summary>
        /// Offset of the first byte in the stream. Minimum value is 0.
        /// </summary>
        public long Start { get; set; } = 0;

        /// <summary>
        /// Number of bytes in the stream. Minimum value is 0.
        /// </summary>
        public long Length { get; set; } = 0;

        #endregion
    }
}
