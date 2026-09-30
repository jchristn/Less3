namespace Less3.Helpers
{
    using System;

    /// <summary>
    /// Normalizes timestamps that Less3 stores in UTC. Values read back from the database usually have
    /// DateTimeKind.Unspecified; calling ToUniversalTime on those treats them as local time and shifts them
    /// by the server's UTC offset. Thread-safe.
    /// </summary>
    internal static class UtcTimestamp
    {
        #region Internal-Methods

        /// <summary>
        /// Return the timestamp as UTC: Unspecified values are UTC already, Local values are converted.
        /// </summary>
        /// <param name="value">Timestamp stored or computed in UTC.</param>
        /// <returns>UTC timestamp.</returns>
        internal static DateTime AsUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Local) return value.ToUniversalTime();
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        #endregion
    }
}
