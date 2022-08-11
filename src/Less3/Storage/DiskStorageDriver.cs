using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Less3.Classes;

namespace Less3.Storage
{
    /// <summary>
    /// Disk storage driver.
    /// </summary>
    public class DiskStorageDriver : StorageDriver
    {
        #region Public-Members

        /// <summary>
        /// Buffer size to use while reading file streams.
        /// </summary>
        public int StreamBufferSize
        {
            get
            {
                return _StreamBufferSize;
            }
            set
            {
                if (value < 1) throw new ArgumentException("Stream buffer size must be greater than zero bytes.");
                _StreamBufferSize = value;
            }
        }

        #endregion

        #region Private-Members

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private CancellationToken _Token;
        private string _BaseDirectory = null;
        private int _StreamBufferSize = 65536;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="baseDirectory">Base directory.</param>
        public DiskStorageDriver(string baseDirectory)
        {
            if (String.IsNullOrEmpty(baseDirectory)) throw new ArgumentNullException(nameof(baseDirectory));
            if (!Directory.Exists(baseDirectory)) Directory.CreateDirectory(baseDirectory);

            baseDirectory = baseDirectory.Replace('\\', '/');
            if (!baseDirectory.EndsWith("/")) baseDirectory += "/";

            _BaseDirectory = baseDirectory;
            _Token = _TokenSource.Token;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Delete an object by key.
        /// </summary>
        /// <param name="key">Key.</param>
        public override void Delete(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            string file = FilePath(key);
            if (File.Exists(file)) File.Delete(file);
        }

        /// <summary>
        /// Verify the existence of an object by key.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>True if exists.</returns>
        public override bool Exists(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            string file = FilePath(key);
            if (File.Exists(file)) return true;
            return false;
        }

        /// <summary>
        /// Read an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Data.</returns>
        public override byte[] Read(string key)
        { 
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            string file = FilePath(key);
            return File.ReadAllBytes(file);
        }

        /// <summary>
        /// Read an object asynchronously.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Data.</returns>
        public override async Task<byte[]> ReadAsync(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            string file = FilePath(key);
            return await File.ReadAllBytesAsync(file);
        }

        /// <summary>
        /// Read an object.
        /// Your code must close the stream when complete.
        /// </summary>
        /// <param name="key">Key.</param> 
        /// <returns>ObjectStream.</returns>
        public override ObjectStream ReadStream(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            string file = FilePath(key);
            FileInfo fi = new FileInfo(file);
            long contentLength = fi.Length;
            FileStream stream = new FileStream(file, FileMode.Open);
            stream.Seek(0, SeekOrigin.Begin);
            return new ObjectStream(key, fi.Length, stream);
        }
         
        /// <summary>
        /// Read a specific number of bytes from a specific location in an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="indexStart">Starting position.</param>
        /// <param name="count">Number of bytes to read.</param> 
        /// <returns>Data.</returns>
        public override byte[] ReadRange(string key, long indexStart, long count)
        { 
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (indexStart < 0) throw new ArgumentException("Index start must be zero or greater.");
            if (count < 0) throw new ArgumentException("Count must be zero or greater.");
            
            string file = FilePath(key);
            FileInfo fi = new FileInfo(file);
            long contentLength = fi.Length;

            if (indexStart + count > contentLength) throw new ArgumentException("Index start combined with count must not result in a position that exceeds the size of the file.");

            using (FileStream fs = new FileStream(file, FileMode.Open))
            {
                long bytesRemaining = count;
                int read = 0;
                byte[] buffer = null;

                using (MemoryStream ms = new MemoryStream())
                {
                    while (bytesRemaining > 0)
                    {
                        if (bytesRemaining > _StreamBufferSize) buffer = new byte[_StreamBufferSize];
                        else buffer = new byte[bytesRemaining];

                        read = fs.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            ms.Write(buffer, 0, read);
                            bytesRemaining -= read;
                        }
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Read a specific number of bytes asynchronously from a specific location in an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="indexStart">Starting position.</param>
        /// <param name="count">Number of bytes to read.</param> 
        /// <returns>Data.</returns>
        public override async Task<byte[]> ReadRangeAsync(string key, long indexStart, long count)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (indexStart < 0) throw new ArgumentException("Index start must be zero or greater.");
            if (count < 0) throw new ArgumentException("Count must be zero or greater.");

            string file = FilePath(key);
            FileInfo fi = new FileInfo(file);
            long contentLength = fi.Length;

            if (indexStart + count > contentLength) throw new ArgumentException("Index start combined with count must not result in a position that exceeds the size of the file.");

            using (FileStream fs = new FileStream(file, FileMode.Open))
            {
                long bytesRemaining = count;
                int read = 0;
                byte[] buffer = null;

                using (MemoryStream ms = new MemoryStream())
                {
                    while (bytesRemaining > 0)
                    {
                        if (bytesRemaining > _StreamBufferSize) buffer = new byte[_StreamBufferSize];
                        else buffer = new byte[bytesRemaining];

                        read = await fs.ReadAsync(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            await ms.WriteAsync(buffer, 0, read);
                            bytesRemaining -= read;
                        }
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Read a specific number of bytes from a specific location in an object.
        /// Your code must close the stream when complete.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="indexStart">Starting position.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>ObjectStream.</returns>
        public override ObjectStream ReadRangeStream(string key, long indexStart, long count)
        { 
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (indexStart < 0) throw new ArgumentException("Index start must be zero or greater.");
            if (count < 0) throw new ArgumentException("Count must be zero or greater.");

            string file = FilePath(key);
            FileInfo fi = new FileInfo(file);
            long contentLength = fi.Length;

            if (indexStart + count > contentLength) throw new ArgumentException("Index start combined with count must not result in a position that exceeds the size of the file.");

            using (FileStream fs = new FileStream(file, FileMode.Open))
            {
                long bytesRemaining = count;
                int read = 0;
                byte[] buffer = null;
                Stream ms = new MemoryStream(); 

                while (bytesRemaining > 0)
                {
                    if (bytesRemaining > _StreamBufferSize) buffer = new byte[_StreamBufferSize];
                    else buffer = new byte[bytesRemaining];

                    read = fs.Read(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        ms.Write(buffer, 0, read);
                        bytesRemaining -= read;
                    }
                }

                ms.Seek(0, SeekOrigin.Begin);
                return new ObjectStream(key, count, ms);
            }
        }

        /// <summary>
        /// Read a specific number of bytes asynchronously from a specific location in an object.
        /// Your code must close the stream when complete.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="indexStart">Starting position.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>ObjectStream.</returns>
        public override async Task<ObjectStream> ReadRangeStreamAsync(string key, long indexStart, long count)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (indexStart < 0) throw new ArgumentException("Index start must be zero or greater.");
            if (count < 0) throw new ArgumentException("Count must be zero or greater.");

            string file = FilePath(key);
            FileInfo fi = new FileInfo(file);
            long contentLength = fi.Length;

            if (indexStart + count > contentLength) throw new ArgumentException("Index start combined with count must not result in a position that exceeds the size of the file.");

            using (FileStream fs = new FileStream(file, FileMode.Open))
            {
                long bytesRemaining = count;
                int read = 0;
                byte[] buffer = null;
                Stream ms = new MemoryStream();

                while (bytesRemaining > 0)
                {
                    if (bytesRemaining > _StreamBufferSize) buffer = new byte[_StreamBufferSize];
                    else buffer = new byte[bytesRemaining];

                    read = await fs.ReadAsync(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        await ms.WriteAsync(buffer, 0, read);
                        bytesRemaining -= read;
                    }
                }

                ms.Seek(0, SeekOrigin.Begin);
                return new ObjectStream(key, count, ms);
            }
        }

        /// <summary>
        /// Write an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="data">Data.</param> 
        /// <returns>MD5 hash.</returns>
        public override byte[] Write(string key, byte[] data)
        { 
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (data == null) data = new byte[0];
            MemoryStream ms = new MemoryStream(data);
            ms.Seek(0, SeekOrigin.Begin);
            return Write(key, data.Length, ms);
        }

        /// <summary>
        /// Write an object asynchronously.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="data">Data.</param> 
        /// <returns>MD5 hash.</returns>
        public override async Task<byte[]> WriteAsync(string key, byte[] data)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (data == null) data = new byte[0];
            MemoryStream ms = new MemoryStream(data);
            ms.Seek(0, SeekOrigin.Begin);
            return await WriteAsync(key, data.Length, ms);
        }

        /// <summary>
        /// Write an object.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="contentLength">Number of bytes to read from the stream.</param>
        /// <param name="stream">Stream.</param>
        /// <returns>MD5 hash.</returns>
        public override byte[] Write(string key, long contentLength, Stream stream)
        { 
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string file = FilePath(key);
            using (FileStream fs = new FileStream(file, FileMode.Create))
            {
                long bytesRemaining = contentLength;
                int read = 0;
                byte[] buffer = new byte[_StreamBufferSize];

                while (bytesRemaining > 0)
                {
                    read = stream.Read(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        fs.Write(buffer, 0, read);
                        bytesRemaining -= read;
                    }
                }
            }

            FileInfo fi = new FileInfo(file);

            using (FileStream fs = new FileStream(file, FileMode.Open))
            {
                return Common.Md5(fs);
            }
        }

        /// <summary>
        /// Write an object asynchronously.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="contentLength">Number of bytes to read from the stream.</param>
        /// <param name="stream">Stream.</param>
        /// <returns>MD5 hash.</returns>
        public override async Task<byte[]> WriteAsync(string key, long contentLength, Stream stream)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string file = FilePath(key);
            using (FileStream fs = new FileStream(file, FileMode.Create))
            {
                long bytesRemaining = contentLength;
                int read = 0;
                byte[] buffer = new byte[_StreamBufferSize];

                while (bytesRemaining > 0)
                {
                    read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        await fs.WriteAsync(buffer, 0, read);
                        bytesRemaining -= read;
                    }
                }
            }

            FileInfo fi = new FileInfo(file);

            using (FileStream fs = new FileStream(file, FileMode.Open))
            {
                return await Common.Md5Async(fs, _StreamBufferSize);
            }
        }

        #endregion

        #region Private-Methods
         
        private string FilePath(string key)
        {
            return _BaseDirectory + key;
        }
         
        #endregion
    }
}
