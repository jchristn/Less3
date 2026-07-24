namespace Test.Shared.Suites
{
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;

    /// <summary>
    /// Regression tests for object metadata handling.
    /// </summary>
    public class ObjectMetadataRegressionTests : TestSuite
    {
        private readonly Less3TestServer _Server;

        /// <inheritdoc />
        public override string Name => "Object Metadata Regression Tests";

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectMetadataRegressionTests"/> class.
        /// </summary>
        public ObjectMetadataRegressionTests(Less3TestServer server)
        {
            _Server = server;
        }

        /// <inheritdoc />
        public override async Task RunTestsAsync()
        {
            await RunTest(
                "ObjectMetadata_GetObjectWithoutUserMetadataDoesNotFail",
                GetObjectWithoutUserMetadataDoesNotFailAsync).ConfigureAwait(false);
        }

        private async Task GetObjectWithoutUserMetadataDoesNotFailAsync()
        {
            string userId = TestIds.User();
            string credentialId = TestIds.Credential();
            string bucketId = TestIds.Bucket();
            string bucketName = "metadata-regression-" + TestIds.Suffix().Substring(0, 8);
            string accessKey = "meta-" + TestIds.Suffix().Substring(0, 8);
            string secretKey = "secret-" + TestIds.Suffix();

            using IAmazonS3 s3Client = _Server.CreateS3Client(accessKey, secretKey);

            try
            {
                HttpResponseMessage userResponse = await _Server.AdminPostAsync("users", JsonSerializer.Serialize(new
                {
                    Id = userId,
                    Name = "MetadataRegressionUser",
                    Email = "metadata-regression@example.com"
                })).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Created, userResponse.StatusCode, "create metadata regression user");

                HttpResponseMessage credentialResponse = await _Server.AdminPostAsync("credentials", JsonSerializer.Serialize(new
                {
                    Id = credentialId,
                    UserId = userId,
                    Description = "Metadata regression credential",
                    AccessKey = accessKey,
                    SecretKey = secretKey,
                    IsBase64 = false
                })).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Created, credentialResponse.StatusCode, "create metadata regression credential");

                HttpResponseMessage bucketResponse = await _Server.AdminPostAsync("buckets", JsonSerializer.Serialize(new
                {
                    Id = bucketId,
                    OwnerId = userId,
                    Name = bucketName
                })).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.Created, bucketResponse.StatusCode, "create metadata regression bucket");

                PutObjectResponse putResponse = await s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = "plain.txt",
                    ContentBody = "hello-without-metadata",
                    ContentType = "text/plain"
                }).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, putResponse.HttpStatusCode, "put object without user metadata");

                GetObjectMetadataResponse headResponse = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = "plain.txt"
                }).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, headResponse.HttpStatusCode, "head object without user metadata");

                using GetObjectResponse getResponse = await s3Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = "plain.txt"
                }).ConfigureAwait(false);
                AssertEqual(HttpStatusCode.OK, getResponse.HttpStatusCode, "get object without user metadata");

                using StreamReader reader = new StreamReader(getResponse.ResponseStream, Encoding.UTF8, true, 1024, leaveOpen: true);
                string body = await reader.ReadToEndAsync().ConfigureAwait(false);
                AssertEqual("hello-without-metadata", body, "object body without user metadata");
            }
            finally
            {
                try { await _Server.AdminDeleteAsync("buckets/" + bucketId + "?destroy=true").ConfigureAwait(false); } catch { }
                try { await _Server.AdminDeleteAsync("credentials/" + credentialId).ConfigureAwait(false); } catch { }
                try { await _Server.AdminDeleteAsync("users/" + userId).ConfigureAwait(false); } catch { }
            }
        }
    }
}
