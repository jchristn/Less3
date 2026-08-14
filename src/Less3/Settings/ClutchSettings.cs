namespace Less3.Settings
{
    using System;

    /// <summary>
    /// Configuration for the optional Clutch distributed lock provider. These values are read
    /// only when <see cref="ClusterSettings.LockProvider"/> is <see cref="LockProviderEnum.Clutch"/>.
    /// Less3 talks to Clutch over its native WebSocket lock protocol (one persistent connection per
    /// node); no Clutch SDK dependency is required. <see cref="Endpoint"/> is given as an http(s) URL
    /// and is upgraded to ws(s) internally.
    /// </summary>
    public class ClutchSettings
    {
        #region Public-Members

        /// <summary>
        /// Base URL of the Clutch server, for example "http://clutch:8080". Null or empty when
        /// the Clutch provider is not in use.
        /// </summary>
        public string Endpoint { get; set; } = null;

        /// <summary>
        /// Clutch access key used to obtain a bearer token for lock operations.
        /// </summary>
        public string AccessKey { get; set; } = null;

        /// <summary>
        /// Clutch tenant identifier whose lock namespace Less3 uses.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// HTTP request timeout for Clutch calls, in milliseconds.
        /// Default value is 15000. Minimum value is 1000.
        /// </summary>
        public int RequestTimeoutMs
        {
            get { return _RequestTimeoutMs; }
            set
            {
                if (value < 1000) throw new ArgumentOutOfRangeException(nameof(RequestTimeoutMs), "RequestTimeoutMs must be at least 1000.");
                _RequestTimeoutMs = value;
            }
        }

        #endregion

        #region Private-Members

        private int _RequestTimeoutMs = 15000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with default settings.
        /// </summary>
        public ClutchSettings()
        {
        }

        #endregion
    }
}
