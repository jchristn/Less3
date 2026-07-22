namespace Less3.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Less3.Requests;

    internal static class ControlPlaneSql
    {
        internal const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static string Escape(string value)
        {
            if (value == null) return null;
            return value.Replace("'", "''");
        }

        internal static string StringLiteral(string value)
        {
            if (value == null) return "NULL";
            return "'" + Escape(value) + "'";
        }

        internal static string DateLiteral(DateTime value)
        {
            return "'" + value.ToUniversalTime().ToString(TimestampFormat) + "'";
        }

        internal static string NullableDateLiteral(DateTime? value)
        {
            if (!value.HasValue) return "NULL";
            return DateLiteral(value.Value);
        }

        internal static string BoolLiteral(bool value, SqlDialect dialect)
        {
            if (dialect == SqlDialect.PostgreSql)
            {
                return value ? "TRUE" : "FALSE";
            }

            return value ? "1" : "0";
        }

        internal static string TenantPredicate(string tenantId, bool includeGlobal)
        {
            if (String.IsNullOrWhiteSpace(tenantId))
            {
                return includeGlobal ? "(tenant_id IS NULL)" : "1 = 1";
            }

            string predicate = "tenant_id = " + StringLiteral(tenantId);
            if (includeGlobal) predicate = "(" + predicate + " OR tenant_id IS NULL)";
            return predicate;
        }

        internal static string OrderBy(
            EnumerationQuery query,
            Dictionary<string, string> allowedSortFields,
            string defaultColumn)
        {
            string column = defaultColumn;

            if (query != null
                && !String.IsNullOrWhiteSpace(query.SortField)
                && allowedSortFields != null
                && allowedSortFields.ContainsKey(query.SortField))
            {
                column = allowedSortFields[query.SortField];
            }

            string direction = "ASC";
            if (query != null && String.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            {
                direction = "DESC";
            }

            return " ORDER BY " + column + " " + direction;
        }

        internal static string Page(EnumerationQuery query, SqlDialect dialect)
        {
            int limit = query != null ? query.Limit : 100;
            int offset = query != null ? query.Offset : 0;

            if (dialect == SqlDialect.SqlServer)
            {
                return " OFFSET " + offset + " ROWS FETCH NEXT " + limit + " ROWS ONLY";
            }

            return " LIMIT " + limit + " OFFSET " + offset;
        }

        internal static string QueryString(EnumerationQuery query, string table, string where, Dictionary<string, string> allowedSortFields, string defaultSortColumn, SqlDialect dialect)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("SELECT * FROM ");
            builder.Append(table);
            if (!String.IsNullOrWhiteSpace(where))
            {
                builder.Append(" WHERE ");
                builder.Append(where);
            }
            builder.Append(OrderBy(query, allowedSortFields, defaultSortColumn));
            builder.Append(Page(query, dialect));
            builder.Append(";");
            return builder.ToString();
        }

        internal static string CountString(string table, string where)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("SELECT COUNT(*) AS cnt FROM ");
            builder.Append(table);
            if (!String.IsNullOrWhiteSpace(where))
            {
                builder.Append(" WHERE ");
                builder.Append(where);
            }
            builder.Append(";");
            return builder.ToString();
        }
    }
}
