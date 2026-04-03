namespace Less3.Database
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Database.Interfaces;

    /// <summary>
    /// Abstract base class for database drivers.
    /// </summary>
    public abstract class DatabaseDriverBase
    {
        #region Public-Members

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

        #endregion
    }
}
