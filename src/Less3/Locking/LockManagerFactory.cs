namespace Less3.Locking
{
    using System;
    using Less3.Database;
    using Less3.Settings;
    using SyslogLogging;

    /// <summary>
    /// Creates the configured <see cref="ILockManager"/> for the deployment.
    /// </summary>
    public static class LockManagerFactory
    {
        /// <summary>
        /// Create a lock manager from settings.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="database">Database driver (used by the Postgres provider).</param>
        /// <param name="nodeId">Resolved node identifier.</param>
        /// <param name="logging">Logging module.</param>
        /// <returns>A lock manager.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the selected provider is incompatible with the database type.</exception>
        public static ILockManager Create(SettingsBase settings, DatabaseDriverBase database, string nodeId, LoggingModule logging)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            LockProviderEnum provider = settings.Cluster.LockProvider;

            switch (provider)
            {
                case LockProviderEnum.Local:
                    logging.Info("[LockManagerFactory] using in-process (Local) lock provider");
                    return new LocalLockManager(settings.Cluster.Lock, logging);

                case LockProviderEnum.Postgres:
                    if (database == null) throw new ArgumentNullException(nameof(database));
                    if (settings.Database.Type != DatabaseTypeEnum.Postgresql)
                        throw new InvalidOperationException("The Postgres lock provider requires a PostgreSQL database (Database.Type=Postgresql).");
                    logging.Info("[LockManagerFactory] using PostgreSQL lock provider");
                    return new PostgresLockManager(database, nodeId, settings.Cluster.Lock, logging);

                case LockProviderEnum.Clutch:
                    logging.Info("[LockManagerFactory] using Clutch lock provider");
                    return new ClutchLockManager(settings.Cluster.Clutch, settings.Cluster.Lock, logging);

                default:
                    throw new InvalidOperationException("Unknown lock provider: " + provider + ".");
            }
        }
    }
}
