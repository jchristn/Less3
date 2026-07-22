namespace Less3.Database.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Classes;
    using Less3.Requests;
    using Less3.Responses;

    /// <summary>
    /// Permission database methods.
    /// </summary>
    public interface IPermissionMethods
    {
        /// <summary>
        /// Create a permission.
        /// </summary>
        Task<Permission> CreateAsync(Permission permission, CancellationToken token = default);

        /// <summary>
        /// Read a permission by tenant and identifier.
        /// </summary>
        Task<Permission> ReadAsync(string tenantId, string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate permissions by query.
        /// </summary>
        Task<EnumerationResult<Permission>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Update a permission.
        /// </summary>
        Task<Permission> UpdateAsync(Permission permission, CancellationToken token = default);

        /// <summary>
        /// Delete a permission by tenant and identifier.
        /// </summary>
        Task<bool> DeleteAsync(string tenantId, string id, CancellationToken token = default);

        /// <summary>
        /// Check if a permission exists.
        /// </summary>
        Task<bool> ExistsAsync(string tenantId, string id, CancellationToken token = default);
    }
}
