namespace Less3.Database
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Database.Implementations;
    using Less3.Database.Interfaces;

    /// <summary>
    /// Abstract base class for database drivers.
    /// </summary>
    public abstract class DatabaseDriverBase
    {
        #region Public-Members

        /// <summary>
        /// SQL dialect used by the concrete database driver.
        /// </summary>
        internal SqlDialect Dialect { get; private protected set; } = SqlDialect.Sqlite;

        /// <summary>
        /// Tenant methods.
        /// </summary>
        public ITenantMethods Tenants { get; protected set; }

        /// <summary>
        /// User methods.
        /// </summary>
        public IUserMethods Users { get; protected set; }

        /// <summary>
        /// Credential methods.
        /// </summary>
        public ICredentialMethods Credentials { get; protected set; }

        /// <summary>
        /// Bucket methods.
        /// </summary>
        public IBucketMethods Buckets { get; protected set; }

        /// <summary>
        /// Object methods.
        /// </summary>
        public IObjMethods Objects { get; protected set; }

        /// <summary>
        /// Bucket ACL methods.
        /// </summary>
        public IBucketAclMethods BucketAcls { get; protected set; }

        /// <summary>
        /// Object ACL methods.
        /// </summary>
        public IObjectAclMethods ObjectAcls { get; protected set; }

        /// <summary>
        /// Bucket tag methods.
        /// </summary>
        public IBucketTagMethods BucketTags { get; protected set; }

        /// <summary>
        /// Object tag methods.
        /// </summary>
        public IObjectTagMethods ObjectTags { get; protected set; }

        /// <summary>
        /// Upload methods.
        /// </summary>
        public IUploadMethods Uploads { get; protected set; }

        /// <summary>
        /// Upload part methods.
        /// </summary>
        public IUploadPartMethods UploadParts { get; protected set; }

        /// <summary>
        /// Request history methods.
        /// </summary>
        public IRequestHistoryMethods RequestHistory { get; protected set; }

        /// <summary>
        /// Role methods.
        /// </summary>
        public IRoleMethods Roles { get; protected set; }

        /// <summary>
        /// Permission methods.
        /// </summary>
        public IPermissionMethods Permissions { get; protected set; }

        /// <summary>
        /// Role assignment methods.
        /// </summary>
        public IRoleAssignmentMethods RoleAssignments { get; protected set; }

        /// <summary>
        /// Auth session methods.
        /// </summary>
        public IAuthSessionMethods AuthSessions { get; protected set; }

        /// <summary>
        /// Authorization audit methods.
        /// </summary>
        public IAuthorizationAuditMethods AuthorizationAudit { get; protected set; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Database driver base class.
        /// Derived classes must initialize all interface properties in their constructors.
        /// </summary>
        public DatabaseDriverBase()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Execute a query.
        /// </summary>
        /// <param name="query">Query.</param>
        /// <param name="isTransaction">Boolean to indicate if it should be within a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Data table.</returns>
        public abstract Task<DataTable> ExecuteQuery(string query, bool isTransaction = false, CancellationToken token = default);

        /// <summary>
        /// Execute multiple queries.
        /// </summary>
        /// <param name="queries">Queries.</param>
        /// <param name="isTransaction">Boolean to indicate if it should be within a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Data table.</returns>
        public abstract Task<DataTable> ExecuteQueries(IEnumerable<string> queries, bool isTransaction = false, CancellationToken token = default);

        /// <summary>
        /// Run a bootstrap action (schema seeding, default data) under a cluster-wide mutual
        /// exclusion so that when multiple nodes start against the same database at once, exactly
        /// one performs the seed while the others wait and then observe the data already present.
        /// The default implementation simply runs the action (single-node engines need no
        /// coordination); the PostgreSQL driver overrides it to hold a database advisory lock for
        /// the duration of the action.
        /// </summary>
        /// <param name="action">The bootstrap action to run exclusively.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when action is null.</exception>
        public virtual void RunExclusiveBootstrap(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            action();
        }

        #endregion
    }
}
