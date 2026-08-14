namespace Less3.Locking
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Wraps an object read stream and holds a shared read lock for the stream's lifetime. When the
    /// stream is disposed — after the response has been fully sent — the read lock is released. This
    /// is what makes a write or delete on the same key wait for in-flight reads to flush.
    /// </summary>
    public sealed class LockReleasingStream : Stream
    {
        #region Private-Members

        private readonly Stream _Inner;
        private readonly ILockManager _LockManager;
        private readonly LockHandle _Handle;
        private bool _Released = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="inner">Underlying object stream.</param>
        /// <param name="lockManager">Lock manager that issued the handle.</param>
        /// <param name="handle">Held shared read lock, released on dispose.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        public LockReleasingStream(Stream inner, ILockManager lockManager, LockHandle handle)
        {
            _Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _LockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
            _Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        #endregion

        #region Public-Members

        /// <inheritdoc />
        public override bool CanRead => _Inner.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => _Inner.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => _Inner.Length;

        /// <inheritdoc />
        public override long Position
        {
            get => _Inner.Position;
            set => _Inner.Position = value;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override void Flush() => _Inner.Flush();

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => _Inner.Read(buffer, offset, count);

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _Inner.ReadAsync(buffer, offset, count, cancellationToken);

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => _Inner.Seek(offset, origin);

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        #endregion

        #region Private-Methods

        private void ReleaseLock()
        {
            if (_Released) return;
            _Released = true;
            try { _LockManager.ReleaseAsync(_Handle, CancellationToken.None).GetAwaiter().GetResult(); }
            catch (Exception) { }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseLock();
                _Inner.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
