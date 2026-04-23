namespace Test.Xunit
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Test.Xunit.Fixtures;
    using global::Xunit;

    /// <summary>
    /// Regression tests for object metadata handling.
    /// </summary>
    [Collection("Integration")]
    public class ObjectMetadataRegressionTests
    {
        private readonly Less3TestServerFixture _Fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectMetadataRegressionTests"/> class.
        /// </summary>
        /// <param name="fixture">The shared test server fixture.</param>
        public ObjectMetadataRegressionTests(Less3TestServerFixture fixture)
        {
            _Fixture = fixture;
        }

        /// <summary>
        /// Verifies that objects written without user metadata remain readable.
        /// </summary>
        [Fact]
        public async Task GetObject_WithoutUserMetadata_DoesNotFail()
        {
            string userGuid = Guid.NewGuid().ToString();
            string credentialGuid = Guid.NewGuid().ToString();
            string bucketGuid = Guid.NewGuid().ToString();
            string bucketName = "metadata-regression-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string accessKey = "meta-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            string secretKey = "secret-" + Guid.NewGuid().ToString("N");

            IAmazonS3 s3Client = _Fixture.Server.CreateS3Client(accessKey, secretKey);

            try
            {
                string userJson = JsonSerializer.Serialize(new
                {
                    GUID = userGuid,
                    Name = "MetadataRegressionUser",
                    Email = "metadata-regression@example.com"
                });
                await _Fixture.Server.AdminPostAsync("users", userJson);

                string credentialJson = JsonSerializer.Serialize(new
                {
                    GUID = credentialGuid,
                    UserGUID = userGuid,
                    Description = "Metadata regression credential",
                    AccessKey = accessKey,
                    SecretKey = secretKey,
                    IsBase64 = false
                });
                await _Fixture.Server.AdminPostAsync("credentials", credentialJson);

                string bucketJson = JsonSerializer.Serialize(new
                {
                    GUID = bucketGuid,
                    OwnerGUID = userGuid,
                    Name = bucketName
                });
                await _Fixture.Server.AdminPostAsync("buckets", bucketJson);

                PutObjectResponse putResponse = await s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = "plain.txt",
                    ContentBody = "hello-without-metadata",
                    ContentType = "text/plain"
                });

                Assert.Equal(HttpStatusCode.OK, putResponse.HttpStatusCode);

                GetObjectMetadataResponse headResponse = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = "plain.txt"
                });

                Assert.Equal(HttpStatusCode.OK, headResponse.HttpStatusCode);

                using GetObjectResponse getResponse = await s3Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = "plain.txt"
                });

                Assert.Equal(HttpStatusCode.OK, getResponse.HttpStatusCode);

                using StreamReader reader = new StreamReader(getResponse.ResponseStream, Encoding.UTF8, true, 1024, leaveOpen: true);
                string body = await reader.ReadToEndAsync();
                Assert.Equal("hello-without-metadata", body);
            }
            finally
            {
                s3Client.Dispose();

                try { await _Fixture.Server.AdminDeleteAsync($"buckets/{bucketGuid}?destroy=true"); } catch { }
                try { await _Fixture.Server.AdminDeleteAsync($"credentials/{credentialGuid}"); } catch { }
                try { await _Fixture.Server.AdminDeleteAsync($"users/{userGuid}"); } catch { }
            }
        }
    }
}
