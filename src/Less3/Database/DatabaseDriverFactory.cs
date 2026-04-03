namespace Less3.Database
{
    using System;
    using Less3.Database.Sqlite;
    using Less3.Database.SqlServer;
    using Less3.Database.MySql;
    using Less3.Database.PostgreSql;
    using SyslogLogging;

    /// <summary>
    /// Factory for creating database driver instances based on configuration.
    /// </summary>
    public static class DatabaseDriverFactory
    {
        /// <summary>
        /// Create a database driver instance based on the supplied settings.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <returns>Database driver instance.</returns>
        public static DatabaseDriverBase Create(DatabaseSettings settings, LoggingModule logging)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            switch (settings.Type)
            {
                case DatabaseTypeEnum.Sqlite:
                    return new SqliteDatabaseDriver(settings, logging);
                case DatabaseTypeEnum.SqlServer:
                    return new SqlServerDatabaseDriver(settings, logging);
                case DatabaseTypeEnum.Mysql:
                    return new MySqlDatabaseDriver(settings, logging);
                case DatabaseTypeEnum.Postgresql:
                    return new PostgreSqlDatabaseDriver(settings, logging);
                default:
                    throw new ArgumentException("Unsupported database type: " + settings.Type.ToString());
            }
        }
    }
}
