namespace Less3.Database.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Classes;
    using Less3.Requests;
    using Less3.Responses;

    /// <summary>
    /// Role assignment database methods.
    /// </summary>
    public interface IRoleAssignmentMethods
    {
        /// <summary>
        /// Create a role assignment.
        /// </summary>
        Task<RoleAssignment> CreateAsync(RoleAssignment assignment, CancellationToken token = default);

        /// <summary>
        /// Read a role assignment by tenant and identifier.
        /// </summary>
        Task<RoleAssignment> ReadAsync(string tenantId, string id, CancellationToken token = default);

        /// <summary>
        /// Enumerate role assignments by query.
        /// </summary>
        Task<EnumerationResult<RoleAssignment>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default);

        /// <summary>
        /// Update a role assignment.
        /// </summary>
        Task<RoleAssignment> UpdateAsync(RoleAssignment assignment, CancellationToken token = default);

        /// <summary>
        /// Delete a role assignment by tenant and identifier.
        /// </summary>
        Task<bool> DeleteAsync(string tenantId, string id, CancellationToken token = default);
    }
}
