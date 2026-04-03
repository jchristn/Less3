namespace Less3.Database.SqlServer
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Data.SqlClient;
    using SyslogLogging;

    using Less3.Database.SqlServer.Implementations;
    using Less3.Database.SqlServer.Queries;

    /// <summary>
    /// SQL Server database driver for Less3.
    /// </summary>
    public class SqlServerDatabaseDriver : DatabaseDriverBase
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[SqlServerDatabaseDriver] ";
        private DatabaseSettings _Settings;
        private LoggingModule _Logging;
        private int _MaxStatementLength = 2097152;
        private string _ConnectionString;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the SQL Server database driver.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        public SqlServerDatabaseDriver(DatabaseSettings settings, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));

            if (String.IsNullOrEmpty(_Settings.Hostname))
                throw new ArgumentException("Database hostname must be specified in settings.");

            if (String.IsNullOrEmpty(_Settings.DatabaseName))
                throw new ArgumentException("Database name must be specified in settings.");

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

            if (!String.IsNullOrEmpty(_Settings.Instance))
                builder.DataSource = _Settings.Hostname + "\\" + _Settings.Instance;
            else if (_Settings.Port > 0)
                builder.DataSource = _Settings.Hostname + "," + _Settings.Port;
            else
                builder.DataSource = _Settings.Hostname;

            builder.InitialCatalog = _Settings.DatabaseName;

            if (!String.IsNullOrEmpty(_Settings.Username))
            {
                builder.UserID = _Settings.Username;
                builder.Password = _Settings.Password;
            }
            else
            {
                builder.IntegratedSecurity = true;
            }

            builder.Encrypt = _Settings.RequireEncryption;
            builder.TrustServerCertificate = !_Settings.RequireEncryption;

            _ConnectionString = builder.ConnectionString;

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

            ExecuteQuery(SetupQueries.CreateTablesAndIndices(), true).Wait();

            ExecuteQuery("EXEC sp_updatestats;").Wait();

            _Logging.Info(_Header + "initialized using " + _Settings.Hostname + "/" + _Settings.DatabaseName);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Execute a query against the SQL Server database.
        /// </summary>
        /// <param name="query">SQL query string.</param>
        /// <param name="isTransaction">Boolean indicating whether to wrap in a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>DataTable containing the results.</returns>
        public override async Task<DataTable> ExecuteQuery(string query, bool isTransaction = false, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

            DataTable result = new DataTable();

            using (SqlConnection conn = new SqlConnection(_ConnectionString))
            {
                await conn.OpenAsync(token).ConfigureAwait(false);

                SqlTransaction txn = null;

                if (isTransaction)
                {
                    txn = conn.BeginTransaction();
                }

                try
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn, txn))
                    {
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
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

            return result;
        }

        /// <summary>
        /// Execute multiple queries against the SQL Server database.
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
