namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Less3.Locking;
    using Less3.Settings;
    using Less3.Storage;
    using Less3.Telemetry;
    using SyslogLogging;

    /// <summary>
    /// Cleanup manager for expired multipart uploads and other maintenance tasks.
    /// </summary>
    internal class CleanupManager : IDisposable
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SettingsBase _Settings = null;
        private LoggingModule _Logging = null;
        private ConfigManager _Config = null;
        private ILockManager _LockManager = null;
        private string _NodeId = null;
        private Timer _CleanupTimer = null;
        private bool _Disposed = false;

        private int _CleanupIntervalMs = 3600000;
        private int _OrphanGraceMs = 900000;
        private DateTime? _LastCleanupRunUtc = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the cleanup manager.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="config">Configuration manager.</param>
        /// <param name="lockManager">Lock manager, used to elect a single cleanup leader per cluster.</param>
        /// <param name="nodeId">Identifier of this node.</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        public CleanupManager(
            SettingsBase settings,
            LoggingModule logging,
            ConfigManager config,
            ILockManager lockManager,
            string nodeId)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Config = config ?? throw new ArgumentNullException(nameof(config));
            _LockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
            _NodeId = String.IsNullOrEmpty(nodeId) ? "unknown" : nodeId;

            _CleanupIntervalMs = _Settings.CleanupIntervalMs;
            _CleanupTimer = new Timer(CleanupCallback, null, _CleanupIntervalMs, _CleanupIntervalMs);

            _Logging.Info("CleanupManager initialized with cleanup interval " + _CleanupIntervalMs + "ms");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Cleanup interval in milliseconds.
        /// Default value is 3600000 (1 hour).
        /// Minimum value is 60000 (1 minute).
        /// </summary>
        public int CleanupIntervalMs
        {
            get { return _CleanupIntervalMs; }
            set
            {
                if (value < 60000)
                    throw new ArgumentOutOfRangeException(nameof(value), "Cleanup interval must be at least 60000ms (1 minute).");

                _CleanupIntervalMs = value;
                _Settings.CleanupIntervalMs = value;

                if (_CleanupTimer != null)
                {
                    _CleanupTimer.Change(_CleanupIntervalMs, _CleanupIntervalMs);
                    _Logging.Info("CleanupManager interval updated to " + _CleanupIntervalMs + "ms");
                }
            }
        }

        /// <summary>
        /// Last cleanup run timestamp in UTC.
        /// </summary>
        public DateTime? LastCleanupRunUtc
        {
            get { return _LastCleanupRunUtc; }
        }

        /// <summary>
        /// Grace period, in milliseconds, below which a temporary or part file is never deleted as
        /// an orphan even if no active upload row references it. Protects files that are being
        /// staged right now on this or another node while their database rows are not yet committed.
        /// Default value is 900000 (15 minutes). Minimum value is 60000 (1 minute).
        /// </summary>
        public int OrphanGraceMs
        {
            get { return _OrphanGraceMs; }
            set
            {
                if (value < 60000) throw new ArgumentOutOfRangeException(nameof(value), "OrphanGraceMs must be at least 60000ms (1 minute).");
                _OrphanGraceMs = value;
            }
        }

        /// <summary>
        /// Run all cleanup tasks immediately.
        /// </summary>
        /// <returns>Maintenance action result.</returns>
        public MaintenanceActionResult RunCleanupCycle()
        {
            MaintenanceActionResult result = new MaintenanceActionResult();
            result.Action = "run-cleanup";
            result.ExpiredUploadCount = CleanupExpiredUploads();
            result.PurgedRequestHistoryCount = CleanupOldRequestHistory();
            result.GeneratedUtc = DateTime.UtcNow;
            _LastCleanupRunUtc = result.GeneratedUtc;
            return result;
        }

        /// <summary>
        /// Purge request history older than the supplied cutoff.
        /// </summary>
        /// <param name="cutoffUtc">UTC cutoff.</param>
        /// <returns>Maintenance action result.</returns>
        public MaintenanceActionResult PurgeRequestHistory(DateTime cutoffUtc)
        {
            MaintenanceActionResult result = new MaintenanceActionResult();
            result.Action = "purge-request-history";
            result.CutoffUtc = cutoffUtc;
            result.PurgedRequestHistoryCount = DeleteRequestHistoryOlderThan(cutoffUtc);
            result.GeneratedUtc = DateTime.UtcNow;
            return result;
        }

        /// <summary>
        /// Delete orphaned temporary upload files that are not associated with active upload rows.
        /// </summary>
        /// <returns>Maintenance action result.</returns>
        public MaintenanceActionResult CleanupTempUploads()
        {
            MaintenanceActionResult result = new MaintenanceActionResult();
            result.Action = "cleanup-temp-uploads";
            result.DeletedTempFileCount = DeleteOrphanTempFiles();
            result.GeneratedUtc = DateTime.UtcNow;
            return result;
        }

        /// <summary>
        /// Dispose of the cleanup manager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Dispose of the cleanup manager.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                if (_CleanupTimer != null)
                {
                    _CleanupTimer.Dispose();
                    _CleanupTimer = null;
                }
            }

            _Disposed = true;
        }

        private void CleanupCallback(object state)
        {
            // Elect a single cleanup leader per cluster. In single-node mode the in-process lock is
            // always granted; in a cluster only the node that wins the lease runs the cycle, so no
            // node ever deletes another node's in-flight files.
            LockHandle leader = null;
            try
            {
                try
                {
                    leader = _LockManager.AcquireAsync(LockKeys.ClusterCleanup, LockMode.Write, new AcquireOptions(), CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (LockDeniedException)
                {
                    _Logging.Debug("CleanupManager skipping cycle; another node holds the cleanup lease");
                    return;
                }

                _Logging.Debug("CleanupManager starting cleanup cycle (leader " + _NodeId + ")");

                RunCleanupCycle();
                Less3Telemetry.CleanupLeaderPass(0);
                _LastCleanupRunUtc = DateTime.UtcNow;

                _Logging.Debug("CleanupManager completed cleanup cycle");
            }
            catch (Exception e)
            {
                _Logging.Exception(e, "CleanupManager", "CleanupCallback");
            }
            finally
            {
                if (leader != null)
                {
                    try { _LockManager.ReleaseAsync(leader, CancellationToken.None).GetAwaiter().GetResult(); }
                    catch (Exception) { }
                }
            }
        }

        internal int CleanupExpiredUploads()
        {
            try
            {
                List<Upload> allUploads = _Config.GetUploads();
                if (allUploads == null || allUploads.Count == 0)
                {
                    _Logging.Debug("CleanupManager found no uploads to evaluate");
                    return 0;
                }

                DateTime now = DateTime.UtcNow;
                int cleanedCount = 0;

                foreach (Upload upload in allUploads)
                {
                    if (upload.ExpirationUtc < now)
                    {
                        try
                        {
                            _Logging.Debug("CleanupManager cleaning up expired upload " + upload.Id + " for key " + upload.Key);

                            List<UploadPart> parts = _Config.GetUploadPartsByUploadId(upload.TenantId, upload.Id);
                            if (parts != null && parts.Count > 0)
                            {
                                foreach (UploadPart part in parts)
                                {
                                    string partFile = GetPartFilePath(upload.BucketId, upload.Id, part.PartNumber);
                                    if (File.Exists(partFile))
                                    {
                                        File.Delete(partFile);
                                        _Logging.Debug("CleanupManager deleted part file " + partFile);
                                    }
                                }
                            }

                            _Config.DeleteUploadParts(upload.TenantId, upload.Id);
                            _Config.DeleteUpload(upload.TenantId, upload.Id);

                            cleanedCount++;
                            _Logging.Info("CleanupManager cleaned up expired upload " + upload.Id);
                        }
                        catch (Exception e)
                        {
                            _Logging.Exception(e, "CleanupManager", "CleanupExpiredUploads processing upload " + upload.Id);
                        }
                    }
                }

                if (cleanedCount > 0)
                {
                    _Logging.Info("CleanupManager cleaned up " + cleanedCount + " expired upload(s)");
                }
                else
                {
                    _Logging.Debug("CleanupManager found no expired uploads to clean");
                }

                return cleanedCount;
            }
            catch (Exception e)
            {
                _Logging.Exception(e, "CleanupManager", "CleanupExpiredUploads");
                return 0;
            }
        }

        internal int CleanupOldRequestHistory()
        {
            try
            {
                DateTime cutoff = DateTime.UtcNow.AddDays(-_Settings.RequestHistoryRetentionDays);
                int deleted = DeleteRequestHistoryOlderThan(cutoff);
                _Logging.Debug("CleanupManager purged request history entries older than " + _Settings.RequestHistoryRetentionDays + " days");
                return deleted;
            }
            catch (Exception e)
            {
                _Logging.Exception(e, "CleanupManager", "CleanupOldRequestHistory");
                return 0;
            }
        }

        private int DeleteRequestHistoryOlderThan(DateTime cutoffUtc)
        {
            int count = _Config.CountRequestHistoriesOlderThan(cutoffUtc);
            _Config.DeleteRequestHistoriesOlderThan(cutoffUtc);
            return count;
        }

        private int DeleteOrphanTempFiles()
        {
            HashSet<string> activeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<Upload> uploads = _Config.GetUploads();
            if (uploads != null)
            {
                foreach (Upload upload in uploads)
                {
                    List<UploadPart> parts = _Config.GetUploadPartsByUploadId(upload.TenantId, upload.Id);
                    if (parts == null) continue;

                    foreach (UploadPart part in parts)
                    {
                        activeFiles.Add(Path.GetFullPath(GetPartFilePath(upload.BucketId, upload.Id, part.PartNumber)));
                    }
                }
            }

            // Scan both the temp staging directory and the (possibly separate, possibly shared)
            // parts directory. In cluster mode, never touch a file younger than the orphan grace
            // period, because it may be a part or staging file being written right now whose
            // database row has not yet been committed on this or another node. In single-node mode
            // there is no such cross-node race, so orphans are removed immediately.
            bool applyGrace = _Settings.Cluster != null && _Settings.Cluster.Enabled;
            DateTime cutoff = DateTime.UtcNow.AddMilliseconds(-_OrphanGraceMs);
            HashSet<string> scanned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int deleted = 0;

            foreach (string dir in new[] { _Settings.Storage.TempDirectory, _Settings.Storage.GetEffectivePartsDirectory() })
            {
                if (String.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;

                foreach (string file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    string fullPath = Path.GetFullPath(file);
                    if (!scanned.Add(fullPath)) continue;
                    if (activeFiles.Contains(fullPath)) continue;

                    try
                    {
                        if (applyGrace && File.GetLastWriteTimeUtc(fullPath) > cutoff) continue;
                        File.Delete(fullPath);
                        deleted++;
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn("CleanupManager failed to delete temporary file " + fullPath + ": " + e.Message);
                    }
                }
            }

            return deleted;
        }

        private string GetPartFilePath(string bucketId, string uploadId, int partNumber)
        {
            return MultipartPaths.PartFilePath(_Settings.Storage, bucketId, uploadId, partNumber);
        }

        #endregion
    }
}
