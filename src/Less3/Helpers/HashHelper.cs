namespace Less3.Helpers
{
    using System;
    using System.Security.Cryptography;

    using Less3.Classes;

    /// <summary>
    /// Hash computation helper.
    /// </summary>
    public static class HashHelper
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute MD5, SHA1, and SHA256 hashes of the supplied data.
        /// </summary>
        /// <param name="data">Byte array containing data to hash.</param>
        /// <returns>HashResult containing MD5, SHA1, and SHA256 hashes in lowercase hexadecimal format.</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
        public static HashResult ComputeHashes(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            HashResult result = new HashResult();

            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(data);
                result.MD5 = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(data);
                result.SHA1 = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(data);
                result.SHA256 = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            return result;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
