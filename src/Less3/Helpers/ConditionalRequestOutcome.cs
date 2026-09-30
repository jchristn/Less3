namespace Less3.Helpers
{
    /// <summary>
    /// Result of evaluating conditional GET/HEAD request headers against an object.
    /// </summary>
    internal enum ConditionalRequestOutcome
    {
        /// <summary>
        /// All conditions passed; serve the object.
        /// </summary>
        Proceed,

        /// <summary>
        /// If-None-Match or If-Modified-Since indicates the client's copy is current; respond 304.
        /// </summary>
        NotModified,

        /// <summary>
        /// If-Match or If-Unmodified-Since failed; respond 412.
        /// </summary>
        PreconditionFailed
    }
}
