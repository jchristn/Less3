namespace Test.Shared.S3Compatibility
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    /// <summary>
    /// Shared live servers and helpers for the S3 compatibility suites. One server is started lazily per
    /// process (plus one with signature validation enabled) and every test case works in its own bucket,
    /// so the suites are fast and can run against an external database selected with the LESS3_TEST_DB_*
    /// environment variables (see <see cref="Less3TestServer"/>). Thread-safe.
    /// </summary>
    public static class S3CompatibilityContext
    {
        #region Public-Members

        /// <summary>
        /// Access key of the seeded default credential.
        /// </summary>
        public const string DefaultAccessKey = "default";

        /// <summary>
        /// Secret key of the seeded default credential.
        /// </summary>
        public const string DefaultSecretKey = "default";

        #endregion

        #region Private-Members

        private static readonly SemaphoreSlim _Lock = new SemaphoreSlim(1, 1);
        private static Less3TestServer? _Server = null;
        private static Less3TestServer? _SignedServer = null;
        private static readonly HttpClient _Http = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };

        #endregion

        #region Public-Methods

        /// <summary>
        /// The shared server with signature validation disabled.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Running server.</returns>
        public static async Task<Less3TestServer> ServerAsync(CancellationToken token)
        {
            await _Lock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_Server == null)
                {
                    Less3TestServer server = new Less3TestServer();
                    await server.StartAsync(token).ConfigureAwait(false);
                    _Server = server;
                }

                return _Server;
            }
            finally
            {
                _Lock.Release();
            }
        }

        /// <summary>
        /// The shared server with AWS signature validation enabled.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Running server.</returns>
        public static async Task<Less3TestServer> SignedServerAsync(CancellationToken token)
        {
            await _Lock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_SignedServer == null)
                {
                    Less3TestServer server = new Less3TestServer(validateSignatures: true);
                    await server.StartAsync(token).ConfigureAwait(false);
                    _SignedServer = server;
                }

                return _SignedServer;
            }
            finally
            {
                _Lock.Release();
            }
        }

        /// <summary>
        /// Create an S3 client for the server, using the default credential unless keys are given.
        /// </summary>
        /// <param name="server">Server.</param>
        /// <param name="accessKey">Access key, or null for the default credential.</param>
        /// <param name="secretKey">Secret key, or null for the default credential.</param>
        /// <returns>S3 client; the caller disposes it.</returns>
        public static IAmazonS3 Client(Less3TestServer server, string? accessKey = null, string? secretKey = null)
        {
            BasicCredentialsClient(server, accessKey ?? DefaultAccessKey, secretKey ?? DefaultSecretKey, out IAmazonS3 client);
            return client;
        }

        /// <summary>
        /// Create a uniquely named bucket, optionally with versioning enabled.
        /// </summary>
        /// <param name="client">S3 client.</param>
        /// <param name="prefix">Bucket name prefix.</param>
        /// <param name="versioned">True to enable versioning.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Bucket name.</returns>
        public static async Task<string> NewBucketAsync(IAmazonS3 client, string prefix, bool versioned, CancellationToken token)
        {
            string name = (prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 10)).ToLowerInvariant();
            await client.PutBucketAsync(new PutBucketRequest { BucketName = name }, token).ConfigureAwait(false);

            if (versioned)
            {
                await client.PutBucketVersioningAsync(new PutBucketVersioningRequest
                {
                    BucketName = name,
                    VersioningConfig = new S3BucketVersioningConfig { Status = VersionStatus.Enabled }
                }, token).ConfigureAwait(false);
            }

            return name;
        }

        /// <summary>
        /// Set the bucket's versioning status.
        /// </summary>
        public static async Task SetVersioningAsync(IAmazonS3 client, string bucket, VersionStatus status, CancellationToken token)
        {
            await client.PutBucketVersioningAsync(new PutBucketVersioningRequest
            {
                BucketName = bucket,
                VersioningConfig = new S3BucketVersioningConfig { Status = status }
            }, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Put a text object and return the response.
        /// </summary>
        public static async Task<PutObjectResponse> PutAsync(IAmazonS3 client, string bucket, string key, string body, CancellationToken token)
        {
            return await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                ContentBody = body,
                ContentType = "text/plain"
            }, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Read an object (or a specific version) as text.
        /// </summary>
        public static async Task<string> GetTextAsync(IAmazonS3 client, string bucket, string key, string? versionId, CancellationToken token)
        {
            GetObjectRequest request = new GetObjectRequest { BucketName = bucket, Key = key };
            if (versionId != null) request.VersionId = versionId;

            using (GetObjectResponse response = await client.GetObjectAsync(request, token).ConfigureAwait(false))
            using (StreamReader reader = new StreamReader(response.ResponseStream))
            {
                return await reader.ReadToEndAsync(token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Run an SDK call that must fail and return the S3 exception.
        /// </summary>
        /// <param name="action">Call that must fail.</param>
        /// <param name="expectedStatus">Expected HTTP status.</param>
        /// <param name="description">Description for failure messages.</param>
        /// <returns>The exception.</returns>
        public static async Task<AmazonS3Exception> ExpectErrorAsync(Func<Task> action, HttpStatusCode expectedStatus, string description)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (AmazonS3Exception e)
            {
                Check.Equal(expectedStatus, e.StatusCode, description + " status");
                return e;
            }

            throw new InvalidOperationException(description + ": expected HTTP " + (int)expectedStatus + " but the call succeeded.");
        }

        /// <summary>
        /// Send a raw S3 request authenticated by access key (the shared server does not validate signatures).
        /// </summary>
        /// <param name="server">Server.</param>
        /// <param name="method">HTTP method.</param>
        /// <param name="pathAndQuery">Path and query beginning with '/'.</param>
        /// <param name="headers">Extra headers, or null.</param>
        /// <param name="body">Request body, or null.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="accessKey">Access key, or null for the default credential.</param>
        /// <returns>Response.</returns>
        public static async Task<RawResponse> SendAsync(
            Less3TestServer server,
            HttpMethod method,
            string pathAndQuery,
            Dictionary<string, string>? headers,
            byte[]? body,
            CancellationToken token,
            string? accessKey = null)
        {
            using (HttpRequestMessage request = server.CreateS3Request(method, pathAndQuery, accessKey ?? DefaultAccessKey))
            {
                if (body != null) request.Content = new ByteArrayContent(body);

                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                        {
                            if (request.Content == null) request.Content = new ByteArrayContent(Array.Empty<byte>());
                            request.Content.Headers.Remove(header.Key);
                            request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                        else
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                return await SendRequestAsync(request, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Send an arbitrary HTTP request and capture the response.
        /// </summary>
        public static async Task<RawResponse> SendRequestAsync(HttpRequestMessage request, CancellationToken token)
        {
            using (HttpResponseMessage response = await _Http.SendAsync(request, token).ConfigureAwait(false))
            {
                RawResponse raw = new RawResponse();
                raw.Status = response.StatusCode;
                raw.Body = await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);

                foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers.Concat(response.Content.Headers))
                {
                    raw.Headers[header.Key] = String.Join(", ", header.Value);
                }

                return raw;
            }
        }

        /// <summary>
        /// Create a non-administrative user with its own credential.
        /// </summary>
        /// <param name="server">Server.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>User id and keys.</returns>
        public static async Task<TestPrincipal> CreateUserAsync(Less3TestServer server, CancellationToken token)
        {
            return await CreateUserAsync(server, false, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a user with its own credential, optionally granted the built-in tenant administrator role
        /// (needed to create buckets). Without the role the user has no rights beyond ACL grants.
        /// </summary>
        /// <param name="server">Server.</param>
        /// <param name="tenantAdmin">True to grant the tenant administrator role.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>User id and keys.</returns>
        public static async Task<TestPrincipal> CreateUserAsync(Less3TestServer server, bool tenantAdmin, CancellationToken token)
        {
            TestPrincipal principal = new TestPrincipal();
            principal.UserId = TestIds.User();
            principal.CredentialId = TestIds.Credential();
            principal.AccessKey = "ak" + Guid.NewGuid().ToString("N").Substring(0, 16);
            principal.SecretKey = "sk" + Guid.NewGuid().ToString("N");
            principal.Email = principal.UserId + "@example.com";

            HttpResponseMessage user = await server.AdminPostAsync("users", JsonSerializer.Serialize(new
            {
                Id = principal.UserId,
                Name = "Compat " + principal.UserId,
                Email = principal.Email
            }), token).ConfigureAwait(false);
            Check.True(user.IsSuccessStatusCode, "create user " + principal.UserId + " returned " + user.StatusCode);

            HttpResponseMessage credential = await server.AdminPostAsync("credentials", JsonSerializer.Serialize(new
            {
                Id = principal.CredentialId,
                UserId = principal.UserId,
                Description = "compat",
                AccessKey = principal.AccessKey,
                SecretKey = principal.SecretKey,
                IsBase64 = false
            }), token).ConfigureAwait(false);
            Check.True(credential.IsSuccessStatusCode, "create credential for " + principal.UserId + " returned " + credential.StatusCode);

            if (tenantAdmin) await server.GrantTenantAdminAsync("User", principal.UserId, "default", token).ConfigureAwait(false);
            return principal;
        }

        /// <summary>
        /// Look up an object row's internal id through the REST API.
        /// </summary>
        public static async Task<string> ObjectIdAsync(Less3TestServer server, string bucketName, string key, CancellationToken token)
        {
            string bucketId = await BucketIdAsync(server, bucketName, token).ConfigureAwait(false);
            List<JsonElement> items = await RestEnumerateAsync(server, "objects", new Dictionary<string, string> { { "bucketId", bucketId }, { "key", key } }, token).ConfigureAwait(false);
            JsonElement? latest = null;
            foreach (JsonElement item in items)
            {
                if (item.GetProperty("Key").GetString() != key) continue;
                if (latest == null || item.GetProperty("Version").GetInt64() > latest.Value.GetProperty("Version").GetInt64()) latest = item;
            }

            Check.True(latest != null, "object " + bucketName + "/" + key + " found through REST");
            return latest!.Value.GetProperty("Id").GetString()!;
        }

        /// <summary>
        /// Look up a bucket's internal id through the REST API.
        /// </summary>
        public static async Task<string> BucketIdAsync(Less3TestServer server, string bucketName, CancellationToken token)
        {
            List<JsonElement> items = await RestEnumerateAsync(server, "buckets", new Dictionary<string, string> { { "name", bucketName } }, token).ConfigureAwait(false);
            JsonElement match = items.FirstOrDefault(i => i.GetProperty("Name").GetString() == bucketName);
            Check.True(match.ValueKind == JsonValueKind.Object, "bucket " + bucketName + " found through REST");
            return match.GetProperty("Id").GetString()!;
        }

        /// <summary>
        /// Enumerate a REST collection in the default tenant through its filtering enumerate endpoint
        /// (GET query strings only honor a few filters) and return up to 1,000 matching items.
        /// </summary>
        public static async Task<List<JsonElement>> RestEnumerateAsync(Less3TestServer server, string resource, Dictionary<string, string> filters, CancellationToken token)
        {
            string body = JsonSerializer.Serialize(new Dictionary<string, object> { { "TenantId", "default" }, { "Limit", 1000 }, { "Filters", filters } });
            HttpResponseMessage response = await server.RestPostAsync(resource + "/enumerate", body, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, response.StatusCode, "REST enumerate " + resource);
            string json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                List<JsonElement> items = new List<JsonElement>();
                foreach (JsonElement item in doc.RootElement.GetProperty("Items").EnumerateArray()) items.Add(item.Clone());
                return items;
            }
        }

        /// <summary>
        /// Enumerate a REST collection and return its items.
        /// </summary>
        public static async Task<List<JsonElement>> RestItemsAsync(Less3TestServer server, string path, CancellationToken token)
        {
            HttpResponseMessage response = await server.RestGetAsync(path, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, response.StatusCode, "REST GET " + path);
            string json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                List<JsonElement> items = new List<JsonElement>();
                foreach (JsonElement item in doc.RootElement.GetProperty("Items").EnumerateArray()) items.Add(item.Clone());
                return items;
            }
        }

        /// <summary>
        /// Keys of every object in a bucket, following pagination, in the order returned.
        /// </summary>
        public static async Task<List<string>> AllKeysV2Async(IAmazonS3 client, string bucket, int pageSize, CancellationToken token, string? prefix = null)
        {
            List<string> keys = new List<string>();
            string? continuation = null;

            do
            {
                ListObjectsV2Response page = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucket,
                    MaxKeys = pageSize,
                    ContinuationToken = continuation,
                    Prefix = prefix
                }, token).ConfigureAwait(false);

                if (page.S3Objects != null) keys.AddRange(page.S3Objects.Select(o => o.Key));
                continuation = page.IsTruncated == true ? page.NextContinuationToken : null;
            }
            while (continuation != null);

            return keys;
        }

        /// <summary>
        /// Base64 MD5 of a byte array, as used by the Content-MD5 header.
        /// </summary>
        public static string ContentMd5(byte[] data)
        {
            return Convert.ToBase64String(System.Security.Cryptography.MD5.HashData(data));
        }

        #endregion

        #region Private-Methods

        private static void BasicCredentialsClient(Less3TestServer server, string accessKey, string secretKey, out IAmazonS3 client)
        {
            AmazonS3Config config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.USWest1,
                ServiceURL = server.BaseUrl + "/",
                ForcePathStyle = true,
                UseHttp = true,
                MaxErrorRetry = 0,
                Timeout = TimeSpan.FromSeconds(180)
            };

            client = new AmazonS3Client(new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey), config);
        }

        #endregion
    }
}
