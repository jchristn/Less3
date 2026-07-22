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

    internal class ControlPlaneAuthSessionMethods : IAuthSessionMethods
    {
        private readonly DatabaseDriverBase _Database;
        private readonly SqlDialect _Dialect;

        internal ControlPlaneAuthSessionMethods(DatabaseDriverBase database, SqlDialect dialect)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Dialect = dialect;
        }

        public async Task<AuthSession> CreateAsync(AuthSession session, CancellationToken token = default)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            string sql = "INSERT INTO authsessions (id, tenant_id, principaltype, principal_id, tokenhash, active, createdutc, expirationutc, revokedutc, sourceip) VALUES ("
                + ControlPlaneSql.StringLiteral(session.Id) + ", "
                + ControlPlaneSql.StringLiteral(session.TenantId) + ", "
                + ControlPlaneSql.StringLiteral(session.PrincipalType) + ", "
                + ControlPlaneSql.StringLiteral(session.PrincipalId) + ", "
                + ControlPlaneSql.StringLiteral(session.TokenHash) + ", "
                + ControlPlaneSql.BoolLiteral(session.Active, _Dialect) + ", "
                + ControlPlaneSql.DateLiteral(session.CreatedUtc) + ", "
                + ControlPlaneSql.DateLiteral(session.ExpirationUtc) + ", "
                + ControlPlaneSql.NullableDateLiteral(session.RevokedUtc) + ", "
                + ControlPlaneSql.StringLiteral(session.SourceIp) + ");";

            await _Database.ExecuteQuery(sql, true, token).ConfigureAwait(false);
            return session;
        }

        public async Task<AuthSession> ReadAsync(string tenantId, string id, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            string where = "id = " + ControlPlaneSql.StringLiteral(id) + " AND " + ControlPlaneSql.TenantPredicate(tenantId, false);
            DataTable table = await _Database.ExecuteQuery("SELECT * FROM authsessions WHERE " + where + ";", false, token).ConfigureAwait(false);
            if (table == null || table.Rows.Count < 1) return null;
            return ControlPlaneDataMapper.AuthSession(table.Rows[0]);
        }

        public async Task<AuthSession> ReadByTokenHashAsync(string tokenHash, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(tokenHash)) throw new ArgumentNullException(nameof(tokenHash));

            string sql = "SELECT * FROM authsessions WHERE tokenhash = " + ControlPlaneSql.StringLiteral(tokenHash) + ";";
            DataTable table = await _Database.ExecuteQuery(sql, false, token).ConfigureAwait(false);
            if (table == null || table.Rows.Count < 1) return null;
            return ControlPlaneDataMapper.AuthSession(table.Rows[0]);
        }

        public async Task<EnumerationResult<AuthSession>> EnumerateAsync(EnumerationQuery query, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationQuery();

            Dictionary<string, string> sortFields = new Dictionary<string, string>
            {
                { "id", "id" },
                { "principalType", "principaltype" },
                { "principalId", "principal_id" },
                { "createdUtc", "createdutc" },
                { "expirationUtc", "expirationutc" }
            };

            List<string> predicates = new List<string>();
            predicates.Add(ControlPlaneSql.TenantPredicate(query.TenantId, false));
            if (query.Filters != null)
            {
                AddFilter(query, predicates, "principalType", "principaltype");
                AddFilter(query, predicates, "principalId", "principal_id");
            }

            string where = String.Join(" AND ", predicates);
            DataTable countTable = await _Database.ExecuteQuery(ControlPlaneSql.CountString("authsessions", where), false, token).ConfigureAwait(false);
            DataTable dataTable = await _Database.ExecuteQuery(
                ControlPlaneSql.QueryString(query, "authsessions", where, sortFields, "createdutc", _Dialect),
                false,
                token).ConfigureAwait(false);

            List<AuthSession> items = ControlPlaneDataMapper.List(dataTable, ControlPlaneDataMapper.AuthSession);
            return BuildResult(query, items, ControlPlaneDataMapper.Count(countTable));
        }

        public async Task<AuthSession> UpdateAsync(AuthSession session, CancellationToken token = default)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            string sql = "UPDATE authsessions SET "
                + "tenant_id = " + ControlPlaneSql.StringLiteral(session.TenantId) + ", "
                + "principaltype = " + ControlPlaneSql.StringLiteral(session.PrincipalType) + ", "
                + "principal_id = " + ControlPlaneSql.StringLiteral(session.PrincipalId) + ", "
                + "tokenhash = " + ControlPlaneSql.StringLiteral(session.TokenHash) + ", "
                + "active = " + ControlPlaneSql.BoolLiteral(session.Active, _Dialect) + ", "
                + "expirationutc = " + ControlPlaneSql.DateLiteral(session.ExpirationUtc) + ", "
                + "revokedutc = " + ControlPlaneSql.NullableDateLiteral(session.RevokedUtc) + ", "
                + "sourceip = " + ControlPlaneSql.StringLiteral(session.SourceIp) + " "
                + "WHERE id = " + ControlPlaneSql.StringLiteral(session.Id) + " AND tenant_id = " + ControlPlaneSql.StringLiteral(session.TenantId) + ";";

            await _Database.ExecuteQuery(sql, true, token).ConfigureAwait(false);
            return session;
        }

        public async Task<bool> RevokeAsync(string tenantId, string id, CancellationToken token = default)
        {
            AuthSession session = await ReadAsync(tenantId, id, token).ConfigureAwait(false);
            if (session == null) return false;

            session.Active = false;
            session.RevokedUtc = DateTime.UtcNow;
            await UpdateAsync(session, token).ConfigureAwait(false);
            return true;
        }

        private static void AddFilter(EnumerationQuery query, List<string> predicates, string filterName, string columnName)
        {
            if (!query.Filters.ContainsKey(filterName)) return;
            if (String.IsNullOrWhiteSpace(query.Filters[filterName])) return;
            predicates.Add(columnName + " = " + ControlPlaneSql.StringLiteral(query.Filters[filterName]));
        }

        private static EnumerationResult<AuthSession> BuildResult(EnumerationQuery query, List<AuthSession> items, long total)
        {
            EnumerationResult<AuthSession> result = new EnumerationResult<AuthSession>();
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
