namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    /// <summary>
    /// Raw protocol and XML response checks for the Less3 S3 interface.
    /// </summary>
    public class S3ProtocolComplianceTests : TestSuite
    {
        #region Private-Members

        private readonly Less3TestServer _Server;
        private readonly string _UserGuid = Guid.NewGuid().ToString();
        private readonly string _CredGuid = Guid.NewGuid().ToString();
        private readonly string _BucketGuid = Guid.NewGuid().ToString();
        private readonly string _BucketName = "s3-protocol-test-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        #endregion

        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "S3 Protocol Compliance Tests";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="S3ProtocolComplianceTests"/> class.
        /// </summary>
        /// <param name="server">The running Less3 test server.</param>
        public S3ProtocolComplianceTests(Less3TestServer server)
        {
            _Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs raw protocol compliance checks.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            await SetupBucket().ConfigureAwait(false);
            string? uploadId = null;

            try
            {
                await _Server.S3Client.PutBucketTaggingAsync(new PutBucketTaggingRequest
                {
                    BucketName = _BucketName,
                    TagSet = new List<Tag>
                    {
                        new Tag { Key = "Protocol", Value = "true" }
                    }
                }).ConfigureAwait(false);

                await _Server.S3Client.PutBucketVersioningAsync(new PutBucketVersioningRequest
                {
                    BucketName = _BucketName,
                    VersioningConfig = new S3BucketVersioningConfig
                    {
                        Status = VersionStatus.Enabled
                    }
                }).ConfigureAwait(false);

                await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _BucketName,
                    Key = "protocol.txt",
                    ContentBody = "version-one"
                }).ConfigureAwait(false);

                await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _BucketName,
                    Key = "protocol.txt",
                    ContentBody = "version-two"
                }).ConfigureAwait(false);

                await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _BucketName,
                    Key = "nested/path/item.txt",
                    ContentBody = "nested"
                }).ConfigureAwait(false);

                InitiateMultipartUploadResponse initiateResponse = await _Server.S3Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
                {
                    BucketName = _BucketName,
                    Key = "protocol-multipart.txt"
                }).ConfigureAwait(false);
                uploadId = initiateResponse.UploadId;

                await RunTest("S3_Protocol_ListBucketsXml", async () =>
                {
                    HttpRequestMessage request = _Server.CreateS3Request(HttpMethod.Get, "/");
                    HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertContains(body, "ListAllMyBucketsResult");
                    AssertContains(body, "<Owner>");
                    AssertContains(body, "<Buckets>");
                });

                await RunTest("S3_Protocol_ListObjectsV2Xml", async () =>
                {
                    HttpRequestMessage request = _Server.CreateS3Request(HttpMethod.Get, $"/{_BucketName}?list-type=2");
                    HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertContains(body, "ListBucketResult");
                    AssertContains(body, "<Name>");
                });

                await RunTest("S3_Protocol_GetBucketAclXml", async () =>
                {
                    HttpRequestMessage request = _Server.CreateS3Request(HttpMethod.Get, $"/{_BucketName}?acl");
                    HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertContains(body, "AccessControlPolicy");
                    AssertContains(body, "<AccessControlList>");
                });

                await RunTest("S3_Protocol_GetBucketTaggingXml", async () =>
                {
                    HttpRequestMessage request = _Server.CreateS3Request(HttpMethod.Get, $"/{_BucketName}?tagging");
                    HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertContains(body, "Tagging");
                    AssertContains(body, "<TagSet>");
                    AssertContains(body, "<Tag>");
                });

                await RunTest("S3_Protocol_GetBucketVersioningXml", async () =>
                {
                    HttpRequestMessage request = _Server.CreateS3Request(HttpMethod.Get, $"/{_BucketName}?versioning");
                    HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertContains(body, "VersioningConfiguration");
                    AssertContains(body, "Enabled");
                });

                await RunTest("S3_Protocol_ListVersionsXml", async () =>
                {
                    HttpRequestMessage request = _Server.CreateS3Request(HttpMethod.Get, $"/{_BucketName}?versions");
                    HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertContains(body, "ListVersionsResult");
                    AssertContains(body, "protocol.txt");
                });

                await RunTest("S3_Protocol_GetBucketLocationXml", async () =>
                {
                    HttpRequestMessage request = _Server.CreateS3Request(HttpMethod.Get, $"/{_BucketName}?location");
                    HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertContains(body, "LocationConstraint");
                    AssertContains(body, "us-west-1");
                });

                await RunTest("S3_Protocol_ErrorResponseXml", async () =>
                {
                    HttpRequestMessage request = _Server.CreateS3Request(HttpMethod.Get, $"/{_BucketName}/does-not-exist.txt");
                    HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.NotFound, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertContains(body, "<Error>");
                    AssertContains(body, "<Code>");
                    AssertContains(body, "NoSuchKey");
                });

                await RunTest("S3_Protocol_ListMultipartUploadsXml", async () =>
                {
                    HttpRequestMessage request = _Server.CreateS3Request(HttpMethod.Get, $"/{_BucketName}?uploads");
                    HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.StatusCode);

                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    AssertContains(body, "ListMultipartUploadsResult");
                    AssertContains(body, "protocol-multipart.txt");
                    AssertContains(body, uploadId!);
                });

                await RunTest("S3_Protocol_SequentialRequests", async () =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        HttpRequestMessage request = _Server.CreateS3Request(HttpMethod.Get, "/");
                        HttpResponseMessage response = await _Server.HttpClient.SendAsync(request).ConfigureAwait(false);
                        AssertEqual(HttpStatusCode.OK, response.StatusCode);
                    }
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(uploadId))
                {
                    try
                    {
                        await _Server.S3Client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
                        {
                            BucketName = _BucketName,
                            Key = "protocol-multipart.txt",
                            UploadId = uploadId
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
        }

        #endregion

        #region Private-Methods

        private async Task SetupBucket()
        {
            string userJson = JsonSerializer.Serialize(new
            {
                GUID = _UserGuid,
                Name = "ProtocolUser",
                Email = "protocol@example.com"
            });
            await _Server.AdminPostAsync("users", userJson).ConfigureAwait(false);

            string credJson = JsonSerializer.Serialize(new
            {
                GUID = _CredGuid,
                UserGUID = _UserGuid,
                Description = "Protocol credential",
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

        #endregion
    }
}
