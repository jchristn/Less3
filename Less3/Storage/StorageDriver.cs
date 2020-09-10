using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Less3.Storage
{
    /// <summary>
    /// Less3 storage driver; allows developers to build their own storage providers for Less3.
    /// </summary>
    public abstract class StorageDriver
    {
        #region Public-Methods

        /// <summary>
        /// Delete an object by key.
        /// </summary>
        /// <param name="key">Key.</param>
        public abstract void Delete(string key);

        /// <summary>
        /// Verify the existence of an object by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>True if exists.</returns>
        public abstract bool Exists(string key);

        /// <summary>
        /// Read an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Data.</returns>
        public abstract byte[] Read(string key);

        /// <summary>
        /// Read an object asynchronously.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Data.</returns>
        public abstract Task<byte[]> ReadAsync(string key);

        /// <summary>
        /// Read an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>ObjectStream.</returns>
        public abstract ObjectStream ReadStream(string key);
         
        /// <summary>
        /// Read a specific number of bytes from a specific location in an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="indexStart">Starting position.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>Data.</returns>
        public abstract byte[] ReadRange(string key, long indexStart, long count);

        /// <summary>
        /// Read a specific number of bytes asynchronously from a specific location in an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="indexStart">Starting position.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>Data.</returns>
        public abstract Task<byte[]> ReadRangeAsync(string key, long indexStart, long count);

        /// <summary>
        /// Read a specific number of bytes from a specific location in an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="indexStart">Starting position.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>ObjectStream.</returns>
        public abstract ObjectStream ReadRangeStream(string key, long indexStart, long count);

        /// <summary>
        /// Read a specific number of bytes asynchronously from a specific location in an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="indexStart">Starting position.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>ObjectStream.</returns>
        public abstract Task<ObjectStream> ReadRangeStreamAsync(string key, long indexStart, long count);

        /// <summary>
        /// Write an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="data">Data.</param> 
        /// <returns>MD5 hash.</returns>
        public abstract byte[] Write(string key, byte[] data);

        /// <summary>
        /// Write an object asynchronously.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="data">Data.</param> 
        /// <returns>MD5 hash.</returns>
        public abstract Task<byte[]> WriteAsync(string key, byte[] data);

        /// <summary>
        /// Write an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="contentLength">Number of bytes to read from the stream.</param>
        /// <param name="stream">Stream.</param> 
        /// <returns>MD5 hash.</returns>
        public abstract byte[] Write(string key, long contentLength, Stream stream);

        /// <summary>
        /// Write an object asynchronously.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="contentLength">Number of bytes to read from the stream.</param>
        /// <param name="stream">Stream.</param> 
        /// <returns>MD5 hash.</returns>
        public abstract Task<byte[]> WriteAsync(string key, long contentLength, Stream stream);
         
        #endregion 
    }
}
