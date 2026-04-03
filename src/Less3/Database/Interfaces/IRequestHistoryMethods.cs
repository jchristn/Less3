namespace Less3.Database.Interfaces
{
    using System;
    using System.Collections.Generic;
    using Less3.Classes;

    /// <summary>
    /// Interface for request history database methods.
    /// </summary>
    public interface IRequestHistoryMethods
    {
        /// <summary>
        /// Retrieve all request history entries.
        /// </summary>
        /// <returns>List of request history entries.</returns>
        List<RequestHistory> GetAll();

        /// <summary>
        /// Retrieve a request history entry by GUID.
        /// </summary>
        /// <param name="guid">Request history GUID.</param>
        /// <returns>Request history entry or null if not found.</returns>
        RequestHistory GetByGuid(string guid);

        /// <summary>
        /// Insert a new request history entry.
        /// </summary>
        /// <param name="entry">Request history entry to insert.</param>
        void Insert(RequestHistory entry);

        /// <summary>
        /// Delete a request history entry by GUID.
        /// </summary>
        /// <param name="guid">Request history GUID.</param>
        void DeleteByGuid(string guid);

        /// <summary>
        /// Delete all request history entries older than the specified cutoff.
        /// </summary>
        /// <param name="cutoff">Cutoff datetime in UTC.</param>
        void DeleteOlderThan(DateTime cutoff);

        /// <summary>
        /// Retrieve request history entries within a date range.
        /// </summary>
        /// <param name="startUtc">Start datetime in UTC.</param>
        /// <param name="endUtc">End datetime in UTC.</param>
        /// <returns>List of matching request history entries.</returns>
        List<RequestHistory> GetInRange(DateTime startUtc, DateTime endUtc);
    }
}
