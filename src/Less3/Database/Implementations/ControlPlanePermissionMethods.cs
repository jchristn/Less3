namespace Less3.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Classes;
    using Less3.Database.Interfaces;
    using Less3.Requests;
    using Less3.Responses;

    internal class ControlPlanePermissionMethods : IPermissionMethods
    {
        private readonly DatabaseDriverBase _Database;
        private readonly SqlDialect _Dialect;

        internal ControlPlanePermissionMethods(DatabaseDriverBase database, SqlDialect dialect)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Dialect = dialect;
        }

        public async Task<Permission> CreateAsync(Permission permission, CancellationToken token = default)
        {
            if (permission == null) throw new ArgumentNullException(nameof(permission));

            string sql = "INSERT INTO permissions (id, tenant_id, role_id, resourcetype, operation, permit, active, createdutc) VALUES ("
                + ControlPlaneSql.StringLiteral(permission.Id) + ", "
                + ControlPlaneSql.StringLiteral(permission.TenantId) + ", "
                + ControlPlaneSql.StringLiteral(permission.RoleId) + ", "
                + ControlPlaneSql.StringLiteral(permission.ResourceType) + ", "
                + ControlPlaneSql.StringLiteral(permission.Operation) + ", "
                + ControlPlaneSql.BoolLiteral(permission.Permit, _Dialect) + ", "
                + ControlPlaneSql.BoolLiteral(permission.Active, _Dialect) + ", "
                + ControlPlaneSql.DateLiteral(permission.CreatedUtc) + ");";

            await _Database.ExecuteQuery(sql, true, token).ConfigureAwait(false);
            return permission;
        }

        public async Task<Permission> ReadAsync(string tenantId, string id, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            string where = "id = " + ControlPlaneSql.StringLiteral(id) + " AND " + ControlPlaneSql.TenantPredicate(tenantId, true);
            DataTable table = await _Database.ExecuteQuery("SELECT * FROM permissions WHERE " + where + ";", false, token).ConfigureAwait(false);
            if (table == null || table.Rows.Count < 1) return null;
            return ControlPlaneDataMapper.Permission(table.Rows[0]);
        }

        public async Task<EnumerationResult<Permission>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationQuery();

            Dictionary<string, string> sortFields = new Dictionary<string, string>
            {
                { "id", "id" },
                { "roleId", "role_id" },
                { "resourceType", "resourcetype" },
                { "operation", "operation" },
                { "createdUtc", "createdutc" }
            };

            List<string> predicates = new List<string>();
            predicates.Add(ControlPlaneSql.TenantPredicate(query.TenantId, true));
            if (query.Filters != null && query.Filters.ContainsKey("roleId") && !String.IsNullOrWhiteSpace(query.Filters["roleId"]))
            {
                predicates.Add("role_id = " + ControlPlaneSql.StringLiteral(query.Filters["roleId"]));
            }

            string where = String.Join(" AND ", predicates);
            DataTable countTable = await _Database.ExecuteQuery(ControlPlaneSql.CountString("permissions", where), false, token).ConfigureAwait(false);
            DataTable dataTable = await _Database.ExecuteQuery(
                ControlPlaneSql.QueryString(query, "permissions", where, sortFields, "createdutc", _Dialect),
                false,
                token).ConfigureAwait(false);

            List<Permission> items = ControlPlaneDataMapper.List(dataTable, ControlPlaneDataMapper.Permission);
            return BuildResult(query, items, ControlPlaneDataMapper.Count(countTable));
        }

        public async Task<Permission> UpdateAsync(Permission permission, CancellationToken token = default)
        {
            if (permission == null) throw new ArgumentNullException(nameof(permission));

            string sql = "UPDATE permissions SET "
                + "tenant_id = " + ControlPlaneSql.StringLiteral(permission.TenantId) + ", "
                + "role_id = " + ControlPlaneSql.StringLiteral(permission.RoleId) + ", "
                + "resourcetype = " + ControlPlaneSql.StringLiteral(permission.ResourceType) + ", "
                + "operation = " + ControlPlaneSql.StringLiteral(permission.Operation) + ", "
                + "permit = " + ControlPlaneSql.BoolLiteral(permission.Permit, _Dialect) + ", "
                + "active = " + ControlPlaneSql.BoolLiteral(permission.Active, _Dialect) + " "
                + "WHERE id = " + ControlPlaneSql.StringLiteral(permission.Id) + ";";

            await _Database.ExecuteQuery(sql, true, token).ConfigureAwait(false);
            return permission;
        }

        public async Task<bool> DeleteAsync(string tenantId, string id, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            bool exists = await ExistsAsync(tenantId, id, token).ConfigureAwait(false);
            if (!exists) return false;

            string where = "id = " + ControlPlaneSql.StringLiteral(id) + " AND " + ControlPlaneSql.TenantPredicate(tenantId, false);
            await _Database.ExecuteQuery("DELETE FROM permissions WHERE " + where + ";", true, token).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> ExistsAsync(string tenantId, string id, CancellationToken token = default)
        {
            Permission permission = await ReadAsync(tenantId, id, token).ConfigureAwait(false);
            return permission != null;
        }

        private static EnumerationResult<Permission> BuildResult(EnumerationQuery query, List<Permission> items, long total)
        {
            EnumerationResult<Permission> result = new EnumerationResult<Permission>();
            result.Items = items;
            result.Total = total;
            result.Limit = query.Limit;
            result.Offset = query.Offset;
            result.HasMore = query.Offset + items.Count < total;
            if (result.HasMore) result.NextContinuationToken = (query.Offset + items.Count).ToString();
            return result;
        }
    }
}
