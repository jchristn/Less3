namespace Test.Shared.Suites
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    /// <summary>
    /// Integration tests for zero-config container startup behavior.
    /// </summary>
    public class ContainerAutoconfigTests : TestSuite
    {
        /// <summary>
        /// The display name of the test suite.
        /// </summary>
        public override string Name => "Container Autoconfig Tests";

        /// <summary>
        /// Runs all container autoconfiguration tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            using Less3TestServer server = new Less3TestServer(
                simulateContainerEnvironment: true,
                omitSystemJson: true);
            await server.StartAsync().ConfigureAwait(false);

            await RunTest("ContainerAutoconfig_SystemJsonGenerated", async () =>
            {
                await Task.Yield();
                AssertTrue(File.Exists(Path.Combine(server.TempDirectory, "system.json")));
            });

            await RunTest("ContainerAutoconfig_AdminUsersSeeded", async () =>
            {
                HttpResponseMessage response = await server.AdminGetAsync("users").ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.StatusCode);

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(body);
                AssertTrue(doc.RootElement.EnumerateArray().Any(e => e.GetProperty("GUID").GetString() == "default"));
            });

            await RunTest("ContainerAutoconfig_DefaultBucketSeeded", async () =>
            {
                using IAmazonS3 client = server.CreateS3Client();
                ListBucketsResponse response = await client.ListBucketsAsync().ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                AssertTrue(response.Buckets.Any(b => String.Equals(b.BucketName, "default", StringComparison.Ordinal)));
            });

            await RunTest("ContainerAutoconfig_SampleObjectsSeeded", async () =>
            {
                using IAmazonS3 client = server.CreateS3Client();
                ListObjectsV2Response response = await client.ListObjectsV2Async(new ListObjectsV2Request
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
