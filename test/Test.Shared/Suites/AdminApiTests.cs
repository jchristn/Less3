namespace Test.Shared.Suites
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// Integration tests for the Less3 Admin API.
    /// Exercises user, credential, and bucket CRUD operations via the admin REST API.
    /// </summary>
    public class AdminApiTests : TestSuite
    {
        #region Private-Members

        private Less3TestServer _Server;

        #endregion

        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Admin API Tests";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminApiTests"/> class.
        /// </summary>
        /// <param name="server">The running Less3 test server.</param>
        public AdminApiTests(Less3TestServer server)
        {
            _Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all admin API tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            string userId = Test.Shared.TestIds.User();
            string credentialId = Test.Shared.TestIds.Credential();
            string bucketId = Test.Shared.TestIds.Bucket();

            #region Users

            await RunTest("AdminApi_CreateUser", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    Id = userId,
                    Name = "TestUser",
                    Email = "test@example.com"
                });

                HttpResponseMessage response = await _Server.AdminPostAsync("users", json).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Created, response.StatusCode);
            });

            await RunTest("AdminApi_ListUsers", async () =>
            {
                HttpResponseMessage response = await _Server.AdminGetAsync("users").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                AssertNotNull(body);
                AssertContains(body, "TestUser");
            });

            await RunTest("AdminApi_GetUser", async () =>
            {
                HttpResponseMessage response = await _Server.AdminGetAsync($"users/{userId}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                AssertContains(body, "TestUser");
                AssertContains(body, userId);
            });

            await RunTest("AdminApi_GetUser_NotFound", async () =>
            {
                HttpResponseMessage response = await _Server.AdminGetAsync($"users/{Test.Shared.TestIds.User()}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.NotFound, response.StatusCode);
            });

            await RunTest("AdminApi_CreateUser_DuplicateEmail_Returns409", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    Id = Test.Shared.TestIds.Object(),
                    Name = "DuplicateUser",
                    Email = "test@example.com"
                });

                HttpResponseMessage response = await _Server.AdminPostAsync("users", json).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Conflict, response.StatusCode);
            });

            await RunTest("AdminApi_UpdateUser", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    Id = userId,
                    Name = "UpdatedUser",
                    Email = "updated@example.com"
                });

                HttpResponseMessage response = await _Server.AdminPutAsync($"users/{userId}", json).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                AssertContains(body, "UpdatedUser");
                AssertContains(body, "updated@example.com");
            });

            #endregion

            #region Credentials

            await RunTest("AdminApi_CreateCredential", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    Id = credentialId,
                    UserId = userId,
                    Description = "Test credential",
                    AccessKey = _Server.AccessKey,
                    SecretKey = _Server.SecretKey,
                    IsBase64 = false
                });

                HttpResponseMessage response = await _Server.AdminPostAsync("credentials", json).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Created, response.StatusCode);
            });

            await RunTest("AdminApi_ListCredentials", async () =>
            {
                HttpResponseMessage response = await _Server.AdminGetAsync("credentials").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                AssertContains(body, _Server.AccessKey);
            });

            await RunTest("AdminApi_GetCredential", async () =>
            {
                HttpResponseMessage response = await _Server.AdminGetAsync($"credentials/{credentialId}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);
            });

            await RunTest("AdminApi_CreateCredential_DuplicateAccessKey_Returns409", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    Id = Test.Shared.TestIds.Object(),
                    UserId = userId,
                    Description = "Duplicate",
                    AccessKey = _Server.AccessKey,
                    SecretKey = "anothersecret",
                    IsBase64 = false
                });

                HttpResponseMessage response = await _Server.AdminPostAsync("credentials", json).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Conflict, response.StatusCode);
            });

            await RunTest("AdminApi_UpdateCredential", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    Id = credentialId,
                    UserId = userId,
                    Description = "Updated credential",
                    AccessKey = "updated-access",
                    SecretKey = "updated-secret",
                    IsBase64 = false
                });

                HttpResponseMessage response = await _Server.AdminPutAsync($"credentials/{credentialId}", json).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                AssertContains(body, "Updated credential");
                AssertContains(body, "updated-access");
            });

            #endregion

            #region Buckets

            await RunTest("AdminApi_CreateBucket", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    Id = bucketId,
                    OwnerId = userId,
                    Name = "admin-test-bucket"
                });

                HttpResponseMessage response = await _Server.AdminPostAsync("buckets", json).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Created, response.StatusCode);
            });

            await RunTest("AdminApi_ListBuckets", async () =>
            {
                HttpResponseMessage response = await _Server.AdminGetAsync("buckets").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                AssertContains(body, "admin-test-bucket");
            });

            await RunTest("AdminApi_GetBucket", async () =>
            {
                HttpResponseMessage response = await _Server.AdminGetAsync($"buckets/{bucketId}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);
            });

            await RunTest("AdminApi_GetDashboardStats", async () =>
            {
                HttpResponseMessage response = await _Server.AdminGetAsync("stats").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                AssertContains(body, "\"BucketCount\"");
                AssertContains(body, "\"TotalObjectCount\"");
                AssertContains(body, "\"TotalBytes\"");
            });

            await RunTest("AdminApi_CreateBucket_Duplicate_ReturnsError", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    Id = Test.Shared.TestIds.Object(),
                    OwnerId = userId,
                    Name = "admin-test-bucket"
                });

                HttpResponseMessage response = await _Server.AdminPostAsync("buckets", json).ConfigureAwait(false);
                // BucketAlreadyExists is returned as an S3 error (409)
                AssertNotEqual(HttpStatusCode.Created, response.StatusCode);
            });

            #endregion

            #region Auth-Failure

            await RunTest("AdminApi_InvalidApiKey_Returns401", async () =>
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{_Server.BaseUrl}/admin/users");
                request.Headers.Add("x-api-key", "wrong-key");
                HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            });

            await RunTest("AdminApi_MissingApiKey_Returns401", async () =>
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{_Server.BaseUrl}/admin/stats");
                HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            });

            #endregion

            #region Cleanup

            await RunTest("AdminApi_DeleteBucket", async () =>
            {
                HttpResponseMessage response = await _Server.AdminDeleteAsync($"buckets/{bucketId}?destroy=true").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.NoContent, response.StatusCode);
            });

            await RunTest("AdminApi_DeleteBucket_NotFound", async () =>
            {
                HttpResponseMessage response = await _Server.AdminDeleteAsync($"buckets/{Test.Shared.TestIds.Bucket()}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.NotFound, response.StatusCode);
            });

            await RunTest("AdminApi_DeleteCredential", async () =>
            {
                HttpResponseMessage response = await _Server.AdminDeleteAsync($"credentials/{credentialId}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.NoContent, response.StatusCode);
            });

            await RunTest("AdminApi_DeleteUser", async () =>
            {
                HttpResponseMessage response = await _Server.AdminDeleteAsync($"users/{userId}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.NoContent, response.StatusCode);
            });

            #endregion
        }

        #endregion
    }
}
