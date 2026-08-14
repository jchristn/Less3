namespace Less3.Locking
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Settings;
    using Less3.Telemetry;
    using SyslogLogging;

    /// <summary>
    /// Shared behavior for lock managers: automatic lease renewal for all held handles, lost-lease
    /// detection, metrics, and a fair FIFO acquisition loop for queue-backed providers.
    ///
    /// The lock is a fair, arrival-ordered read/write/delete lock:
    /// <list type="bullet">
    /// <item>Reads take a shared lock. Any number run concurrently — there is no cap.</item>
    /// <item>Writes are exclusive. A write is granted only after every request that arrived before
    /// it (the pending reads it must let flush, and any earlier write/delete) has released.</item>
    /// <item>Deletes are exclusive and drain everything. Like a write, a delete is granted only
    /// after all locks that arrived before it release.</item>
    /// </list>
    /// Fairness is by arrival order, so a steady stream of readers cannot starve a waiting writer or
    /// deleter: a request that arrives after a queued exclusive waits behind it.
    /// Thread-safe.
    /// </summary>
    public abstract class LockManagerBase : ILockManager, IDisposable
    {
        #region Public-Members

        /// <inheritdoc />
        public abstract string Provider { get; }

        #endregion

        #region Private-Members

        /// <summary>
        /// Lock manager tuning.
        /// </summary>
        protected readonly LockSettings _LockConfig;

        /// <summary>
        /// Logging module.
        /// </summary>
        protected readonly LoggingModule _Logging;

        private readonly ConcurrentDictionary<string, LockHandle> _Live = new ConcurrentDictionary<string, LockHandle>();
        private readonly Timer _HeartbeatTimer;
        private readonly CancellationTokenSource _Cts = new CancellationTokenSource();
        private int _RenewInProgress = 0;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="lockConfig">Lock manager tuning.</param>
        /// <param name="logging">Logging module.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        protected LockManagerBase(LockSettings lockConfig, LoggingModule logging)
        {
            _LockConfig = lockConfig ?? throw new ArgumentNullException(nameof(lockConfig));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));

            int interval = _LockConfig.HeartbeatIntervalMs;
            _HeartbeatTimer = new Timer(OnHeartbeatTick, null, interval, interval);
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<LockHandle> AcquireAsync(string key, LockMode mode, AcquireOptions options = null, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (options == null) options = new AcquireOptions();

            int leaseMs = options.LeaseMs ?? _LockConfig.DefaultLeaseMs;
            int timeoutMs = options.TimeoutMs ?? _LockConfig.AcquireTimeoutMs;
            Stopwatch sw = Stopwatch.StartNew();

            LockHandle handle;

            try
            {
                handle = await AcquireCoreAsync(key, mode, leaseMs, timeoutMs, options.Behavior, token).ConfigureAwait(false);
            }
            catch (LockDeniedException e)
            {
                Less3Telemetry.LockDenied(Provider, mode.ToString(), e.Result.ToString());
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _Logging.Warn("[" + Provider + "LockManager] acquire error for key " + key + ": " + e.Message);
                Less3Telemetry.LockDenied(Provider, mode.ToString(), "Error");
                throw new LockDeniedException(key, AcquireResult.Error, e);
            }

            _Live[handle.HolderId] = handle;
            Less3Telemetry.LockAcquired(Provider, mode.ToString(), sw.Elapsed.TotalMilliseconds);
            return handle;
        }

        /// <inheritdoc />
        public async Task<bool> HeartbeatAsync(LockHandle handle, CancellationToken token = default)
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));

            bool renewed = await HeartbeatCoreAsync(handle, _LockConfig.DefaultLeaseMs, token).ConfigureAwait(false);
            if (!renewed)
            {
                handle.IsLost = true;
                _Live.TryRemove(handle.HolderId, out _);
            }

            return renewed;
        }

        /// <inheritdoc />
        public async Task<bool> ReleaseAsync(LockHandle handle, CancellationToken token = default)
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));

            _Live.TryRemove(handle.HolderId, out _);

            try
            {
                return await ReleaseCoreAsync(handle, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn("[" + Provider + "LockManager] release error for key " + handle.Key + ": " + e.Message);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ValidateAsync(LockHandle handle, CancellationToken token = default)
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            if (handle.IsLost) return false;

            return await ValidateCoreAsync(handle, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Generate a unique holder identifier.
        /// </summary>
        /// <returns>Holder identifier.</returns>
        protected static string NewHolderId()
        {
            return Less3.Helpers.IdGenerator.GenerateSessionId();
        }

        /// <summary>
        /// Fair FIFO acquisition loop for queue-backed providers. Enqueues the caller at the tail of
        /// the key's queue, then polls the provider's grant function until the caller reaches the
        /// front under the read/write/delete rules, the wait times out, or fail-fast gives up.
        /// </summary>
        /// <param name="key">Lock key.</param>
        /// <param name="mode">Requested mode.</param>
        /// <param name="leaseMs">Requested lease duration in milliseconds.</param>
        /// <param name="timeoutMs">Maximum wait in milliseconds.</param>
        /// <param name="behavior">Fail-fast or wait.</param>
        /// <param name="enqueue">Adds the holder to the queue and returns its arrival sequence.</param>
        /// <param name="tryGrant">Attempts to grant the holder; returns a handle or null if not yet its turn.</param>
        /// <param name="dequeue">Removes the holder from the queue on give-up.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A granted handle.</returns>
        /// <exception cref="LockDeniedException">Thrown when the lock is contended (fail-fast) or the wait times out.</exception>
        protected async Task<LockHandle> RunFairAcquireAsync(
            string key,
            LockMode mode,
            int leaseMs,
            int timeoutMs,
            LockBehavior behavior,
            Func<string, Task<long>> enqueue,
            Func<string, long, Task<LockHandle>> tryGrant,
            Func<string, Task> dequeue,
            CancellationToken token)
        {
            string holderId = NewHolderId();
            long seq = await enqueue(holderId).ConfigureAwait(false);
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            while (true)
            {
                token.ThrowIfCancellationRequested();

                LockHandle handle;
                try
                {
                    handle = await tryGrant(holderId, seq).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    try { await dequeue(holderId).ConfigureAwait(false); } catch (Exception) { }
                    throw;
                }

                if (handle != null) return handle;

                if (behavior == LockBehavior.FailFast)
                {
                    await dequeue(holderId).ConfigureAwait(false);
                    throw new LockDeniedException(key, AcquireResult.Denied);
                }

                if (DateTime.UtcNow >= deadline)
                {
                    await dequeue(holderId).ConfigureAwait(false);
                    throw new LockDeniedException(key, AcquireResult.Timeout);
                }

                await Task.Delay(_LockConfig.WaiterPollMs, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Provider-specific acquisition, including any waiting. Returns a granted handle or throws
        /// <see cref="LockDeniedException"/>. Queue-backed providers implement this by calling
        /// <see cref="RunFairAcquireAsync"/>.
        /// </summary>
        /// <param name="key">Lock key.</param>
        /// <param name="mode">Requested mode.</param>
        /// <param name="leaseMs">Requested lease duration in milliseconds.</param>
        /// <param name="timeoutMs">Maximum wait in milliseconds.</param>
        /// <param name="behavior">Fail-fast or wait.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A granted handle.</returns>
        protected abstract Task<LockHandle> AcquireCoreAsync(string key, LockMode mode, int leaseMs, int timeoutMs, LockBehavior behavior, CancellationToken token);

        /// <summary>
        /// Provider-specific lease renewal. Returns false when the lease can no longer be renewed.
        /// On success, updates the handle's lease expiry.
        /// </summary>
        /// <param name="handle">Held handle.</param>
        /// <param name="leaseMs">Requested lease extension in milliseconds.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if renewed; otherwise false.</returns>
        protected abstract Task<bool> HeartbeatCoreAsync(LockHandle handle, int leaseMs, CancellationToken token);

        /// <summary>
        /// Provider-specific release. Idempotent.
        /// </summary>
        /// <param name="handle">Held handle.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if a holder was removed; otherwise false.</returns>
        protected abstract Task<bool> ReleaseCoreAsync(LockHandle handle, CancellationToken token);

        /// <summary>
        /// Provider-specific ownership validation.
        /// </summary>
        /// <param name="handle">Held handle.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if still owned with a live lease; otherwise false.</returns>
        protected abstract Task<bool> ValidateCoreAsync(LockHandle handle, CancellationToken token);

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Whether managed resources should be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                try { _Cts.Cancel(); } catch (Exception) { }
                _HeartbeatTimer?.Dispose();

                foreach (LockHandle handle in _Live.Values)
                {
                    try { ReleaseCoreAsync(handle, CancellationToken.None).GetAwaiter().GetResult(); }
                    catch (Exception) { }
                }

                _Live.Clear();
                _Cts.Dispose();
            }

            _Disposed = true;
        }

        #endregion

        #region Private-Methods

        private void OnHeartbeatTick(object state)
        {
            if (Interlocked.CompareExchange(ref _RenewInProgress, 1, 0) != 0) return;
            _ = RenewAllAsync();
        }

        private async Task RenewAllAsync()
        {
            try
            {
                foreach (LockHandle handle in _Live.Values)
                {
                    if (_Cts.IsCancellationRequested) break;

                    try
                    {
                        bool renewed = await HeartbeatCoreAsync(handle, _LockConfig.DefaultLeaseMs, _Cts.Token).ConfigureAwait(false);
                        if (!renewed)
                        {
                            handle.IsLost = true;
                            _Live.TryRemove(handle.HolderId, out _);
                            _Logging.Warn("[" + Provider + "LockManager] lease lost for key " + handle.Key + " holder " + handle.HolderId);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn("[" + Provider + "LockManager] heartbeat error for key " + handle.Key + ": " + e.Message);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _RenewInProgress, 0);
            }
        }

        #endregion
    }
}
