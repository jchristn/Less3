namespace Less3.Database.MySql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using global::MySql.Data.MySqlClient;
    using SyslogLogging;

    using Less3.Database.MySql.Implementations;
    using Less3.Database.MySql.Queries;

    /// <summary>
    /// MySQL database driver for Less3.
    /// </summary>
    public class MySqlDatabaseDriver : DatabaseDriverBase
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[MySqlDatabaseDriver] ";
        private DatabaseSettings _Settings;
        private LoggingModule _Logging;
        private int _MaxStatementLength = 2097152;
        private string _ConnectionString;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the MySQL database driver.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public MySqlDatabaseDriver(DatabaseSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));

            if (String.IsNullOrEmpty(_Settings.Hostname))
                throw new ArgumentException("Database hostname must be specified in settings.");

            MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder();
            csb.Server = _Settings.Hostname;
            csb.Port = _Settings.Port > 0 ? (uint)_Settings.Port : 3306;
            csb.Database = !String.IsNullOrEmpty(_Settings.DatabaseName) ? _Settings.DatabaseName : "less3";
            csb.UserID = !String.IsNullOrEmpty(_Settings.Username) ? _Settings.Username : "root";
            csb.Password = _Settings.Password ?? "";
            csb.SslMode = _Settings.RequireEncryption ? MySqlSslMode.Required : MySqlSslMode.Preferred;
            csb.AllowUserVariables = true;
            csb.DefaultCommandTimeout = 120;

            _ConnectionString = csb.ConnectionString;

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

            ExecuteQuery(SetupQueries.CreateTables(), true).Wait();

            List<string> indices = SetupQueries.CreateIndices();
            foreach (string indexQuery in indices)
            {
                try
                {
                    ExecuteQuery(indexQuery).Wait();
                }
                catch (AggregateException ae)
                {
                    bool handled = false;
                    foreach (Exception inner in ae.InnerExceptions)
                    {
                        if (inner is MySqlException mex && mex.Number == 1061)
                        {
                            handled = true;
                            break;
                        }
                    }
                    if (!handled) throw;
                }
                catch (MySqlException mex) when (mex.Number == 1061)
                {
                    // Duplicate key name - index already exists, ignore
                }
            }

            ExecuteQuery(SetupQueries.AnalyzeTables()).Wait();

            _Logging.Info(_Header + "initialized using " + _Settings.Hostname + ":" + csb.Port + "/" + csb.Database);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Execute a query against the MySQL database.
        /// </summary>
        /// <param name="query">SQL query string.</param>
        /// <param name="isTransaction">Boolean indicating whether to wrap in a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>DataTable containing the results.</returns>
        public override async Task<DataTable> ExecuteQuery(string query, bool isTransaction = false, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

            DataTable result = new DataTable();

            using (MySqlConnection conn = new MySqlConnection(_ConnectionString))
            {
                await conn.OpenAsync(token).ConfigureAwait(false);

                MySqlTransaction txn = null;

                if (isTransaction)
                {
                    txn = await conn.BeginTransactionAsync(token).ConfigureAwait(false) as MySqlTransaction;
                }

                try
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, conn, txn))
                    {
                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
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
                        await txn.CommitAsync(token).ConfigureAwait(false);
                    }
                }
                catch (Exception)
                {
                    if (txn != null)
                    {
                        await txn.RollbackAsync(token).ConfigureAwait(false);
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

            return result;
        }

        /// <summary>
        /// Execute multiple queries against the MySQL database.
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

        #endregion

        #region Private-Methods

        #endregion
    }
}
