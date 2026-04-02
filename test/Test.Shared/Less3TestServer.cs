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
        public Less3TestServer(bool validateSignatures = false)
        {
            _Port = GetRandomPort();
            _TempDirectory = Path.Combine(Path.GetTempPath(), "less3-test-" + Guid.NewGuid().ToString("N"));
            _ValidateSignatures = validateSignatures;

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
            Directory.CreateDirectory(_TempDirectory);
            Directory.CreateDirectory(Path.Combine(_TempDirectory, "disk"));
            Directory.CreateDirectory(Path.Combine(_TempDirectory, "temp"));
            Directory.CreateDirectory(Path.Combine(_TempDirectory, "logs"));

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

            WriteSystemJson();
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

            _Process = Process.Start(psi);
            if (_Process == null)
                throw new InvalidOperationException("Failed to start Less3 process");

            _Process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine("[Less3] " + e.Data);
            };
            _Process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.Error.WriteLine("[Less3 ERR] " + e.Data);
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
        /// <returns>The configured request.</returns>
        public HttpRequestMessage CreateS3Request(HttpMethod method, string relativePathAndQuery)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, BaseUrl + relativePathAndQuery);
            request.Headers.TryAddWithoutValidation("Authorization", BuildAuthHeader());
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
                BaseDomain = (string?)null,
                HeaderApiKey = "x-api-key",
                AdminApiKey = _AdminApiKey,
                RegionString = "us-west-1",
                Database = new
                {
                    Type = "Sqlite",
                    Filename = "./less3.db"
                },
                Webserver = new
                {
                    Hostname = "localhost",
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
            // Look for the built Less3.dll relative to the test project
            string[] searchPaths = new string[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Less3", "bin", "Debug", "net10.0", "Less3.dll")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Less3", "bin", "Release", "net10.0", "Less3.dll")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Less3", "bin", "Debug", "net10.0", "Less3.dll")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Less3", "bin", "Release", "net10.0", "Less3.dll")),
            };

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

        private string BuildAuthHeader()
        {
            return $"AWS4-HMAC-SHA256 Credential={_AccessKey}/20260101/us-west-1/s3/aws4_request, SignedHeaders=host, Signature=placeholder";
        }
        #endregion
    }
}
