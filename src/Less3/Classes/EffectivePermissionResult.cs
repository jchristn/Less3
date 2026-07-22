namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Effective RBAC decision for a principal and resource operation.
    /// </summary>
    public class EffectivePermissionResult
    {
        /// <summary>
        /// Tenant identifier.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// Principal type, such as User or Credential.
        /// </summary>
        public string PrincipalType { get; set; } = null;

        /// <summary>
        /// Principal identifier.
        /// </summary>
        public string PrincipalId { get; set; } = null;

        /// <summary>
        /// Resource type.
        /// </summary>
        public string ResourceType { get; set; } = null;

        /// <summary>
        /// Resource identifier.
        /// </summary>
        public string ResourceId { get; set; } = null;

        /// <summary>
        /// Operation being evaluated.
        /// </summary>
        public string Operation { get; set; } = null;

        /// <summary>
        /// Whether RBAC, admin bypass, or tenant-admin bypass produced a decision.
        /// </summary>
        public bool HasDecision { get; set; } = false;

        /// <summary>
        /// Whether the effective decision permits the operation.
        /// </summary>
        public bool Permitted { get; set; } = false;

        /// <summary>
        /// Whether the decision came from global administrator bypass.
        /// </summary>
        public bool IsAdminBypass { get; set; } = false;

        /// <summary>
        /// Whether the decision came from tenant administrator bypass.
        /// </summary>
        public bool IsTenantAdminBypass { get; set; } = false;

        /// <summary>
        /// Human-readable decision reason.
        /// </summary>
        public string Reason { get; set; } = null;

        /// <summary>
        /// Matching active role assignments considered for the request.
        /// </summary>
        public List<RoleAssignment> MatchingAssignments { get; set; } = new List<RoleAssignment>();

        /// <summary>
        /// Matching active permission rules considered for the request.
        /// </summary>
        public List<Permission> MatchingPermissions { get; set; } = new List<Permission>();

        /// <summary>
        /// Generation timestamp.
        /// </summary>
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    }
}
