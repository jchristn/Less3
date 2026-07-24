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

    internal class ControlPlaneRoleAssignmentMethods : IRoleAssignmentMethods
    {
        private readonly DatabaseDriverBase _Database;
        private readonly SqlDialect _Dialect;

        internal ControlPlaneRoleAssignmentMethods(DatabaseDriverBase database, SqlDialect dialect)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Dialect = dialect;
        }

        public async Task<RoleAssignment> CreateAsync(RoleAssignment assignment, CancellationToken token = default)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));

            string sql = "INSERT INTO roleassignments (id, tenant_id, role_id, principaltype, principal_id, resourcetype, resource_id, active, createdutc) VALUES ("
                + ControlPlaneSql.StringLiteral(assignment.Id) + ", "
                + ControlPlaneSql.StringLiteral(assignment.TenantId) + ", "
                + ControlPlaneSql.StringLiteral(assignment.RoleId) + ", "
                + ControlPlaneSql.StringLiteral(assignment.PrincipalType) + ", "
                + ControlPlaneSql.StringLiteral(assignment.PrincipalId) + ", "
                + ControlPlaneSql.StringLiteral(assignment.ResourceType) + ", "
                + ControlPlaneSql.StringLiteral(assignment.ResourceId) + ", "
                + ControlPlaneSql.BoolLiteral(assignment.Active, _Dialect) + ", "
                + ControlPlaneSql.DateLiteral(assignment.CreatedUtc) + ");";

            await _Database.ExecuteQuery(sql, true, token).ConfigureAwait(false);
            return assignment;
        }

        public async Task<RoleAssignment> ReadAsync(string tenantId, string id, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            string where = "id = " + ControlPlaneSql.StringLiteral(id) + " AND " + ControlPlaneSql.TenantPredicate(tenantId, false);
            DataTable table = await _Database.ExecuteQuery("SELECT * FROM roleassignments WHERE " + where + ";", false, token).ConfigureAwait(false);
            if (table == null || table.Rows.Count < 1) return null;
            return ControlPlaneDataMapper.RoleAssignment(table.Rows[0]);
        }

        public async Task<EnumerationResult<RoleAssignment>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationQuery();

            Dictionary<string, string> sortFields = new Dictionary<string, string>
            {
                { "id", "id" },
                { "roleId", "role_id" },
                { "principalType", "principaltype" },
                { "principalId", "principal_id" },
                { "createdUtc", "createdutc" }
            };

            List<string> predicates = new List<string>();
            predicates.Add(ControlPlaneSql.TenantPredicate(query.TenantId, false));

            if (query.Filters != null)
            {
                AddFilter(query, predicates, "roleId", "role_id");
                AddFilter(query, predicates, "principalType", "principaltype");
                AddFilter(query, predicates, "principalId", "principal_id");
                AddFilter(query, predicates, "resourceType", "resourcetype");
                AddFilter(query, predicates, "resourceId", "resource_id");
            }

            string where = String.Join(" AND ", predicates);
            DataTable countTable = await _Database.ExecuteQuery(ControlPlaneSql.CountString("roleassignments", where), false, token).ConfigureAwait(false);
            DataTable dataTable = await _Database.ExecuteQuery(
                ControlPlaneSql.QueryString(query, "roleassignments", where, sortFields, "createdutc", _Dialect),
                false,
                token).ConfigureAwait(false);

            List<RoleAssignment> items = ControlPlaneDataMapper.List(dataTable, ControlPlaneDataMapper.RoleAssignment);
            return BuildResult(query, items, ControlPlaneDataMapper.Count(countTable));
        }

        public async Task<RoleAssignment> UpdateAsync(RoleAssignment assignment, CancellationToken token = default)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));

            string sql = "UPDATE roleassignments SET "
                + "tenant_id = " + ControlPlaneSql.StringLiteral(assignment.TenantId) + ", "
                + "role_id = " + ControlPlaneSql.StringLiteral(assignment.RoleId) + ", "
                + "principaltype = " + ControlPlaneSql.StringLiteral(assignment.PrincipalType) + ", "
                + "principal_id = " + ControlPlaneSql.StringLiteral(assignment.PrincipalId) + ", "
                + "resourcetype = " + ControlPlaneSql.StringLiteral(assignment.ResourceType) + ", "
                + "resource_id = " + ControlPlaneSql.StringLiteral(assignment.ResourceId) + ", "
                + "active = " + ControlPlaneSql.BoolLiteral(assignment.Active, _Dialect) + " "
                + "WHERE id = " + ControlPlaneSql.StringLiteral(assignment.Id) + " AND tenant_id = " + ControlPlaneSql.StringLiteral(assignment.TenantId) + ";";

            await _Database.ExecuteQuery(sql, true, token).ConfigureAwait(false);
            return assignment;
        }

        public async Task<bool> DeleteAsync(string tenantId, string id, CancellationToken token = default)
        {
            RoleAssignment existing = await ReadAsync(tenantId, id, token).ConfigureAwait(false);
            if (existing == null) return false;

            string where = "id = " + ControlPlaneSql.StringLiteral(id) + " AND " + ControlPlaneSql.TenantPredicate(tenantId, false);
            await _Database.ExecuteQuery("DELETE FROM roleassignments WHERE " + where + ";", true, token).ConfigureAwait(false);
            return true;
        }

        private static void AddFilter(EnumerationQuery query, List<string> predicates, string filterName, string columnName)
        {
            if (!query.Filters.ContainsKey(filterName)) return;
            if (String.IsNullOrWhiteSpace(query.Filters[filterName])) return;
            predicates.Add(columnName + " = " + ControlPlaneSql.StringLiteral(query.Filters[filterName]));
        }

        private static EnumerationResult<RoleAssignment> BuildResult(EnumerationQuery query, List<RoleAssignment> items, long total)
        {
            EnumerationResult<RoleAssignment> result = new EnumerationResult<RoleAssignment>();
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
