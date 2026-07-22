namespace Less3.Database.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Classes;
    using Less3.Requests;
    using Less3.Responses;

    /// <summary>
    /// Tenant database methods.
    /// </summary>
    public interface ITenantMethods
    {
        /// <summary>
        /// Create a tenant.
        /// </summary>
        /// <param name="tenant">Tenant to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created tenant.</returns>
        Task<Tenant> CreateAsync(Tenant tenant, CancellationToken token = default);

        /// <summary>
        /// Read a tenant by identifier.
        /// </summary>
        /// <param name="id">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tenant when found.</returns>
        Task<Tenant> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate tenants.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<Tenant>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Update a tenant.
        /// </summary>
        /// <param name="tenant">Tenant to update.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated tenant.</returns>
        Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken token = default);

        /// <summary>
        /// Delete a tenant by identifier.
        /// </summary>
        /// <param name="id">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if a tenant was deleted.</returns>
        Task<bool> DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Check if a tenant exists by identifier.
        /// </summary>
        /// <param name="id">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the tenant exists.</returns>
        Task<bool> ExistsAsync(string id, CancellationToken token = default);
    }
}
