namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Less3.Helpers;

    /// <summary>
    /// Live-server performance regression tests for bounded REST enumeration and S3 operations.
    /// </summary>
    public class PerformanceRegressionTests : TestSuite
    {
        #region Private-Members

        private readonly Less3TestServer _Server;

        #endregion

        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Performance Regression Tests";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceRegressionTests"/> class.
        /// </summary>
        /// <param name="server">The running Less3 test server.</param>
        public PerformanceRegressionTests(Less3TestServer server)
        {
            _Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all performance regression tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            using IAmazonS3 client = await CreatePerformanceClientAsync(CancellationToken.None).ConfigureAwait(false);

            await RunTest(
                "Performance_REST_ObjectsEnumerationUsesBoundedPagingAndTotal",
                () => RestObjectsEnumerationUsesBoundedPagingAndTotalAsync(client, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "Performance_REST_RequestHistoryEnumerationFiltersWithoutUnboundedPage",
                () => RestRequestHistoryEnumerationFiltersWithoutUnboundedPageAsync(client, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "Performance_REST_RequestHistoryCreateSupportsSyntheticDashboardRows",
                () => RestRequestHistoryCreateSupportsSyntheticDashboardRowsAsync(CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "Performance_S3_ListObjectsCompletesWithinSmokeThreshold",
                () => S3ListObjectsCompletesWithinSmokeThresholdAsync(client, CancellationToken.None)).ConfigureAwait(false);

            await RunTest(
                "Performance_S3_ConcurrentBucketCreatesCompleteWithinSmokeThreshold",
                () => S3ConcurrentBucketCreatesCompleteWithinSmokeThresholdAsync(client, CancellationToken.None)).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private async Task RestObjectsEnumerationUsesBoundedPagingAndTotalAsync(IAmazonS3 client, CancellationToken cancellationToken)
        {
            string bucketName = "perf-enum-" + TestIds.Suffix().Substring(0, 8);
            string prefix = "paged/" + TestIds.Suffix().Substring(0, 6) + "/";

            await PutBucketAsync(client, bucketName, cancellationToken).ConfigureAwait(false);
            string bucketId = await GetBucketIdAsync(bucketName, cancellationToken).ConfigureAwait(false);

            const int objectCount = 12;
            for (int i = 0; i < objectCount; i++)
            {
                await client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = prefix + "object-" + i.ToString("D2") + ".txt",
                    ContentBody = "payload-" + i
                }, cancellationToken).ConfigureAwait(false);
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            using HttpResponseMessage response = await _Server.RestPostAsync(
                "objects/enumerate?tenantId=default&bucketId=" + bucketId,
                JsonSerializer.Serialize(new
                {
                    Limit = 5,
                    Offset = 0,
                    SortField = "key",
                    SortDirection = "asc",
                    Filters = new Dictionary<string, string>
                    {
                        { "prefix", prefix }
                    }
                }),
                cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            AssertEqual(HttpStatusCode.OK, response.StatusCode, "Object enumeration should succeed: " + body);
            Assert(stopwatch.Elapsed < TimeSpan.FromSeconds(5), "Object enumeration exceeded smoke threshold.");

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            JsonElement items = root.GetProperty("Items");

            AssertEqual(5, items.GetArrayLength(), "Object enumeration should return exactly the requested first page.");
            AssertGreaterThan(GetTotal(root), 11, "Object enumeration total should include all matching objects.");
            AssertTrue(root.GetProperty("HasMore").GetBoolean(), "Object enumeration should expose a next page.");

            foreach (JsonElement item in items.EnumerateArray())
            {
                AssertStartsWith(item.GetProperty("Key").GetString() ?? String.Empty, prefix, "Returned object key should match the prefix filter.");
                AssertEqual(bucketId, item.GetProperty("BucketId").GetString(), "Returned object should be scoped to the requested bucket.");
            }
        }

        private async Task RestRequestHistoryEnumerationFiltersWithoutUnboundedPageAsync(IAmazonS3 client, CancellationToken cancellationToken)
        {
            string bucketName = "perf-hist-" + TestIds.Suffix().Substring(0, 8);
            DateTime startUtc = DateTime.UtcNow.AddMinutes(-1);

            await PutBucketAsync(client, bucketName, cancellationToken).ConfigureAwait(false);
            await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = "history.txt",
                ContentBody = "history-payload"
            }, cancellationToken).ConfigureAwait(false);

            Stopwatch stopwatch = Stopwatch.StartNew();
            using HttpResponseMessage response = await _Server.RestPostAsync(
                "requesthistory/enumerate?tenantId=default",
                JsonSerializer.Serialize(new
                {
                    Limit = 2,
                    Offset = 0,
                    StartUtc = startUtc,
                    EndUtc = DateTime.UtcNow.AddMinutes(1),
                    SortField = "createdUtc",
                    SortDirection = "desc",
                    Filters = new Dictionary<string, string>
                    {
                        { "method", "PUT" },
                        { "success", "true" }
                    }
                }),
                cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            AssertEqual(HttpStatusCode.OK, response.StatusCode, "Request history enumeration should succeed: " + body);
            Assert(stopwatch.Elapsed < TimeSpan.FromSeconds(5), "Request history enumeration exceeded smoke threshold.");

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            JsonElement items = root.GetProperty("Items");

            AssertTrue(items.GetArrayLength() <= 2, "Request history enumeration should respect the requested page size.");
            AssertGreaterThan(GetTotal(root), 0, "Request history enumeration should report matching PUT requests.");

            foreach (JsonElement item in items.EnumerateArray())
            {
                AssertEqual("default", item.GetProperty("TenantId").GetString(), "Request history row should be tenant scoped.");
                AssertEqual("PUT", item.GetProperty("HttpMethod").GetString(), "Request history method filter should be applied.");
                AssertTrue(item.GetProperty("Success").GetBoolean(), "Request history success filter should be applied.");
            }
        }

        private async Task RestRequestHistoryCreateSupportsSyntheticDashboardRowsAsync(CancellationToken cancellationToken)
        {
            string requestHistoryId = TestIds.RequestHistory();
            string syntheticAccessKey = IdGenerator.GenerateAccessKey();
            DateTime createdUtc = DateTime.UtcNow.AddMinutes(-30);

            using HttpResponseMessage createResponse = await _Server.RestPostAsync(
                "requesthistory?tenantId=default",
                JsonSerializer.Serialize(new
                {
                    Id = requestHistoryId,
                    TenantId = "default",
                    HttpMethod = "GET",
                    RequestUrl = _Server.BaseUrl + "/synthetic/dashboard-demo.txt",
                    SourceIp = "10.10.10.10",
                    StatusCode = 200,
                    Success = true,
                    DurationMs = 17,
                    RequestType = "SyntheticDashboard",
                    UserId = "usr_default_admin",
                    AccessKey = syntheticAccessKey,
                    RequestContentType = (string?)null,
                    RequestBodyLength = 0,
                    ResponseContentType = "text/plain",
                    ResponseBodyLength = 128,
                    RequestBody = (string?)null,
                    ResponseBody = (string?)null,
                    CreatedUtc = createdUtc
                }),
                cancellationToken).ConfigureAwait(false);

            string createBody = await createResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            AssertEqual(HttpStatusCode.Created, createResponse.StatusCode, "Request history create should succeed: " + createBody);

            using HttpResponseMessage readResponse = await _Server.RestGetAsync(
                "requesthistory/" + requestHistoryId + "?tenantId=default",
                cancellationToken).ConfigureAwait(false);
            string readBody = await readResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            AssertEqual(HttpStatusCode.OK, readResponse.StatusCode, "Created request history row should be readable: " + readBody);

            using HttpResponseMessage enumerateResponse = await _Server.RestPostAsync(
                "requesthistory/enumerate?tenantId=default",
                JsonSerializer.Serialize(new
                {
                    Limit = 5,
                    StartUtc = createdUtc.AddMinutes(-1),
                    EndUtc = createdUtc.AddMinutes(1),
                    Filters = new Dictionary<string, string>
                    {
                        { "accessKey", syntheticAccessKey },
                        { "requestType", "SyntheticDashboard" }
                    }
                }),
                cancellationToken).ConfigureAwait(false);
            string enumerateBody = await enumerateResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            AssertEqual(HttpStatusCode.OK, enumerateResponse.StatusCode, "Synthetic request history enumeration should succeed: " + enumerateBody);

            using JsonDocument document = JsonDocument.Parse(enumerateBody);
            JsonElement items = document.RootElement.GetProperty("Items");
            AssertEqual(1, items.GetArrayLength(), "Synthetic request history row should match date and field filters.");
            AssertEqual(requestHistoryId, items[0].GetProperty("Id").GetString(), "Synthetic request history Id should round-trip.");
        }

        private async Task S3ListObjectsCompletesWithinSmokeThresholdAsync(IAmazonS3 client, CancellationToken cancellationToken)
        {
            string bucketName = "perf-list-" + TestIds.Suffix().Substring(0, 8);
            string prefix = "list/" + TestIds.Suffix().Substring(0, 6) + "/";

            await PutBucketAsync(client, bucketName, cancellationToken).ConfigureAwait(false);

            const int objectCount = 24;
            for (int i = 0; i < objectCount; i++)
            {
                await client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = prefix + "object-" + i.ToString("D2") + ".txt",
                    ContentBody = "payload-" + i
                }, cancellationToken).ConfigureAwait(false);
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            ListObjectsV2Response response = await client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix,
                MaxKeys = 10
            }, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            AssertEqual(HttpStatusCode.OK, response.HttpStatusCode, "ListObjectsV2 should succeed.");
            AssertEqual(10, response.S3Objects.Count, "ListObjectsV2 should return the requested page size.");
            AssertTrue(response.IsTruncated == true, "ListObjectsV2 should indicate there are more keys.");
            Assert(stopwatch.Elapsed < TimeSpan.FromSeconds(5), "ListObjectsV2 exceeded smoke threshold.");
        }

        private async Task S3ConcurrentBucketCreatesCompleteWithinSmokeThresholdAsync(IAmazonS3 client, CancellationToken cancellationToken)
        {
            string prefix = "perf-conc-" + TestIds.Suffix().Substring(0, 5) + "-";
            List<string> bucketNames = Enumerable.Range(0, 6)
                .Select(i => prefix + i.ToString("D2"))
                .ToList();

            Stopwatch stopwatch = Stopwatch.StartNew();
            PutBucketResponse[] responses = await Task.WhenAll(bucketNames.Select(name =>
                client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = name
                }, cancellationToken))).ConfigureAwait(false);
            stopwatch.Stop();

            foreach (PutBucketResponse response in responses)
            {
                AssertTrue(
                    response.HttpStatusCode == HttpStatusCode.OK || response.HttpStatusCode == HttpStatusCode.Created,
                    "Concurrent bucket create should succeed.");
            }

            Assert(stopwatch.Elapsed < TimeSpan.FromSeconds(10), "Concurrent bucket creates exceeded smoke threshold.");
        }

        private async Task PutBucketAsync(IAmazonS3 client, string bucketName, CancellationToken cancellationToken)
        {
            PutBucketResponse response = await client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken).ConfigureAwait(false);

            AssertTrue(
                response.HttpStatusCode == HttpStatusCode.OK || response.HttpStatusCode == HttpStatusCode.Created,
                "PutBucket should succeed.");
        }

        private async Task<string> GetBucketIdAsync(string bucketName, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _Server.RestPostAsync(
                "buckets/enumerate?tenantId=default",
                JsonSerializer.Serialize(new
                {
                    Limit = 1,
                    Filters = new Dictionary<string, string>
                    {
                        { "name", bucketName }
                    }
                }),
                cancellationToken).ConfigureAwait(false);

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            AssertEqual(HttpStatusCode.OK, response.StatusCode, "Bucket enumeration should succeed: " + body);

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement items = document.RootElement.GetProperty("Items");
            AssertEqual(1, items.GetArrayLength(), "Created bucket should be discoverable through REST enumeration.");
            return items[0].GetProperty("Id").GetString() ?? throw new InvalidOperationException("Bucket Id was empty.");
        }

        private async Task<IAmazonS3> CreatePerformanceClientAsync(CancellationToken cancellationToken)
        {
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string accessKey = IdGenerator.GenerateAccessKey();
            string secretKey = "secret-" + TestIds.Suffix();

            using HttpResponseMessage userResponse = await _Server.RestPostAsync(
                "users?tenantId=default",
                JsonSerializer.Serialize(new
                {
                    Id = userId,
                    TenantId = "default",
                    Name = "PerformanceUser",
                    Email = "performance-" + TestIds.Suffix().Substring(0, 8) + "@example.com",
                    Active = true
                }),
                cancellationToken).ConfigureAwait(false);
            await EnsureCreatedAsync(userResponse, "performance user", cancellationToken).ConfigureAwait(false);

            using HttpResponseMessage credentialResponse = await _Server.RestPostAsync(
                "credentials?tenantId=default",
                JsonSerializer.Serialize(new
                {
                    Id = credentialId,
                    TenantId = "default",
                    UserId = userId,
                    Description = "Performance regression credential",
                    AccessKey = accessKey,
                    SecretKey = secretKey,
                    IsBase64 = false,
                    Active = true
                }),
                cancellationToken).ConfigureAwait(false);
            await EnsureCreatedAsync(credentialResponse, "performance credential", cancellationToken).ConfigureAwait(false);

            await _Server.GrantTenantAdminAsync("User", userId, cancellationToken: cancellationToken).ConfigureAwait(false);
            await _Server.GrantTenantAdminAsync("Credential", credentialId, cancellationToken: cancellationToken).ConfigureAwait(false);

            return _Server.CreateS3Client(accessKey, secretKey);
        }

        private async Task EnsureCreatedAsync(HttpResponseMessage response, string resourceName, CancellationToken cancellationToken)
        {
            if (response.StatusCode != HttpStatusCode.Created)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException("Failed to create " + resourceName + "; status " + response.StatusCode + ": " + body);
            }
        }

        private static long GetTotal(JsonElement root)
        {
            JsonElement total = root.GetProperty("Total");
            if (total.ValueKind == JsonValueKind.Number) return total.GetInt64();
            return 0;
        }

        #endregion
    }
}
