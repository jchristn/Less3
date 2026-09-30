namespace Less3.Storage
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Read-only, forward-only view over a contiguous slice of an underlying stream. Reading stops after
    /// the configured number of bytes, so a range read can be streamed without buffering it in memory.
    /// Disposing this stream disposes the underlying stream. Not thread-safe.
    /// </summary>
    public sealed class BoundedReadStream : Stream
    {
        #region Public-Members

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => _Length;

        /// <inheritdoc />
        public override long Position
        {
            get { return _Position; }
            set { throw new NotSupportedException("BoundedReadStream does not support seeking."); }
        }

        #endregion

        #region Private-Members

        private Stream _Inner = null;
        private long _Length = 0;
        private long _Position = 0;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate over the next <paramref name="length"/> bytes of <paramref name="inner"/>, starting
        /// at its current position.
        /// </summary>
        /// <param name="inner">Underlying readable stream, already positioned at the start of the slice.</param>
        /// <param name="length">Number of bytes in the slice. Minimum value is 0.</param>
        /// <exception cref="ArgumentNullException">Thrown when inner is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when length is negative.</exception>
        public BoundedReadStream(Stream inner, long length)
        {
            _Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be zero or greater.");
            _Length = length;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            int toRead = Clamp(count);
            if (toRead == 0) return 0;
            int read = _Inner.Read(buffer, offset, toRead);
            _Position += read;
            return read;
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int toRead = Clamp(count);
            if (toRead == 0) return 0;
            int read = await _Inner.ReadAsync(buffer, offset, toRead, cancellationToken).ConfigureAwait(false);
            _Position += read;
            return read;
        }

        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int toRead = Clamp(buffer.Length);
            if (toRead == 0) return 0;
            int read = await _Inner.ReadAsync(buffer.Slice(0, toRead), cancellationToken).ConfigureAwait(false);
            _Position += read;
            return read;
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("BoundedReadStream does not support seeking.");
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException("BoundedReadStream is read-only.");
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("BoundedReadStream is read-only.");
        }

        #endregion

        #region Protected-Methods

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing && _Inner != null)
            {
                _Inner.Dispose();
                _Inner = null;
            }

            _Disposed = true;
            base.Dispose(disposing);
        }

        #endregion

        #region Private-Methods

        private int Clamp(int count)
        {
            if (_Disposed) throw new ObjectDisposedException(nameof(BoundedReadStream));
            long remaining = _Length - _Position;
            if (remaining <= 0 || count <= 0) return 0;
            return (int)Math.Min(count, remaining);
        }

        #endregion
    }
}
