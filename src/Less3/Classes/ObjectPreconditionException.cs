namespace Less3.Classes
{
    using System;

    /// <summary>
    /// Thrown when a conditional write's precondition is not met.
    /// </summary>
    public class ObjectPreconditionException : Exception
    {
        #region Public-Members

        /// <summary>
        /// True when the precondition failed because the object does not exist (If-Match against a missing key),
        /// which Amazon S3 reports as NoSuchKey rather than PreconditionFailed.
        /// </summary>
        public bool ObjectMissing { get; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="message">Message describing the failed precondition.</param>
        /// <param name="objectMissing">True when the object does not exist.</param>
        public ObjectPreconditionException(string message, bool objectMissing) : base(message)
        {
            ObjectMissing = objectMissing;
        }

        #endregion
    }
}
