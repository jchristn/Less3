namespace Less3.Settings
{
    using System;

    /// <summary>
    /// Observability configuration. Less3 instruments its own code with base-class-library meters
    /// and activity sources under the "Less3.*" names and subscribes to Watson's "Watson" meter.
    /// A Radiant host collects those, optionally serves a Prometheus scrape endpoint, and can push
    /// OpenTelemetry traces, metrics, and logs to a collector. When <see cref="Enabled"/> is false
    /// (the native single-node default), the meters still exist but nothing collects them, which is
    /// effectively free.
    /// </summary>
    public class ObservabilitySettings
    {
        #region Public-Members

        /// <summary>
        /// Master switch for the telemetry host. Default value is false. The Docker image enables
        /// it. Individual exporters below are still gated by their own flags.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Service name reported on the telemetry resource. Default value is "less3".
        /// </summary>
        public string ServiceName
        {
            get { return _ServiceName; }
            set { _ServiceName = String.IsNullOrEmpty(value) ? throw new ArgumentNullException(nameof(ServiceName)) : value; }
        }

        /// <summary>
        /// Enable a Prometheus scrape endpoint at <see cref="PrometheusPath"/> served by the Watson
        /// webserver on the main listener port (the same port that serves S3 and REST). That port
        /// binds all interfaces, so the endpoint is reachable across a network for scraping. It
        /// exposes the webserver's HTTP/server metrics; the Less3.* domain metrics are exported via
        /// OTLP (<see cref="OtlpEnabled"/>) to a collector instead. Default value is true (effective
        /// only when <see cref="Enabled"/> is also true).
        /// </summary>
        public bool PrometheusEnabled { get; set; } = true;

        /// <summary>
        /// Reserved. Intended hostname for an OpenTelemetry in-process Prometheus listener, which is
        /// not usable on Linux (wildcard hosts fail URI parsing and a specific bound host rejects
        /// mismatched Host headers). It is NOT used: the scrape endpoint is served by the Watson
        /// webserver on the main listener port. Default value is "localhost".
        /// </summary>
        public string PrometheusHostname { get; set; } = "localhost";

        /// <summary>
        /// Reserved. Intended port for an OpenTelemetry in-process Prometheus listener; NOT used, as
        /// the scrape endpoint is served on the main webserver port. Default value is 9464. Minimum
        /// value is 1. Maximum value is 65535.
        /// </summary>
        public int PrometheusPort
        {
            get { return _PrometheusPort; }
            set
            {
                if (value < 1 || value > 65535) throw new ArgumentOutOfRangeException(nameof(PrometheusPort), "PrometheusPort must be between 1 and 65535.");
                _PrometheusPort = value;
            }
        }

        /// <summary>
        /// URL path of the Prometheus scrape endpoint. Default value is "/metrics".
        /// </summary>
        public string PrometheusPath
        {
            get { return _PrometheusPath; }
            set { _PrometheusPath = String.IsNullOrEmpty(value) ? throw new ArgumentNullException(nameof(PrometheusPath)) : value; }
        }

        /// <summary>
        /// Enable OTLP export of metrics and traces to a collector. Default value is false.
        /// </summary>
        public bool OtlpEnabled { get; set; } = false;

        /// <summary>
        /// OTLP collector endpoint, for example "http://otel-collector:4317". Read only when
        /// <see cref="OtlpEnabled"/> is true.
        /// </summary>
        public string OtlpEndpoint { get; set; } = "http://localhost:4317";

        /// <summary>
        /// Export application logs through the OpenTelemetry logging pipeline (in addition to the
        /// existing syslog sink). Default value is false. Requires <see cref="OtlpEnabled"/>.
        /// </summary>
        public bool ExportLogs { get; set; } = false;

        #endregion

        #region Private-Members

        private string _ServiceName = "less3";
        private int _PrometheusPort = 9464;
        private string _PrometheusPath = "/metrics";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with default settings.
        /// </summary>
        public ObservabilitySettings()
        {
        }

        #endregion
    }
}
