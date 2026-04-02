namespace Test.Shared.Suites
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    /// <summary>
    /// Integration tests for S3 object operations via the Less3 server.
    /// </summary>
    public class ObjectApiTests : TestSuite
    {
        #region Private-Members

        private Less3TestServer _Server;
        private string _UserGuid = Guid.NewGuid().ToString();
        private string _CredGuid = Guid.NewGuid().ToString();
        private string _BucketGuid = Guid.NewGuid().ToString();
        private string _BucketName = "s3-object-test-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        #endregion

        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Object API Tests";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectApiTests"/> class.
        /// </summary>
        /// <param name="server">The running Less3 test server.</param>
        public ObjectApiTests(Less3TestServer server)
        {
            _Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all object API tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            await SetupBucket().ConfigureAwait(false);

            try
            {
                await RunTest("S3_PutObject_Text", async () =>
                {
                    PutObjectResponse response = await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "hello.txt",
                        ContentBody = "Hello, Less3!",
                        ContentType = "text/plain"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_HeadObject_Exists", async () =>
                {
                    GetObjectMetadataResponse response = await _Server.S3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                    {
                        BucketName = _BucketName,
                        Key = "hello.txt"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertEqual(13L, response.Headers.ContentLength);
                });

                await RunTest("S3_GetObject_Text", async () =>
                {
                    using GetObjectResponse response = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "hello.txt"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);

                    string body = await ReadResponseStringAsync(response).ConfigureAwait(false);
                    AssertEqual("Hello, Less3!", body);
                });

                await RunTest("S3_PutObject_BinaryData", async () =>
                {
                    byte[] data = new byte[256];
                    for (int i = 0; i < 256; i++) data[i] = (byte)i;

                    using MemoryStream stream = new MemoryStream(data);
                    PutObjectResponse response = await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "binary.dat",
                        InputStream = stream,
                        ContentType = "application/octet-stream"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_GetObject_BinaryData", async () =>
                {
                    using GetObjectResponse response = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "binary.dat"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);

                    byte[] body = await ReadResponseBytesAsync(response).ConfigureAwait(false);
                    AssertEqual(256, body.Length);
                    for (int i = 0; i < 256; i++)
                    {
                        AssertEqual((byte)i, body[i], $"Byte mismatch at position {i}");
                    }
                });

                await RunTest("S3_PutObject_JsonContentType", async () =>
                {
                    PutObjectResponse response = await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "data.json",
                        ContentBody = "{\"key\":\"value\"}",
                        ContentType = "application/json"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_GetObject_JsonContentType", async () =>
                {
                    using GetObjectResponse response = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "data.json"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);

                    string body = await ReadResponseStringAsync(response).ConfigureAwait(false);
                    AssertEqual("{\"key\":\"value\"}", body);
                });

                await RunTest("S3_PutObject_EmptyBody", async () =>
                {
                    using MemoryStream stream = new MemoryStream(Array.Empty<byte>());
                    PutObjectResponse response = await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "empty.dat",
                        InputStream = stream,
                        ContentType = "application/octet-stream"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_GetObject_EmptyBody", async () =>
                {
                    using GetObjectResponse response = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "empty.dat"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);

                    byte[] body = await ReadResponseBytesAsync(response).ConfigureAwait(false);
                    AssertEqual(0, body.Length);
                });

                await RunTest("S3_PutObject_LargeFile", async () =>
                {
                    byte[] data = new byte[512 * 1024];
                    Random random = new Random(42);
                    random.NextBytes(data);

                    using MemoryStream stream = new MemoryStream(data);
                    PutObjectResponse response = await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "large.bin",
                        InputStream = stream,
                        ContentType = "application/octet-stream"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_GetObject_LargeFile", async () =>
                {
                    using GetObjectResponse response = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "large.bin"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);

                    byte[] body = await ReadResponseBytesAsync(response).ConfigureAwait(false);
                    AssertEqual(512 * 1024, body.Length);
                });

                await RunTest("S3_GetObject_Range", async () =>
                {
                    await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "alphabet.txt",
                        ContentBody = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                        ContentType = "text/plain"
                    }).ConfigureAwait(false);

                    using GetObjectResponse response = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "alphabet.txt",
                        ByteRange = new ByteRange(0, 4)
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.PartialContent, response.HttpStatusCode);

                    byte[] body = await ReadResponseBytesAsync(response).ConfigureAwait(false);
                    AssertEqual(5, body.Length);
                    AssertEqual("ABCDE", Encoding.UTF8.GetString(body));
                });

                await RunTest("S3_ListObjects_WithObjects", async () =>
                {
                    ListObjectsV2Response response = await _Server.S3Client.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = _BucketName
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertTrue(response.S3Objects.Exists(o => String.Equals(o.Key, "hello.txt", StringComparison.Ordinal)));
                    AssertTrue(response.S3Objects.Exists(o => String.Equals(o.Key, "binary.dat", StringComparison.Ordinal)));
                });

                await RunTest("S3_DeleteObject", async () =>
                {
                    DeleteObjectResponse response = await _Server.S3Client.DeleteObjectAsync(new DeleteObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "hello.txt"
                    }).ConfigureAwait(false);
                    AssertTrue(
                        response.HttpStatusCode == HttpStatusCode.OK || response.HttpStatusCode == HttpStatusCode.NoContent,
                        $"Expected 200 or 204 but got {(int)response.HttpStatusCode}");
                });

                await RunTest("S3_GetObject_NotExists", async () =>
                {
                    AmazonS3Exception exception = await AssertThrowsAsync<AmazonS3Exception>(async () =>
                    {
                        using GetObjectResponse response = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                        {
                            BucketName = _BucketName,
                            Key = "nonexistent.txt"
                        }).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.NotFound, exception.StatusCode);
                    AssertTrue(
                        String.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.Ordinal)
                        || String.Equals(exception.ErrorCode, "NotFound", StringComparison.Ordinal),
                        $"Expected NoSuchKey or NotFound but got [{exception.ErrorCode}]");
                });

                await RunTest("S3_GetObject_AfterDelete", async () =>
                {
                    AmazonS3Exception exception = await AssertThrowsAsync<AmazonS3Exception>(async () =>
                    {
                        using GetObjectResponse response = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                        {
                            BucketName = _BucketName,
                            Key = "hello.txt"
                        }).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.NotFound, exception.StatusCode);
                });
            }
            finally
            {
                await CleanupBucket().ConfigureAwait(false);
            }
        }

        #endregion

        #region Private-Methods

        private async Task SetupBucket()
        {
            string userJson = JsonSerializer.Serialize(new
            {
                GUID = _UserGuid,
                Name = "ObjectTestUser",
                Email = "objecttest@example.com"
            });
            await _Server.AdminPostAsync("users", userJson).ConfigureAwait(false);

            string credJson = JsonSerializer.Serialize(new
            {
                GUID = _CredGuid,
                UserGUID = _UserGuid,
                Description = "ObjectTest credential",
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

        private async Task CleanupBucket()
        {
            string[] keys = { "binary.dat", "alphabet.txt", "large.bin", "empty.dat", "data.json" };
            foreach (string key in keys)
            {
                try
                {
                    await _Server.S3Client.DeleteObjectAsync(new DeleteObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = key
                    }).ConfigureAwait(false);
                }
                catch
                {
                }
            }

            try { await _Server.AdminDeleteAsync($"buckets/{_BucketGuid}?destroy=true").ConfigureAwait(false); } catch { }
            try { await _Server.AdminDeleteAsync($"credentials/{_CredGuid}").ConfigureAwait(false); } catch { }
            try { await _Server.AdminDeleteAsync($"users/{_UserGuid}").ConfigureAwait(false); } catch { }
        }

        private static async Task<string> ReadResponseStringAsync(GetObjectResponse response)
        {
            byte[] bytes = await ReadResponseBytesAsync(response).ConfigureAwait(false);
            return Encoding.UTF8.GetString(bytes);
        }

        private static async Task<byte[]> ReadResponseBytesAsync(GetObjectResponse response)
        {
            using MemoryStream stream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(stream).ConfigureAwait(false);
            return stream.ToArray();
        }

        #endregion
    }
}
