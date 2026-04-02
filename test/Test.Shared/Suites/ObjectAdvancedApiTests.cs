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
    /// Expanded integration tests for S3 object features via the Less3 server.
    /// </summary>
    public class ObjectAdvancedApiTests : TestSuite
    {
        #region Private-Members

        private readonly Less3TestServer _Server;
        private readonly string _UserGuid = Guid.NewGuid().ToString();
        private readonly string _CredGuid = Guid.NewGuid().ToString();
        private readonly string _BucketGuid = Guid.NewGuid().ToString();
        private readonly string _BucketName = "s3-object-advanced-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        private string? _VersionId1;
        private string? _VersionId2;

        #endregion

        #region Public-Members

        /// <summary>
        /// The display name of this test suite.
        /// </summary>
        public override string Name => "Object Advanced API Tests";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectAdvancedApiTests"/> class.
        /// </summary>
        /// <param name="server">The running Less3 test server.</param>
        public ObjectAdvancedApiTests(Less3TestServer server)
        {
            _Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Runs all advanced object API tests.
        /// </summary>
        public override async Task RunTestsAsync()
        {
            await SetupBucket().ConfigureAwait(false);

            try
            {
                await RunTest("S3_ObjectAdvanced_PutObject_WithMetadata", async () =>
                {
                    PutObjectRequest request = new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "metadata.txt",
                        ContentBody = "metadata-body",
                        ContentType = "text/plain"
                    };
                    request.Metadata.Add("color", "blue");
                    request.Metadata.Add("shape", "circle");

                    PutObjectResponse response = await _Server.S3Client.PutObjectAsync(request).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_ObjectAdvanced_HeadObject_ReturnsMetadata", async () =>
                {
                    GetObjectMetadataResponse response = await _Server.S3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                    {
                        BucketName = _BucketName,
                        Key = "metadata.txt"
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    string metadataKeys = string.Join(",", response.Metadata.Keys);
                    AssertTrue(metadataKeys.Contains("color", StringComparison.OrdinalIgnoreCase));
                    AssertTrue(metadataKeys.Contains("shape", StringComparison.OrdinalIgnoreCase));
                });

                await RunTest("S3_ObjectAdvanced_OverwriteObject_WithoutVersioning", async () =>
                {
                    PutObjectResponse response = await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "overwrite.txt",
                        ContentBody = "first-version",
                        ContentType = "text/plain"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);

                    response = await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "overwrite.txt",
                        ContentBody = "second-version",
                        ContentType = "text/plain"
                    }).ConfigureAwait(false);
                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);

                    using GetObjectResponse readResponse = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "overwrite.txt"
                    }).ConfigureAwait(false);

                    string body = await ReadResponseStringAsync(readResponse).ConfigureAwait(false);
                    AssertEqual("second-version", body);
                });

                await RunTest("S3_ObjectAdvanced_PrefixAndDelimiterListing", async () =>
                {
                    await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "prefix/root.txt",
                        ContentBody = "root"
                    }).ConfigureAwait(false);

                    await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "prefix/folder/child.txt",
                        ContentBody = "child"
                    }).ConfigureAwait(false);

                    ListObjectsV2Response response = await _Server.S3Client.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = _BucketName,
                        Prefix = "prefix/",
                        Delimiter = "/"
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertTrue(response.S3Objects.Exists(o => string.Equals(o.Key, "prefix/root.txt", StringComparison.Ordinal)));
                    AssertTrue(response.CommonPrefixes.Contains("prefix/folder/"));
                });

                await RunTest("S3_ObjectAdvanced_PutObjectTagging", async () =>
                {
                    PutObjectTaggingResponse response = await _Server.S3Client.PutObjectTaggingAsync(new PutObjectTaggingRequest
                    {
                        BucketName = _BucketName,
                        Key = "metadata.txt",
                        Tagging = new Amazon.S3.Model.Tagging
                        {
                            TagSet = new System.Collections.Generic.List<Tag>
                            {
                                new Tag { Key = "Type", Value = "Metadata" },
                                new Tag { Key = "Owner", Value = "Less3" }
                            }
                        }
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_ObjectAdvanced_GetObjectTagging", async () =>
                {
                    GetObjectTaggingResponse response = await _Server.S3Client.GetObjectTaggingAsync(new GetObjectTaggingRequest
                    {
                        BucketName = _BucketName,
                        Key = "metadata.txt"
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.Tagging);
                    AssertEqual(2, response.Tagging.Count);
                    AssertTrue(response.Tagging.Exists(t => t.Key == "Type" && t.Value == "Metadata"));
                });

                await RunTest("S3_ObjectAdvanced_DeleteObjectTagging", async () =>
                {
                    DeleteObjectTaggingResponse response = await _Server.S3Client.DeleteObjectTaggingAsync(new DeleteObjectTaggingRequest
                    {
                        BucketName = _BucketName,
                        Key = "metadata.txt"
                    }).ConfigureAwait(false);

                    AssertTrue(
                        response.HttpStatusCode == HttpStatusCode.NoContent || response.HttpStatusCode == HttpStatusCode.OK,
                        $"Expected 204 or 200 but got {(int)response.HttpStatusCode}");
                });

                await RunTest("S3_ObjectAdvanced_PutObjectAcl_PublicRead", async () =>
                {
                    PutObjectAclResponse response = await _Server.S3Client.PutObjectAclAsync(new PutObjectAclRequest
                    {
                        BucketName = _BucketName,
                        Key = "metadata.txt",
                        ACL = S3CannedACL.PublicRead
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                });

                await RunTest("S3_ObjectAdvanced_GetObjectAcl", async () =>
                {
                    GetObjectAclResponse response = await _Server.S3Client.GetObjectAclAsync(new GetObjectAclRequest
                    {
                        BucketName = _BucketName,
                        Key = "metadata.txt"
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.Grants);
                    AssertTrue(response.Grants.Count > 0, "Expected ACL grants to be returned");
                });

                await RunTest("S3_ObjectAdvanced_DeleteMultiple", async () =>
                {
                    await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "multi-1.txt",
                        ContentBody = "1"
                    }).ConfigureAwait(false);

                    await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "multi-2.txt",
                        ContentBody = "2"
                    }).ConfigureAwait(false);

                    DeleteObjectsResponse response = await _Server.S3Client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = _BucketName,
                        Objects = new System.Collections.Generic.List<KeyVersion>
                        {
                            new KeyVersion { Key = "multi-1.txt" },
                            new KeyVersion { Key = "multi-2.txt" }
                        }
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertEqual(2, response.DeletedObjects.Count);
                });

                await RunTest("S3_ObjectAdvanced_InvalidRangeFails", async () =>
                {
                    AmazonS3Exception exception = await AssertThrowsAsync<AmazonS3Exception>(async () =>
                    {
                        using GetObjectResponse response = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                        {
                            BucketName = _BucketName,
                            Key = "overwrite.txt",
                            ByteRange = new ByteRange(1000, 2000)
                        }).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    AssertTrue((int)exception.StatusCode >= 400, $"Expected error status but got {(int)exception.StatusCode}");
                });

                await RunTest("S3_ObjectAdvanced_EnableBucketVersioning", async () =>
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

                await RunTest("S3_ObjectAdvanced_WriteVersionedObject_V1", async () =>
                {
                    PutObjectResponse response = await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "versioned.txt",
                        ContentBody = "version-one"
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.VersionId);
                    _VersionId1 = response.VersionId;
                });

                await RunTest("S3_ObjectAdvanced_WriteVersionedObject_V2", async () =>
                {
                    PutObjectResponse response = await _Server.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "versioned.txt",
                        ContentBody = "version-two"
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertNotNull(response.VersionId);
                    _VersionId2 = response.VersionId;
                    AssertNotEqual(_VersionId1, _VersionId2);
                });

                await RunTest("S3_ObjectAdvanced_GetSpecificVersion", async () =>
                {
                    AssertNotNull(_VersionId1);

                    using GetObjectResponse response = await _Server.S3Client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = _BucketName,
                        Key = "versioned.txt",
                        VersionId = _VersionId1
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    string body = await ReadResponseStringAsync(response).ConfigureAwait(false);
                    AssertEqual("version-one", body);
                });

                await RunTest("S3_ObjectAdvanced_ListVersions_ReturnsMultipleVersions", async () =>
                {
                    ListVersionsResponse response = await _Server.S3Client.ListVersionsAsync(new ListVersionsRequest
                    {
                        BucketName = _BucketName,
                        Prefix = "versioned.txt"
                    }).ConfigureAwait(false);

                    AssertEqual(HttpStatusCode.OK, response.HttpStatusCode);
                    AssertTrue(response.Versions.Count >= 2, "Expected at least two versions");
                    AssertTrue(response.Versions.Exists(v => string.Equals(v.Key, "versioned.txt", StringComparison.Ordinal) && v.VersionId == _VersionId1));
                    AssertTrue(response.Versions.Exists(v => string.Equals(v.Key, "versioned.txt", StringComparison.Ordinal) && v.VersionId == _VersionId2));
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
                Name = "ObjectAdvancedUser",
                Email = "object-advanced@example.com"
            });
            await _Server.AdminPostAsync("users", userJson).ConfigureAwait(false);

            string credJson = JsonSerializer.Serialize(new
            {
                GUID = _CredGuid,
                UserGUID = _UserGuid,
                Description = "Object advanced credential",
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
