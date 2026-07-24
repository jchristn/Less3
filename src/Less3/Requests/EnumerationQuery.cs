namespace Less3.Requests
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Standard request shape for server-side enumeration, filtering, sorting, and pagination.
    /// </summary>
    public class EnumerationQuery
    {
        #region Public-Members

        /// <summary>
        /// Tenant identifier. Normal tenant-scoped routes derive this from authentication.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Maximum number of records to return. Default is 100. Minimum is 1. Maximum is 1000.
        /// </summary>
        public int Limit
        {
            get
            {
                return _Limit;
            }
            set
            {
                _Limit = Math.Clamp(value, 1, 1000);
            }
        }

        /// <summary>
        /// Zero-based offset for offset pagination. Default is 0. Minimum is 0.
        /// </summary>
        public int Offset
        {
            get
            {
                return _Offset;
            }
            set
            {
                _Offset = Math.Max(0, value);
            }
        }

        /// <summary>
        /// Optional continuation cursor for cursor pagination.
        /// </summary>
        public string ContinuationToken { get; set; } = null;

        /// <summary>
        /// Optional field name to sort by.
        /// </summary>
        public string SortField { get; set; } = null;

        /// <summary>
        /// Sort direction. Allowed values are asc and desc. Default is asc.
        /// </summary>
        public string SortDirection
        {
            get
            {
                return _SortDirection;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    _SortDirection = "asc";
                    return;
                }

                if (String.Equals(value, "desc", StringComparison.OrdinalIgnoreCase))
                {
                    _SortDirection = "desc";
                    return;
                }

                _SortDirection = "asc";
            }
        }

        /// <summary>
        /// Optional UTC start timestamp for time-bounded queries.
        /// </summary>
        public DateTime? StartUtc { get; set; } = null;

        /// <summary>
        /// Optional UTC end timestamp for time-bounded queries.
        /// </summary>
        public DateTime? EndUtc { get; set; } = null;

        /// <summary>
        /// Field filters keyed by API field name.
        /// </summary>
        public Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();

        #endregion

        #region Private-Members

        private int _Limit = 100;
        private int _Offset = 0;
        private string _SortDirection = "asc";

        #endregion
    }
}
