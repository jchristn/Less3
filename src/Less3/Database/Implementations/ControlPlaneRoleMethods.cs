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

    internal class ControlPlaneRoleMethods : IRoleMethods
    {
        private readonly DatabaseDriverBase _Database;
        private readonly SqlDialect _Dialect;

        internal ControlPlaneRoleMethods(DatabaseDriverBase database, SqlDialect dialect)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Dialect = dialect;
        }

        public async Task<Role> CreateAsync(Role role, CancellationToken token = default)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            if (role.LastUpdateUtc == default) role.LastUpdateUtc = role.CreatedUtc;

            string sql = "INSERT INTO roles (id, tenant_id, name, description, isbuiltin, inheritstochildren, active, createdutc, lastupdateutc) VALUES ("
                + ControlPlaneSql.StringLiteral(role.Id) + ", "
                + ControlPlaneSql.StringLiteral(role.TenantId) + ", "
                + ControlPlaneSql.StringLiteral(role.Name) + ", "
                + ControlPlaneSql.StringLiteral(role.Description) + ", "
                + ControlPlaneSql.BoolLiteral(role.IsBuiltIn, _Dialect) + ", "
                + ControlPlaneSql.BoolLiteral(role.InheritsToChildren, _Dialect) + ", "
                + ControlPlaneSql.BoolLiteral(role.Active, _Dialect) + ", "
                + ControlPlaneSql.DateLiteral(role.CreatedUtc) + ", "
                + ControlPlaneSql.DateLiteral(role.LastUpdateUtc) + ");";

            await _Database.ExecuteQuery(sql, true, token).ConfigureAwait(false);
            return role;
        }

        public async Task<Role> ReadAsync(string tenantId, string id, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            string where = "id = " + ControlPlaneSql.StringLiteral(id) + " AND " + ControlPlaneSql.TenantPredicate(tenantId, true);
            DataTable table = await _Database.ExecuteQuery("SELECT * FROM roles WHERE " + where + ";", false, token).ConfigureAwait(false);
            if (table == null || table.Rows.Count < 1) return null;
            return ControlPlaneDataMapper.Role(table.Rows[0]);
        }

        public async Task<EnumerationResult<Role>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationQuery();

            Dictionary<string, string> sortFields = new Dictionary<string, string>
            {
                { "id", "id" },
                { "name", "name" },
                { "active", "active" },
                { "createdUtc", "createdutc" },
                { "lastUpdateUtc", "lastupdateutc" }
            };

            string where = ControlPlaneSql.TenantPredicate(query.TenantId, true);
            DataTable countTable = await _Database.ExecuteQuery(ControlPlaneSql.CountString("roles", where), false, token).ConfigureAwait(false);
            DataTable dataTable = await _Database.ExecuteQuery(
                ControlPlaneSql.QueryString(query, "roles", where, sortFields, "name", _Dialect),
                false,
                token).ConfigureAwait(false);

            List<Role> items = ControlPlaneDataMapper.List(dataTable, ControlPlaneDataMapper.Role);
            return BuildResult(query, items, ControlPlaneDataMapper.Count(countTable));
        }

        public async Task<Role> UpdateAsync(Role role, CancellationToken token = default)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            Role existing = await ReadAsync(role.TenantId, role.Id, token).ConfigureAwait(false);
            if (existing == null) return null;
            if (existing.IsBuiltIn) throw new InvalidOperationException("Built-in roles cannot be updated through tenant role methods.");

            role.LastUpdateUtc = DateTime.UtcNow;
            string sql = "UPDATE roles SET "
                + "tenant_id = " + ControlPlaneSql.StringLiteral(role.TenantId) + ", "
                + "name = " + ControlPlaneSql.StringLiteral(role.Name) + ", "
                + "description = " + ControlPlaneSql.StringLiteral(role.Description) + ", "
                + "isbuiltin = " + ControlPlaneSql.BoolLiteral(role.IsBuiltIn, _Dialect) + ", "
                + "inheritstochildren = " + ControlPlaneSql.BoolLiteral(role.InheritsToChildren, _Dialect) + ", "
                + "active = " + ControlPlaneSql.BoolLiteral(role.Active, _Dialect) + ", "
                + "lastupdateutc = " + ControlPlaneSql.DateLiteral(role.LastUpdateUtc) + " "
                + "WHERE id = " + ControlPlaneSql.StringLiteral(role.Id) + ";";

            await _Database.ExecuteQuery(sql, true, token).ConfigureAwait(false);
            return role;
        }

        public async Task<bool> DeleteAsync(string tenantId, string id, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            Role existing = await ReadAsync(tenantId, id, token).ConfigureAwait(false);
            if (existing == null) return false;
            if (existing.IsBuiltIn) throw new InvalidOperationException("Built-in roles cannot be deleted through tenant role methods.");

            string where = "id = " + ControlPlaneSql.StringLiteral(id) + " AND " + ControlPlaneSql.TenantPredicate(tenantId, false);
            await _Database.ExecuteQuery("DELETE FROM roles WHERE " + where + ";", true, token).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> ExistsAsync(string tenantId, string id, CancellationToken token = default)
        {
            Role role = await ReadAsync(tenantId, id, token).ConfigureAwait(false);
            return role != null;
        }

        private static EnumerationResult<Role> BuildResult(EnumerationQuery query, List<Role> items, long total)
        {
            EnumerationResult<Role> result = new EnumerationResult<Role>();
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
