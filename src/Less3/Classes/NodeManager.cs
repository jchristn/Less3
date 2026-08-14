namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;

    using Less3.Database;
    using Less3.Database.PostgreSql;
    using Less3.Settings;
    using SyslogLogging;

    /// <summary>
    /// Tracks cluster membership. In a PostgreSQL-backed cluster each node registers itself in the
    /// less3_node table and refreshes a heartbeat; a node whose heartbeat is older than the
    /// staleness window is reported unhealthy. In single-node deployments there is exactly one node
    /// (this one) and no database rows are written.
    /// Thread-safe.
    /// </summary>
    public sealed class NodeManager : IDisposable
    {
        #region Private-Members

        private readonly SettingsBase _Settings;
        private readonly LoggingModule _Logging;
        private readonly DatabaseDriverBase _Database;
        private readonly string _NodeId;
        private readonly string _Version;
        private readonly bool _Active;
        private readonly DateTime _StartedUtc;
        private Timer _Heartbeat;
        private bool _Disposed = false;

        private const string _NowUtc = "(now() at time zone 'utc')";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate. Registers this node and starts its heartbeat when running as a PostgreSQL
        /// cluster.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="database">Database driver.</param>
        /// <param name="nodeId">This node's identifier.</param>
        /// <param name="version">Less3 version string.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        public NodeManager(SettingsBase settings, LoggingModule logging, DatabaseDriverBase database, string nodeId, string version)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _NodeId = String.IsNullOrEmpty(nodeId) ? "less3-node" : nodeId;
            _Version = version ?? "";
            _StartedUtc = DateTime.UtcNow;

            _Active = _Settings.Cluster != null && _Settings.Cluster.Enabled && _Settings.Database.Type == DatabaseTypeEnum.Postgresql;

            if (_Active)
            {
                EnsureSchema();
                Register();
                int interval = _Settings.Cluster.NodeHeartbeatIntervalMs;
                _Heartbeat = new Timer(HeartbeatCallback, null, interval, interval);
                _Logging.Info("[NodeManager] registered node " + _NodeId + " in cluster");
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// List known nodes with health computed against the staleness window.
        /// </summary>
        /// <returns>Node list.</returns>
        public List<ClusterNodeInfo> GetNodes()
        {
            List<ClusterNodeInfo> nodes = new List<ClusterNodeInfo>();

            if (!_Active)
            {
                nodes.Add(new ClusterNodeInfo
                {
                    NodeId = _NodeId,
                    Hostname = Environment.MachineName,
                    Version = _Version,
                    StartedUtc = _StartedUtc,
                    LastSeenUtc = DateTime.UtcNow,
                    Healthy = true,
                    IsSelf = true
                });
                return nodes;
            }

            try
            {
                DataTable dt = _Database.ExecuteQuery("SELECT node_id, hostname, version, started_utc, last_seen_utc FROM less3_node;", false).GetAwaiter().GetResult();
                double staleMs = _Settings.Cluster.NodeStaleAfterMs;
                DateTime now = DateTime.UtcNow;

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        DateTime lastSeen = Convert.ToDateTime(row["last_seen_utc"]);
                        ClusterNodeInfo info = new ClusterNodeInfo
                        {
                            NodeId = Convert.ToString(row["node_id"]),
                            Hostname = row["hostname"] == DBNull.Value ? null : Convert.ToString(row["hostname"]),
                            Version = row["version"] == DBNull.Value ? null : Convert.ToString(row["version"]),
                            StartedUtc = row["started_utc"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["started_utc"]),
                            LastSeenUtc = lastSeen,
                            Healthy = (now - lastSeen).TotalMilliseconds <= staleMs
                        };
                        info.IsSelf = String.Equals(info.NodeId, _NodeId, StringComparison.Ordinal);
                        nodes.Add(info);
                    }
                }
            }
            catch (Exception e)
            {
                _Logging.Warn("[NodeManager] GetNodes error: " + e.Message);
            }

            return nodes;
        }

        /// <summary>
        /// Build an aggregate cluster health result.
        /// </summary>
        /// <returns>Cluster health.</returns>
        public ClusterHealthResult GetHealth()
        {
            List<ClusterNodeInfo> nodes = GetNodes();
            ClusterHealthResult result = new ClusterHealthResult
            {
                ClusterEnabled = _Settings.Cluster != null && _Settings.Cluster.Enabled,
                LockProvider = _Settings.Cluster != null ? _Settings.Cluster.LockProvider.ToString() : "Local",
                SelfNodeId = _NodeId,
                TotalNodes = nodes.Count,
                Nodes = nodes,
                GeneratedUtc = DateTime.UtcNow
            };

            int healthy = 0;
            foreach (ClusterNodeInfo n in nodes) if (n.Healthy) healthy++;
            result.HealthyNodes = healthy;

            return result;
        }

        /// <summary>
        /// List currently-held distributed locks (PostgreSQL clusters only; empty otherwise).
        /// </summary>
        /// <returns>Held locks.</returns>
        public List<LockInfo> GetActiveLocks()
        {
            List<LockInfo> locks = new List<LockInfo>();
            if (!_Active) return locks;

            try
            {
                DataTable dt = _Database.ExecuteQuery(
                    "SELECT lock_key, mode, holder_id, fencing_token, node_id, enqueued_utc, lease_expires_utc " +
                    "FROM less3_lock_queue WHERE state = 'granted' AND lease_expires_utc > " + _NowUtc + " ORDER BY lock_key;", false).GetAwaiter().GetResult();

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        locks.Add(new LockInfo
                        {
                            LockKey = Convert.ToString(row["lock_key"]),
                            Mode = Convert.ToString(row["mode"]),
                            HolderId = row["holder_id"] == DBNull.Value ? null : Convert.ToString(row["holder_id"]),
                            FencingToken = row["fencing_token"] == DBNull.Value ? 0 : Convert.ToInt64(row["fencing_token"]),
                            NodeId = row["node_id"] == DBNull.Value ? null : Convert.ToString(row["node_id"]),
                            AcquiredUtc = row["enqueued_utc"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["enqueued_utc"]),
                            LeaseExpiresUtc = row["lease_expires_utc"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["lease_expires_utc"])
                        });
                    }
                }
            }
            catch (Exception e)
            {
                _Logging.Warn("[NodeManager] GetActiveLocks error: " + e.Message);
            }

            return locks;
        }

        /// <summary>
        /// Identify the node currently holding the cleanup leader lease, or null if none.
        /// </summary>
        /// <returns>Leader node id, or null.</returns>
        public string GetLeaderNodeId()
        {
            if (!_Active) return _NodeId;

            try
            {
                DataTable dt = _Database.ExecuteQuery(
                    "SELECT node_id FROM less3_lock_queue WHERE lock_key = 'cluster:cleanup' AND state = 'granted' AND lease_expires_utc > " + _NowUtc + ";", false).GetAwaiter().GetResult();
                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["node_id"] != DBNull.Value)
                    return Convert.ToString(dt.Rows[0]["node_id"]);
            }
            catch (Exception e)
            {
                _Logging.Warn("[NodeManager] GetLeaderNodeId error: " + e.Message);
            }

            return null;
        }

        /// <summary>
        /// Dispose. Deregisters this node.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;

            _Heartbeat?.Dispose();

            if (_Active)
            {
                try
                {
                    _Database.ExecuteQuery("DELETE FROM less3_node WHERE node_id = '" + Sanitizer.SanitizeString(_NodeId) + "';", false).GetAwaiter().GetResult();
                }
                catch (Exception) { }
            }

            _Disposed = true;
        }

        #endregion

        #region Private-Methods

        private void EnsureSchema()
        {
            string sql =
                "CREATE TABLE IF NOT EXISTS less3_node (" +
                "  node_id VARCHAR(128) PRIMARY KEY, " +
                "  hostname VARCHAR(256) NULL, " +
                "  version VARCHAR(32) NULL, " +
                "  started_utc TIMESTAMP NOT NULL, " +
                "  last_seen_utc TIMESTAMP NOT NULL" +
                ");";

            // Serialize DDL across nodes on a dedicated-connection advisory lock.
            try { _Database.RunExclusiveBootstrap(() => _Database.ExecuteQuery(sql, false).GetAwaiter().GetResult()); }
            catch (Exception e) { _Logging.Warn("[NodeManager] EnsureSchema: " + e.Message); }
        }

        private void Register()
        {
            string node = "'" + Sanitizer.SanitizeString(_NodeId) + "'";
            string host = "'" + Sanitizer.SanitizeString(Environment.MachineName) + "'";
            string ver = "'" + Sanitizer.SanitizeString(_Version) + "'";

            string sql =
                "INSERT INTO less3_node (node_id, hostname, version, started_utc, last_seen_utc) " +
                "VALUES (" + node + ", " + host + ", " + ver + ", " + _NowUtc + ", " + _NowUtc + ") " +
                "ON CONFLICT (node_id) DO UPDATE SET hostname = EXCLUDED.hostname, version = EXCLUDED.version, started_utc = " + _NowUtc + ", last_seen_utc = " + _NowUtc + ";";

            try { _Database.ExecuteQuery(sql, false).GetAwaiter().GetResult(); }
            catch (Exception e) { _Logging.Warn("[NodeManager] Register: " + e.Message); }
        }

        private void HeartbeatCallback(object state)
        {
            try
            {
                _Database.ExecuteQuery("UPDATE less3_node SET last_seen_utc = " + _NowUtc + " WHERE node_id = '" + Sanitizer.SanitizeString(_NodeId) + "';", false).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _Logging.Warn("[NodeManager] heartbeat error: " + e.Message);
            }
        }

        #endregion
    }
}
