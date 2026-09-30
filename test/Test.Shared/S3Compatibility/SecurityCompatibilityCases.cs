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
    using Amazon.S3;
    using Amazon.S3.Model;
    using Touchstone.Core;

    /// <summary>
    /// Signature enforcement for the operations Less3 answers itself, and the request-metadata diagnostic.
    /// Previously a canned-ACL PUT (no body) was answered before AWS signature validation, so a request
    /// carrying only a known access key could change a bucket's ACL.
    /// </summary>
    public static class SecurityCompatibilityCases
    {
        #region Public-Methods

        /// <summary>
        /// Suite descriptor.
        /// </summary>
        public static TestSuiteDescriptor Suite()
        {
            const string suite = "S3CompatSecurity";
            return new TestSuiteDescriptor(
                suiteId: suite,
                displayName: "S3 Compatibility: Signature enforcement",
                cases: new List<TestCaseDescriptor>
                {
                    S3CompatibilitySuites.Case(suite, "ForgedSignature_CannedBucketAcl_Rejected", "A canned bucket ACL with a forged signature is rejected and changes nothing", ForgedCannedBucketAclAsync),
                    S3CompatibilitySuites.Case(suite, "ForgedSignature_CannedObjectAcl_Rejected", "A canned object ACL with a forged signature is rejected and changes nothing", ForgedCannedObjectAclAsync),
                    S3CompatibilitySuites.Case(suite, "ForgedSignature_ListObjects_Rejected", "ListObjects with a forged signature is rejected", ForgedListAsync),
                    S3CompatibilitySuites.Case(suite, "ForgedSignature_ListVersions_Rejected", "ListObjectVersions with a forged signature is rejected", ForgedListVersionsAsync),
                    S3CompatibilitySuites.Case(suite, "ForgedSignature_DeleteObjects_Rejected", "DeleteObjects with a forged signature is rejected and deletes nothing", ForgedDeleteObjectsAsync),
                    S3CompatibilitySuites.Case(suite, "ForgedSignature_CopyObject_Rejected", "CopyObject with a forged signature is rejected", ForgedCopyAsync),
                    S3CompatibilitySuites.Case(suite, "ForgedSignature_MetadataQuery_Rejected", "?metadata with a forged signature returns no request metadata", ForgedMetadataAsync),
                    S3CompatibilitySuites.Case(suite, "SignedRequests_Succeed", "Correctly signed ACL, list, version-list, copy and multi-delete requests succeed", SignedRequestsAsync),
                    S3CompatibilitySuites.Case(suite, "MetadataQuery_AdminOnly_NoSecrets", "?metadata is served only for the admin API key and never includes secrets", MetadataAdminOnlyAsync)
                });
        }

        #endregion

        #region Private-Methods

        private static Dictionary<string, string> Forged()
        {
            // SendAsync already supplies an Authorization header with a valid access key and a placeholder
            // signature; nothing else is needed to forge a request.
            return new Dictionary<string, string>();
        }

        private static async Task<TestBucket> SetupAsync(string prefix, CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.SignedServerAsync(token).ConfigureAwait(false);
            TestBucket tb = await TestBucket.CreateAsync(server, prefix, false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "k.txt", "keep", token).ConfigureAwait(false);
            return tb;
        }

        private static async Task ForgedCannedBucketAclAsync(CancellationToken token)
        {
            using TestBucket tb = await SetupAsync("sacl", token).ConfigureAwait(false);
            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "?acl",
                new Dictionary<string, string> { { "x-amz-acl", "public-read-write" } }, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.Forbidden, raw.Status, "forged canned ACL");

            GetACLResponse acl = await tb.Client.GetACLAsync(new GetACLRequest { BucketName = tb.Name }, token).ConfigureAwait(false);
            Check.False((acl.AccessControlList.Grants ?? new List<S3Grant>()).Any(g => g.Grantee?.URI != null && g.Grantee.URI.Contains("AllUsers")), "bucket ACL unchanged");
        }

        private static async Task ForgedCannedObjectAclAsync(CancellationToken token)
        {
            using TestBucket tb = await SetupAsync("soacl", token).ConfigureAwait(false);
            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/k.txt?acl",
                new Dictionary<string, string> { { "x-amz-acl", "public-read" } }, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.Forbidden, raw.Status, "forged canned object ACL");

            GetACLResponse acl = await tb.Client.GetACLAsync(new GetACLRequest { BucketName = tb.Name, Key = "k.txt" }, token).ConfigureAwait(false);
            Check.False((acl.AccessControlList.Grants ?? new List<S3Grant>()).Any(g => g.Grantee?.URI != null && g.Grantee.URI.Contains("AllUsers")), "object ACL unchanged");
        }

        private static async Task ForgedListAsync(CancellationToken token)
        {
            using TestBucket tb = await SetupAsync("slist", token).ConfigureAwait(false);
            foreach (string query in new[] { "", "?list-type=2" })
            {
                RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get, "/" + tb.Name + query, Forged(), null, token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.Forbidden, raw.Status, "forged list " + query);
                Check.NotContains(raw.Text, "k.txt", "no keys disclosed");
            }
        }

        private static async Task ForgedListVersionsAsync(CancellationToken token)
        {
            using TestBucket tb = await SetupAsync("slistv", token).ConfigureAwait(false);
            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get, "/" + tb.Name + "?versions", Forged(), null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.Forbidden, raw.Status, "forged version list");
            Check.NotContains(raw.Text, "k.txt", "no keys disclosed");
        }

        private static async Task ForgedDeleteObjectsAsync(CancellationToken token)
        {
            using TestBucket tb = await SetupAsync("sdel", token).ConfigureAwait(false);
            byte[] body = Encoding.UTF8.GetBytes("<Delete><Object><Key>k.txt</Key></Object></Delete>");
            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Post, "/" + tb.Name + "?delete", Forged(), body, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.Forbidden, raw.Status, "forged DeleteObjects");
            Check.Equal("keep", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "k.txt", null, token).ConfigureAwait(false), "nothing deleted");
        }

        private static async Task ForgedCopyAsync(CancellationToken token)
        {
            using TestBucket tb = await SetupAsync("scopy", token).ConfigureAwait(false);
            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/copy.txt",
                new Dictionary<string, string> { { "x-amz-copy-source", "/" + tb.Name + "/k.txt" } }, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.Forbidden, raw.Status, "forged copy");
            await S3CompatibilityContext.ExpectErrorAsync(() => tb.Client.GetObjectMetadataAsync(tb.Name, "copy.txt", token), HttpStatusCode.NotFound, "nothing copied").ConfigureAwait(false);
        }

        private static async Task ForgedMetadataAsync(CancellationToken token)
        {
            using TestBucket tb = await SetupAsync("smeta", token).ConfigureAwait(false);
            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get, "/" + tb.Name + "?metadata", Forged(), null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.Forbidden, raw.Status, "forged metadata query");
            Check.NotContains(raw.Text, "SecretKey", "no credential fields");
            Check.NotContains(raw.Text, S3CompatibilityContext.DefaultSecretKey + "\"", "no secret value");
        }

        private static async Task SignedRequestsAsync(CancellationToken token)
        {
            using TestBucket tb = await SetupAsync("ssigned", token).ConfigureAwait(false);

            await tb.Client.PutACLAsync(new PutACLRequest { BucketName = tb.Name, CannedACL = S3CannedACL.PublicRead }, token).ConfigureAwait(false);
            GetACLResponse acl = await tb.Client.GetACLAsync(new GetACLRequest { BucketName = tb.Name }, token).ConfigureAwait(false);
            Check.True((acl.AccessControlList.Grants ?? new List<S3Grant>()).Any(g => g.Grantee?.URI != null && g.Grantee.URI.Contains("AllUsers")), "signed canned ACL applied");

            await tb.Client.PutACLAsync(new PutACLRequest { BucketName = tb.Name, Key = "k.txt", CannedACL = S3CannedACL.PublicRead }, token).ConfigureAwait(false);

            ListObjectsV2Response v2 = await tb.Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = tb.Name }, token).ConfigureAwait(false);
            Check.Equal(1, v2.S3Objects.Count, "signed v2 list");
            ListObjectsResponse v1 = await tb.Client.ListObjectsAsync(new ListObjectsRequest { BucketName = tb.Name }, token).ConfigureAwait(false);
            Check.Equal(1, v1.S3Objects.Count, "signed v1 list");
            ListVersionsResponse versions = await tb.Client.ListVersionsAsync(new ListVersionsRequest { BucketName = tb.Name }, token).ConfigureAwait(false);
            Check.Equal(1, versions.Versions.Count, "signed version list");

            await tb.Client.CopyObjectAsync(new CopyObjectRequest { SourceBucket = tb.Name, SourceKey = "k.txt", DestinationBucket = tb.Name, DestinationKey = "copy.txt" }, token).ConfigureAwait(false);
            DeleteObjectsResponse deleted = await tb.Client.DeleteObjectsAsync(new DeleteObjectsRequest
            {
                BucketName = tb.Name,
                Objects = new List<KeyVersion> { new KeyVersion { Key = "k.txt" }, new KeyVersion { Key = "copy.txt" } }
            }, token).ConfigureAwait(false);
            Check.Equal(2, deleted.DeletedObjects.Count, "signed DeleteObjects");
        }

        private static async Task MetadataAdminOnlyAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "smetaadm", false, token).ConfigureAwait(false);

            // A regular credential gets the normal S3 response for the bucket, not a metadata dump.
            RawResponse user = await S3CompatibilityContext.SendAsync(server, HttpMethod.Get, "/" + bucket + "?metadata", null, null, token).ConfigureAwait(false);
            Check.NotContains(user.Text, "\"Authentication\"", "no metadata dump for a regular credential");

            // The admin API key gets the diagnostic, with secrets removed.
            using (HttpRequestMessage request = server.CreateS3Request(HttpMethod.Get, "/" + bucket + "?metadata", S3CompatibilityContext.DefaultAccessKey))
            {
                request.Headers.TryAddWithoutValidation("x-api-key", server.AdminApiKey);
                RawResponse admin = await S3CompatibilityContext.SendRequestAsync(request, token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.OK, admin.Status, "admin metadata status");
                Check.NotContains(admin.Text, "\"SecretKey\": \"" + S3CompatibilityContext.DefaultSecretKey + "\"", "secret key redacted");
            }
        }

        #endregion
    }
}
