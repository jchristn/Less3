namespace Less3.Database
{
    using System;

    /// <summary>
    /// Database settings.
    /// </summary>
    public class DatabaseSettings
    {
        #region Public-Members

        /// <summary>
        /// Database type.
        /// Default value is Sqlite.
        /// </summary>
        public DatabaseTypeEnum Type { get; set; } = DatabaseTypeEnum.Sqlite;

        /// <summary>
        /// Filename for SQLite databases.
        /// Default value is "./less3.db".
        /// </summary>
        public string Filename { get; set; } = "./less3.db";

        /// <summary>
        /// Hostname for network database connections.
        /// </summary>
        public string Hostname { get; set; } = null;

        /// <summary>
        /// Port number for network database connections.
        /// Default value is 0. Minimum value is 0. Maximum value is 65535.
        /// </summary>
        public int Port
        {
            get { return _Port; }
            set
            {
                if (value < 0 || value > 65535)
                    throw new ArgumentOutOfRangeException(nameof(Port), "Port must be between 0 and 65535.");
                _Port = value;
            }
        }

        /// <summary>
        /// Username for network database connections.
        /// </summary>
        public string Username { get; set; } = null;

        /// <summary>
        /// Password for network database connections.
        /// </summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// Instance name for SQL Server connections.
        /// </summary>
        public string Instance { get; set; } = null;

        /// <summary>
        /// Database name for network database connections.
        /// </summary>
        public string DatabaseName { get; set; } = null;

        /// <summary>
        /// Boolean indicating if encryption is required for network database connections.
        /// Default value is false.
        /// </summary>
        public bool RequireEncryption { get; set; } = false;

        /// <summary>
        /// Boolean indicating if queries should be logged.
        /// Default value is false.
        /// </summary>
        public bool LogQueries { get; set; } = false;

        #endregion

        #region Private-Members

        private int _Port = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with default settings (SQLite, ./less3.db).
        /// </summary>
        public DatabaseSettings()
        {
        }

        /// <summary>
        /// Instantiate for SQLite with specified filename.
        /// </summary>
        /// <param name="filename">SQLite database filename.</param>
        public DatabaseSettings(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            Type = DatabaseTypeEnum.Sqlite;
            Filename = filename;
        }

        /// <summary>
        /// Instantiate for SQL Server with hostname, port, credentials, instance, and database name.
        /// </summary>
        /// <param name="hostname">Hostname.</param>
        /// <param name="port">Port number.</param>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        /// <param name="instance">Instance name (for SQL Server Express).</param>
        /// <param name="databaseName">Database name.</param>
        public DatabaseSettings(string hostname, int port, string username, string password, string instance, string databaseName)
        {
            Type = DatabaseTypeEnum.SqlServer;
            Hostname = hostname;
            Port = port;
            Username = username;
            Password = password;
            Instance = instance;
            DatabaseName = databaseName;
        }

        /// <summary>
        /// Instantiate for a specific database type with hostname, port, credentials, and database name.
        /// </summary>
        /// <param name="type">Database type.</param>
        /// <param name="hostname">Hostname.</param>
        /// <param name="port">Port number.</param>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        /// <param name="databaseName">Database name.</param>
        public DatabaseSettings(DatabaseTypeEnum type, string hostname, int port, string username, string password, string databaseName)
        {
            Type = type;
            Hostname = hostname;
            Port = port;
            Username = username;
            Password = password;
            DatabaseName = databaseName;
        }

        #endregion
    }
}
