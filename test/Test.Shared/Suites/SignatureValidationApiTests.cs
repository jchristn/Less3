namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    /// <summary>
    /// Integration tests for AWS signature validation via the Less3 server.
    /// </summary>
    public class SignatureValidationApiTests : TestSuite
    {
        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Signature Validation API Tests";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all signature validation tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            using Less3TestServer server = new Less3TestServer(validateSignatures: true);
            await server.StartAsync().ConfigureAwait(false);

            string userGuid = Guid.NewGuid().ToString();
            string credGuid = Guid.NewGuid().ToString();
            string bucketGuid = Guid.NewGuid().ToString();
            string bucketName = "s3-signature-test-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            try
            {
                await SetupUserAndCredential(server, userGuid, credGuid).ConfigureAwait(false);
                await SetupBucket(server, userGuid, bucketGuid, bucketName).ConfigureAwait(false);

                await RunTest("S3_Signature_CreateBucket", async () =>
                {
                    string createdBucketName = bucketName + "-created";
                    PutBucketResponse response = await server.S3Client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = createdBucketName
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);

                    DeleteBucketResponse deleteResponse = await server.S3Client.DeleteBucketAsync(new DeleteBucketRequest
                    {
                        BucketName = createdBucketName
                    }).ConfigureAwait(false);
                    AssertTrue(
                        deleteResponse.HttpStatusCode == HttpStatusCode.NoContent || deleteResponse.HttpStatusCode == HttpStatusCode.OK,
                        $"Expected 204 or 200 but got {(int)deleteResponse.HttpStatusCode}");
                });

                await RunTest("S3_Signature_ListBuckets", async () =>
                {
                    ListBucketsResponse response = await server.S3Client.ListBucketsAsync().ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertTrue(response.Buckets.Exists(b => string.Equals(b.BucketName, bucketName, StringComparison.Ordinal)));
                });

                await RunTest("S3_Signature_PutObject", async () =>
                {
                    PutObjectResponse response = await server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = "sig-test.txt",
                        ContentBody = "signature test content",
                        ContentType = "text/plain"
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_Signature_GetAndHeadObject", async () =>
                {
                    GetObjectMetadataResponse metadataResponse = await server.S3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                    {
                        BucketName = bucketName,
                        Key = "sig-test.txt"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, metadataResponse.HttpStatusCode);

                    using GetObjectResponse response = await server.S3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = bucketName,
                        Key = "sig-test.txt"
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    using StreamReader reader = new StreamReader(response.ResponseStream, Encoding.UTF8, true, 1024, leaveOpen: true);
                    string body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    AssertEqual("signature test content", body);
                });

                await RunTest("S3_Signature_PutBucketTagging", async () =>
                {
                    PutBucketTaggingResponse response = await server.S3Client.PutBucketTaggingAsync(new PutBucketTaggingRequest
                    {
                        BucketName = bucketName,
                        TagSet = new List<Tag>
                        {
                            new Tag { Key = "Signed", Value = "true" }
                        }
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_Signature_InitiateMultipartUpload", async () =>
                {
                    InitiateMultipartUploadResponse response = await server.S3Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
                    {
                        BucketName = bucketName,
                        Key = "sig-multipart.txt",
                        ContentType = "text/plain"
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.UploadId);
                });

                await RunTest("S3_Signature_DeleteMultiple", async () =>
                {
                    await server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = "del1.txt",
                        ContentBody = "1"
                    }).ConfigureAwait(false);

                    await server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = "del2.txt",
                        ContentBody = "2"
                    }).ConfigureAwait(false);

                    DeleteObjectsResponse response = await server.S3Client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = bucketName,
                        Objects = new List<KeyVersion>
                        {
                            new KeyVersion { Key = "del1.txt" },
                            new KeyVersion { Key = "del2.txt" }
                        }
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertEqual(2, response.DeletedObjects.Count);
                });

                await RunTest("S3_Signature_WrongSecretRejected", async () =>
                {
                    using IAmazonS3 wrongClient = server.CreateS3Client(secretKey: "WRONG_SECRET_KEY_FOR_TESTING_1234567");

                    AmazonS3Exception exception = await AssertThrowsAsync<AmazonS3Exception>(async () =>
                    {
                        await wrongClient.ListBucketsAsync().ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.Forbidden, exception.StatusCode);
                });

                await RunTest("S3_Signature_UnknownAccessKeyRejected", async () =>
                {
                    using IAmazonS3 unknownClient = server.CreateS3Client(accessKey: "AKIAUNKNOWNKEYEXAMPLE", secretKey: "SomeRandomSecretKeyForTestPurposes123");

                    await AssertThrowsAsync<Exception>(async () =>
                    {
                        await unknownClient.ListBucketsAsync().ConfigureAwait(false);
                    }).ConfigureAwait(false);
                });

                await RunTest("S3_Signature_V2Rejected", async () =>
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, server.BaseUrl + "/");
                    request.Headers.TryAddWithoutValidation("Authorization", $"AWS {server.AccessKey}:somesignature");
                    request.Headers.TryAddWithoutValidation("Date", DateTime.UtcNow.ToString("R"));

                    HttpResponseMessage response = await server.HttpClient.SendAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.Forbidden, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertContains(body, "SignatureDoesNotMatch");
                });
            }
            finally
            {
                try { await server.AdminDeleteAsync($"buckets/{bucketGuid}?destroy=true").ConfigureAwait(false); } catch { }
                try { await server.AdminDeleteAsync($"credentials/{credGuid}").ConfigureAwait(false); } catch { }
                try { await server.AdminDeleteAsync($"users/{userGuid}").ConfigureAwait(false); } catch { }
            }
        }

        #endregion

        #region Private-Methods

        private static async Task SetupUserAndCredential(Less3TestServer server, string userGuid, string credGuid)
        {
            string userJson = JsonSerializer.Serialize(new
            {
                GUID = userGuid,
                Name = "SignatureUser",
                Email = "signature@example.com"
            });
            await server.AdminPostAsync("users", userJson).ConfigureAwait(false);

            string credJson = JsonSerializer.Serialize(new
            {
                GUID = credGuid,
                UserGUID = userGuid,
                Description = "Signature credential",
                AccessKey = server.AccessKey,
                SecretKey = server.SecretKey,
                IsBase64 = false
            });
            await server.AdminPostAsync("credentials", credJson).ConfigureAwait(false);
        }

        private static async Task SetupBucket(Less3TestServer server, string userGuid, string bucketGuid, string bucketName)
        {
            string bucketJson = JsonSerializer.Serialize(new
            {
                GUID = bucketGuid,
                OwnerGUID = userGuid,
                Name = bucketName
            });
            await server.AdminPostAsync("buckets", bucketJson).ConfigureAwait(false);
        }

        #endregion
    }
}
