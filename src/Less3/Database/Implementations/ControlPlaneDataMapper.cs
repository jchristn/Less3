namespace Less3.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using Less3.Classes;

    internal static class ControlPlaneDataMapper
    {
        internal static string StringValue(DataRow row, string column)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (!row.Table.Columns.Contains(column)) return null;
            if (row[column] == null || row[column] == DBNull.Value) return null;
            return row[column].ToString();
        }

        internal static bool BoolValue(DataRow row, string column)
        {
            string value = StringValue(row, column);
            if (String.IsNullOrWhiteSpace(value)) return false;

            if (String.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (String.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) return false;

            return Convert.ToInt32(value) != 0;
        }

        internal static DateTime DateValue(DataRow row, string column)
        {
            string value = StringValue(row, column);
            if (String.IsNullOrWhiteSpace(value)) return DateTime.UtcNow;
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        internal static DateTime? NullableDateValue(DataRow row, string column)
        {
            string value = StringValue(row, column);
            if (String.IsNullOrWhiteSpace(value)) return null;
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        internal static long Count(DataTable table)
        {
            if (table == null || table.Rows.Count < 1) return 0;
            return Convert.ToInt64(table.Rows[0]["cnt"]);
        }

        internal static Tenant Tenant(DataRow row)
        {
            Tenant tenant = new Tenant();
            tenant.Id = StringValue(row, "id");
            tenant.ParentId = StringValue(row, "parent_id");
            tenant.Name = StringValue(row, "name");
            tenant.Active = BoolValue(row, "active");
            tenant.CreatedUtc = DateValue(row, "createdutc");
            tenant.LastUpdateUtc = DateValue(row, "lastupdateutc");
            return tenant;
        }

        internal static Role Role(DataRow row)
        {
            Role role = new Role();
            role.Id = StringValue(row, "id");
            role.TenantId = StringValue(row, "tenant_id");
            role.Name = StringValue(row, "name");
            role.Description = StringValue(row, "description");
            role.IsBuiltIn = BoolValue(row, "isbuiltin");
            role.InheritsToChildren = BoolValue(row, "inheritstochildren");
            role.Active = BoolValue(row, "active");
            role.CreatedUtc = DateValue(row, "createdutc");
            role.LastUpdateUtc = DateValue(row, "lastupdateutc");
            return role;
        }

        internal static Permission Permission(DataRow row)
        {
            Permission permission = new Permission();
            permission.Id = StringValue(row, "id");
            permission.TenantId = StringValue(row, "tenant_id");
            permission.RoleId = StringValue(row, "role_id");
            permission.ResourceType = StringValue(row, "resourcetype");
            permission.Operation = StringValue(row, "operation");
            permission.Permit = BoolValue(row, "permit");
            permission.Active = BoolValue(row, "active");
            permission.CreatedUtc = DateValue(row, "createdutc");
            return permission;
        }

        internal static RoleAssignment RoleAssignment(DataRow row)
        {
            RoleAssignment assignment = new RoleAssignment();
            assignment.Id = StringValue(row, "id");
            assignment.TenantId = StringValue(row, "tenant_id");
            assignment.RoleId = StringValue(row, "role_id");
            assignment.PrincipalType = StringValue(row, "principaltype");
            assignment.PrincipalId = StringValue(row, "principal_id");
            assignment.ResourceType = StringValue(row, "resourcetype");
            assignment.ResourceId = StringValue(row, "resource_id");
            assignment.Active = BoolValue(row, "active");
            assignment.CreatedUtc = DateValue(row, "createdutc");
            return assignment;
        }

        internal static AuthSession AuthSession(DataRow row)
        {
            AuthSession session = new AuthSession();
            session.Id = StringValue(row, "id");
            session.TenantId = StringValue(row, "tenant_id");
            session.PrincipalType = StringValue(row, "principaltype");
            session.PrincipalId = StringValue(row, "principal_id");
            session.TokenHash = StringValue(row, "tokenhash");
            session.Active = BoolValue(row, "active");
            session.CreatedUtc = DateValue(row, "createdutc");
            session.ExpirationUtc = DateValue(row, "expirationutc");
            session.RevokedUtc = NullableDateValue(row, "revokedutc");
            session.SourceIp = StringValue(row, "sourceip");
            return session;
        }

        internal static AuthorizationAudit AuthorizationAudit(DataRow row)
        {
            AuthorizationAudit audit = new AuthorizationAudit();
            audit.Id = StringValue(row, "id");
            audit.TenantId = StringValue(row, "tenant_id");
            audit.UserId = StringValue(row, "user_id");
            audit.CredentialId = StringValue(row, "credential_id");
            audit.ResourceType = StringValue(row, "resourcetype");
            audit.ResourceId = StringValue(row, "resource_id");
            audit.Operation = StringValue(row, "operation");
            audit.Permitted = BoolValue(row, "permitted");
            audit.Reason = StringValue(row, "reason");
            audit.CreatedUtc = DateValue(row, "createdutc");
            return audit;
        }

        internal static List<T> List<T>(DataTable table, Func<DataRow, T> mapper)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            List<T> results = new List<T>();
            if (table == null) return results;

            foreach (DataRow row in table.Rows)
            {
                results.Add(mapper(row));
            }

            return results;
        }
    }
}
