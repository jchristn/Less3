namespace Less3.Database.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Classes;
    using Less3.Requests;
    using Less3.Responses;

    /// <summary>
    /// Role database methods.
    /// </summary>
    public interface IRoleMethods
    {
        /// <summary>
        /// Create a role.
        /// </summary>
        Task<Role> CreateAsync(Role role, CancellationToken token = default);

        /// <summary>
        /// Read a role by tenant and identifier.
        /// </summary>
        Task<Role> ReadAsync(string tenantId, string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate roles by query.
        /// </summary>
        Task<EnumerationResult<Role>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Update a role.
        /// </summary>
        Task<Role> UpdateAsync(Role role, CancellationToken token = default);

        /// <summary>
        /// Delete a role by tenant and identifier.
        /// </summary>
        Task<bool> DeleteAsync(string tenantId, string id, CancellationToken token = default);

        /// <summary>
        /// Check if a role exists.
        /// </summary>
        Task<bool> ExistsAsync(string tenantId, string id, CancellationToken token = default);
    }
}
