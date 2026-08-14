namespace Less3.Locking
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net.WebSockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Less3.Settings;
    using SyslogLogging;

    /// <summary>
    /// Optional distributed lock manager backed by a Clutch server over its native WebSocket lock
    /// protocol (<c>/v1.0/lock/connect</c>). One connection is maintained per node; every lock the
    /// connection holds is released automatically by Clutch when the socket closes, so a crashed
    /// node cannot leave a lock stranded until its lease lapses — the socket is the lease of last
    /// resort. Clutch performs the fair read/write/delete queueing and drain server-side, and shares
    /// the same PostgreSQL database via bring-your-own-database, so the database remains the lock
    /// authority. Clutch is alpha; this provider is opt-in and never the default.
    /// Thread-safe.
    /// </summary>
    public class ClutchLockManager : LockManagerBase
    {
        #region Public-Members

        /// <inheritdoc />
        public override string Provider
        {
            get { return "Clutch"; }
        }

        #endregion

        #region Private-Members

        private readonly ClutchSettings _Clutch;
        private readonly Uri _WsUri;
        private readonly SemaphoreSlim _ConnectLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _SendLock = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _Pending = new ConcurrentDictionary<string, TaskCompletionSource<JsonElement>>();
        private readonly ConcurrentDictionary<string, DateTime> _RenewedLeaseUtc = new ConcurrentDictionary<string, DateTime>();
        private readonly ConcurrentDictionary<string, DateTime> _RenewedAtUtc = new ConcurrentDictionary<string, DateTime>();

        private ClientWebSocket _Socket;
        private CancellationTokenSource _ReceiveCts;
        private Task _ReceiveTask;
        private TaskCompletionSource<bool> _WelcomeTcs;
        private string _SessionId;
        private long _RequestCounter = 0;
        private volatile bool _Ready = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="clutchSettings">Clutch connection settings.</param>
        /// <param name="lockConfig">Lock manager tuning.</param>
        /// <param name="logging">Logging module.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the Clutch endpoint or access key is not configured.</exception>
        public ClutchLockManager(ClutchSettings clutchSettings, LockSettings lockConfig, LoggingModule logging)
            : base(lockConfig, logging)
        {
            _Clutch = clutchSettings ?? throw new ArgumentNullException(nameof(clutchSettings));
            if (String.IsNullOrEmpty(_Clutch.Endpoint)) throw new ArgumentException("Clutch endpoint is not configured.", nameof(clutchSettings));
            if (String.IsNullOrEmpty(_Clutch.AccessKey)) throw new ArgumentException("Clutch access key is not configured.", nameof(clutchSettings));

            string baseUrl = _Clutch.Endpoint.TrimEnd('/');
            if (baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) baseUrl = "wss://" + baseUrl.Substring("https://".Length);
            else if (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) baseUrl = "ws://" + baseUrl.Substring("http://".Length);
            _WsUri = new Uri(baseUrl + "/v1.0/lock/connect");
        }

        #endregion

        #region Protected-Methods

        /// <inheritdoc />
        protected override async Task<LockHandle> AcquireCoreAsync(string key, LockMode mode, int leaseMs, int timeoutMs, LockBehavior behavior, CancellationToken token)
        {
            await EnsureConnectedAsync(token).ConfigureAwait(false);

            string requestId = "r" + Interlocked.Increment(ref _RequestCounter);
            TaskCompletionSource<JsonElement> tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            _Pending[requestId] = tcs;

            try
            {
                object frame = behavior == LockBehavior.Wait
                    ? (object)new { type = "acquire", requestId = requestId, key = key, mode = mode.ToString(), behavior = "Wait", timeoutMs = timeoutMs, leaseMs = leaseMs }
                    : new { type = "acquire", requestId = requestId, key = key, mode = mode.ToString(), behavior = "FailFast", leaseMs = leaseMs };

                await SendFrameAsync(frame, token).ConfigureAwait(false);

                // A Wait acquire is decided server-side; allow it up to its timeout plus a margin.
                int awaitMs = behavior == LockBehavior.Wait ? timeoutMs + _Clutch.RequestTimeoutMs : _Clutch.RequestTimeoutMs;
                JsonElement response = await AwaitResponseAsync(tcs, awaitMs, token).ConfigureAwait(false);

                string type = GetString(response, "type");
                if (type == "acquired")
                {
                    string holderId = GetString(response, "holderId");
                    long fencingToken = response.TryGetProperty("fencingToken", out JsonElement ft) && ft.ValueKind == JsonValueKind.Number ? ft.GetInt64() : 0;
                    DateTime leaseExpiresUtc = response.TryGetProperty("leaseExpiresUtc", out JsonElement le) && le.TryGetDateTime(out DateTime dt)
                        ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                        : DateTime.UtcNow.AddMilliseconds(leaseMs);

                    return new LockHandle(key, mode, holderId, fencingToken, leaseExpiresUtc, Provider);
                }

                if (type == "denied")
                {
                    string result = GetString(response, "result");
                    AcquireResult ar = result == "Timeout" ? AcquireResult.Timeout : AcquireResult.Denied;
                    throw new LockDeniedException(key, ar);
                }

                throw new InvalidOperationException("Clutch acquire error: " + (GetString(response, "message") ?? type));
            }
            finally
            {
                _Pending.TryRemove(requestId, out _);
            }
        }

        /// <inheritdoc />
        protected override async Task<bool> HeartbeatCoreAsync(LockHandle handle, int leaseMs, CancellationToken token)
        {
            if (!_Ready || _Socket == null || _Socket.State != WebSocketState.Open) return false;

            DateTime sentUtc = DateTime.UtcNow;
            try
            {
                await SendFrameAsync(new { type = "heartbeat", holderIds = new[] { handle.HolderId } }, token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return false;
            }

            // The heartbeat response carries no request id, so confirm the renewal by observing a
            // fresh renewed-lease entry for this holder recorded after we sent.
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Min(_Clutch.RequestTimeoutMs, 5000));
            while (DateTime.UtcNow < deadline)
            {
                if (_RenewedAtUtc.TryGetValue(handle.HolderId, out DateTime at) && at >= sentUtc
                    && _RenewedLeaseUtc.TryGetValue(handle.HolderId, out DateTime lease) && lease > DateTime.UtcNow)
                {
                    handle.LeaseExpiresUtc = lease;
                    return true;
                }

                await Task.Delay(100, token).ConfigureAwait(false);
            }

            return false;
        }

        /// <inheritdoc />
        protected override async Task<bool> ReleaseCoreAsync(LockHandle handle, CancellationToken token)
        {
            // If the socket is gone the lock was already released by Clutch on disconnect.
            if (!_Ready || _Socket == null || _Socket.State != WebSocketState.Open) return true;

            string requestId = "r" + Interlocked.Increment(ref _RequestCounter);
            TaskCompletionSource<JsonElement> tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            _Pending[requestId] = tcs;

            try
            {
                await SendFrameAsync(new { type = "release", requestId = requestId, key = handle.Key, holderId = handle.HolderId }, token).ConfigureAwait(false);
                JsonElement response = await AwaitResponseAsync(tcs, _Clutch.RequestTimeoutMs, token).ConfigureAwait(false);
                return response.TryGetProperty("released", out JsonElement r) && r.ValueKind == JsonValueKind.True;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                _Pending.TryRemove(requestId, out _);
                _RenewedAtUtc.TryRemove(handle.HolderId, out _);
                _RenewedLeaseUtc.TryRemove(handle.HolderId, out _);
            }
        }

        /// <inheritdoc />
        protected override async Task<bool> ValidateCoreAsync(LockHandle handle, CancellationToken token)
        {
            // A successful renewal proves the holder still owns the lease at the authority.
            return await HeartbeatCoreAsync(handle, _LockConfig.DefaultLeaseMs, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _ReceiveCts?.Cancel(); } catch (Exception) { }
                try
                {
                    if (_Socket != null && _Socket.State == WebSocketState.Open)
                        _Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "shutdown", CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (Exception) { }

                _Socket?.Dispose();
                _ReceiveCts?.Dispose();
                _ConnectLock?.Dispose();
                _SendLock?.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Private-Methods

        private static string GetString(JsonElement root, string name)
        {
            return root.TryGetProperty(name, out JsonElement el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
        }

        private async Task EnsureConnectedAsync(CancellationToken token)
        {
            if (_Ready && _Socket != null && _Socket.State == WebSocketState.Open) return;

            await _ConnectLock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_Ready && _Socket != null && _Socket.State == WebSocketState.Open) return;

                // Tear down any dead connection. Held locks are already released server-side when the
                // old socket closed; their handles will fail validation and be marked lost.
                try { _ReceiveCts?.Cancel(); } catch (Exception) { }
                try { _Socket?.Abort(); } catch (Exception) { }
                _Socket?.Dispose();
                _Ready = false;
                FailAllPending("clutch connection reset");

                _Socket = new ClientWebSocket();
                _Socket.Options.SetRequestHeader("x-clutch-access-key", _Clutch.AccessKey);
                _ReceiveCts = new CancellationTokenSource();
                _WelcomeTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                await _Socket.ConnectAsync(_WsUri, token).ConfigureAwait(false);
                _ReceiveTask = Task.Run(() => ReceiveLoopAsync(_Socket, _ReceiveCts.Token));

                using (CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    timeoutCts.CancelAfter(_Clutch.RequestTimeoutMs);
                    Task completed = await Task.WhenAny(_WelcomeTcs.Task, Task.Delay(Timeout.Infinite, timeoutCts.Token)).ConfigureAwait(false);
                    if (completed != _WelcomeTcs.Task) throw new InvalidOperationException("Timed out waiting for Clutch welcome frame.");
                }

                _Ready = true;
                _Logging.Info("[ClutchLockManager] connected to " + _WsUri + " (session " + _SessionId + ")");
            }
            finally
            {
                _ConnectLock.Release();
            }
        }

        private async Task SendFrameAsync(object frame, CancellationToken token)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(frame);
            await _SendLock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await _Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
            }
            finally
            {
                _SendLock.Release();
            }
        }

        private async Task<JsonElement> AwaitResponseAsync(TaskCompletionSource<JsonElement> tcs, int timeoutMs, CancellationToken token)
        {
            using (CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                timeoutCts.CancelAfter(timeoutMs);
                Task completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, timeoutCts.Token)).ConfigureAwait(false);
                if (completed != tcs.Task) throw new TimeoutException("Timed out awaiting Clutch response.");
                return await tcs.Task.ConfigureAwait(false);
            }
        }

        private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken token)
        {
            byte[] buffer = new byte[16384];
            try
            {
                while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                _Ready = false;
                                FailAllPending("clutch socket closed");
                                return;
                            }
                            ms.Write(buffer, 0, result.Count);
                        }
                        while (!result.EndOfMessage);

                        DispatchFrame(ms.ToArray());
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                _Logging.Warn("[ClutchLockManager] receive loop ended: " + e.Message);
            }
            finally
            {
                _Ready = false;
                FailAllPending("clutch receive loop ended");
            }
        }

        private void DispatchFrame(byte[] payload)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(payload))
                {
                    JsonElement root = doc.RootElement;
                    string type = GetString(root, "type");

                    switch (type)
                    {
                        case "welcome":
                            _SessionId = GetString(root, "sessionId");
                            _WelcomeTcs?.TrySetResult(true);
                            return;

                        case "heartbeat":
                            if (root.TryGetProperty("renewed", out JsonElement renewed) && renewed.ValueKind == JsonValueKind.Array)
                            {
                                DateTime now = DateTime.UtcNow;
                                foreach (JsonElement entry in renewed.EnumerateArray())
                                {
                                    string holderId = GetString(entry, "holderId");
                                    if (String.IsNullOrEmpty(holderId)) continue;
                                    if (entry.TryGetProperty("leaseExpiresUtc", out JsonElement le) && le.TryGetDateTime(out DateTime dt))
                                        _RenewedLeaseUtc[holderId] = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                                    _RenewedAtUtc[holderId] = now;
                                }
                            }
                            return;

                        case "acquired":
                        case "denied":
                        case "released":
                        case "error":
                            string requestId = GetString(root, "requestId");
                            if (!String.IsNullOrEmpty(requestId) && _Pending.TryGetValue(requestId, out TaskCompletionSource<JsonElement> tcs))
                            {
                                tcs.TrySetResult(root.Clone());
                            }
                            return;

                        default:
                            return;
                    }
                }
            }
            catch (Exception e)
            {
                _Logging.Warn("[ClutchLockManager] failed to parse frame: " + e.Message);
            }
        }

        private void FailAllPending(string reason)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, TaskCompletionSource<JsonElement>> kvp in _Pending)
            {
                kvp.Value.TrySetException(new InvalidOperationException(reason));
            }
            _Pending.Clear();
        }

        #endregion
    }
}
