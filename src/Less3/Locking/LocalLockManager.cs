namespace Less3.Locking
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Settings;
    using SyslogLogging;

    /// <summary>
    /// In-process fair read/write/delete lock manager for standalone single-node deployments. Reads
    /// are shared and unbounded; writes and deletes are exclusive and are granted in arrival order
    /// once every earlier holder has released. There is no lease — a single process is the only
    /// holder, so if it exits all locks vanish with it. This is the default provider and the only
    /// supported provider when the database is SQLite.
    /// Thread-safe.
    /// </summary>
    public class LocalLockManager : LockManagerBase
    {
        #region Public-Members

        /// <inheritdoc />
        public override string Provider
        {
            get { return "Local"; }
        }

        #endregion

        #region Private-Members

        private readonly ConcurrentDictionary<string, LocalLockQueue> _Queues = new ConcurrentDictionary<string, LocalLockQueue>();
        private long _GlobalSeq = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="lockConfig">Lock manager tuning.</param>
        /// <param name="logging">Logging module.</param>
        public LocalLockManager(LockSettings lockConfig, LoggingModule logging)
            : base(lockConfig, logging)
        {
        }

        #endregion

        #region Protected-Methods

        /// <inheritdoc />
        protected override Task<LockHandle> AcquireCoreAsync(string key, LockMode mode, int leaseMs, int timeoutMs, LockBehavior behavior, CancellationToken token)
        {
            LocalLockQueue queue = _Queues.GetOrAdd(key, k => new LocalLockQueue(k, Provider));
            DateTime leaseExpires = DateTime.UtcNow.AddMilliseconds(_LockConfig.MaxHoldMs);

            return RunFairAcquireAsync(
                key, mode, leaseMs, timeoutMs, behavior,
                holderId => Task.FromResult(queue.Enqueue(holderId, mode, Interlocked.Increment(ref _GlobalSeq))),
                (holderId, seq) => Task.FromResult(queue.TryGrant(holderId, leaseExpires)),
                holderId => { queue.Release(holderId); return Task.CompletedTask; },
                token);
        }

        /// <inheritdoc />
        protected override Task<bool> HeartbeatCoreAsync(LockHandle handle, int leaseMs, CancellationToken token)
        {
            bool held = _Queues.TryGetValue(handle.Key, out LocalLockQueue queue) && queue.IsGranted(handle.HolderId);
            return Task.FromResult(held);
        }

        /// <inheritdoc />
        protected override Task<bool> ReleaseCoreAsync(LockHandle handle, CancellationToken token)
        {
            bool removed = _Queues.TryGetValue(handle.Key, out LocalLockQueue queue) && queue.Release(handle.HolderId);
            return Task.FromResult(removed);
        }

        /// <inheritdoc />
        protected override Task<bool> ValidateCoreAsync(LockHandle handle, CancellationToken token)
        {
            bool held = _Queues.TryGetValue(handle.Key, out LocalLockQueue queue) && queue.IsGranted(handle.HolderId);
            return Task.FromResult(held);
        }

        #endregion
    }
}
