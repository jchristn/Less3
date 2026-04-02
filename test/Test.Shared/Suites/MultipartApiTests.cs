namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    /// <summary>
    /// Integration tests for multipart upload behavior via the Less3 server.
    /// </summary>
    public class MultipartApiTests : TestSuite
    {
        #region Private-Members

        private readonly Less3TestServer _Server;
        private readonly string _UserGuid = Guid.NewGuid().ToString();
        private readonly string _CredGuid = Guid.NewGuid().ToString();
        private readonly string _BucketGuid = Guid.NewGuid().ToString();
        private readonly string _BucketName = "s3-multipart-test-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        private string? _UploadId;
        private string? _AbortUploadId;
        private PartETag? _Part1;
        private PartETag? _Part2;

        #endregion

        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Multipart API Tests";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipartApiTests"/> class.
        /// </summary>
        /// <param name="server">The running Less3 test server.</param>
        public MultipartApiTests(Less3TestServer server)
        {
            _Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all multipart API tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            await SetupBucket().ConfigureAwait(false);

            try
            {
                await RunTest("S3_Multipart_InitiateUpload", async () =>
                {
                    InitiateMultipartUploadResponse response = await _Server.S3Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-object.txt",
                        ContentType = "text/plain"
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.UploadId);
                    _UploadId = response.UploadId;
                });

                await RunTest("S3_Multipart_UploadPart1", async () =>
                {
                    AssertNotNull(_UploadId);

                    using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("part-one-"));
                    UploadPartResponse response = await _Server.S3Client.UploadPartAsync(new UploadPartRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-object.txt",
                        UploadId = _UploadId,
                        PartNumber = 1,
                        InputStream = stream
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.ETag);
                    _Part1 = new PartETag(1, response.ETag);
                });

                await RunTest("S3_Multipart_UploadPart2", async () =>
                {
                    AssertNotNull(_UploadId);

                    using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("part-two"));
                    UploadPartResponse response = await _Server.S3Client.UploadPartAsync(new UploadPartRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-object.txt",
                        UploadId = _UploadId,
                        PartNumber = 2,
                        InputStream = stream
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.ETag);
                    _Part2 = new PartETag(2, response.ETag);
                });

                await RunTest("S3_Multipart_ListMultipartUploads", async () =>
                {
                    ListMultipartUploadsResponse response = await _Server.S3Client.ListMultipartUploadsAsync(new ListMultipartUploadsRequest
                    {
                        BucketName = _BucketName
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response);
                });

                await RunTest("S3_Multipart_ListParts", async () =>
                {
                    AssertNotNull(_UploadId);

                    ListPartsResponse response = await _Server.S3Client.ListPartsAsync(new ListPartsRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-object.txt",
                        UploadId = _UploadId
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertEqual(2, response.Parts.Count);
                    AssertEqual(1, response.Parts[0].PartNumber);
                    AssertEqual(2, response.Parts[1].PartNumber);
                });

                await RunTest("S3_Multipart_CompleteUpload", async () =>
                {
                    AssertNotNull(_UploadId);
                    AssertNotNull(_Part1);
                    AssertNotNull(_Part2);

                    CompleteMultipartUploadResponse response = await _Server.S3Client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-object.txt",
                        UploadId = _UploadId,
                        PartETags = new List<PartETag> { _Part1!, _Part2! }
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.ETag);

                    using GetObjectResponse readResponse = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-object.txt"
                    }).ConfigureAwait(false);

                    string body = await ReadResponseStringAsync(readResponse).ConfigureAwait(false);
                    AssertEqual("part-one-part-two", body);
                });

                await RunTest("S3_Multipart_OverwriteExistingObjectWithoutVersioning", async () =>
                {
                    InitiateMultipartUploadResponse initiateResponse = await _Server.S3Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-object.txt",
                        ContentType = "text/plain"
                    }).ConfigureAwait(false);

                    using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("replacement"));
                    UploadPartResponse uploadResponse = await _Server.S3Client.UploadPartAsync(new UploadPartRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-object.txt",
                        UploadId = initiateResponse.UploadId,
                        PartNumber = 1,
                        InputStream = stream
                    }).ConfigureAwait(false);

                    CompleteMultipartUploadResponse response = await _Server.S3Client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-object.txt",
                        UploadId = initiateResponse.UploadId,
                        PartETags = new List<PartETag> { new PartETag(1, uploadResponse.ETag) }
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);

                    using GetObjectResponse readResponse = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-object.txt"
                    }).ConfigureAwait(false);

                    string body = await ReadResponseStringAsync(readResponse).ConfigureAwait(false);
                    AssertEqual("replacement", body);
                });

                await RunTest("S3_Multipart_AbortUpload", async () =>
                {
                    InitiateMultipartUploadResponse initiateResponse = await _Server.S3Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-abort.txt",
                        ContentType = "text/plain"
                    }).ConfigureAwait(false);
                    _AbortUploadId = initiateResponse.UploadId;

                    using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("abort-me"));
                    await _Server.S3Client.UploadPartAsync(new UploadPartRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-abort.txt",
                        UploadId = _AbortUploadId,
                        PartNumber = 1,
                        InputStream = stream
                    }).ConfigureAwait(false);

                    AbortMultipartUploadResponse response = await _Server.S3Client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
                    {
                        BucketName = _BucketName,
                        Key = "multipart-abort.txt",
                        UploadId = _AbortUploadId
                    }).ConfigureAwait(false);

                    AssertTrue(
                        response.HttpStatusCode == HttpStatusCode.NoContent || response.HttpStatusCode == HttpStatusCode.OK,
                        $"Expected 204 or 200 but got {(int)response.HttpStatusCode}");
                });

                await RunTest("S3_Multipart_ListPartsAfterAbortFails", async () =>
                {
                    AssertNotNull(_AbortUploadId);

                    AmazonS3Exception exception = await AssertThrowsAsync<AmazonS3Exception>(async () =>
                    {
                        await _Server.S3Client.ListPartsAsync(new ListPartsRequest
                        {
                            BucketName = _BucketName,
                            Key = "multipart-abort.txt",
                            UploadId = _AbortUploadId
                        }).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    AssertTrue((int)exception.StatusCode >= 400, $"Expected error status but got {(int)exception.StatusCode}");
                });
            }
            finally
            {
                try { await _Server.AdminDeleteAsync($"buckets/{_BucketGuid}?destroy=true").ConfigureAwait(false); } catch { }
                try { await _Server.AdminDeleteAsync($"credentials/{_CredGuid}").ConfigureAwait(false); } catch { }
                try { await _Server.AdminDeleteAsync($"users/{_UserGuid}").ConfigureAwait(false); } catch { }
            }
        }

        #endregion

        #region Private-Methods

        private async Task SetupBucket()
        {
            string userJson = JsonSerializer.Serialize(new
            {
                GUID = _UserGuid,
                Name = "MultipartTestUser",
                Email = "multipart@example.com"
            });
            await _Server.AdminPostAsync("users", userJson).ConfigureAwait(false);

            string credJson = JsonSerializer.Serialize(new
            {
                GUID = _CredGuid,
                UserGUID = _UserGuid,
                Description = "Multipart credential",
                AccessKey = _Server.AccessKey,
                SecretKey = _Server.SecretKey,
                IsBase64 = false
            });
            await _Server.AdminPostAsync("credentials", credJson).ConfigureAwait(false);

            string bucketJson = JsonSerializer.Serialize(new
            {
                GUID = _BucketGuid,
                OwnerGUID = _UserGuid,
                Name = _BucketName
            });
            await _Server.AdminPostAsync("buckets", bucketJson).ConfigureAwait(false);
        }

        private static async Task<string> ReadResponseStringAsync(GetObjectResponse response)
        {
            using StreamReader reader = new StreamReader(response.ResponseStream, Encoding.UTF8, true, 1024, leaveOpen: true);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        #endregion
    }
}
