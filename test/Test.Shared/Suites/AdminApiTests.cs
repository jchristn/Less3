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
            string userGuid = Guid.NewGuid().ToString();
            string credGuid = Guid.NewGuid().ToString();
            string bucketGuid = Guid.NewGuid().ToString();

            #region Users

            await RunTest("AdminApi_CreateUser", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    GUID = userGuid,
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
                HttpResponseMessage response = await _Server.AdminGetAsync($"users/{userGuid}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                AssertContains(body, "TestUser");
                AssertContains(body, userGuid);
            });

            await RunTest("AdminApi_GetUser_NotFound", async () =>
            {
                HttpResponseMessage response = await _Server.AdminGetAsync($"users/{Guid.NewGuid()}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.NotFound, response.StatusCode);
            });

            await RunTest("AdminApi_CreateUser_DuplicateEmail_Returns409", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    GUID = Guid.NewGuid().ToString(),
                    Name = "DuplicateUser",
                    Email = "test@example.com"
                });

                HttpResponseMessage response = await _Server.AdminPostAsync("users", json).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Conflict, response.StatusCode);
            });

            #endregion

            #region Credentials

            await RunTest("AdminApi_CreateCredential", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    GUID = credGuid,
                    UserGUID = userGuid,
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
                HttpResponseMessage response = await _Server.AdminGetAsync($"credentials/{credGuid}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);
            });

            await RunTest("AdminApi_CreateCredential_DuplicateAccessKey_Returns409", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    GUID = Guid.NewGuid().ToString(),
                    UserGUID = userGuid,
                    Description = "Duplicate",
                    AccessKey = _Server.AccessKey,
                    SecretKey = "anothersecret",
                    IsBase64 = false
                });

                HttpResponseMessage response = await _Server.AdminPostAsync("credentials", json).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Conflict, response.StatusCode);
            });

            #endregion

            #region Buckets

            await RunTest("AdminApi_CreateBucket", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    GUID = bucketGuid,
                    OwnerGUID = userGuid,
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
                HttpResponseMessage response = await _Server.AdminGetAsync($"buckets/{bucketGuid}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);
            });

            await RunTest("AdminApi_CreateBucket_Duplicate_ReturnsError", async () =>
            {
                string json = JsonSerializer.Serialize(new
                {
                    GUID = Guid.NewGuid().ToString(),
                    OwnerGUID = userGuid,
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

            #endregion

            #region Cleanup

            await RunTest("AdminApi_DeleteBucket", async () =>
            {
                HttpResponseMessage response = await _Server.AdminDeleteAsync($"buckets/{bucketGuid}?destroy=true").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.NoContent, response.StatusCode);
            });

            await RunTest("AdminApi_DeleteBucket_NotFound", async () =>
            {
                HttpResponseMessage response = await _Server.AdminDeleteAsync($"buckets/{Guid.NewGuid()}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.NotFound, response.StatusCode);
            });

            await RunTest("AdminApi_DeleteCredential", async () =>
            {
                HttpResponseMessage response = await _Server.AdminDeleteAsync($"credentials/{credGuid}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.NoContent, response.StatusCode);
            });

            await RunTest("AdminApi_DeleteUser", async () =>
            {
                HttpResponseMessage response = await _Server.AdminDeleteAsync($"users/{userGuid}").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.NoContent, response.StatusCode);
            });

            #endregion
        }

        #endregion
    }
}
