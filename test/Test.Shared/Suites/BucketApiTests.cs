namespace Test.Shared.Suites
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.S3.Model;
    using Amazon.S3.Util;

    /// <summary>
    /// Integration tests for S3 bucket operations via the Less3 server.
    /// </summary>
    public class BucketApiTests : TestSuite
    {
        #region Private-Members

        private Less3TestServer _Server;
        private string _UserGuid = Guid.NewGuid().ToString();
        private string _CredGuid = Guid.NewGuid().ToString();
        private string _BucketName = "s3-bucket-test-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        #endregion

        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Bucket API Tests";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketApiTests"/> class.
        /// </summary>
        /// <param name="server">The running Less3 test server.</param>
        public BucketApiTests(Less3TestServer server)
        {
            _Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all bucket API tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            await SetupUser().ConfigureAwait(false);

            await RunTest("S3_CreateBucket", async () =>
            {
                PutBucketResponse response = await _Server.S3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _BucketName
                }).ConfigureAwait(false);
                AssertTrue(
                    (int)response.HttpStatusCode == 200 || (int)response.HttpStatusCode == 201,
                    $"Expected 200 or 201 but got {(int)response.HttpStatusCode}");
            });

            await RunTest("S3_HeadBucket_Exists", async () =>
            {
                bool exists = await AmazonS3Util.DoesS3BucketExistV2Async(_Server.S3Client, _BucketName).ConfigureAwait(false);
                AssertTrue(exists);
            });

            await RunTest("S3_HeadBucket_NotExists", async () =>
            {
                bool exists = await AmazonS3Util.DoesS3BucketExistV2Async(_Server.S3Client, "nonexistent-bucket-xyz").ConfigureAwait(false);
                AssertFalse(exists);
            });

            await RunTest("S3_ListBuckets", async () =>
            {
                ListBucketsResponse response = await _Server.S3Client.ListBucketsAsync().ConfigureAwait(false);
                AssertEqual(200, (int)response.HttpStatusCode);
                AssertTrue(response.Buckets.Exists(b => String.Equals(b.BucketName, _BucketName, StringComparison.Ordinal)));
            });

            await RunTest("S3_ListObjects_EmptyBucket", async () =>
            {
                ListObjectsV2Response response = await _Server.S3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = _BucketName
                }).ConfigureAwait(false);
                AssertEqual(200, (int)response.HttpStatusCode);
                AssertEqual(0, response.S3Objects?.Count ?? 0);
            });

            await RunTest("S3_DeleteBucket", async () =>
            {
                DeleteBucketResponse response = await _Server.S3Client.DeleteBucketAsync(new DeleteBucketRequest
                {
                    BucketName = _BucketName
                }).ConfigureAwait(false);
                AssertTrue(
                    (int)response.HttpStatusCode == 200 || (int)response.HttpStatusCode == 204,
                    $"Expected 200 or 204 but got {(int)response.HttpStatusCode}");
            });

            await RunTest("S3_HeadBucket_AfterDelete", async () =>
            {
                bool exists = await AmazonS3Util.DoesS3BucketExistV2Async(_Server.S3Client, _BucketName).ConfigureAwait(false);
                AssertFalse(exists);
            });

            await CleanupUser().ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private async Task SetupUser()
        {
            string userJson = JsonSerializer.Serialize(new
            {
                GUID = _UserGuid,
                Name = "BucketTestUser",
                Email = "buckettest@example.com"
            });
            await _Server.AdminPostAsync("users", userJson).ConfigureAwait(false);

            string credJson = JsonSerializer.Serialize(new
            {
                GUID = _CredGuid,
                UserGUID = _UserGuid,
                Description = "BucketTest credential",
                AccessKey = _Server.AccessKey,
                SecretKey = _Server.SecretKey,
                IsBase64 = false
            });
            await _Server.AdminPostAsync("credentials", credJson).ConfigureAwait(false);
        }

        private async Task CleanupUser()
        {
            try { await _Server.AdminDeleteAsync($"credentials/{_CredGuid}").ConfigureAwait(false); } catch { }
            try { await _Server.AdminDeleteAsync($"users/{_UserGuid}").ConfigureAwait(false); } catch { }
        }

        #endregion
    }
}
