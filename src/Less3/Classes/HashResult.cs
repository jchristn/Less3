namespace Less3.Classes
{
    /// <summary>
    /// Hash computation result.
    /// </summary>
    public class HashResult
    {
        /// <summary>
        /// MD5 hash in lowercase hexadecimal format.
        /// </summary>
        public string MD5 { get; set; } = null;

        /// <summary>
        /// SHA1 hash in lowercase hexadecimal format.
        /// </summary>
        public string SHA1 { get; set; } = null;

        /// <summary>
        /// SHA256 hash in lowercase hexadecimal format.
        /// </summary>
        public string SHA256 { get; set; } = null;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public HashResult()
        {

        }
    }
}
