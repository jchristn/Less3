namespace Less3.Database.PostgreSql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Database.Implementations;
    using Less3.Database.PostgreSql.Implementations;
    using Less3.Database.PostgreSql.Queries;
    using Npgsql;
    using SyslogLogging;

    /// <summary>
    /// PostgreSQL database driver for Less3.
    /// No application-level locking is used; PostgreSQL handles concurrent access natively through connection pools.
    /// </summary>
    public class PostgreSqlDatabaseDriver : DatabaseDriverBase
    {
        #region Private-Members

        private DatabaseSettings _Settings;
        private LoggingModule _Logging;
        private string _Header = "[PostgreSqlDatabaseDriver] ";
        private string _ConnectionString;
        private int _MaxStatementLength = 4194304;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the PostgreSQL database driver.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public PostgreSqlDatabaseDriver(DatabaseSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            Dialect = SqlDialect.PostgreSql;

            _ConnectionString = BuildConnectionString(_Settings);

            Users = new PostgreSqlUserMethods(this);
            Credentials = new PostgreSqlCredentialMethods(this);
            Buckets = new PostgreSqlBucketMethods(this);
            Objects = new PostgreSqlObjMethods(this);
            BucketAcls = new PostgreSqlBucketAclMethods(this);
            ObjectAcls = new PostgreSqlObjectAclMethods(this);
            BucketTags = new PostgreSqlBucketTagMethods(this);
            ObjectTags = new PostgreSqlObjectTagMethods(this);
            Uploads = new PostgreSqlUploadMethods(this);
            UploadParts = new PostgreSqlUploadPartMethods(this);
            RequestHistory = new PostgreSqlRequestHistoryMethods(this);
            Tenants = new ControlPlaneTenantMethods(this, SqlDialect.PostgreSql);
            Roles = new ControlPlaneRoleMethods(this, SqlDialect.PostgreSql);
            Permissions = new ControlPlanePermissionMethods(this, SqlDialect.PostgreSql);
            RoleAssignments = new ControlPlaneRoleAssignmentMethods(this, SqlDialect.PostgreSql);
            AuthSessions = new ControlPlaneAuthSessionMethods(this, SqlDialect.PostgreSql);
            AuthorizationAudit = new ControlPlaneAuthorizationAuditMethods(this, SqlDialect.PostgreSql);

            PostgreSqlLegacyV2Migrator.RunIfNeeded(_ConnectionString, _Logging, _Header);

            // Serialize schema creation across nodes. In a multi-node cluster every node runs this
            // setup at boot against the same database; PostgreSQL's "CREATE TABLE/TYPE IF NOT EXISTS"
            // is NOT safe under concurrency (two sessions creating the same object race on the
            // pg_type/pg_class unique indexes and one fails with 23505). Hold a database advisory
            // lock on a dedicated connection for the whole schema + migration phase, so exactly one
            // node creates the schema while the others wait and then find everything already present
            // (archive/MULTINODE_PLAN.md R16). The lock is taken on its own connection before any DDL runs,
            // which serializes reliably — embedding the lock inside the multi-statement setup command
            // does not, because the statements can begin executing before the lock takes effect.
            RunExclusiveBootstrap(() =>
            {
                string setupQuery = SetupQueries.CreateTablesAndIndices();
                ExecuteQuery(setupQuery, false).Wait();
                RunMigrations();
                ExecuteQuery("ANALYZE;", false).Wait();
            });

            _Logging.Info("PostgreSqlDatabaseDriver initialized successfully");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Execute a query against the PostgreSQL database.
        /// </summary>
        /// <param name="query">Query.</param>
        /// <param name="isTransaction">Boolean to indicate if it should be within a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Data table.</returns>
        public override async Task<DataTable> ExecuteQuery(string query, bool isTransaction = false, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

            token.ThrowIfCancellationRequested();

            if (_Settings.LogQueries) _Logging.Debug("PostgreSqlDatabaseDriver query: " + query);

            DataTable result = new DataTable();

            using (NpgsqlConnection conn = new NpgsqlConnection(_ConnectionString))
            {
                await conn.OpenAsync(token).ConfigureAwait(false);

                if (isTransaction)
                {
                    NpgsqlTransaction txn = await conn.BeginTransactionAsync(token).ConfigureAwait(false);

                    try
                    {
                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn, txn))
                        {
                            cmd.CommandTimeout = 120;

                            using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                            {
                                result.Load(reader);
                            }
                        }

                        await txn.CommitAsync(token).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        await txn.RollbackAsync(token).ConfigureAwait(false);
                        throw;
                    }
                    finally
                    {
                        txn.Dispose();
                    }
                }
                else
                {
                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.CommandTimeout = 120;

                        using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                        {
                            result.Load(reader);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Execute multiple queries against the PostgreSQL database.
        /// </summary>
        /// <param name="queries">Queries.</param>
        /// <param name="isTransaction">Boolean to indicate if it should be within a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Data table from the last query.</returns>
        public override async Task<DataTable> ExecuteQueries(IEnumerable<string> queries, bool isTransaction = false, CancellationToken token = default)
        {
            if (queries == null) throw new ArgumentNullException(nameof(queries));

            token.ThrowIfCancellationRequested();

            List<string> queryList = queries.ToList();
            if (queryList.Count == 0) return new DataTable();

            StringBuilder combined = new StringBuilder();
            foreach (string query in queryList)
            {
                combined.Append(query);
                if (!query.TrimEnd().EndsWith(";")) combined.Append(";");
                combined.Append(" ");
            }

            string combinedStr = combined.ToString();

            if (combinedStr.Length <= _MaxStatementLength)
            {
                return await ExecuteQuery(combinedStr, isTransaction, token).ConfigureAwait(false);
            }

            DataTable lastResult = new DataTable();

            foreach (string query in queryList)
            {
                token.ThrowIfCancellationRequested();
                lastResult = await ExecuteQuery(query, isTransaction, token).ConfigureAwait(false);
            }

            return lastResult;
        }

        /// <summary>
        /// Run a bootstrap action while holding a PostgreSQL advisory lock on a dedicated connection,
        /// so concurrent node startups seed the database exactly once.
        /// </summary>
        /// <param name="action">The bootstrap action to run exclusively.</param>
        /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
        public override void RunExclusiveBootstrap(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            using (NpgsqlConnection conn = new NpgsqlConnection(_ConnectionString))
            {
                conn.Open();

                using (NpgsqlCommand lockCmd = new NpgsqlCommand("SELECT pg_advisory_lock(hashtext('less3_bootstrap_seed'))", conn))
                {
                    lockCmd.CommandTimeout = 300;
                    lockCmd.ExecuteScalar();
                }

                try
                {
                    action();
                }
                finally
                {
                    try
                    {
                        using (NpgsqlCommand unlockCmd = new NpgsqlCommand("SELECT pg_advisory_unlock(hashtext('less3_bootstrap_seed'))", conn))
                        {
                            unlockCmd.ExecuteScalar();
                        }
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn(_Header + "failed to release bootstrap advisory lock: " + e.Message);
                    }
                }
            }
        }

        #endregion

        #region Private-Methods

        private void RunMigrations()
        {
            List<string> migrations = MigrationQueries.GetMigrations();
            foreach (string migration in migrations)
            {
                try
                {
                    ExecuteQuery(migration, false).Wait();
                }
                catch (Exception)
                {
                    // Column may already exist; ignore
                }
            }
        }

        private string BuildConnectionString(DatabaseSettings settings)
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder();
            builder.Host = !String.IsNullOrEmpty(settings.Hostname) ? settings.Hostname : "localhost";
            builder.Port = settings.Port > 0 ? settings.Port : 5432;
            builder.Database = !String.IsNullOrEmpty(settings.DatabaseName) ? settings.DatabaseName : "less3";
            builder.Username = !String.IsNullOrEmpty(settings.Username) ? settings.Username : "postgres";

            if (!String.IsNullOrEmpty(settings.Password))
                builder.Password = settings.Password;

            if (settings.RequireEncryption)
                builder.SslMode = SslMode.Require;
            else
                builder.SslMode = SslMode.Prefer;

            builder.CommandTimeout = 120;
            builder.Timeout = 30;

            return builder.ToString();
        }

        #endregion
    }
}
