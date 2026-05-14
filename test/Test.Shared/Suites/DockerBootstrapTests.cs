namespace Test.Shared.Suites
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    /// <summary>
    /// Integration tests for first-run container bootstrap behavior.
    /// </summary>
    public class DockerBootstrapTests : TestSuite
    {
        /// <summary>
        /// The display name of the test suite.
        /// </summary>
        public override string Name => "Docker Bootstrap Tests";

        /// <summary>
        /// Runs all Docker bootstrap tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            using Less3TestServer server = new Less3TestServer(simulateContainerEnvironment: true);
            await server.StartAsync().ConfigureAwait(false);

            using IAmazonS3 seededClient = server.CreateS3Client(accessKey: "default", secretKey: "default");

            await RunTest("DockerBootstrap_AdminUsersSeeded", async () =>
            {
                var response = await server.AdminGetAsync("users").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(body);
                AssertTrue(doc.RootElement.ValueKind == JsonValueKind.Array);
                AssertTrue(doc.RootElement.EnumerateArray().Any(e => e.GetProperty("GUID").GetString() == "default"));
            });

            await RunTest("DockerBootstrap_AdminCredentialsSeeded", async () =>
            {
                var response = await server.AdminGetAsync("credentials").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(body);
                AssertTrue(doc.RootElement.EnumerateArray().Any(e => e.GetProperty("AccessKey").GetString() == "default"));
            });

            await RunTest("DockerBootstrap_DefaultBucketSeeded", async () =>
            {
                ListBucketsResponse response = await seededClient.ListBucketsAsync().ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                AssertTrue(response.Buckets.Any(b => String.Equals(b.BucketName, "default", StringComparison.Ordinal)));
            });

            await RunTest("DockerBootstrap_SampleObjectsSeeded", async () =>
            {
                ListObjectsV2Response response = await seededClient.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = "default"
                }).ConfigureAwait(false);

                AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                AssertTrue(response.S3Objects.Any(o => String.Equals(o.Key, "hello.html", StringComparison.Ordinal)));
                AssertTrue(response.S3Objects.Any(o => String.Equals(o.Key, "hello.txt", StringComparison.Ordinal)));
                AssertTrue(response.S3Objects.Any(o => String.Equals(o.Key, "hello.json", StringComparison.Ordinal)));
            });
        }
    }
}
