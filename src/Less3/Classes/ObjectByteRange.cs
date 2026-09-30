namespace Less3.Classes
{
    using System;

    /// <summary>
    /// A contiguous run of bytes within an object.
    /// </summary>
    public class ObjectByteRange
    {
        #region Public-Members

        /// <summary>
        /// Offset of the first byte. Minimum value is 0.
        /// </summary>
        public long Start { get; }

        /// <summary>
        /// Number of bytes. Minimum value is 0.
        /// </summary>
        public long Length { get; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="start">Offset of the first byte. Minimum value is 0.</param>
        /// <param name="length">Number of bytes. Minimum value is 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when start or length is negative.</exception>
        public ObjectByteRange(long start, long length)
        {
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(start), "Start must be zero or greater.");
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be zero or greater.");
            Start = start;
            Length = length;
        }

        #endregion
    }
}
