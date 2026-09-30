namespace Test.Shared.S3Compatibility
{
    using System.Collections.Generic;

    /// <summary>
    /// Orders byte arrays lexicographically by unsigned byte value, the order Amazon S3 uses for UTF-8 keys.
    /// </summary>
    public sealed class ByteArrayComparer : IComparer<byte[]>
    {
        #region Public-Methods

        /// <inheritdoc />
        public int Compare(byte[]? x, byte[]? y)
        {
            if (x == null) return y == null ? 0 : -1;
            if (y == null) return 1;

            int length = x.Length < y.Length ? x.Length : y.Length;
            for (int i = 0; i < length; i++)
            {
                if (x[i] != y[i]) return x[i].CompareTo(y[i]);
            }

            return x.Length.CompareTo(y.Length);
        }

        #endregion
    }
}
