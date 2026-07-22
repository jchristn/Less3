namespace Less3.Classes
{
    /// <summary>
    /// Ranked item in a request reporting summary.
    /// </summary>
    public class RequestReportingTopItem
    {
        /// <summary>
        /// Display name or identifier for the ranked item.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Optional object identifier for the ranked item.
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Count represented by the ranked item.
        /// </summary>
        public long Count { get; set; } = 0;

        /// <summary>
        /// Byte total represented by the ranked item.
        /// </summary>
        public long Bytes { get; set; } = 0;
    }
}
