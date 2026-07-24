namespace Less3.Database.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Classes;
    using Less3.Requests;
    using Less3.Responses;

    /// <summary>
    /// Auth session database methods.
    /// </summary>
    public interface IAuthSessionMethods
    {
        /// <summary>
        /// Create an auth session.
        /// </summary>
        Task<AuthSession> CreateAsync(AuthSession session, CancellationToken token = default);

        /// <summary>
        /// Read an auth session by tenant and identifier.
        /// </summary>
        Task<AuthSession> ReadAsync(string tenantId, string id, CancellationToken token = default);

        /// <summary>
        /// Read an auth session by token hash.
        /// </summary>
        Task<AuthSession> ReadByTokenHashAsync(string tokenHash, CancellationToken token = default);

        /// <summary>
        /// Enumerate auth sessions by query.
        /// </summary>
        Task<EnumerationResult<AuthSession>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Update an auth session.
        /// </summary>
        Task<AuthSession> UpdateAsync(AuthSession session, CancellationToken token = default);

        /// <summary>
        /// Revoke an auth session.
        /// </summary>
        Task<bool> RevokeAsync(string tenantId, string id, CancellationToken token = default);
    }
}
