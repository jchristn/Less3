namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    /// <summary>
    /// Expanded integration tests for S3 bucket features via the Less3 server.
    /// </summary>
    public class BucketAdvancedApiTests : TestSuite
    {
        #region Private-Members

        private readonly Less3TestServer _Server;
        private readonly string _UserGuid = Guid.NewGuid().ToString();
        private readonly string _CredGuid = Guid.NewGuid().ToString();
        private readonly string _BucketName = "s3-bucket-advanced-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        #endregion

        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Bucket Advanced API Tests";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketAdvancedApiTests"/> class.
        /// </summary>
        /// <param name="server">The running Less3 test server.</param>
        public BucketAdvancedApiTests(Less3TestServer server)
        {
            _Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all advanced bucket API tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            await SetupUser().ConfigureAwait(false);

            try
            {
                await RunTest("S3_BucketAdvanced_CreateBucket", async () =>
                {
                    PutBucketResponse response = await _Server.S3Client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = _BucketName
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_BucketAdvanced_DeleteBucket_NotEmptyFails", async () =>
                {
                    await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "bucket-not-empty.txt",
                        ContentBody = "not empty",
                        ContentType = "text/plain"
                    }).ConfigureAwait(false);

                    AmazonS3Exception exception = await AssertThrowsAsync<AmazonS3Exception>(async () =>
                    {
                        await _Server.S3Client.DeleteBucketAsync(new DeleteBucketRequest
                        {
                            BucketName = _BucketName
                        }).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.Conflict, exception.StatusCode);

                    await _Server.S3Client.DeleteObjectAsync(new DeleteObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "bucket-not-empty.txt"
                    }).ConfigureAwait(false);
                });

                await RunTest("S3_BucketAdvanced_GetBucketLocation", async () =>
                {
                    GetBucketLocationResponse response = await _Server.S3Client.GetBucketLocationAsync(new GetBucketLocationRequest
                    {
                        BucketName = _BucketName
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response);
                });

                await RunTest("S3_BucketAdvanced_PutBucketTagging", async () =>
                {
                    PutBucketTaggingResponse response = await _Server.S3Client.PutBucketTaggingAsync(new PutBucketTaggingRequest
                    {
                        BucketName = _BucketName,
                        TagSet = new List<Tag>
                        {
                            new Tag { Key = "Environment", Value = "Test" },
                            new Tag { Key = "Component", Value = "Less3" }
                        }
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_BucketAdvanced_GetBucketTagging", async () =>
                {
                    GetBucketTaggingResponse response = await _Server.S3Client.GetBucketTaggingAsync(new GetBucketTaggingRequest
                    {
                        BucketName = _BucketName
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.TagSet);
                    AssertEqual(2, response.TagSet.Count);
                    AssertTrue(response.TagSet.Exists(t => t.Key == "Environment" && t.Value == "Test"));
                });

                await RunTest("S3_BucketAdvanced_DeleteBucketTagging", async () =>
                {
                    DeleteBucketTaggingResponse response = await _Server.S3Client.DeleteBucketTaggingAsync(new DeleteBucketTaggingRequest
                    {
                        BucketName = _BucketName
                    }).ConfigureAwait(false);

                    AssertTrue(
                        response.HttpStatusCode == HttpStatusCode.NoContent || response.HttpStatusCode == HttpStatusCode.OK,
                        $"Expected 204 or 200 but got {(int)response.HttpStatusCode}");
                });

                await RunTest("S3_BucketAdvanced_PutBucketAcl_PublicRead", async () =>
                {
                    PutBucketAclResponse response = await _Server.S3Client.PutBucketAclAsync(new PutBucketAclRequest
                    {
                        BucketName = _BucketName,
                        ACL = S3CannedACL.PublicRead
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_BucketAdvanced_GetBucketAcl", async () =>
                {
                    GetBucketAclResponse response = await _Server.S3Client.GetBucketAclAsync(new GetBucketAclRequest
                    {
                        BucketName = _BucketName
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.Grants);
                    AssertTrue(response.Grants.Count > 0, "Expected at least one ACL grant");
                });

                await RunTest("S3_BucketAdvanced_EnableBucketVersioning", async () =>
                {
                    PutBucketVersioningResponse response = await _Server.S3Client.PutBucketVersioningAsync(new PutBucketVersioningRequest
                    {
                        BucketName = _BucketName,
                        VersioningConfig = new S3BucketVersioningConfig
                        {
                            Status = VersionStatus.Enabled
                        }
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_BucketAdvanced_GetBucketVersioning", async () =>
                {
                    GetBucketVersioningResponse response = await _Server.S3Client.GetBucketVersioningAsync(new GetBucketVersioningRequest
                    {
                        BucketName = _BucketName
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.VersioningConfig);
                    AssertEqual(VersionStatus.Enabled, response.VersioningConfig.Status);
                });
            }
            finally
            {
                try
                {
                    await _Server.S3Client.DeleteBucketAsync(new DeleteBucketRequest
                    {
                        BucketName = _BucketName
                    }).ConfigureAwait(false);
                }
                catch
                {
                }

                await CleanupUser().ConfigureAwait(false);
            }
        }

        #endregion

        #region Private-Methods

        private async Task SetupUser()
        {
            string userJson = JsonSerializer.Serialize(new
            {
                GUID = _UserGuid,
                Name = "BucketAdvancedUser",
                Email = "bucket-advanced@example.com"
            });
            await _Server.AdminPostAsync("users", userJson).ConfigureAwait(false);

            string credJson = JsonSerializer.Serialize(new
            {
                GUID = _CredGuid,
                UserGUID = _UserGuid,
                Description = "Bucket advanced credential",
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
