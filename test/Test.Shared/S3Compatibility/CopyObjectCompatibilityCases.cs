namespace Test.Shared.S3Compatibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Touchstone.Core;

    /// <summary>
    /// CopyObject and UploadPartCopy compatibility with Amazon S3. Before these were implemented a server-side
    /// copy was treated as a PUT with an empty body, silently replacing the destination with a 0-byte object.
    /// </summary>
    public static class CopyObjectCompatibilityCases
    {
        #region Public-Methods

        /// <summary>
        /// Suite descriptor.
        /// </summary>
        public static TestSuiteDescriptor Suite()
        {
            const string suite = "S3CompatCopy";
            return new TestSuiteDescriptor(
                suiteId: suite,
                displayName: "S3 Compatibility: CopyObject",
                cases: new List<TestCaseDescriptor>
                {
                    S3CompatibilitySuites.Case(suite, "SameBucket_CopiesContentAndMetadata", "CopyObject copies content, content type, user and system metadata", SameBucketAsync),
                    S3CompatibilitySuites.Case(suite, "CrossBucket", "CopyObject copies between buckets", CrossBucketAsync),
                    S3CompatibilitySuites.Case(suite, "ResponseIsCopyObjectResult", "The response is a CopyObjectResult with the new ETag", ResponseShapeAsync),
                    S3CompatibilitySuites.Case(suite, "MissingSource_404_DestinationUntouched", "A missing source fails with NoSuchKey and leaves the destination untouched", MissingSourceAsync),
                    S3CompatibilitySuites.Case(suite, "MissingSourceBucket_404", "A missing source bucket fails with NoSuchBucket", MissingSourceBucketAsync),
                    S3CompatibilitySuites.Case(suite, "InvalidCopySourceHeader_400", "A malformed x-amz-copy-source is rejected", InvalidHeaderAsync),
                    S3CompatibilitySuites.Case(suite, "SpecificSourceVersion", "?versionId= on the copy source copies that version and reports it", SourceVersionAsync),
                    S3CompatibilitySuites.Case(suite, "SourceIsDeleteMarker_Fails", "Copying a key whose latest version is a delete marker fails", SourceDeleteMarkerAsync),
                    S3CompatibilitySuites.Case(suite, "MetadataDirectiveReplace", "MetadataDirective REPLACE takes content type and metadata from the request", MetadataReplaceAsync),
                    S3CompatibilitySuites.Case(suite, "InvalidDirective_400", "An unknown metadata directive is rejected", InvalidDirectiveAsync),
                    S3CompatibilitySuites.Case(suite, "OntoSelf_RequiresReplace", "Copying an object onto itself without REPLACE is rejected; with REPLACE it updates metadata", OntoSelfAsync),
                    S3CompatibilitySuites.Case(suite, "TaggingDirective", "Tags are copied by default and replaced from x-amz-tagging with REPLACE", TaggingDirectiveAsync),
                    S3CompatibilitySuites.Case(suite, "CopySourceConditions", "x-amz-copy-source-if-match and -if-none-match are enforced with 412", CopySourceConditionsAsync),
                    S3CompatibilitySuites.Case(suite, "SourceReadPermissionRequired", "A caller without read access to the source cannot copy it", SourcePermissionAsync),
                    S3CompatibilitySuites.Case(suite, "RestoreOldVersionByCopy", "Copying an older version onto its own key makes it the latest version", RestoreByCopyAsync),
                    S3CompatibilitySuites.Case(suite, "UploadPartCopy_FullAndRanged", "UploadPartCopy copies whole objects and byte ranges into a multipart upload", UploadPartCopyAsync),
                    S3CompatibilitySuites.Case(suite, "UploadPartCopy_SourceConditions", "UploadPartCopy enforces x-amz-copy-source-if-match", UploadPartCopyConditionsAsync),
                    S3CompatibilitySuites.Case(suite, "UploadPartCopy_InvalidRange_400", "An out-of-bounds x-amz-copy-source-range is rejected", UploadPartCopyBadRangeAsync)
                });
        }

        #endregion

        #region Private-Methods

        private static async Task<RawResponse> CopyRawAsync(TestBucket tb, string destinationKey, string copySource, Dictionary<string, string>? extraHeaders, CancellationToken token, string? destinationBucket = null)
        {
            Dictionary<string, string> headers = new Dictionary<string, string> { { "x-amz-copy-source", copySource } };
            if (extraHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in extraHeaders) headers[header.Key] = header.Value;
            }

            return await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + (destinationBucket ?? tb.Name) + "/" + destinationKey, headers, null, token).ConfigureAwait(false);
        }

        private static async Task SameBucketAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("csame", false, token).ConfigureAwait(false);
            PutObjectRequest put = new PutObjectRequest { BucketName = tb.Name, Key = "src.txt", ContentBody = "copy me", ContentType = "text/x-custom" };
            put.Metadata.Add("color", "blue");
            put.Headers.CacheControl = "max-age=60";
            await tb.Client.PutObjectAsync(put, token).ConfigureAwait(false);

            CopyObjectResponse copy = await tb.Client.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = tb.Name,
                SourceKey = "src.txt",
                DestinationBucket = tb.Name,
                DestinationKey = "dst.txt"
            }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, copy.HttpStatusCode, "status");

            Check.Equal("copy me", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "dst.txt", null, token).ConfigureAwait(false), "content");
            GetObjectMetadataResponse meta = await tb.Client.GetObjectMetadataAsync(tb.Name, "dst.txt", token).ConfigureAwait(false);
            Check.Equal("text/x-custom", meta.Headers.ContentType, "content type");
            Check.Equal("blue", meta.Metadata["x-amz-meta-color"], "user metadata");
            Check.Equal("max-age=60", meta.Headers.CacheControl, "system metadata");
            Check.Equal("copy me", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "src.txt", null, token).ConfigureAwait(false), "source unchanged");
        }

        private static async Task CrossBucketAsync(CancellationToken token)
        {
            using TestBucket source = await TestBucket.CreateAsync("csrc", false, token).ConfigureAwait(false);
            string destination = await S3CompatibilityContext.NewBucketAsync(source.Client, "cdst", false, token).ConfigureAwait(false);
            byte[] data = new byte[300 * 1024];
            new Random(7).NextBytes(data);
            await source.Client.PutObjectAsync(new PutObjectRequest { BucketName = source.Name, Key = "blob.bin", InputStream = new System.IO.MemoryStream(data) }, token).ConfigureAwait(false);

            await source.Client.CopyObjectAsync(new CopyObjectRequest { SourceBucket = source.Name, SourceKey = "blob.bin", DestinationBucket = destination, DestinationKey = "copied/blob.bin" }, token).ConfigureAwait(false);

            RawResponse copied = await S3CompatibilityContext.SendAsync(source.Server, HttpMethod.Get, "/" + destination + "/copied/blob.bin", null, null, token).ConfigureAwait(false);
            Check.True(copied.Body.AsSpan().SequenceEqual(data), "binary content copied exactly");
        }

        private static async Task ResponseShapeAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("cshape", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "src", "abc", token).ConfigureAwait(false);

            RawResponse raw = await CopyRawAsync(tb, "dst", "/" + tb.Name + "/src", null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, raw.Status, "status");
            Check.Contains(raw.Text, "<CopyObjectResult", "root element");
            string md5 = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("abc"))).ToLowerInvariant();
            Check.Contains(raw.Text, "<ETag>\"" + md5 + "\"</ETag>", "ETag of the copied content");
            Check.Contains(raw.Text, "<LastModified>", "LastModified");
        }

        private static async Task MissingSourceAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("cmiss", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "dst", "original", token).ConfigureAwait(false);

            AmazonS3Exception e = await S3CompatibilityContext.ExpectErrorAsync(
                () => tb.Client.CopyObjectAsync(new CopyObjectRequest { SourceBucket = tb.Name, SourceKey = "nope", DestinationBucket = tb.Name, DestinationKey = "dst" }, token),
                HttpStatusCode.NotFound,
                "missing source").ConfigureAwait(false);
            Check.Equal("NoSuchKey", e.ErrorCode, "code");
            Check.Equal("original", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "dst", null, token).ConfigureAwait(false), "destination untouched, not truncated to 0 bytes");
        }

        private static async Task MissingSourceBucketAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("cmissb", false, token).ConfigureAwait(false);
            RawResponse raw = await CopyRawAsync(tb, "dst", "/no-such-bucket-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "/k", null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotFound, raw.Status, "status");
            Check.Contains(raw.Text, "NoSuchBucket", "code");
        }

        private static async Task InvalidHeaderAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("cbadhdr", false, token).ConfigureAwait(false);
            foreach (string bad in new[] { "nobucketorkey", "/bucket-only/", "/" + tb.Name + "/k?versionId=not-a-version" })
            {
                RawResponse raw = await CopyRawAsync(tb, "dst", bad, null, token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.BadRequest, raw.Status, "copy source " + bad);
            }

            await S3CompatibilityContext.ExpectErrorAsync(() => tb.Client.GetObjectMetadataAsync(tb.Name, "dst", token), HttpStatusCode.NotFound, "no destination created").ConfigureAwait(false);
        }

        private static async Task SourceVersionAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("cver", true, token).ConfigureAwait(false);
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "src", "old", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "src", "new", token).ConfigureAwait(false);

            CopyObjectResponse copy = await tb.Client.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = tb.Name,
                SourceKey = "src",
                SourceVersionId = v1.VersionId,
                DestinationBucket = tb.Name,
                DestinationKey = "dst"
            }, token).ConfigureAwait(false);

            Check.Equal(v1.VersionId, copy.SourceVersionId, "x-amz-copy-source-version-id");
            Check.False(String.IsNullOrEmpty(copy.VersionId), "destination version reported");
            Check.Equal("old", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "dst", null, token).ConfigureAwait(false), "older version copied");
        }

        private static async Task SourceDeleteMarkerAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("cmarker", true, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "src", "x", token).ConfigureAwait(false);
            DeleteObjectResponse marker = await tb.Client.DeleteObjectAsync(tb.Name, "src", token).ConfigureAwait(false);

            RawResponse latest = await CopyRawAsync(tb, "dst", "/" + tb.Name + "/src", null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotFound, latest.Status, "latest is a delete marker");

            RawResponse named = await CopyRawAsync(tb, "dst", "/" + tb.Name + "/src?versionId=" + marker.VersionId, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, named.Status, "copying a delete marker version");
        }

        private static async Task MetadataReplaceAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("creplace", false, token).ConfigureAwait(false);
            PutObjectRequest put = new PutObjectRequest { BucketName = tb.Name, Key = "src", ContentBody = "x", ContentType = "text/plain" };
            put.Metadata.Add("color", "blue");
            await tb.Client.PutObjectAsync(put, token).ConfigureAwait(false);

            CopyObjectRequest copy = new CopyObjectRequest
            {
                SourceBucket = tb.Name,
                SourceKey = "src",
                DestinationBucket = tb.Name,
                DestinationKey = "dst",
                MetadataDirective = S3MetadataDirective.REPLACE,
                ContentType = "application/json"
            };
            copy.Metadata.Add("shape", "round");
            await tb.Client.CopyObjectAsync(copy, token).ConfigureAwait(false);

            GetObjectMetadataResponse meta = await tb.Client.GetObjectMetadataAsync(tb.Name, "dst", token).ConfigureAwait(false);
            Check.Equal("application/json", meta.Headers.ContentType, "replaced content type");
            Check.Equal("round", meta.Metadata["x-amz-meta-shape"], "new metadata");
            Check.False(meta.Metadata.Keys.Contains("x-amz-meta-color"), "source metadata not carried over");
        }

        private static async Task InvalidDirectiveAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("cbaddir", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "src", "x", token).ConfigureAwait(false);
            RawResponse raw = await CopyRawAsync(tb, "dst", "/" + tb.Name + "/src", new Dictionary<string, string> { { "x-amz-metadata-directive", "MERGE" } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, raw.Status, "status");
        }

        private static async Task OntoSelfAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("cself", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "k", "content", token).ConfigureAwait(false);

            await S3CompatibilityContext.ExpectErrorAsync(
                () => tb.Client.CopyObjectAsync(new CopyObjectRequest { SourceBucket = tb.Name, SourceKey = "k", DestinationBucket = tb.Name, DestinationKey = "k" }, token),
                HttpStatusCode.BadRequest,
                "copy onto self without changes").ConfigureAwait(false);

            CopyObjectRequest replace = new CopyObjectRequest
            {
                SourceBucket = tb.Name,
                SourceKey = "k",
                DestinationBucket = tb.Name,
                DestinationKey = "k",
                MetadataDirective = S3MetadataDirective.REPLACE,
                ContentType = "text/markdown"
            };
            await tb.Client.CopyObjectAsync(replace, token).ConfigureAwait(false);

            GetObjectMetadataResponse meta = await tb.Client.GetObjectMetadataAsync(tb.Name, "k", token).ConfigureAwait(false);
            Check.Equal("text/markdown", meta.Headers.ContentType, "metadata updated in place");
            Check.Equal("content", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "k", null, token).ConfigureAwait(false), "content preserved");
        }

        private static async Task TaggingDirectiveAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("ctags", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "src", "x", token).ConfigureAwait(false);
            await tb.Client.PutObjectTaggingAsync(new PutObjectTaggingRequest { BucketName = tb.Name, Key = "src", Tagging = new Tagging { TagSet = new List<Tag> { new Tag { Key = "origin", Value = "src" } } } }, token).ConfigureAwait(false);

            await tb.Client.CopyObjectAsync(new CopyObjectRequest { SourceBucket = tb.Name, SourceKey = "src", DestinationBucket = tb.Name, DestinationKey = "copied" }, token).ConfigureAwait(false);
            GetObjectTaggingResponse copied = await tb.Client.GetObjectTaggingAsync(new GetObjectTaggingRequest { BucketName = tb.Name, Key = "copied" }, token).ConfigureAwait(false);
            Check.True(copied.Tagging.Any(t => t.Key == "origin" && t.Value == "src"), "tags copied by default");

            RawResponse replaced = await CopyRawAsync(tb, "replaced", "/" + tb.Name + "/src",
                new Dictionary<string, string> { { "x-amz-tagging-directive", "REPLACE" }, { "x-amz-tagging", "fresh=yes" } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, replaced.Status, "status");
            GetObjectTaggingResponse replacedTags = await tb.Client.GetObjectTaggingAsync(new GetObjectTaggingRequest { BucketName = tb.Name, Key = "replaced" }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "fresh=yes" }, replacedTags.Tagging.Select(t => t.Key + "=" + t.Value).ToList(), "tags replaced");
        }

        private static async Task CopySourceConditionsAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("ccond", false, token).ConfigureAwait(false);
            PutObjectResponse put = await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "src", "x", token).ConfigureAwait(false);

            RawResponse mismatch = await CopyRawAsync(tb, "dst", "/" + tb.Name + "/src", new Dictionary<string, string> { { "x-amz-copy-source-if-match", "\"nope\"" } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PreconditionFailed, mismatch.Status, "if-match mismatch");

            RawResponse noneMatch = await CopyRawAsync(tb, "dst", "/" + tb.Name + "/src", new Dictionary<string, string> { { "x-amz-copy-source-if-none-match", put.ETag } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PreconditionFailed, noneMatch.Status, "if-none-match on the current ETag");

            await S3CompatibilityContext.ExpectErrorAsync(() => tb.Client.GetObjectMetadataAsync(tb.Name, "dst", token), HttpStatusCode.NotFound, "nothing copied").ConfigureAwait(false);

            RawResponse match = await CopyRawAsync(tb, "dst", "/" + tb.Name + "/src", new Dictionary<string, string> { { "x-amz-copy-source-if-match", put.ETag } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, match.Status, "if-match on the current ETag");
        }

        private static async Task SourcePermissionAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            TestPrincipal alice = await S3CompatibilityContext.CreateUserAsync(server, true, token).ConfigureAwait(false);
            TestPrincipal bob = await S3CompatibilityContext.CreateUserAsync(server, token).ConfigureAwait(false);

            using IAmazonS3 aliceClient = S3CompatibilityContext.Client(server, alice.AccessKey, alice.SecretKey);
            string privateBucket = await S3CompatibilityContext.NewBucketAsync(aliceClient, "cprivate", false, token).ConfigureAwait(false);
            string bobBucket = await S3CompatibilityContext.NewBucketAsync(aliceClient, "cbobs", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(aliceClient, privateBucket, "secret.txt", "secret", token).ConfigureAwait(false);

            // Bob may write to bobBucket but has no access to privateBucket.
            await aliceClient.PutACLAsync(new PutACLRequest { BucketName = bobBucket, CannedACL = S3CannedACL.PublicReadWrite }, token).ConfigureAwait(false);

            using IAmazonS3 bobClient = S3CompatibilityContext.Client(server, bob.AccessKey, bob.SecretKey);
            await S3CompatibilityContext.ExpectErrorAsync(
                () => bobClient.CopyObjectAsync(new CopyObjectRequest { SourceBucket = privateBucket, SourceKey = "secret.txt", DestinationBucket = bobBucket, DestinationKey = "stolen.txt" }, token),
                HttpStatusCode.Forbidden,
                "copy without read access to the source").ConfigureAwait(false);

            await S3CompatibilityContext.ExpectErrorAsync(() => aliceClient.GetObjectMetadataAsync(bobBucket, "stolen.txt", token), HttpStatusCode.NotFound, "nothing copied").ConfigureAwait(false);

            // Once the source is readable, the same copy succeeds.
            await aliceClient.PutACLAsync(new PutACLRequest { BucketName = privateBucket, Key = "secret.txt", CannedACL = S3CannedACL.PublicRead }, token).ConfigureAwait(false);
            CopyObjectResponse allowed = await bobClient.CopyObjectAsync(new CopyObjectRequest { SourceBucket = privateBucket, SourceKey = "secret.txt", DestinationBucket = bobBucket, DestinationKey = "shared.txt" }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, allowed.HttpStatusCode, "copy with read access");
        }

        private static async Task RestoreByCopyAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("crestore", true, token).ConfigureAwait(false);
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "doc", "good", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "doc", "bad", token).ConfigureAwait(false);

            CopyObjectResponse restored = await tb.Client.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = tb.Name,
                SourceKey = "doc",
                SourceVersionId = v1.VersionId,
                DestinationBucket = tb.Name,
                DestinationKey = "doc"
            }, token).ConfigureAwait(false);

            Check.Equal("good", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "doc", null, token).ConfigureAwait(false), "old version is now latest");
            ListVersionsResponse versions = await tb.Client.ListVersionsAsync(new ListVersionsRequest { BucketName = tb.Name }, token).ConfigureAwait(false);
            Check.Equal(3, versions.Versions.Count, "restore adds a version");
            Check.Equal(restored.VersionId, versions.Versions[0].VersionId, "restored copy is newest");
        }

        private static async Task UploadPartCopyAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("cpart", false, token).ConfigureAwait(false);
            byte[] data = new byte[6 * 1024 * 1024];
            new Random(11).NextBytes(data);
            await tb.Client.PutObjectAsync(new PutObjectRequest { BucketName = tb.Name, Key = "source.bin", InputStream = new System.IO.MemoryStream(data) }, token).ConfigureAwait(false);

            InitiateMultipartUploadResponse init = await tb.Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = tb.Name, Key = "assembled.bin" }, token).ConfigureAwait(false);

            int split = 5 * 1024 * 1024;
            CopyPartResponse part1 = await tb.Client.CopyPartAsync(new CopyPartRequest
            {
                SourceBucket = tb.Name,
                SourceKey = "source.bin",
                DestinationBucket = tb.Name,
                DestinationKey = "assembled.bin",
                UploadId = init.UploadId,
                PartNumber = 1,
                FirstByte = 0,
                LastByte = split - 1
            }, token).ConfigureAwait(false);
            CopyPartResponse part2 = await tb.Client.CopyPartAsync(new CopyPartRequest
            {
                SourceBucket = tb.Name,
                SourceKey = "source.bin",
                DestinationBucket = tb.Name,
                DestinationKey = "assembled.bin",
                UploadId = init.UploadId,
                PartNumber = 2,
                FirstByte = split,
                LastByte = data.Length - 1
            }, token).ConfigureAwait(false);

            Check.False(String.IsNullOrEmpty(part1.ETag), "part 1 ETag");
            Check.False(String.IsNullOrEmpty(part2.ETag), "part 2 ETag");

            await tb.Client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
            {
                BucketName = tb.Name,
                Key = "assembled.bin",
                UploadId = init.UploadId,
                PartETags = new List<PartETag> { new PartETag(1, part1.ETag), new PartETag(2, part2.ETag) }
            }, token).ConfigureAwait(false);

            RawResponse assembled = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get, "/" + tb.Name + "/assembled.bin", null, null, token).ConfigureAwait(false);
            Check.True(assembled.Body.AsSpan().SequenceEqual(data), "assembled object equals the source");
        }

        private static async Task UploadPartCopyConditionsAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("cpartcond", false, token).ConfigureAwait(false);
            PutObjectResponse source = await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "src", "0123456789", token).ConfigureAwait(false);
            InitiateMultipartUploadResponse init = await tb.Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = tb.Name, Key = "dst" }, token).ConfigureAwait(false);
            string path = "/" + tb.Name + "/dst?partNumber=1&uploadId=" + init.UploadId;

            RawResponse mismatch = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, path,
                new Dictionary<string, string> { { "x-amz-copy-source", "/" + tb.Name + "/src" }, { "x-amz-copy-source-if-match", "\"nope\"" } }, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PreconditionFailed, mismatch.Status, "stale ETag rejected");

            RawResponse match = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, path,
                new Dictionary<string, string> { { "x-amz-copy-source", "/" + tb.Name + "/src" }, { "x-amz-copy-source-if-match", source.ETag } }, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, match.Status, "current ETag accepted");
            Check.Contains(match.Text, "<CopyPartResult", "CopyPartResult returned");
        }

        private static async Task UploadPartCopyBadRangeAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("cpartbad", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "src", "0123456789", token).ConfigureAwait(false);
            InitiateMultipartUploadResponse init = await tb.Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = tb.Name, Key = "dst" }, token).ConfigureAwait(false);

            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/dst?partNumber=1&uploadId=" + init.UploadId,
                new Dictionary<string, string> { { "x-amz-copy-source", "/" + tb.Name + "/src" }, { "x-amz-copy-source-range", "bytes=5-50" } }, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, raw.Status, "range past the end of the source");

            ListPartsResponse parts = await tb.Client.ListPartsAsync(new ListPartsRequest { BucketName = tb.Name, Key = "dst", UploadId = init.UploadId }, token).ConfigureAwait(false);
            Check.True(parts.Parts == null || parts.Parts.Count == 0, "no part stored");
        }

        #endregion
    }
}
