namespace Less3.Publisher
{
    using System;

    /// <summary>
    /// A packaging step cannot continue: a missing tool, a missing input file, or an inconsistent manifest. The
    /// message says what to do about it.
    /// </summary>
    public sealed class PublisherException : Exception
    {
        /// <summary>
        /// Creates the exception.
        /// </summary>
        /// <param name="message">What went wrong and how to fix it.</param>
        public PublisherException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates the exception with the error that caused it.
        /// </summary>
        /// <param name="message">What went wrong and how to fix it.</param>
        /// <param name="inner">The underlying error.</param>
        public PublisherException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
