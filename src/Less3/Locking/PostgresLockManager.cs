namespace Less3.Locking
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Database;
    using Less3.Database.PostgreSql;
    using Less3.Settings;
    using SyslogLogging;

    /// <summary>
    /// PostgreSQL-backed fair read/write/delete lock manager. The database is the single authority.
    /// Each request is appended to a per-key FIFO queue (less3_lock_queue). The grant decision runs
    /// inside a per-key advisory-locked transaction (less3_lock_try_grant), so exactly one node
    /// decides at a time and two nodes can never both hold an exclusive lock. Reads are shared and
    /// unbounded; a write or delete is granted only when it is the oldest waiter and every earlier
    /// holder has released. Lease expiry uses the database clock, and each grant bumps a per-key
    /// monotonic fencing token that a mutation re-checks before it commits.
    /// Thread-safe.
    /// </summary>
    public class PostgresLockManager : LockManagerBase
    {
        #region Public-Members

        /// <inheritdoc />
        public override string Provider
        {
            get { return "Postgres"; }
        }

        #endregion

        #region Private-Members

        private readonly DatabaseDriverBase _Database;
        private readonly string _NodeId;
        private readonly int _WaiterGraceMs = 60000;
        private const string _NowUtc = "(now() at time zone 'utc')";
        private const string _Table = "less3_lock_queue";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate. Ensures the queue tables and the grant function exist (idempotent, guarded
        /// by a Postgres advisory lock so concurrent node startups create them exactly once).
        /// </summary>
        /// <param name="database">Database driver (must be a PostgreSQL driver in cluster mode).</param>
        /// <param name="nodeId">Identifier of this node, stamped on held locks for observability.</param>
        /// <param name="lockConfig">Lock manager tuning.</param>
        /// <param name="logging">Logging module.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        public PostgresLockManager(DatabaseDriverBase database, string nodeId, LockSettings lockConfig, LoggingModule logging)
            : base(lockConfig, logging)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _NodeId = String.IsNullOrEmpty(nodeId) ? "unknown" : nodeId;

            EnsureSchema();
        }

        #endregion

        #region Protected-Methods

        /// <inheritdoc />
        protected override Task<LockHandle> AcquireCoreAsync(string key, LockMode mode, int leaseMs, int timeoutMs, LockBehavior behavior, CancellationToken token)
        {
            return RunFairAcquireAsync(
                key, mode, leaseMs, timeoutMs, behavior,
                holderId => EnqueueAsync(key, holderId, mode, timeoutMs, token),
                (holderId, seq) => TryGrantAsync(key, holderId, mode, leaseMs, token),
                holderId => DequeueAsync(key, holderId, token),
                token);
        }

        /// <inheritdoc />
        protected override async Task<bool> HeartbeatCoreAsync(LockHandle handle, int leaseMs, CancellationToken token)
        {
            string keyLit = "'" + Sanitizer.SanitizeString(handle.Key) + "'";
            string holderLit = "'" + Sanitizer.SanitizeString(handle.HolderId) + "'";
            string leaseExpr = _NowUtc + " + (" + leaseMs + " * INTERVAL '1 millisecond')";
            string maxHoldExpr = "enqueued_utc + (" + _LockConfig.MaxHoldMs + " * INTERVAL '1 millisecond')";

            string sql =
                "UPDATE " + _Table + " SET lease_expires_utc = " + leaseExpr + " " +
                "WHERE holder_id = " + holderLit + " AND lock_key = " + keyLit + " AND state = 'granted' " +
                "  AND lease_expires_utc > " + _NowUtc + " AND " + maxHoldExpr + " > " + _NowUtc + " " +
                "RETURNING lease_expires_utc;";

            DataTable dt = await _Database.ExecuteQuery(sql, false, token).ConfigureAwait(false);
            if (dt == null || dt.Rows.Count == 0) return false;

            handle.LeaseExpiresUtc = DateTime.SpecifyKind(Convert.ToDateTime(dt.Rows[0]["lease_expires_utc"]), DateTimeKind.Utc);
            return true;
        }

        /// <inheritdoc />
        protected override async Task<bool> ReleaseCoreAsync(LockHandle handle, CancellationToken token)
        {
            string keyLit = "'" + Sanitizer.SanitizeString(handle.Key) + "'";
            string holderLit = "'" + Sanitizer.SanitizeString(handle.HolderId) + "'";

            string sql = "DELETE FROM " + _Table + " WHERE holder_id = " + holderLit + " AND lock_key = " + keyLit + " RETURNING id;";
            DataTable dt = await _Database.ExecuteQuery(sql, false, token).ConfigureAwait(false);
            return dt != null && dt.Rows.Count > 0;
        }

        /// <inheritdoc />
        protected override async Task<bool> ValidateCoreAsync(LockHandle handle, CancellationToken token)
        {
            string keyLit = "'" + Sanitizer.SanitizeString(handle.Key) + "'";
            string holderLit = "'" + Sanitizer.SanitizeString(handle.HolderId) + "'";

            string sql =
                "SELECT 1 FROM " + _Table + " WHERE holder_id = " + holderLit + " AND lock_key = " + keyLit + " " +
                "  AND state = 'granted' AND fencing_token = " + handle.FencingToken + " AND lease_expires_utc > " + _NowUtc + ";";

            DataTable dt = await _Database.ExecuteQuery(sql, false, token).ConfigureAwait(false);
            return dt != null && dt.Rows.Count > 0;
        }

        #endregion

        #region Private-Methods

        private async Task<long> EnqueueAsync(string key, string holderId, LockMode mode, int timeoutMs, CancellationToken token)
        {
            string keyLit = "'" + Sanitizer.SanitizeString(key) + "'";
            string holderLit = "'" + Sanitizer.SanitizeString(holderId) + "'";
            string modeLit = "'" + mode.ToString() + "'";
            string nodeLit = "'" + Sanitizer.SanitizeString(_NodeId) + "'";
            int waiterTtl = timeoutMs + _WaiterGraceMs;
            string leaseExpr = _NowUtc + " + (" + waiterTtl + " * INTERVAL '1 millisecond')";

            string sql =
                "INSERT INTO " + _Table + " (lock_key, holder_id, mode, state, fencing_token, enqueued_utc, lease_expires_utc, node_id) " +
                "VALUES (" + keyLit + ", " + holderLit + ", " + modeLit + ", 'waiting', 0, " + _NowUtc + ", " + leaseExpr + ", " + nodeLit + ") " +
                "RETURNING id;";

            DataTable dt = await _Database.ExecuteQuery(sql, false, token).ConfigureAwait(false);
            if (dt == null || dt.Rows.Count == 0) return 0;
            return Convert.ToInt64(dt.Rows[0]["id"]);
        }

        private async Task<LockHandle> TryGrantAsync(string key, string holderId, LockMode mode, int leaseMs, CancellationToken token)
        {
            string keyLit = "'" + Sanitizer.SanitizeString(key) + "'";
            string holderLit = "'" + Sanitizer.SanitizeString(holderId) + "'";

            string sql = "SELECT out_fencing, out_lease FROM less3_lock_try_grant(" + keyLit + ", " + holderLit + ", " + leaseMs + ");";

            DataTable dt = await _Database.ExecuteQuery(sql, false, token).ConfigureAwait(false);
            if (dt == null || dt.Rows.Count == 0) return null;
            if (dt.Rows[0]["out_fencing"] == DBNull.Value) return null;

            long fencingToken = Convert.ToInt64(dt.Rows[0]["out_fencing"]);
            DateTime leaseExpiresUtc = DateTime.SpecifyKind(Convert.ToDateTime(dt.Rows[0]["out_lease"]), DateTimeKind.Utc);

            return new LockHandle(key, mode, holderId, fencingToken, leaseExpiresUtc, Provider);
        }

        private async Task DequeueAsync(string key, string holderId, CancellationToken token)
        {
            string keyLit = "'" + Sanitizer.SanitizeString(key) + "'";
            string holderLit = "'" + Sanitizer.SanitizeString(holderId) + "'";
            await _Database.ExecuteQuery("DELETE FROM " + _Table + " WHERE holder_id = " + holderLit + " AND lock_key = " + keyLit + ";", false, token).ConfigureAwait(false);
        }

        private void EnsureSchema()
        {
            string sql =
                "CREATE TABLE IF NOT EXISTS " + _Table + " (" +
                "  id BIGSERIAL PRIMARY KEY, " +
                "  lock_key VARCHAR(512) NOT NULL, " +
                "  holder_id VARCHAR(64) NOT NULL, " +
                "  mode VARCHAR(16) NOT NULL, " +
                "  state VARCHAR(16) NOT NULL DEFAULT 'waiting', " +
                "  fencing_token BIGINT NOT NULL DEFAULT 0, " +
                "  enqueued_utc TIMESTAMP NOT NULL, " +
                "  lease_expires_utc TIMESTAMP NOT NULL, " +
                "  node_id VARCHAR(128) NULL" +
                "); " +
                "CREATE UNIQUE INDEX IF NOT EXISTS idx_less3_lock_queue_holder ON " + _Table + " (holder_id); " +
                "CREATE INDEX IF NOT EXISTS idx_less3_lock_queue_key ON " + _Table + " (lock_key, id); " +
                "CREATE INDEX IF NOT EXISTS idx_less3_lock_queue_lease ON " + _Table + " (lease_expires_utc); " +
                "CREATE TABLE IF NOT EXISTS less3_lock_fence (lock_key VARCHAR(512) PRIMARY KEY, fencing BIGINT NOT NULL DEFAULT 0); " +
                "CREATE OR REPLACE FUNCTION less3_lock_try_grant(p_key text, p_holder text, p_lease_ms bigint) " +
                "RETURNS TABLE(out_fencing bigint, out_lease timestamp) AS $LESS3$ " +
                "DECLARE " +
                "  v_now timestamp := (now() at time zone 'utc'); " +
                "  v_id bigint; v_mode text; v_min_id bigint; v_earlier_excl boolean; v_granted_count int; v_fence bigint; v_grantable boolean := false; " +
                "BEGIN " +
                "  PERFORM pg_advisory_xact_lock(hashtext(p_key)); " +
                "  DELETE FROM less3_lock_queue WHERE lock_key = p_key AND lease_expires_utc <= v_now; " +
                "  SELECT id, mode INTO v_id, v_mode FROM less3_lock_queue WHERE holder_id = p_holder AND lock_key = p_key; " +
                "  IF v_id IS NULL THEN RETURN; END IF; " +
                "  SELECT min(id) INTO v_min_id FROM less3_lock_queue WHERE lock_key = p_key; " +
                "  SELECT EXISTS(SELECT 1 FROM less3_lock_queue WHERE lock_key = p_key AND id < v_id AND mode IN ('Write','Delete')) INTO v_earlier_excl; " +
                "  SELECT count(*) INTO v_granted_count FROM less3_lock_queue WHERE lock_key = p_key AND state = 'granted'; " +
                "  IF v_mode = 'Read' THEN v_grantable := NOT v_earlier_excl; " +
                "  ELSE v_grantable := (v_id = v_min_id) AND (v_granted_count = 0); END IF; " +
                "  IF NOT v_grantable THEN RETURN; END IF; " +
                "  INSERT INTO less3_lock_fence(lock_key, fencing) VALUES (p_key, 1) " +
                "    ON CONFLICT (lock_key) DO UPDATE SET fencing = less3_lock_fence.fencing + 1 RETURNING fencing INTO v_fence; " +
                "  UPDATE less3_lock_queue SET state = 'granted', fencing_token = v_fence, " +
                "    lease_expires_utc = v_now + (p_lease_ms * INTERVAL '1 millisecond') " +
                "    WHERE holder_id = p_holder AND lock_key = p_key; " +
                "  RETURN QUERY SELECT v_fence, (v_now + (p_lease_ms * INTERVAL '1 millisecond')); " +
                "END; " +
                "$LESS3$ LANGUAGE plpgsql;";

            try
            {
                // Serialize DDL across nodes on a dedicated-connection advisory lock (embedding the
                // lock inside the multi-statement command does not serialize reliably).
                _Database.RunExclusiveBootstrap(() => _Database.ExecuteQuery(sql, false).GetAwaiter().GetResult());
                _Logging.Info("[PostgresLockManager] fair lock schema ensured");
            }
            catch (Exception e)
            {
                _Logging.Warn("[PostgresLockManager] lock schema ensure encountered: " + e.Message);
            }
        }

        #endregion
    }
}
