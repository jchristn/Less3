namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    using Less3.Settings;
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
        private Timer _CleanupTimer = null;
        private bool _Disposed = false;

        private int _CleanupIntervalMs = 3600000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the cleanup manager.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="config">Configuration manager.</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        public CleanupManager(
            SettingsBase settings,
            LoggingModule logging,
            ConfigManager config)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Config = config ?? throw new ArgumentNullException(nameof(config));

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

                if (_CleanupTimer != null)
                {
                    _CleanupTimer.Change(_CleanupIntervalMs, _CleanupIntervalMs);
                    _Logging.Info("CleanupManager interval updated to " + _CleanupIntervalMs + "ms");
                }
            }
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
            try
            {
                _Logging.Debug("CleanupManager starting cleanup cycle");

                CleanupExpiredUploads();

                _Logging.Debug("CleanupManager completed cleanup cycle");
            }
            catch (Exception e)
            {
                _Logging.Exception(e, "CleanupManager", "CleanupCallback");
            }
        }

        private void CleanupExpiredUploads()
        {
            try
            {
                List<Upload> allUploads = _Config.GetUploads();
                if (allUploads == null || allUploads.Count == 0)
                {
                    _Logging.Debug("CleanupManager found no uploads to evaluate");
                    return;
                }

                DateTime now = DateTime.UtcNow;
                int cleanedCount = 0;

                foreach (Upload upload in allUploads)
                {
                    if (upload.ExpirationUtc < now)
                    {
                        try
                        {
                            _Logging.Debug("CleanupManager cleaning up expired upload " + upload.GUID + " for key " + upload.Key);

                            List<UploadPart> parts = _Config.GetUploadPartsByUploadGuid(upload.GUID);
                            if (parts != null && parts.Count > 0)
                            {
                                foreach (UploadPart part in parts)
                                {
                                    string partFile = GetPartFilePath(upload.BucketGUID, upload.GUID, part.PartNumber);
                                    if (File.Exists(partFile))
                                    {
                                        File.Delete(partFile);
                                        _Logging.Debug("CleanupManager deleted part file " + partFile);
                                    }
                                }
                            }

                            _Config.DeleteUploadParts(upload.GUID);
                            _Config.DeleteUpload(upload.GUID);

                            cleanedCount++;
                            _Logging.Info("CleanupManager cleaned up expired upload " + upload.GUID);
                        }
                        catch (Exception e)
                        {
                            _Logging.Exception(e, "CleanupManager", "CleanupExpiredUploads processing upload " + upload.GUID);
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
            }
            catch (Exception e)
            {
                _Logging.Exception(e, "CleanupManager", "CleanupExpiredUploads");
            }
        }

        private string GetPartFilePath(string bucketGuid, string uploadGuid, int partNumber)
        {
            if (String.IsNullOrEmpty(bucketGuid)) throw new ArgumentNullException(nameof(bucketGuid));
            if (String.IsNullOrEmpty(uploadGuid)) throw new ArgumentNullException(nameof(uploadGuid));
            if (partNumber < 1) throw new ArgumentOutOfRangeException(nameof(partNumber));

            string tempDir = _Settings.Storage.TempDirectory;
            if (!tempDir.EndsWith("/") && !tempDir.EndsWith("\\"))
            {
                tempDir += "/";
            }

            return tempDir + bucketGuid + "-upload-" + uploadGuid + "-part-" + partNumber;
        }

        #endregion
    }
}
