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

    internal class ControlPlaneAuthorizationAuditMethods : IAuthorizationAuditMethods
    {
        private readonly DatabaseDriverBase _Database;
        private readonly SqlDialect _Dialect;

        internal ControlPlaneAuthorizationAuditMethods(DatabaseDriverBase database, SqlDialect dialect)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Dialect = dialect;
        }

        public async Task<AuthorizationAudit> CreateAsync(AuthorizationAudit audit, CancellationToken token = default)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));

            string sql = "INSERT INTO authorizationaudit (id, tenant_id, user_id, credential_id, resourcetype, resource_id, operation, permitted, reason, createdutc) VALUES ("
                + ControlPlaneSql.StringLiteral(audit.Id) + ", "
                + ControlPlaneSql.StringLiteral(audit.TenantId) + ", "
                + ControlPlaneSql.StringLiteral(audit.UserId) + ", "
                + ControlPlaneSql.StringLiteral(audit.CredentialId) + ", "
                + ControlPlaneSql.StringLiteral(audit.ResourceType) + ", "
                + ControlPlaneSql.StringLiteral(audit.ResourceId) + ", "
                + ControlPlaneSql.StringLiteral(audit.Operation) + ", "
                + ControlPlaneSql.BoolLiteral(audit.Permitted, _Dialect) + ", "
                + ControlPlaneSql.StringLiteral(audit.Reason) + ", "
                + ControlPlaneSql.DateLiteral(audit.CreatedUtc) + ");";

            await _Database.ExecuteQuery(sql, true, token).ConfigureAwait(false);
            return audit;
        }

        public async Task<AuthorizationAudit> ReadAsync(string tenantId, string id, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            string where = "id = " + ControlPlaneSql.StringLiteral(id) + " AND " + ControlPlaneSql.TenantPredicate(tenantId, false);
            DataTable table = await _Database.ExecuteQuery("SELECT * FROM authorizationaudit WHERE " + where + ";", false, token).ConfigureAwait(false);
            if (table == null || table.Rows.Count < 1) return null;
            return ControlPlaneDataMapper.AuthorizationAudit(table.Rows[0]);
        }

        public async Task<EnumerationResult<AuthorizationAudit>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationQuery();

            Dictionary<string, string> sortFields = new Dictionary<string, string>
            {
                { "id", "id" },
                { "userId", "user_id" },
                { "credentialId", "credential_id" },
                { "resourceType", "resourcetype" },
                { "operation", "operation" },
                { "createdUtc", "createdutc" }
            };

            List<string> predicates = new List<string>();
            predicates.Add(ControlPlaneSql.TenantPredicate(query.TenantId, false));
            if (query.Filters != null)
            {
                AddFilter(query, predicates, "userId", "user_id");
                AddFilter(query, predicates, "credentialId", "credential_id");
                AddFilter(query, predicates, "resourceType", "resourcetype");
                AddFilter(query, predicates, "operation", "operation");
            }

            if (query.StartUtc.HasValue) predicates.Add("createdutc >= " + ControlPlaneSql.DateLiteral(query.StartUtc.Value));
            if (query.EndUtc.HasValue) predicates.Add("createdutc <= " + ControlPlaneSql.DateLiteral(query.EndUtc.Value));

            string where = String.Join(" AND ", predicates);
            DataTable countTable = await _Database.ExecuteQuery(ControlPlaneSql.CountString("authorizationaudit", where), false, token).ConfigureAwait(false);
            DataTable dataTable = await _Database.ExecuteQuery(
                ControlPlaneSql.QueryString(query, "authorizationaudit", where, sortFields, "createdutc", _Dialect),
                false,
                token).ConfigureAwait(false);

            List<AuthorizationAudit> items = ControlPlaneDataMapper.List(dataTable, ControlPlaneDataMapper.AuthorizationAudit);
            return BuildResult(query, items, ControlPlaneDataMapper.Count(countTable));
        }

        public async Task<bool> DeleteAsync(string tenantId, string id, CancellationToken token = default)
        {
            AuthorizationAudit existing = await ReadAsync(tenantId, id, token).ConfigureAwait(false);
            if (existing == null) return false;

            string where = "id = " + ControlPlaneSql.StringLiteral(id) + " AND " + ControlPlaneSql.TenantPredicate(tenantId, false);
            await _Database.ExecuteQuery("DELETE FROM authorizationaudit WHERE " + where + ";", true, token).ConfigureAwait(false);
            return true;
        }

        public async Task<int> DeleteOlderThanAsync(DateTime olderThanUtc, CancellationToken token = default)
        {
            string where = "createdutc < " + ControlPlaneSql.DateLiteral(olderThanUtc);
            DataTable countTable = await _Database.ExecuteQuery(ControlPlaneSql.CountString("authorizationaudit", where), false, token).ConfigureAwait(false);
            long count = ControlPlaneDataMapper.Count(countTable);
            await _Database.ExecuteQuery("DELETE FROM authorizationaudit WHERE " + where + ";", true, token).ConfigureAwait(false);
            return Convert.ToInt32(count);
        }

        private static void AddFilter(EnumerationQuery query, List<string> predicates, string filterName, string columnName)
        {
            if (!query.Filters.ContainsKey(filterName)) return;
            if (String.IsNullOrWhiteSpace(query.Filters[filterName])) return;
            predicates.Add(columnName + " = " + ControlPlaneSql.StringLiteral(query.Filters[filterName]));
        }

        private static EnumerationResult<AuthorizationAudit> BuildResult(EnumerationQuery query, List<AuthorizationAudit> items, long total)
        {
            EnumerationResult<AuthorizationAudit> result = new EnumerationResult<AuthorizationAudit>();
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
