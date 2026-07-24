namespace Test.Shared
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.Runtime;
    using Amazon.S3;

    /// <summary>
    /// Manages a Less3 server instance for integration testing.
    /// Creates a temporary working directory, generates configuration, starts the server on a random port,
    /// and cleans up on disposal.
    /// </summary>
    public class Less3TestServer : IDisposable
    {
        #region Private-Members

        private string _TempDirectory;
        private int _Port;
        private string _AdminApiKey = "testadminkey";
        private string _AccessKey = "testaccess";
        private string _SecretKey = "testsecret";
        private bool _ValidateSignatures = false;
        private bool _SimulateContainerEnvironment = false;
        private bool _OmitSystemJson = false;
        private string? _BaseDomain = null;
        private Process? _Process;
        private bool _Disposed = false;
        private HttpClient _HttpClient;

        #endregion

        #region Public-Members

        /// <summary>
        /// The base URL of the running Less3 server.
        /// </summary>
        public string BaseUrl => $"http://127.0.0.1:{_Port}";

        /// <summary>
        /// The TCP port the server is listening on.
        /// </summary>
        public int Port => _Port;

        /// <summary>
        /// The admin API key configured for this test server.
        /// </summary>
        public string AdminApiKey => _AdminApiKey;

        /// <summary>
        /// The default access key configured for this test server.
        /// </summary>
        public string AccessKey => _AccessKey;

        /// <summary>
        /// The default secret key configured for this test server.
        /// </summary>
        public string SecretKey => _SecretKey;

        /// <summary>
        /// The temporary directory used by this test server instance.
        /// </summary>
        public string TempDirectory => _TempDirectory;

        /// <summary>
        /// An HttpClient configured to communicate with this test server.
        /// </summary>
        public HttpClient HttpClient => _HttpClient;

        /// <summary>
        /// An AWS S3 client configured to communicate with this test server.
        /// </summary>
        public IAmazonS3 S3Client { get; }

        /// <summary>
        /// Whether this test server validates AWS signatures.
        /// </summary>
        public bool ValidateSignatures => _ValidateSignatures;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="Less3TestServer"/> class.
        /// Does not start the server; call <see cref="StartAsync"/> to begin.
        /// </summary>
        public Less3TestServer(
            bool validateSignatures = false,
            bool simulateContainerEnvironment = false,
            bool omitSystemJson = false,
            string? baseDomain = null)
        {
            _Port = GetRandomPort();
            _TempDirectory = Path.Combine(Path.GetTempPath(), "less3-test-" + Path.GetRandomFileName().Replace(".", ""));
            _ValidateSignatures = validateSignatures;
            _SimulateContainerEnvironment = simulateContainerEnvironment;
            _OmitSystemJson = omitSystemJson;
            _BaseDomain = baseDomain;

            if (_OmitSystemJson)
            {
                _AdminApiKey = "less3admin";
                _AccessKey = "default";
                _SecretKey = "default";
            }

            SocketsHttpHandler handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromSeconds(5),
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(5),
                MaxConnectionsPerServer = 10
            };

            _HttpClient = new HttpClient(handler);
            _HttpClient.Timeout = TimeSpan.FromSeconds(5);
            _HttpClient.DefaultRequestHeaders.ConnectionClose = true;

            S3Client = CreateS3Client();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Starts the Less3 server process, waits for it to become available, and seeds default data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_OmitSystemJson && !_SimulateContainerEnvironment)
            {
                throw new InvalidOperationException("Omitting system.json requires simulateContainerEnvironment=true.");
            }

            Directory.CreateDirectory(_TempDirectory);
            if (!_OmitSystemJson)
            {
                Directory.CreateDirectory(Path.Combine(_TempDirectory, "disk"));
                Directory.CreateDirectory(Path.Combine(_TempDirectory, "temp"));
                Directory.CreateDirectory(Path.Combine(_TempDirectory, "logs"));
            }

            string? assetsSource = FindAssetsDirectory();
            if (assetsSource != null)
            {
                string assetsDest = Path.Combine(_TempDirectory, "Assets");
                Directory.CreateDirectory(assetsDest);
                foreach (string file in Directory.GetFiles(assetsSource))
                {
                    File.Copy(file, Path.Combine(assetsDest, Path.GetFileName(file)), true);
                }
            }

            if (!_OmitSystemJson) WriteSystemJson();
            WriteLess3Database();

            string less3Dll = FindLess3Dll();
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{less3Dll}\"",
                WorkingDirectory = _TempDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            psi.Environment["DOTNET_ENVIRONMENT"] = "Test";
            if (_SimulateContainerEnvironment)
            {
                psi.Environment["DOTNET_RUNNING_IN_CONTAINER"] = "true";
            }
            if (_OmitSystemJson)
            {
                psi.Environment["LESS3_PORT"] = _Port.ToString();
            }

            _Process = Process.Start(psi);
            if (_Process == null)
                throw new InvalidOperationException("Failed to start Less3 process");

            _Process.OutputDataReceived += (sender, e) =>
            {
            };
            _Process.ErrorDataReceived += (sender, e) =>
            {
            };

            _Process.BeginOutputReadLine();
            _Process.BeginErrorReadLine();

            await WaitForServerReadyAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a GET request to the admin API.
        /// </summary>
        /// <param name="path">The admin API path (e.g., "users", "buckets").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponseMessage> AdminGetAsync(string path, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/admin/{path}");
            request.Headers.Add("x-api-key", _AdminApiKey);
            return await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a POST request to the admin API with a JSON body.
        /// </summary>
        /// <param name="path">The admin API path.</param>
        /// <param name="jsonBody">The JSON body string.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponseMessage> AdminPostAsync(string path, string jsonBody, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/admin/{path}");
            request.Headers.Add("x-api-key", _AdminApiKey);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a PUT request to the admin API with a JSON body.
        /// </summary>
        /// <param name="path">The admin API path.</param>
        /// <param name="jsonBody">The JSON body string.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponseMessage> AdminPutAsync(string path, string jsonBody, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/admin/{path}");
            request.Headers.Add("x-api-key", _AdminApiKey);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a DELETE request to the admin API.
        /// </summary>
        /// <param name="path">The admin API path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponseMessage> AdminDeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/admin/{path}");
            request.Headers.Add("x-api-key", _AdminApiKey);
            return await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a GET request to the Less3 REST API.
        /// </summary>
        /// <param name="path">The REST API path below /api/v1.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponseMessage> RestGetAsync(string path, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/v1/{path}");
            request.Headers.Add("x-api-key", _AdminApiKey);
            return await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a POST request to the Less3 REST API.
        /// </summary>
        /// <param name="path">The REST API path below /api/v1.</param>
        /// <param name="jsonBody">The JSON request body.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponseMessage> RestPostAsync(string path, string jsonBody, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/v1/{path}");
            request.Headers.Add("x-api-key", _AdminApiKey);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an unauthenticated POST request to the Less3 REST API.
        /// </summary>
        /// <param name="path">The REST API path below /api/v1.</param>
        /// <param name="jsonBody">The JSON request body.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponseMessage> RestPostUnauthenticatedAsync(string path, string jsonBody, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/v1/{path}");
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a PUT request to the Less3 REST API.
        /// </summary>
        /// <param name="path">The REST API path below /api/v1.</param>
        /// <param name="jsonBody">The JSON request body.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponseMessage> RestPutAsync(string path, string jsonBody, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/api/v1/{path}");
            request.Headers.Add("x-api-key", _AdminApiKey);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a DELETE request to the Less3 REST API.
        /// </summary>
        /// <param name="path">The REST API path below /api/v1.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponseMessage> RestDeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/api/v1/{path}");
            request.Headers.Add("x-api-key", _AdminApiKey);
            return await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Grants the built-in tenant administrator role to a test principal in a tenant.
        /// </summary>
        /// <param name="principalType">Principal type, such as User or Credential.</param>
        /// <param name="principalId">Principal identifier.</param>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GrantTenantAdminAsync(
            string principalType,
            string principalId,
            string tenantId = "default",
            CancellationToken cancellationToken = default)
        {
            string json = JsonSerializer.Serialize(new
            {
                Id = TestIds.Assignment(),
                TenantId = tenantId,
                RoleId = "rol_builtin_tenantadmin",
                PrincipalType = principalType,
                PrincipalId = principalId,
                ResourceType = "Tenant",
                ResourceId = tenantId,
                Active = true
            });

            HttpResponseMessage response = await RestPostAsync("roleassignments?tenantId=" + tenantId, json, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new InvalidOperationException("Failed to grant tenant admin to " + principalType + " " + principalId + "; status " + response.StatusCode + ".");
            }
        }

        /// <summary>
        /// Creates an AWS S3 client configured for this test server.
        /// </summary>
        /// <param name="accessKey">Optional access key override.</param>
        /// <param name="secretKey">Optional secret key override.</param>
        /// <returns>The configured S3 client.</returns>
        public IAmazonS3 CreateS3Client(string? accessKey = null, string? secretKey = null)
        {
            BasicAWSCredentials credentials = new BasicAWSCredentials(accessKey ?? _AccessKey, secretKey ?? _SecretKey);
            AmazonS3Config config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USWest1,
                ServiceURL = BaseUrl + "/",
                ForcePathStyle = true,
                UseHttp = true,
                MaxErrorRetry = 0,
                Timeout = TimeSpan.FromSeconds(5)
            };

            return new AmazonS3Client(credentials, config);
        }

        /// <summary>
        /// Creates a raw S3 HTTP request with Less3-compatible authorization headers.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="relativePathAndQuery">Path and query, beginning with '/'.</param>
        /// <param name="accessKey">Optional access key to place in the Authorization header.</param>
        /// <returns>The configured request.</returns>
        public HttpRequestMessage CreateS3Request(HttpMethod method, string relativePathAndQuery, string? accessKey = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, BaseUrl + relativePathAndQuery);
            request.Headers.TryAddWithoutValidation("Authorization", BuildAuthHeader(accessKey ?? _AccessKey));
            return request;
        }

        /// <summary>
        /// Releases all resources used by this test server instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Releases all resources.
        /// </summary>
        /// <param name="disposing">Whether managed resources should be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                if (_Process != null && !_Process.HasExited)
                {
                    try
                    {
                        _Process.Kill(true);
                        _Process.WaitForExit(5000);
                    }
                    catch
                    {
                    }

                    _Process.Dispose();
                    _Process = null;
                }

                _HttpClient?.Dispose();
                S3Client?.Dispose();

                if (Directory.Exists(_TempDirectory))
                {
                    try
                    {
                        Directory.Delete(_TempDirectory, true);
                    }
                    catch
                    {
                    }
                }
            }

            _Disposed = true;
        }

        private static int GetRandomPort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private void WriteSystemJson()
        {
            string json = JsonSerializer.Serialize(new
            {
                EnableConsole = false,
                ValidateSignatures = _ValidateSignatures,
                BaseDomain = _BaseDomain,
                HeaderApiKey = "x-api-key",
                AdminApiKey = _AdminApiKey,
                RegionString = "us-west-1",
                RequestHistoryRetentionDays = 30,
                CleanupIntervalMs = 3600000,
                Database = new
                {
                    Type = "Sqlite",
                    Filename = "./less3.db"
                },
                Webserver = new
                {
                    Hostname = "127.0.0.1",
                    Port = _Port
                },
                Storage = new
                {
                    TempDirectory = "./temp/",
                    StorageType = "Disk",
                    DiskDirectory = "./disk/"
                },
                Logging = new
                {
                    SyslogServerIp = "127.0.0.1",
                    SyslogServerPort = 514,
                    MinimumLevel = "Info",
                    LogHttpRequests = false,
                    LogS3Requests = false,
                    LogExceptions = false,
                    LogSignatureValidation = false,
                    ConsoleLogging = false,
                    DiskLogging = false,
                    DiskDirectory = "./logs/"
                },
                Debug = new
                {
                    Authentication = false,
                    S3Requests = false,
                    Exceptions = false
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(Path.Combine(_TempDirectory, "system.json"), json);
        }

        private void WriteLess3Database()
        {
            // The database is created automatically by WatsonORM on startup.
            // No pre-seeding needed; we use the admin API to create users, credentials, and buckets.
        }

        private string FindLess3Dll()
        {
            bool preferRelease = AppContext.BaseDirectory.IndexOf(Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0;

            // Look for the built Less3.dll relative to the test project
            List<string> searchPaths = new List<string>();

            string[] debugPaths = new string[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Less3", "bin", "Debug", "net10.0", "Less3.dll")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Less3", "bin", "Debug", "net10.0", "Less3.dll")),
            };

            string[] releasePaths = new string[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Less3", "bin", "Release", "net10.0", "Less3.dll")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Less3", "bin", "Release", "net10.0", "Less3.dll")),
            };

            if (preferRelease)
            {
                searchPaths.AddRange(releasePaths);
                searchPaths.AddRange(debugPaths);
            }
            else
            {
                searchPaths.AddRange(debugPaths);
                searchPaths.AddRange(releasePaths);
            }

            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            throw new FileNotFoundException(
                "Could not find Less3.dll. Ensure Less3 is built before running tests. " +
                $"Searched: {string.Join(", ", searchPaths)}");
        }

        private string? FindAssetsDirectory()
        {
            string[] searchPaths = new string[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Less3", "Assets")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Less3", "Assets")),
            };

            foreach (string path in searchPaths)
            {
                if (Directory.Exists(path))
                    return path;
            }

            return null;
        }

        private async Task WaitForServerReadyAsync(CancellationToken cancellationToken)
        {
            int maxAttempts = 60;
            int delayMs = 500;

            for (int i = 0; i < maxAttempts; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_Process != null && _Process.HasExited)
                    throw new InvalidOperationException(
                        $"Less3 process exited unexpectedly with code {_Process.ExitCode}");

                try
                {
                    HttpResponseMessage response = await _HttpClient.GetAsync(BaseUrl + "/", cancellationToken).ConfigureAwait(false);
                    if ((int)response.StatusCode < 500)
                        return;
                }
                catch (HttpRequestException)
                {
                }
                catch (TaskCanceledException)
                {
                }

                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException($"Less3 server did not become ready within {maxAttempts * delayMs / 1000} seconds");
        }

        private string BuildAuthHeader(string accessKey)
        {
            return $"AWS4-HMAC-SHA256 Credential={accessKey}/20260101/us-west-1/s3/aws4_request, SignedHeaders=host, Signature=placeholder";
        }
        #endregion
    }
}
