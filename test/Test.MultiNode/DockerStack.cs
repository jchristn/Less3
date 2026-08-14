namespace Test.MultiNode
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Generates a temporary multi-node stack (PostgreSQL, two Less3 nodes built from source, an
    /// nginx load balancer, and a Clutch server) on randomized host ports, and controls its
    /// lifecycle through the docker compose CLI. All host ports are randomized so the stack never
    /// collides with anything already running on the machine.
    /// </summary>
    public sealed class DockerStack
    {
        /// <summary>Host port published for the nginx load balancer.</summary>
        public int LbPort { get; }

        /// <summary>Host port published for node 1 directly.</summary>
        public int Node1Port { get; }

        /// <summary>Host port published for node 2 directly.</summary>
        public int Node2Port { get; }

        /// <summary>Host port published for PostgreSQL.</summary>
        public int PostgresPort { get; }

        /// <summary>Host port published for the Clutch server.</summary>
        public int ClutchPort { get; }

        /// <summary>Compose project name (isolates this run's containers/network/volumes).</summary>
        public string ProjectName { get; }

        private readonly string _TempDir;
        private readonly string _ComposeFile;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="tempDir">Directory to write generated configs into.</param>
        /// <param name="projectName">Unique compose project name.</param>
        /// <param name="lbPort">nginx host port.</param>
        /// <param name="node1Port">node 1 host port (also serves the Prometheus /metrics endpoint).</param>
        /// <param name="node2Port">node 2 host port (also serves the Prometheus /metrics endpoint).</param>
        /// <param name="postgresPort">PostgreSQL host port.</param>
        /// <param name="clutchPort">Clutch host port.</param>
        public DockerStack(string tempDir, string projectName, int lbPort, int node1Port, int node2Port, int postgresPort, int clutchPort)
        {
            _TempDir = tempDir;
            ProjectName = projectName;
            LbPort = lbPort;
            Node1Port = node1Port;
            Node2Port = node2Port;
            PostgresPort = postgresPort;
            ClutchPort = clutchPort;
            _ComposeFile = Path.Combine(_TempDir, "compose.yaml");
        }

        /// <summary>
        /// Write the generated node config, nginx config, and compose file.
        /// </summary>
        public void WriteConfigs()
        {
            Directory.CreateDirectory(_TempDir);
            File.WriteAllText(Path.Combine(_TempDir, "system.node.json"), NodeConfigJson());
            File.WriteAllText(Path.Combine(_TempDir, "nginx.conf"), NginxConf());
            File.WriteAllText(Path.Combine(_TempDir, "clutch.json"), ClutchConfigJson());
            File.WriteAllText(_ComposeFile, ComposeYaml());
        }

        /// <summary>
        /// Build and start the given services (or the whole stack when none are named), returning
        /// the compose exit code and captured output.
        /// </summary>
        /// <param name="services">Space-separated service names, or empty for all.</param>
        /// <param name="build">Whether to pass --build.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Process result.</returns>
        public async Task<ProcessRunResult> UpAsync(string services, bool build, CancellationToken token)
        {
            string buildFlag = build ? " --build" : "";
            string svc = String.IsNullOrEmpty(services) ? "" : " " + services;
            return await RunAsync("compose -p " + ProjectName + " -f \"" + _ComposeFile + "\" up -d" + buildFlag + svc, 900000, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Stop and remove the stack, including volumes.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        public async Task DownAsync(CancellationToken token)
        {
            await RunAsync("compose -p " + ProjectName + " -f \"" + _ComposeFile + "\" down -v --remove-orphans", 180000, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Forcibly stop a single service (simulating a node crash).
        /// </summary>
        /// <param name="service">Service name.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task KillAsync(string service, CancellationToken token)
        {
            await RunAsync("compose -p " + ProjectName + " -f \"" + _ComposeFile + "\" kill " + service, 60000, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Restart a previously killed service (or start it for the first time).
        /// </summary>
        /// <param name="service">Service name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Process result.</returns>
        public async Task<ProcessRunResult> StartAsync(string service, CancellationToken token)
        {
            return await RunAsync("compose -p " + ProjectName + " -f \"" + _ComposeFile + "\" up -d " + service, 120000, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Capture recent logs from a service for failure diagnostics.
        /// </summary>
        /// <param name="service">Service name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Log text.</returns>
        public async Task<string> LogsAsync(string service, CancellationToken token)
        {
            ProcessRunResult result = await RunAsync("compose -p " + ProjectName + " -f \"" + _ComposeFile + "\" logs --tail 40 " + service, 30000, token).ConfigureAwait(false);
            return result.Output;
        }

        private async Task<ProcessRunResult> RunAsync(string arguments, int timeoutMs, CancellationToken token)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _TempDir
            };

            StringBuilder output = new StringBuilder();

            using (Process process = new Process())
            {
                process.StartInfo = psi;
                process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    timeoutCts.CancelAfter(timeoutMs);
                    try
                    {
                        await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        try { process.Kill(true); } catch (Exception) { }
                        throw new TimeoutException("docker " + arguments + " timed out after " + timeoutMs + "ms. Output:\n" + output);
                    }
                }

                return new ProcessRunResult { ExitCode = process.ExitCode, Output = output.ToString() };
            }
        }

        private string ComposeYaml()
        {
            string context = RepoPaths.SrcDir.Replace("\\", "/");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("name: " + ProjectName);
            sb.AppendLine("services:");

            sb.AppendLine("  postgres:");
            sb.AppendLine("    image: 'postgres:17'");
            sb.AppendLine("    environment:");
            sb.AppendLine("      - POSTGRES_USER=postgres");
            sb.AppendLine("      - POSTGRES_PASSWORD=postgres");
            sb.AppendLine("      - POSTGRES_DB=less3");
            sb.AppendLine("    ports:");
            sb.AppendLine("      - \"" + PostgresPort + ":5432\"");
            sb.AppendLine("    healthcheck:");
            sb.AppendLine("      test: [\"CMD-SHELL\", \"pg_isready -U postgres -d less3\"]");
            sb.AppendLine("      interval: 3s");
            sb.AppendLine("      timeout: 3s");
            sb.AppendLine("      retries: 20");
            sb.AppendLine("      start_period: 5s");

            AppendNode(sb, "less3-node1", Node1Port, context);
            AppendNode(sb, "less3-node2", Node2Port, context);

            sb.AppendLine("  nginx:");
            sb.AppendLine("    image: 'nginx:1.27'");
            sb.AppendLine("    volumes:");
            sb.AppendLine("      - ./nginx.conf:/etc/nginx/nginx.conf:ro");
            sb.AppendLine("    ports:");
            sb.AppendLine("      - \"" + LbPort + ":8000\"");
            sb.AppendLine("    depends_on:");
            sb.AppendLine("      less3-node1:");
            sb.AppendLine("        condition: service_healthy");
            sb.AppendLine("      less3-node2:");
            sb.AppendLine("        condition: service_healthy");

            sb.AppendLine("  clutch:");
            sb.AppendLine("    image: 'jchristn77/clutch-server:v0.2.0'");
            sb.AppendLine("    environment:");
            sb.AppendLine("      - CLUTCH_NODE_ID=clutch-node1");
            sb.AppendLine("      - CLUTCH_DB_TYPE=Postgresql");
            sb.AppendLine("      - CLUTCH_DB_HOST=postgres");
            sb.AppendLine("      - CLUTCH_DB_PORT=5432");
            sb.AppendLine("      - CLUTCH_DB_DATABASE=less3");
            sb.AppendLine("      - CLUTCH_DB_USERNAME=postgres");
            sb.AppendLine("      - CLUTCH_DB_PASSWORD=postgres");
            sb.AppendLine("    volumes:");
            sb.AppendLine("      - ./clutch.json:/app/clutch.json");
            sb.AppendLine("    ports:");
            sb.AppendLine("      - \"" + ClutchPort + ":8080\"");
            sb.AppendLine("    depends_on:");
            sb.AppendLine("      postgres:");
            sb.AppendLine("        condition: service_healthy");

            sb.AppendLine("volumes:");
            sb.AppendLine("  less3-data:");
            sb.AppendLine("  pgdata:");

            return sb.ToString();
        }

        private void AppendNode(StringBuilder sb, string name, int hostPort, string context)
        {
            sb.AppendLine("  " + name + ":");
            sb.AppendLine("    build:");
            sb.AppendLine("      context: \"" + context + "\"");
            sb.AppendLine("      dockerfile: Less3/Dockerfile");
            sb.AppendLine("    image: 'less3-multinode-test:latest'");
            sb.AppendLine("    environment:");
            sb.AppendLine("      - LESS3_NODE_ID=" + name);
            sb.AppendLine("    volumes:");
            sb.AppendLine("      - ./system.node.json:/app/system.json");
            sb.AppendLine("      - less3-data:/less3");
            sb.AppendLine("    ports:");
            sb.AppendLine("      - \"" + hostPort + ":8000\"");
            sb.AppendLine("    healthcheck:");
            sb.AppendLine("      test: [\"CMD-SHELL\", \"curl -sf http://localhost:8000/healthz || exit 1\"]");
            sb.AppendLine("      interval: 3s");
            sb.AppendLine("      timeout: 3s");
            sb.AppendLine("      retries: 20");
            sb.AppendLine("      start_period: 15s");
            sb.AppendLine("    depends_on:");
            sb.AppendLine("      postgres:");
            sb.AppendLine("        condition: service_healthy");
        }

        private string ClutchConfigJson()
        {
            // Rest.Hostname "*" binds the REST + WebSocket listener on all interfaces so the lock
            // WebSocket is reachable through the published host port (the image's baked-in default
            // would otherwise bind loopback only). Clutch creates its own tables and seeds the
            // default access key on first boot against the shared PostgreSQL. Telemetry and MCP are
            // off since this minimal stack ships no collector.
            return "{\n" +
                "  \"NodeId\": \"clutch-node1\",\n" +
                "  \"Rest\": { \"Hostname\": \"*\", \"Port\": 8080, \"Ssl\": false },\n" +
                "  \"Database\": { \"Type\": \"Postgresql\", \"Host\": \"postgres\", \"Port\": 5432, \"DatabaseName\": \"less3\", \"Username\": \"postgres\", \"Password\": \"postgres\", \"ManageSchema\": true, \"MaxPoolSize\": 50 },\n" +
                "  \"Logging\": { \"ConsoleLogging\": true, \"FileLogging\": false, \"MinimumSeverity\": 1 },\n" +
                "  \"Auth\": { \"Issuer\": \"clutch\", \"SigningKey\": \"change-me-in-production\", \"TokenLifetimeMinutes\": 60, \"AdminApiKey\": \"clutchadmin\" },\n" +
                "  \"Lock\": { \"DefaultLeaseMs\": 30000, \"MaxLeaseMs\": 300000, \"MaxHoldMs\": 3600000, \"MaxWaitMs\": 60000, \"WaiterPollMs\": 1000, \"SweepIntervalMs\": 1000 },\n" +
                "  \"Telemetry\": { \"Enabled\": false },\n" +
                "  \"Mcp\": { \"Enable\": false }\n" +
                "}\n";
        }

        private string NginxConf()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("worker_processes auto;");
            sb.AppendLine("events { worker_connections 1024; }");
            sb.AppendLine("http {");
            sb.AppendLine("  upstream less3_nodes {");
            sb.AppendLine("    least_conn;");
            sb.AppendLine("    server less3-node1:8000 max_fails=1 fail_timeout=5s;");
            sb.AppendLine("    server less3-node2:8000 max_fails=1 fail_timeout=5s;");
            sb.AppendLine("  }");
            sb.AppendLine("  client_max_body_size 0;");
            sb.AppendLine("  proxy_read_timeout 300s;");
            sb.AppendLine("  server {");
            sb.AppendLine("    listen 8000;");
            sb.AppendLine("    location / {");
            sb.AppendLine("      proxy_pass http://less3_nodes;");
            sb.AppendLine("      proxy_http_version 1.1;");
            sb.AppendLine("      proxy_set_header Host $host;");
            sb.AppendLine("      proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
            sb.AppendLine("      proxy_set_header Connection \"\";");
            sb.AppendLine("      proxy_next_upstream error timeout;");
            sb.AppendLine("      proxy_next_upstream_tries 2;");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string NodeConfigJson()
        {
            // Telemetry on with the in-process Prometheus endpoint bound to all interfaces
            // (0.0.0.0), so the harness can scrape each node's /metrics on its published host port.
            // OTLP is off since this minimal stack ships no collector. Fast heartbeat and staleness
            // so the failover test observes an unhealthy node quickly. Signatures off so the AWS
            // SDK's requests are accepted without shared-secret negotiation.
            return "{\n" +
                "  \"EnableConsole\": false,\n" +
                "  \"ValidateSignatures\": false,\n" +
                "  \"AdminApiKey\": \"less3admin\",\n" +
                "  \"RegionString\": \"us-west-1\",\n" +
                "  \"Database\": { \"Type\": \"Postgresql\", \"Hostname\": \"postgres\", \"Port\": 5432, \"Username\": \"postgres\", \"Password\": \"postgres\", \"DatabaseName\": \"less3\" },\n" +
                "  \"Cluster\": { \"Enabled\": true, \"LockProvider\": \"Postgres\", \"NodeHeartbeatIntervalMs\": 3000, \"NodeStaleAfterMs\": 12000, \"BucketClientCacheTtlMs\": 2000,\n" +
                "    \"Lock\": { \"DefaultLeaseMs\": 15000, \"HeartbeatIntervalMs\": 5000, \"AcquireTimeoutMs\": 15000, \"WaiterPollMs\": 150 } },\n" +
                "  \"Observability\": { \"Enabled\": true, \"ServiceName\": \"less3\", \"PrometheusEnabled\": true, \"PrometheusHostname\": \"0.0.0.0\", \"PrometheusPort\": 9464, \"PrometheusPath\": \"/metrics\", \"OtlpEnabled\": false },\n" +
                "  \"Webserver\": { \"Hostname\": \"*\", \"Port\": 8000, \"AccessControl\": { \"Mode\": \"DefaultPermit\" } },\n" +
                "  \"Storage\": { \"TempDirectory\": \"/less3/temp/\", \"PartsDirectory\": \"/less3/temp/parts/\", \"StorageType\": \"Disk\", \"DiskDirectory\": \"/less3/disk/\" },\n" +
                "  \"Logging\": { \"ConsoleLogging\": true, \"DiskLogging\": false, \"MinimumLevel\": \"Warn\" }\n" +
                "}\n";
        }
    }
}
