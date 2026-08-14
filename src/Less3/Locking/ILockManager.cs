namespace Less3.Locking
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Distributed lock manager. Implementations coordinate mutating access to object state across
    /// one or many Less3 nodes. The database is the authority for lock state; fencing tokens make a
    /// lapsed lease safe by letting a downstream mutation reject a stale holder.
    /// Implementations are thread-safe.
    /// </summary>
    public interface ILockManager
    {
        /// <summary>
        /// Name of this provider (for example "Local", "Postgres", "Clutch").
        /// </summary>
        string Provider { get; }

        /// <summary>
        /// Acquire a lock on a key. With fail-fast behavior, throws immediately when contended;
        /// with wait behavior, retries until the timeout elapses.
        /// </summary>
        /// <param name="key">Lock key, for example "obj:{tenant}:{bucket}:{objectKey}".</param>
        /// <param name="mode">Requested mode.</param>
        /// <param name="options">Acquisition options, or null for defaults.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A granted lock handle.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when key is null or empty.</exception>
        /// <exception cref="LockDeniedException">Thrown when the lock is contended (fail-fast) or the wait timeout elapses.</exception>
        Task<LockHandle> AcquireAsync(string key, LockMode mode, AcquireOptions options = null, CancellationToken token = default);

        /// <summary>
        /// Renew the lease on a held lock. Called automatically by the manager's heartbeat loop;
        /// callers rarely invoke it directly.
        /// </summary>
        /// <param name="handle">The held handle.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the lease was renewed; false if the lease was lost.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when handle is null.</exception>
        Task<bool> HeartbeatAsync(LockHandle handle, CancellationToken token = default);

        /// <summary>
        /// Release a held lock. Idempotent: releasing an already-released or expired lock is a no-op.
        /// </summary>
        /// <param name="handle">The held handle.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if a lock row was removed; false if nothing was held.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when handle is null.</exception>
        Task<bool> ReleaseAsync(LockHandle handle, CancellationToken token = default);

        /// <summary>
        /// Confirm the handle still owns the key with a live lease and current fencing token. A
        /// mutating operation calls this immediately before committing; a false result means the
        /// operation must be abandoned to preserve data integrity.
        /// </summary>
        /// <param name="handle">The held handle.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the handle is still valid; otherwise false.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when handle is null.</exception>
        Task<bool> ValidateAsync(LockHandle handle, CancellationToken token = default);
    }
}
