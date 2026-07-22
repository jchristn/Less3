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

    internal class ControlPlaneTenantMethods : ITenantMethods
    {
        private readonly DatabaseDriverBase _Database;
        private readonly SqlDialect _Dialect;

        internal ControlPlaneTenantMethods(DatabaseDriverBase database, SqlDialect dialect)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Dialect = dialect;
        }

        public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            if (tenant.LastUpdateUtc == default) tenant.LastUpdateUtc = tenant.CreatedUtc;

            string sql = "INSERT INTO tenants (id, parent_id, name, active, createdutc, lastupdateutc) VALUES ("
                + ControlPlaneSql.StringLiteral(tenant.Id) + ", "
                + ControlPlaneSql.StringLiteral(tenant.ParentId) + ", "
                + ControlPlaneSql.StringLiteral(tenant.Name) + ", "
                + ControlPlaneSql.BoolLiteral(tenant.Active, _Dialect) + ", "
                + ControlPlaneSql.DateLiteral(tenant.CreatedUtc) + ", "
                + ControlPlaneSql.DateLiteral(tenant.LastUpdateUtc) + ");";

            await _Database.ExecuteQuery(sql, true, token).ConfigureAwait(false);
            return tenant;
        }

        public async Task<Tenant> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            string sql = "SELECT * FROM tenants WHERE id = " + ControlPlaneSql.StringLiteral(id) + ";";
            DataTable table = await _Database.ExecuteQuery(sql, false, token).ConfigureAwait(false);
            if (table == null || table.Rows.Count < 1) return null;
            return ControlPlaneDataMapper.Tenant(table.Rows[0]);
        }

        public async Task<EnumerationResult<Tenant>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
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

            string where = BuildTenantWhere(query, _Dialect);
            DataTable countTable = await _Database.ExecuteQuery(ControlPlaneSql.CountString("tenants", where), false, token).ConfigureAwait(false);
            DataTable dataTable = await _Database.ExecuteQuery(
                ControlPlaneSql.QueryString(query, "tenants", where, sortFields, "createdutc", _Dialect),
                false,
                token).ConfigureAwait(false);

            List<Tenant> items = ControlPlaneDataMapper.List(dataTable, ControlPlaneDataMapper.Tenant);
            long total = ControlPlaneDataMapper.Count(countTable);
            return BuildResult(query, items, total);
        }

        public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken token = default)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            tenant.LastUpdateUtc = DateTime.UtcNow;

            string sql = "UPDATE tenants SET "
                + "parent_id = " + ControlPlaneSql.StringLiteral(tenant.ParentId) + ", "
                + "name = " + ControlPlaneSql.StringLiteral(tenant.Name) + ", "
                + "active = " + ControlPlaneSql.BoolLiteral(tenant.Active, _Dialect) + ", "
                + "lastupdateutc = " + ControlPlaneSql.DateLiteral(tenant.LastUpdateUtc) + " "
                + "WHERE id = " + ControlPlaneSql.StringLiteral(tenant.Id) + ";";

            await _Database.ExecuteQuery(sql, true, token).ConfigureAwait(false);
            return tenant;
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            bool exists = await ExistsAsync(id, token).ConfigureAwait(false);
            if (!exists) return false;

            await _Database.ExecuteQuery("DELETE FROM tenants WHERE id = " + ControlPlaneSql.StringLiteral(id) + ";", true, token).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> ExistsAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            DataTable table = await _Database.ExecuteQuery(
                "SELECT COUNT(*) AS cnt FROM tenants WHERE id = " + ControlPlaneSql.StringLiteral(id) + ";",
                false,
                token).ConfigureAwait(false);

            return ControlPlaneDataMapper.Count(table) > 0;
        }

        private static string BuildTenantWhere(EnumerationQuery query, SqlDialect dialect)
        {
            List<string> predicates = new List<string>();

            if (query.Filters != null)
            {
                if (query.Filters.ContainsKey("name") && !String.IsNullOrWhiteSpace(query.Filters["name"]))
                {
                    predicates.Add("name = " + ControlPlaneSql.StringLiteral(query.Filters["name"]));
                }

                if (query.Filters.ContainsKey("active") && Boolean.TryParse(query.Filters["active"], out bool active))
                {
                    predicates.Add("active = " + ControlPlaneSql.BoolLiteral(active, dialect));
                }
            }

            return String.Join(" AND ", predicates);
        }

        private static EnumerationResult<Tenant> BuildResult(EnumerationQuery query, List<Tenant> items, long total)
        {
            EnumerationResult<Tenant> result = new EnumerationResult<Tenant>();
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
