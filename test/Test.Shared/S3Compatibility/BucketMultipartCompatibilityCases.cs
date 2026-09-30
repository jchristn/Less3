namespace Test.Shared.S3Compatibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Touchstone.Core;

    /// <summary>
    /// Bucket creation, bucket ACL writes, and multipart listing/completion compatibility with Amazon S3.
    /// </summary>
    public static class BucketMultipartCompatibilityCases
    {
        #region Public-Methods

        /// <summary>
        /// Suite descriptor.
        /// </summary>
        public static TestSuiteDescriptor Suite()
        {
            const string suite = "S3CompatBucketsMultipart";
            return new TestSuiteDescriptor(
                suiteId: suite,
                displayName: "S3 Compatibility: Buckets and multipart uploads",
                cases: new List<TestCaseDescriptor>
                {
                    S3CompatibilitySuites.Case(suite, "CreateBucket_AlreadyOwnedByYou", "Re-creating your own bucket returns 409 BucketAlreadyOwnedByYou", AlreadyOwnedAsync),
                    S3CompatibilitySuites.Case(suite, "CreateBucket_OwnedByAnother", "Creating another user's bucket name returns 409 BucketAlreadyExists", OwnedByAnotherAsync),
                    S3CompatibilitySuites.Case(suite, "CreateBucket_InvalidName", "An invalid bucket name returns 400 InvalidBucketName", InvalidNameAsync),
                    S3CompatibilitySuites.Case(suite, "Versioning_NeverEnabled_NoStatus", "A bucket that never had versioning reports no status", NeverVersionedAsync),
                    S3CompatibilitySuites.Case(suite, "BucketAcl_CannedAndXml", "Bucket ACLs can be set with a canned header or an XML body", BucketAclAsync),
                    S3CompatibilitySuites.Case(suite, "BucketAcl_MalformedXml_400", "A malformed ACL body is rejected with MalformedACLError", BucketAclMalformedAsync),
                    S3CompatibilitySuites.Case(suite, "ListParts_Paginates", "ListParts honors max-parts and part-number-marker", ListPartsPaginationAsync),
                    S3CompatibilitySuites.Case(suite, "ListMultipartUploads_Paginates", "ListMultipartUploads honors max-uploads, key-marker and upload-id-marker", ListUploadsPaginationAsync),
                    S3CompatibilitySuites.Case(suite, "ListMultipartUploads_Delimiter", "ListMultipartUploads rolls keys into common prefixes", ListUploadsDelimiterAsync),
                    S3CompatibilitySuites.Case(suite, "Complete_WrongKey_NoSuchUpload", "Completing an upload under a different key fails with NoSuchUpload", CompleteWrongKeyAsync),
                    S3CompatibilitySuites.Case(suite, "Complete_NoParts_MalformedXml", "Completing with no parts fails with MalformedXML", CompleteNoPartsAsync),
                    S3CompatibilitySuites.Case(suite, "Complete_Versioned_NewVersion", "Completing an upload in a versioned bucket adds a version and keeps the previous one", CompleteVersionedAsync),
                    S3CompatibilitySuites.Case(suite, "Complete_KeepsUploadMetadata", "Content type and metadata given at initiation apply to the completed object", CompleteMetadataAsync)
                });
        }

        #endregion

        #region Private-Methods

        private static async Task AlreadyOwnedAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("bowned", false, token).ConfigureAwait(false);
            AmazonS3Exception e = await S3CompatibilityContext.ExpectErrorAsync(() => tb.Client.PutBucketAsync(new PutBucketRequest { BucketName = tb.Name }, token), HttpStatusCode.Conflict, "re-create own bucket").ConfigureAwait(false);
            Check.Equal("BucketAlreadyOwnedByYou", e.ErrorCode, "code");
        }

        private static async Task OwnedByAnotherAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("bother", false, token).ConfigureAwait(false);
            TestPrincipal other = await S3CompatibilityContext.CreateUserAsync(tb.Server, true, token).ConfigureAwait(false);
            using IAmazonS3 otherClient = S3CompatibilityContext.Client(tb.Server, other.AccessKey, other.SecretKey);

            AmazonS3Exception e = await S3CompatibilityContext.ExpectErrorAsync(() => otherClient.PutBucketAsync(new PutBucketRequest { BucketName = tb.Name }, token), HttpStatusCode.Conflict, "create another user's bucket").ConfigureAwait(false);
            Check.Equal("BucketAlreadyExists", e.ErrorCode, "code");
        }

        private static async Task InvalidNameAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            RawResponse raw = await S3CompatibilityContext.SendAsync(server, HttpMethod.Put, "/ab", null, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, raw.Status, "status");
            Check.Contains(raw.Text, "InvalidBucketName", "code");
        }

        private static async Task NeverVersionedAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("bnever", false, token).ConfigureAwait(false);
            GetBucketVersioningResponse response = await tb.Client.GetBucketVersioningAsync(new GetBucketVersioningRequest { BucketName = tb.Name }, token).ConfigureAwait(false);
            Check.True(response.VersioningConfig.Status == null || response.VersioningConfig.Status.Value == "Off", "no status reported");
        }

        private static async Task BucketAclAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("bacl", false, token).ConfigureAwait(false);

            RawResponse canned = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "?acl",
                new Dictionary<string, string> { { "x-amz-acl", "public-read" } }, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, canned.Status, "canned ACL without a body");
            GetACLResponse afterCanned = await tb.Client.GetACLAsync(new GetACLRequest { BucketName = tb.Name }, token).ConfigureAwait(false);
            Check.True((afterCanned.AccessControlList.Grants ?? new List<S3Grant>()).Any(g => g.Grantee?.URI != null && g.Grantee.URI.Contains("AllUsers")), "canned ACL applied");

            GetACLResponse current = await tb.Client.GetACLAsync(new GetACLRequest { BucketName = tb.Name }, token).ConfigureAwait(false);
            string ownerId = current.AccessControlList.Owner.Id;
            string body = "<AccessControlPolicy xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><Owner><ID>" + ownerId + "</ID></Owner><AccessControlList>"
                + "<Grant><Grantee xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"CanonicalUser\"><ID>" + ownerId + "</ID></Grantee><Permission>FULL_CONTROL</Permission></Grant>"
                + "</AccessControlList></AccessControlPolicy>";
            RawResponse xml = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "?acl", null, Encoding.UTF8.GetBytes(body), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, xml.Status, "XML ACL");

            GetACLResponse afterXml = await tb.Client.GetACLAsync(new GetACLRequest { BucketName = tb.Name }, token).ConfigureAwait(false);
            Check.False((afterXml.AccessControlList.Grants ?? new List<S3Grant>()).Any(g => g.Grantee?.URI != null && g.Grantee.URI.Contains("AllUsers")), "XML ACL replaced the canned grants");
        }

        private static async Task BucketAclMalformedAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("baclbad", false, token).ConfigureAwait(false);
            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "?acl", null, Encoding.UTF8.GetBytes("<AccessControlPolicy><Owner>"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, raw.Status, "status");
            Check.Contains(raw.Text, "<Code>MalformedACLError</Code>", "code (Amazon S3 reports MalformedACLError for a bad ACL body)");
        }

        private static async Task ListPartsPaginationAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("bparts", false, token).ConfigureAwait(false);
            InitiateMultipartUploadResponse init = await tb.Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = tb.Name, Key = "mp" }, token).ConfigureAwait(false);
            for (int part = 1; part <= 5; part++)
            {
                await tb.Client.UploadPartAsync(new UploadPartRequest { BucketName = tb.Name, Key = "mp", UploadId = init.UploadId, PartNumber = part, InputStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("part" + part)) }, token).ConfigureAwait(false);
            }

            List<int> seen = new List<int>();
            string? marker = null;
            for (int page = 0; page < 10; page++)
            {
                ListPartsResponse response = await tb.Client.ListPartsAsync(new ListPartsRequest { BucketName = tb.Name, Key = "mp", UploadId = init.UploadId, MaxParts = 2, PartNumberMarker = marker }, token).ConfigureAwait(false);
                if (page == 0)
                {
                    Check.Equal(2, response.Parts.Count, "first page holds max-parts parts");
                    Check.True(response.IsTruncated == true, "first page truncated");
                }

                seen.AddRange(response.Parts.Select(p => p.PartNumber ?? 0));
                if (response.IsTruncated != true) break;
                marker = response.NextPartNumberMarker?.ToString();
            }

            Check.SequenceEqual(new List<int> { 1, 2, 3, 4, 5 }, seen, "every part exactly once");
            await tb.Client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest { BucketName = tb.Name, Key = "mp", UploadId = init.UploadId }, token).ConfigureAwait(false);
        }

        private static async Task ListUploadsPaginationAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("buploads", false, token).ConfigureAwait(false);
            List<string> expected = new List<string>();
            foreach (string key in new[] { "c", "a", "b" })
            {
                for (int i = 0; i < 2; i++)
                {
                    InitiateMultipartUploadResponse init = await tb.Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = tb.Name, Key = key }, token).ConfigureAwait(false);
                    expected.Add(key + "/" + init.UploadId);
                }
            }

            List<string> seen = new List<string>();
            string? keyMarker = null;
            string? uploadIdMarker = null;
            for (int page = 0; page < 10; page++)
            {
                ListMultipartUploadsResponse response = await tb.Client.ListMultipartUploadsAsync(new ListMultipartUploadsRequest
                {
                    BucketName = tb.Name,
                    MaxUploads = 4,
                    KeyMarker = keyMarker,
                    UploadIdMarker = uploadIdMarker
                }, token).ConfigureAwait(false);

                if (page == 0)
                {
                    Check.Equal(4, response.MultipartUploads.Count, "first page holds max-uploads uploads");
                    Check.True(response.IsTruncated == true, "first page truncated");
                }

                if (response.MultipartUploads != null) seen.AddRange(response.MultipartUploads.Select(u => u.Key + "/" + u.UploadId));
                if (response.IsTruncated != true) break;
                keyMarker = response.NextKeyMarker;
                uploadIdMarker = response.NextUploadIdMarker;
            }

            Check.Equal(6, seen.Count, "every upload listed");
            Check.Equal(6, seen.Distinct().Count(), "no duplicates");
            Check.SequenceEqual(new List<string> { "a", "a", "b", "b", "c", "c" }, seen.Select(s => s.Split('/')[0]).ToList(), "key order");
            Check.True(expected.All(seen.Contains), "the uploads created");
        }

        private static async Task ListUploadsDelimiterAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("bupdelim", false, token).ConfigureAwait(false);
            foreach (string key in new[] { "logs/1", "logs/2", "top" })
            {
                await tb.Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = tb.Name, Key = key }, token).ConfigureAwait(false);
            }

            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get, "/" + tb.Name + "?uploads&delimiter=%2F", null, null, token).ConfigureAwait(false);
            XElement root = XElement.Parse(raw.Text);
            XNamespace s3 = "http://s3.amazonaws.com/doc/2006-03-01/";
            Check.Equal(s3 + "ListMultipartUploadsResult", root.Name, "root in the S3 namespace");
            List<string> prefixes = root.Elements(s3 + "CommonPrefixes").Select(c => c.Element(s3 + "Prefix")!.Value).ToList();
            List<string> keys = root.Elements(s3 + "Upload").Select(u => u.Element(s3 + "Key")!.Value).ToList();
            Check.SequenceEqual(new List<string> { "logs/" }, prefixes, "common prefixes");
            Check.SequenceEqual(new List<string> { "top" }, keys, "top-level uploads");
        }

        private static async Task CompleteWrongKeyAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("bwrongkey", false, token).ConfigureAwait(false);
            InitiateMultipartUploadResponse init = await tb.Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = tb.Name, Key = "right" }, token).ConfigureAwait(false);
            UploadPartResponse part = await tb.Client.UploadPartAsync(new UploadPartRequest { BucketName = tb.Name, Key = "right", UploadId = init.UploadId, PartNumber = 1, InputStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("x")) }, token).ConfigureAwait(false);

            AmazonS3Exception e = await S3CompatibilityContext.ExpectErrorAsync(
                () => tb.Client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest { BucketName = tb.Name, Key = "wrong", UploadId = init.UploadId, PartETags = new List<PartETag> { new PartETag(1, part.ETag) } }, token),
                HttpStatusCode.NotFound,
                "complete under another key").ConfigureAwait(false);
            Check.Equal("NoSuchUpload", e.ErrorCode, "code");
            await S3CompatibilityContext.ExpectErrorAsync(() => tb.Client.GetObjectMetadataAsync(tb.Name, "wrong", token), HttpStatusCode.NotFound, "no object created").ConfigureAwait(false);
        }

        private static async Task CompleteNoPartsAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("bnoparts", false, token).ConfigureAwait(false);
            InitiateMultipartUploadResponse init = await tb.Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = tb.Name, Key = "k" }, token).ConfigureAwait(false);

            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Post, "/" + tb.Name + "/k?uploadId=" + init.UploadId, null,
                Encoding.UTF8.GetBytes("<CompleteMultipartUpload xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"></CompleteMultipartUpload>"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, raw.Status, "status");
            Check.Contains(raw.Text, "MalformedXML", "code");
        }

        private static async Task CompleteVersionedAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("bmpver", true, token).ConfigureAwait(false);
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "k", "single", token).ConfigureAwait(false);

            InitiateMultipartUploadResponse init = await tb.Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = tb.Name, Key = "k" }, token).ConfigureAwait(false);
            UploadPartResponse part = await tb.Client.UploadPartAsync(new UploadPartRequest { BucketName = tb.Name, Key = "k", UploadId = init.UploadId, PartNumber = 1, InputStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("multi")) }, token).ConfigureAwait(false);
            CompleteMultipartUploadResponse complete = await tb.Client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest { BucketName = tb.Name, Key = "k", UploadId = init.UploadId, PartETags = new List<PartETag> { new PartETag(1, part.ETag) } }, token).ConfigureAwait(false);

            Check.False(String.IsNullOrEmpty(complete.VersionId), "version reported");
            Check.True(complete.VersionId != v1.VersionId, "new version");
            Check.Equal("multi", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "k", null, token).ConfigureAwait(false), "latest is the multipart object");
            Check.Equal("single", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "k", v1.VersionId, token).ConfigureAwait(false), "previous version kept");
        }

        private static async Task CompleteMetadataAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("bmpmeta", false, token).ConfigureAwait(false);
            InitiateMultipartUploadRequest request = new InitiateMultipartUploadRequest { BucketName = tb.Name, Key = "k", ContentType = "application/x-parquet" };
            request.Metadata.Add("pipeline", "etl");
            request.Headers.ContentDisposition = "attachment";
            InitiateMultipartUploadResponse init = await tb.Client.InitiateMultipartUploadAsync(request, token).ConfigureAwait(false);
            UploadPartResponse part = await tb.Client.UploadPartAsync(new UploadPartRequest { BucketName = tb.Name, Key = "k", UploadId = init.UploadId, PartNumber = 1, InputStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("x")) }, token).ConfigureAwait(false);
            await tb.Client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest { BucketName = tb.Name, Key = "k", UploadId = init.UploadId, PartETags = new List<PartETag> { new PartETag(1, part.ETag) } }, token).ConfigureAwait(false);

            GetObjectMetadataResponse meta = await tb.Client.GetObjectMetadataAsync(tb.Name, "k", token).ConfigureAwait(false);
            Check.Equal("application/x-parquet", meta.Headers.ContentType, "content type");
            Check.Equal("etl", meta.Metadata["x-amz-meta-pipeline"], "user metadata");
            Check.Equal("attachment", meta.Headers.ContentDisposition, "system metadata");
        }

        #endregion
    }
}
