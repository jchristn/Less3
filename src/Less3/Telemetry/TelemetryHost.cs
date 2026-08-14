namespace Less3.Telemetry
{
    using System;
    using Microsoft.Extensions.Logging;
    using Less3.Settings;
    using Radiant;
    using SyslogLogging;
    using WatsonWebserver.Core.Telemetry;

    /// <summary>
    /// Owns the process-wide telemetry host. Subscribes a Radiant host to Less3's own meters and to
    /// Watson's meter and activity source, exposes a Prometheus scrape endpoint, and optionally
    /// pushes OpenTelemetry metrics, traces, and logs to a collector. When observability is disabled
    /// the meters still exist but nothing collects them, which is effectively free.
    /// Thread-safe.
    /// </summary>
    public sealed class TelemetryHost : IDisposable
    {
        #region Private-Members

        private readonly RadiantHost _Host;
        private readonly LoggingModule _Logging;
        private Action<LogEntry> _LogBridge = null;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        private TelemetryHost(RadiantHost host, LoggingModule logging, Action<LogEntry> logBridge)
        {
            _Host = host;
            _Logging = logging;
            _LogBridge = logBridge;
        }

        /// <summary>
        /// Start the telemetry host from settings. Returns null when observability is disabled.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="nodeId">Node identifier, reported as the telemetry service instance id.</param>
        /// <param name="logging">Logging module.</param>
        /// <returns>A started telemetry host, or null when disabled.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        public static TelemetryHost Start(SettingsBase settings, string nodeId, LoggingModule logging)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            ObservabilitySettings obs = settings.Observability;
            if (obs == null || !obs.Enabled)
            {
                return null;
            }

            RadiantSettings radiant = new RadiantSettings(obs.ServiceName);
            radiant.Enable = true;
            if (!String.IsNullOrEmpty(nodeId)) radiant.ServiceInstanceId = nodeId;

            // Subscribe to Less3's own meters (exported via OTLP to the collector) and to Watson's
            // activity source for traces. Watson's own METER is not exported here — its metrics are
            // served directly on the main-port /metrics endpoint and scraped there — so it is not
            // double-counted between the node scrape and the collector.
            foreach (string meterName in Less3Telemetry.AllMeterNames) radiant.Sources.AddMeter(meterName);
            radiant.Sources.AddActivitySource(Less3Telemetry.ActivitySourceName);
            radiant.Sources.AddActivitySource(WatsonTelemetryNames.ActivitySourceName);

            radiant.Metrics.IncludeRuntime = true;
            radiant.Metrics.IncludeProcess = true;

            // Radiant's own in-process Prometheus listener (an OpenTelemetry HttpListener) cannot
            // bind all interfaces on Linux — wildcard hosts fail URI parsing and "0.0.0.0" fails the
            // listener, and a specific bound host rejects mismatched Host headers. So the reachable
            // per-node /metrics endpoint is served by Watson on the main webserver port instead
            // (enabled in Program.cs from the same PrometheusEnabled flag), and Radiant exports the
            // full Less3.* metric set over OTLP to the collector, which Prometheus scrapes.
            radiant.Prometheus.Enable = false;

            radiant.Otlp.Enable = obs.OtlpEnabled;
            if (obs.OtlpEnabled && !String.IsNullOrEmpty(obs.OtlpEndpoint)) radiant.Otlp.Endpoint = obs.OtlpEndpoint;

            radiant.Logs.Enable = obs.ExportLogs && obs.OtlpEnabled;
            // Export the rendered message text so Loki shows the log line, not just structured state.
            radiant.Logs.IncludeFormattedMessage = true;

            // Telemetry must never take down the server. If the pipeline fails to build (for
            // example a Prometheus port already in use, or an exporter that cannot bind), log it and
            // continue with telemetry disabled rather than crashing the data plane.
            RadiantHost host;
            try
            {
                host = RadiantHost.Start(radiant);
            }
            catch (Exception e)
            {
                logging.Warn("[TelemetryHost] telemetry disabled; failed to start: " + Flatten(e));
                return null;
            }

            // Bridge Less3's SyslogLogging output into the OTLP log pipeline: every emitted log entry
            // is forwarded to a Radiant ILogger, which exports it over OTLP to the collector and on to
            // Loki. Requires SyslogLogging >= 2.2.0 (the MessageLogged event). The handler is exception-
            // isolated inside SyslogLogging (routed to OnLoggingError), so a telemetry hiccup can never
            // disrupt logging.
            Action<LogEntry> bridge = null;
            if (radiant.Logs.Enable)
            {
                ILogger otel = host.CreateLogger(obs.ServiceName);
                bridge = entry => ForwardToOtel(otel, entry);
                logging.MessageLogged += bridge;
            }

            logging.Info("[TelemetryHost] started; service=" + obs.ServiceName + " instance=" + nodeId +
                (obs.PrometheusEnabled ? " prometheus=" + radiant.Prometheus.ToScrapeUrl() : "") +
                (obs.OtlpEnabled ? " otlp=" + obs.OtlpEndpoint : "") +
                (radiant.Logs.Enable ? " logs=otlp" : ""));

            return new TelemetryHost(host, logging, bridge);
        }

        private static void ForwardToOtel(ILogger otel, LogEntry entry)
        {
            if (otel == null || entry == null) return;

            LogLevel level = MapSeverity(entry.Severity);
            if (!otel.IsEnabled(level)) return;

            // State is the raw message string; the formatter returns it verbatim so arbitrary log text
            // (including braces) is never interpreted as a message template.
            otel.Log(level, default, entry.Message, entry.Exception, static (state, _) => state);
        }

        private static LogLevel MapSeverity(Severity severity)
        {
            switch (severity)
            {
                case Severity.Debug: return LogLevel.Debug;
                case Severity.Info: return LogLevel.Information;
                case Severity.Warn: return LogLevel.Warning;
                case Severity.Error: return LogLevel.Error;
                case Severity.Alert: return LogLevel.Critical;
                case Severity.Critical: return LogLevel.Critical;
                case Severity.Emergency: return LogLevel.Critical;
                default: return LogLevel.Information;
            }
        }

        private static string Flatten(Exception e)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            Exception current = e;
            int depth = 0;
            while (current != null && depth < 5)
            {
                if (depth > 0) sb.Append(" -> ");
                sb.Append(current.GetType().Name + ": " + current.Message);
                current = current.InnerException;
                depth++;
            }
            return sb.ToString();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;

            if (_LogBridge != null)
            {
                try { _Logging.MessageLogged -= _LogBridge; }
                catch (Exception e) { _Logging?.Warn("[TelemetryHost] log-bridge detach encountered: " + e.Message); }
                _LogBridge = null;
            }

            try { _Host?.Dispose(); }
            catch (Exception e) { _Logging?.Warn("[TelemetryHost] dispose encountered: " + e.Message); }

            _Disposed = true;
        }

        #endregion
    }
}
