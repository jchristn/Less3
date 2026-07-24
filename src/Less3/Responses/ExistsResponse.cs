namespace Less3.Responses
{
    /// <summary>
    /// Response indicating whether a resource exists.
    /// </summary>
    public class ExistsResponse
    {
        #region Public-Members

        /// <summary>
        /// Whether the requested resource exists.
        /// </summary>
        public bool Exists { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the response.
        /// </summary>
        public ExistsResponse()
        {

        }

        /// <summary>
        /// Instantiate the response.
        /// </summary>
        /// <param name="exists">Whether the requested resource exists.</param>
        public ExistsResponse(bool exists)
        {
            Exists = exists;
        }

        #endregion
    }
}
