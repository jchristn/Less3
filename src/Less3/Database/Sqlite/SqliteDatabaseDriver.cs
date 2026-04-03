namespace Less3.Database.Sqlite
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Data.Sqlite;
    using SyslogLogging;

    using Less3.Database.Sqlite.Implementations;
    using Less3.Database.Sqlite.Queries;

    /// <summary>
    /// SQLite database driver for Less3.
    /// </summary>
    public class SqliteDatabaseDriver : DatabaseDriverBase, IDisposable
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[SqliteDatabaseDriver] ";
        private DatabaseSettings _Settings;
        private LoggingModule _Logging;
        private int _MaxStatementLength = 2097152;
        private ReaderWriterLockSlim _Lock = new ReaderWriterLockSlim();
        private string _ConnectionString;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the SQLite database driver.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public SqliteDatabaseDriver(DatabaseSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));

            if (String.IsNullOrEmpty(_Settings.Filename))
                throw new ArgumentException("Database filename must be specified in settings.");

            _ConnectionString = "Data Source=" + _Settings.Filename + ";";

            Users = new UserMethods(this);
            Credentials = new CredentialMethods(this);
            Buckets = new BucketMethods(this);
            Objects = new ObjMethods(this);
            BucketAcls = new BucketAclMethods(this);
            ObjectAcls = new ObjectAclMethods(this);
            BucketTags = new BucketTagMethods(this);
            ObjectTags = new ObjectTagMethods(this);
            Uploads = new UploadMethods(this);
            UploadParts = new UploadPartMethods(this);
            RequestHistory = new RequestHistoryMethods(this);

            ExecuteQuery("PRAGMA journal_mode=WAL;").Wait();
            ExecuteQuery("PRAGMA synchronous=NORMAL;").Wait();
            ExecuteQuery("PRAGMA cache_size=-1000000;").Wait();
            ExecuteQuery("PRAGMA temp_store=MEMORY;").Wait();
            ExecuteQuery("PRAGMA page_size=4096;").Wait();
            ExecuteQuery("PRAGMA mmap_size=2147483648;").Wait();
            ExecuteQuery("PRAGMA wal_autocheckpoint=1000;").Wait();

            ExecuteQuery(SetupQueries.CreateTablesAndIndices(), true).Wait();

            ExecuteQuery("ANALYZE;").Wait();

            _Logging.Info(_Header + "initialized using " + _Settings.Filename);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Execute a query against the SQLite database.
        /// </summary>
        /// <param name="query">SQL query string.</param>
        /// <param name="isTransaction">Boolean indicating whether to wrap in a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>DataTable containing the results.</returns>
        public override async Task<DataTable> ExecuteQuery(string query, bool isTransaction = false, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

            DataTable result = new DataTable();

            bool isWrite = IsWriteQuery(query);

            if (isWrite)
                _Lock.EnterWriteLock();
            else
                _Lock.EnterReadLock();

            try
            {
                using (SqliteConnection conn = new SqliteConnection(_ConnectionString))
                {
                    await conn.OpenAsync(token).ConfigureAwait(false);

                    SqliteTransaction txn = null;

                    if (isTransaction)
                    {
                        txn = conn.BeginTransaction();
                    }

                    try
                    {
                        using (SqliteCommand cmd = new SqliteCommand(query, conn, txn))
                        {
                            using (SqliteDataReader reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string colName = reader.GetName(i);
                                    if (!result.Columns.Contains(colName))
                                        result.Columns.Add(colName, typeof(string));
                                }

                                while (await reader.ReadAsync(token).ConfigureAwait(false))
                                {
                                    DataRow row = result.NewRow();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        if (reader.IsDBNull(i))
                                            row[i] = DBNull.Value;
                                        else
                                            row[i] = reader.GetValue(i).ToString();
                                    }
                                    result.Rows.Add(row);
                                }
                            }
                        }

                        if (txn != null)
                        {
                            txn.Commit();
                        }
                    }
                    catch (Exception)
                    {
                        if (txn != null)
                        {
                            txn.Rollback();
                        }
                        throw;
                    }
                    finally
                    {
                        if (txn != null)
                        {
                            txn.Dispose();
                        }
                    }
                }
            }
            finally
            {
                if (isWrite)
                    _Lock.ExitWriteLock();
                else
                    _Lock.ExitReadLock();
            }

            return result;
        }

        /// <summary>
        /// Execute multiple queries against the SQLite database.
        /// Queries are combined into a single statement if under the maximum statement length, otherwise executed individually.
        /// </summary>
        /// <param name="queries">Collection of SQL query strings.</param>
        /// <param name="isTransaction">Boolean indicating whether to wrap in a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>DataTable containing the results of the last query.</returns>
        public override async Task<DataTable> ExecuteQueries(IEnumerable<string> queries, bool isTransaction = false, CancellationToken token = default)
        {
            if (queries == null) throw new ArgumentNullException(nameof(queries));

            List<string> queryList = queries.ToList();
            if (queryList.Count == 0) return new DataTable();

            string combined = String.Join(" ", queryList);

            if (combined.Length <= _MaxStatementLength)
            {
                return await ExecuteQuery(combined, isTransaction, token).ConfigureAwait(false);
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
        /// Dispose of resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                if (_Lock != null)
                {
                    _Lock.Dispose();
                    _Lock = null;
                }
            }

            _Disposed = true;
        }

        private bool IsWriteQuery(string query)
        {
            string trimmed = query.TrimStart();
            return trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("DROP", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("ALTER", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("ANALYZE", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
