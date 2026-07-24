namespace Less3.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Classes;
    using Less3.Requests;
    using Less3.Responses;

    /// <summary>
    /// Authorization audit database methods.
    /// </summary>
    public interface IAuthorizationAuditMethods
    {
        /// <summary>
        /// Create an authorization audit record.
        /// </summary>
        Task<AuthorizationAudit> CreateAsync(AuthorizationAudit audit, CancellationToken token = default);

        /// <summary>
        /// Read an authorization audit record by tenant and identifier.
        /// </summary>
        Task<AuthorizationAudit> ReadAsync(string tenantId, string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate authorization audit records by query.
        /// </summary>
        Task<EnumerationResult<AuthorizationAudit>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Delete an authorization audit record by tenant and identifier.
        /// </summary>
        Task<bool> DeleteAsync(string tenantId, string id, CancellationToken token = default);

        /// <summary>
        /// Delete audit records older than a UTC timestamp.
        /// </summary>
        Task<int> DeleteOlderThanAsync(DateTime olderThanUtc, CancellationToken token = default);
    }
}
