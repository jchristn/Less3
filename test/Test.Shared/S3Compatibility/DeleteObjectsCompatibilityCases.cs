namespace Test.Shared.S3Compatibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Touchstone.Core;

    /// <summary>
    /// DeleteObjects (multi-object delete) compatibility with Amazon S3, including every defect listed in
    /// archive/MULTI_DELETE_BUG.md: missing keys, ACL/tag cleanup, invalid version IDs, Quiet mode, version IDs in
    /// results, per-key failures, and per-key authorization.
    /// </summary>
    public static class DeleteObjectsCompatibilityCases
    {
        #region Public-Methods

        /// <summary>
        /// Suite descriptor.
        /// </summary>
        public static TestSuiteDescriptor Suite()
        {
            const string suite = "S3CompatDeleteObjects";
            return new TestSuiteDescriptor(
                suiteId: suite,
                displayName: "S3 Compatibility: DeleteObjects",
                cases: new List<TestCaseDescriptor>
                {
                    S3CompatibilitySuites.Case(suite, "MissingKey_ReportedDeleted", "A missing key is reported as deleted, not NoSuchKey", MissingKeyReportedDeletedAsync),
                    S3CompatibilitySuites.Case(suite, "ExistingAndMissing_BothDeleted", "Existing and missing keys are both reported deleted and the existing object is gone", ExistingAndMissingBothDeletedAsync),
                    S3CompatibilitySuites.Case(suite, "SameKeyTwice_BothDeleted", "The same key named twice is reported deleted twice", SameKeyTwiceAsync),
                    S3CompatibilitySuites.Case(suite, "Unversioned_ResultHasNoVersionId", "Results for an unversioned bucket carry no VersionId, nil element, or DeleteMarker", UnversionedResultHasNoVersionIdAsync),
                    S3CompatibilitySuites.Case(suite, "Quiet_SuccessesOmitted", "Quiet mode returns an empty DeleteResult when every key succeeds", QuietSuccessesOmittedAsync),
                    S3CompatibilitySuites.Case(suite, "Quiet_ErrorsStillReturned", "Quiet mode still returns per-key errors", QuietErrorsReturnedAsync),
                    S3CompatibilitySuites.Case(suite, "InvalidVersionId_PerKeyError", "A malformed VersionId fails only that key; later keys are still deleted", InvalidVersionIdPerKeyErrorAsync),
                    S3CompatibilitySuites.Case(suite, "NullVersionId_DeletesUnversionedObject", "VersionId \"null\" deletes the object in an unversioned bucket", NullVersionIdAsync),
                    S3CompatibilitySuites.Case(suite, "Versioned_NoVersion_CreatesDeleteMarker", "Without a VersionId a versioned bucket gets a delete marker and older versions survive", VersionedCreatesDeleteMarkerAsync),
                    S3CompatibilitySuites.Case(suite, "Versioned_ExplicitVersion_PermanentlyDeleted", "An explicit VersionId permanently removes only that version", VersionedExplicitVersionAsync),
                    S3CompatibilitySuites.Case(suite, "Versioned_MissingVersion_ReportedDeleted", "A well-formed VersionId that does not exist is reported deleted", VersionedMissingVersionAsync),
                    S3CompatibilitySuites.Case(suite, "Versioned_MultiVersionKey_DeletesLatestNotOldest", "Deleting a multi-version key hides the latest version rather than marking version 1", VersionedMultiVersionKeyAsync),
                    S3CompatibilitySuites.Case(suite, "AclAndTagRowsRemoved", "ACL and tag rows of deleted objects are removed from the database", AclAndTagRowsRemovedAsync),
                    S3CompatibilitySuites.Case(suite, "SingleDelete_AclAndTagRowsRemoved", "Single-object DeleteObject also removes ACL and tag rows", SingleDeleteAclAndTagRowsRemovedAsync),
                    S3CompatibilitySuites.Case(suite, "RecreatedKey_HasNoStaleAclOrTags", "A key re-created after a batch delete has no ACL grants or tags from the old object", RecreatedKeyAsync),
                    S3CompatibilitySuites.Case(suite, "ThousandKeys_AllProcessed", "1,000 keys in one request are all processed", ThousandKeysAsync),
                    S3CompatibilitySuites.Case(suite, "TooManyKeys_MalformedXml", "More than 1,000 keys is rejected with MalformedXML", TooManyKeysAsync),
                    S3CompatibilitySuites.Case(suite, "NoKeys_MalformedXml", "A request naming no keys is rejected with MalformedXML", NoKeysAsync),
                    S3CompatibilitySuites.Case(suite, "MalformedBody_MalformedXml", "An unparseable body is rejected with MalformedXML", MalformedBodyAsync),
                    S3CompatibilitySuites.Case(suite, "BadContentMd5_BadDigest", "A Content-MD5 that does not match the body is rejected with BadDigest", BadContentMd5Async),
                    S3CompatibilitySuites.Case(suite, "ObjectScopedRbacDeny_EnforcedPerKey", "An object-scoped RBAC deny blocks that key in DeleteObjects exactly as in DeleteObject", ObjectScopedRbacDenyAsync),
                    S3CompatibilitySuites.Case(suite, "UnauthorizedCaller_AccessDenied", "A caller with no rights on the bucket cannot delete anything", UnauthorizedCallerAsync)
                });
        }

        #endregion

        #region Private-Methods

        private static async Task MissingKeyReportedDeletedAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delmiss", false, token).ConfigureAwait(false);

            DeleteObjectsResponse response = await client.DeleteObjectsAsync(new DeleteObjectsRequest
            {
                BucketName = bucket,
                Objects = new List<KeyVersion> { new KeyVersion { Key = "missing.txt" } }
            }, token).ConfigureAwait(false);

            Check.Equal(HttpStatusCode.OK, response.HttpStatusCode, "status");
            Check.Equal(1, response.DeletedObjects.Count, "deleted count");
            Check.Equal("missing.txt", response.DeletedObjects[0].Key, "deleted key");
            Check.True(response.DeleteErrors == null || response.DeleteErrors.Count == 0, "no errors");
        }

        private static async Task ExistingAndMissingBothDeletedAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delmix", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "exists.txt", "x", token).ConfigureAwait(false);

            DeleteObjectsResponse response = await client.DeleteObjectsAsync(new DeleteObjectsRequest
            {
                BucketName = bucket,
                Objects = new List<KeyVersion> { new KeyVersion { Key = "exists.txt" }, new KeyVersion { Key = "missing.txt" } }
            }, token).ConfigureAwait(false);

            Check.SequenceEqual(new List<string> { "exists.txt", "missing.txt" }, response.DeletedObjects.Select(d => d.Key).ToList(), "deleted keys");
            await S3CompatibilityContext.ExpectErrorAsync(() => client.GetObjectAsync(bucket, "exists.txt", token), HttpStatusCode.NotFound, "existing object is gone").ConfigureAwait(false);
        }

        private static async Task SameKeyTwiceAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "deltwice", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "dup.txt", "x", token).ConfigureAwait(false);

            DeleteObjectsResponse response = await client.DeleteObjectsAsync(new DeleteObjectsRequest
            {
                BucketName = bucket,
                Objects = new List<KeyVersion> { new KeyVersion { Key = "dup.txt" }, new KeyVersion { Key = "dup.txt" } }
            }, token).ConfigureAwait(false);

            Check.Equal(2, response.DeletedObjects.Count, "both entries deleted");
        }

        private static async Task UnversionedResultHasNoVersionIdAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delnover", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "a.txt", "x", token).ConfigureAwait(false);

            RawResponse raw = await PostDeleteAsync(server, bucket, new List<string[]> { new[] { "a.txt", null! }, new[] { "b.txt", null! } }, false, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, raw.Status, "status");
            Check.Contains(raw.Text, "<Deleted><Key>a.txt</Key></Deleted>", "exact Deleted entry for existing key");
            Check.Contains(raw.Text, "<Deleted><Key>b.txt</Key></Deleted>", "exact Deleted entry for missing key");
            Check.NotContains(raw.Text, "VersionId", "no VersionId element");
            Check.NotContains(raw.Text, "nil", "no nil elements");
            Check.NotContains(raw.Text, "DeleteMarker", "no DeleteMarker element");
            Check.NotContains(raw.Text, "<Error>", "no errors");
        }

        private static async Task QuietSuccessesOmittedAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delquiet", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "a.txt", "x", token).ConfigureAwait(false);

            RawResponse raw = await PostDeleteAsync(server, bucket, new List<string[]> { new[] { "a.txt", null! }, new[] { "missing.txt", null! } }, true, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, raw.Status, "status");
            Check.NotContains(raw.Text, "<Deleted>", "quiet omits Deleted entries");
            Check.NotContains(raw.Text, "<Error>", "no errors");
            await S3CompatibilityContext.ExpectErrorAsync(() => client.GetObjectAsync(bucket, "a.txt", token), HttpStatusCode.NotFound, "object deleted in quiet mode").ConfigureAwait(false);
        }

        private static async Task QuietErrorsReturnedAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delquieterr", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "a.txt", "x", token).ConfigureAwait(false);

            RawResponse raw = await PostDeleteAsync(server, bucket, new List<string[]> { new[] { "a.txt", null! }, new[] { "bad.txt", "not-a-version" } }, true, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, raw.Status, "status");
            Check.NotContains(raw.Text, "<Deleted>", "quiet omits Deleted entries");
            Check.True(HasDeleteError(raw.Text, "bad.txt", "not-a-version", "NoSuchVersion"), "error for the failing key in " + raw.Text);
        }

        private static async Task InvalidVersionIdPerKeyErrorAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delbadver", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "first.txt", "x", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "last.txt", "x", token).ConfigureAwait(false);

            RawResponse raw = await PostDeleteAsync(server, bucket, new List<string[]>
            {
                new[] { "first.txt", null! },
                new[] { "first.txt", "abc" },
                new[] { "last.txt", null! }
            }, false, token).ConfigureAwait(false);

            Check.Equal(HttpStatusCode.OK, raw.Status, "a malformed VersionId does not fail the request");
            Check.Contains(raw.Text, "<Code>NoSuchVersion</Code>", "per-key error");
            Check.Contains(raw.Text, "<Deleted><Key>last.txt</Key></Deleted>", "key after the bad one still processed");
            await S3CompatibilityContext.ExpectErrorAsync(() => client.GetObjectAsync(bucket, "last.txt", token), HttpStatusCode.NotFound, "later key deleted").ConfigureAwait(false);
        }

        private static async Task NullVersionIdAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delnullver", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "n.txt", "x", token).ConfigureAwait(false);

            RawResponse raw = await PostDeleteAsync(server, bucket, new List<string[]> { new[] { "n.txt", "null" } }, false, token).ConfigureAwait(false);
            Check.Contains(raw.Text, "<Deleted><Key>n.txt</Key><VersionId>null</VersionId></Deleted>", "null version reported deleted");
            await S3CompatibilityContext.ExpectErrorAsync(() => client.GetObjectAsync(bucket, "n.txt", token), HttpStatusCode.NotFound, "null version removed").ConfigureAwait(false);
        }

        private static async Task VersionedCreatesDeleteMarkerAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delvermark", true, token).ConfigureAwait(false);
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(client, bucket, "k.txt", "one", token).ConfigureAwait(false);

            DeleteObjectsResponse response = await client.DeleteObjectsAsync(new DeleteObjectsRequest
            {
                BucketName = bucket,
                Objects = new List<KeyVersion> { new KeyVersion { Key = "k.txt" } }
            }, token).ConfigureAwait(false);

            DeletedObject deleted = response.DeletedObjects.Single();
            Check.True(deleted.DeleteMarker == true, "DeleteMarker reported");
            Check.False(String.IsNullOrEmpty(deleted.DeleteMarkerVersionId), "DeleteMarkerVersionId reported");
            Check.True(deleted.DeleteMarkerVersionId != v1.VersionId, "marker is a new version");

            await S3CompatibilityContext.ExpectErrorAsync(() => client.GetObjectAsync(bucket, "k.txt", token), HttpStatusCode.NotFound, "latest hidden by marker").ConfigureAwait(false);
            Check.Equal("one", await S3CompatibilityContext.GetTextAsync(client, bucket, "k.txt", v1.VersionId, token).ConfigureAwait(false), "previous version still readable");
        }

        private static async Task VersionedExplicitVersionAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delverexp", true, token).ConfigureAwait(false);
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(client, bucket, "k.txt", "one", token).ConfigureAwait(false);
            PutObjectResponse v2 = await S3CompatibilityContext.PutAsync(client, bucket, "k.txt", "two", token).ConfigureAwait(false);

            DeleteObjectsResponse response = await client.DeleteObjectsAsync(new DeleteObjectsRequest
            {
                BucketName = bucket,
                Objects = new List<KeyVersion> { new KeyVersion { Key = "k.txt", VersionId = v1.VersionId } }
            }, token).ConfigureAwait(false);

            DeletedObject deleted = response.DeletedObjects.Single();
            Check.Equal(v1.VersionId, deleted.VersionId, "reported version");
            Check.True(deleted.DeleteMarker != true, "no delete marker for a permanent delete");

            await S3CompatibilityContext.ExpectErrorAsync(() => client.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = "k.txt", VersionId = v1.VersionId }, token), HttpStatusCode.NotFound, "version 1 gone").ConfigureAwait(false);
            Check.Equal("two", await S3CompatibilityContext.GetTextAsync(client, bucket, "k.txt", null, token).ConfigureAwait(false), "latest untouched");

            ListVersionsResponse versions = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { v2.VersionId }, versions.Versions.Select(v => v.VersionId).ToList(), "only version 2 remains");
        }

        private static async Task VersionedMissingVersionAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delvermiss", true, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k.txt", "one", token).ConfigureAwait(false);

            DeleteObjectsResponse response = await client.DeleteObjectsAsync(new DeleteObjectsRequest
            {
                BucketName = bucket,
                Objects = new List<KeyVersion> { new KeyVersion { Key = "k.txt", VersionId = "999" } }
            }, token).ConfigureAwait(false);

            Check.Equal("999", response.DeletedObjects.Single().VersionId, "missing version reported deleted");
            Check.Equal("one", await S3CompatibilityContext.GetTextAsync(client, bucket, "k.txt", null, token).ConfigureAwait(false), "existing object untouched");
        }

        private static async Task VersionedMultiVersionKeyAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delvermulti", true, token).ConfigureAwait(false);
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(client, bucket, "k.txt", "one", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k.txt", "two", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k.txt", "three", token).ConfigureAwait(false);

            await client.DeleteObjectsAsync(new DeleteObjectsRequest
            {
                BucketName = bucket,
                Objects = new List<KeyVersion> { new KeyVersion { Key = "k.txt" } }
            }, token).ConfigureAwait(false);

            await S3CompatibilityContext.ExpectErrorAsync(() => client.GetObjectAsync(bucket, "k.txt", token), HttpStatusCode.NotFound, "latest version hidden").ConfigureAwait(false);
            Check.Equal("one", await S3CompatibilityContext.GetTextAsync(client, bucket, "k.txt", v1.VersionId, token).ConfigureAwait(false), "version 1 intact");

            ListObjectsV2Response list = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket }, token).ConfigureAwait(false);
            Check.True(list.S3Objects == null || list.S3Objects.Count == 0, "deleted key not listed");
        }

        private static async Task AclAndTagRowsRemovedAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);

            foreach (bool versioned in new[] { false, true })
            {
                string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delrows", versioned, token).ConfigureAwait(false);
                PutObjectResponse put = await S3CompatibilityContext.PutAsync(client, bucket, "tagged.txt", "x", token).ConfigureAwait(false);
                await TagAndGrantAsync(client, bucket, "tagged.txt", token).ConfigureAwait(false);
                string objectId = await S3CompatibilityContext.ObjectIdAsync(server, bucket, "tagged.txt", token).ConfigureAwait(false);
                await ExpectRowCountsAsync(server, objectId, true, "before delete (versioned=" + versioned + ")", token).ConfigureAwait(false);

                // In a versioned bucket the version's rows go when that version is permanently removed.
                KeyVersion target = versioned
                    ? new KeyVersion { Key = "tagged.txt", VersionId = put.VersionId }
                    : new KeyVersion { Key = "tagged.txt" };
                await client.DeleteObjectsAsync(new DeleteObjectsRequest { BucketName = bucket, Objects = new List<KeyVersion> { target } }, token).ConfigureAwait(false);

                await ExpectRowCountsAsync(server, objectId, false, "after delete (versioned=" + versioned + ")", token).ConfigureAwait(false);
            }
        }

        private static async Task SingleDeleteAclAndTagRowsRemovedAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delrows1", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "tagged.txt", "x", token).ConfigureAwait(false);
            await TagAndGrantAsync(client, bucket, "tagged.txt", token).ConfigureAwait(false);
            string objectId = await S3CompatibilityContext.ObjectIdAsync(server, bucket, "tagged.txt", token).ConfigureAwait(false);

            await client.DeleteObjectAsync(bucket, "tagged.txt", token).ConfigureAwait(false);
            await ExpectRowCountsAsync(server, objectId, false, "after single delete", token).ConfigureAwait(false);
        }

        private static async Task RecreatedKeyAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delrecreate", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "r.txt", "old", token).ConfigureAwait(false);
            await TagAndGrantAsync(client, bucket, "r.txt", token).ConfigureAwait(false);

            await client.DeleteObjectsAsync(new DeleteObjectsRequest { BucketName = bucket, Objects = new List<KeyVersion> { new KeyVersion { Key = "r.txt" } } }, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "r.txt", "new", token).ConfigureAwait(false);

            GetObjectTaggingResponse tags = await client.GetObjectTaggingAsync(new GetObjectTaggingRequest { BucketName = bucket, Key = "r.txt" }, token).ConfigureAwait(false);
            Check.True(tags.Tagging == null || tags.Tagging.Count == 0, "re-created key has no tags");

            GetACLResponse acl = await client.GetACLAsync(new GetACLRequest { BucketName = bucket, Key = "r.txt" }, token).ConfigureAwait(false);
            bool publicGrant = acl.AccessControlList != null
                && (acl.AccessControlList.Grants ?? new List<S3Grant>()).Any(g => g.Grantee != null && g.Grantee.URI != null && g.Grantee.URI.Contains("AllUsers"));
            Check.False(publicGrant, "re-created key has no AllUsers grant");
        }

        private static async Task ThousandKeysAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "del1000", false, token).ConfigureAwait(false);

            for (int i = 0; i < 20; i++)
            {
                await S3CompatibilityContext.PutAsync(client, bucket, "k" + i.ToString("D4"), "x", token).ConfigureAwait(false);
            }

            List<KeyVersion> keys = Enumerable.Range(0, 1000).Select(i => new KeyVersion { Key = "k" + i.ToString("D4") }).ToList();
            DeleteObjectsResponse response = await client.DeleteObjectsAsync(new DeleteObjectsRequest { BucketName = bucket, Objects = keys }, token).ConfigureAwait(false);

            Check.Equal(1000, response.DeletedObjects.Count, "every key reported");
            ListObjectsV2Response list = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket }, token).ConfigureAwait(false);
            Check.True(list.S3Objects == null || list.S3Objects.Count == 0, "all existing keys deleted");
        }

        private static async Task TooManyKeysAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "del1001", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k0000", "x", token).ConfigureAwait(false);

            List<string[]> keys = Enumerable.Range(0, 1001).Select(i => new[] { "k" + i.ToString("D4"), (string)null! }).ToList();
            RawResponse raw = await PostDeleteAsync(server, bucket, keys, false, token).ConfigureAwait(false);

            Check.Equal(HttpStatusCode.BadRequest, raw.Status, "status");
            Check.Contains(raw.Text, "MalformedXML", "error code");
            Check.Equal("x", await S3CompatibilityContext.GetTextAsync(client, bucket, "k0000", null, token).ConfigureAwait(false), "nothing deleted");
        }

        private static async Task NoKeysAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delnone", false, token).ConfigureAwait(false);

            RawResponse raw = await PostDeleteAsync(server, bucket, new List<string[]>(), false, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, raw.Status, "status");
            Check.Contains(raw.Text, "MalformedXML", "error code");
        }

        private static async Task MalformedBodyAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delbadxml", false, token).ConfigureAwait(false);

            RawResponse raw = await S3CompatibilityContext.SendAsync(server, HttpMethod.Post, "/" + bucket + "?delete", null, Encoding.UTF8.GetBytes("<Delete><Object>"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, raw.Status, "status");
            Check.Contains(raw.Text, "MalformedXML", "error code");
        }

        private static async Task BadContentMd5Async(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delmd5", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "a.txt", "x", token).ConfigureAwait(false);

            byte[] body = Encoding.UTF8.GetBytes(DeleteXml(new List<string[]> { new[] { "a.txt", null! } }, false));
            RawResponse bad = await S3CompatibilityContext.SendAsync(server, HttpMethod.Post, "/" + bucket + "?delete",
                new Dictionary<string, string> { { "Content-MD5", S3CompatibilityContext.ContentMd5(Encoding.UTF8.GetBytes("other")) } }, body, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, bad.Status, "mismatched Content-MD5 status");
            Check.Contains(bad.Text, "BadDigest", "error code");
            Check.Equal("x", await S3CompatibilityContext.GetTextAsync(client, bucket, "a.txt", null, token).ConfigureAwait(false), "nothing deleted");

            RawResponse good = await S3CompatibilityContext.SendAsync(server, HttpMethod.Post, "/" + bucket + "?delete",
                new Dictionary<string, string> { { "Content-MD5", S3CompatibilityContext.ContentMd5(body) } }, body, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, good.Status, "matching Content-MD5 accepted");
        }

        private static async Task ObjectScopedRbacDenyAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            TestPrincipal owner = await S3CompatibilityContext.CreateUserAsync(server, true, token).ConfigureAwait(false);
            using IAmazonS3 client = S3CompatibilityContext.Client(server, owner.AccessKey, owner.SecretKey);
            string bucket = await S3CompatibilityContext.NewBucketAsync(client, "delrbac", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "protected.txt", "keep", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "ordinary.txt", "x", token).ConfigureAwait(false);
            string protectedId = await S3CompatibilityContext.ObjectIdAsync(server, bucket, "protected.txt", token).ConfigureAwait(false);

            // The bucket owner is denied Delete on this one object.
            string roleId = TestIds.Role();
            HttpResponseMessage role = await server.RestPostAsync("roles?tenantId=default", JsonSerializer.Serialize(new { Id = roleId, TenantId = "default", Name = "deny " + roleId, Description = "deny", InheritsToChildren = true, Active = true }), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.Created, role.StatusCode, "create role");
            HttpResponseMessage permission = await server.RestPostAsync("permissions?tenantId=default", JsonSerializer.Serialize(new { Id = TestIds.Permission(), TenantId = "default", RoleId = roleId, ResourceType = "Object", Operation = "Delete", Permit = false, Active = true }), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.Created, permission.StatusCode, "create deny permission");
            HttpResponseMessage assignment = await server.RestPostAsync("roleassignments?tenantId=default", JsonSerializer.Serialize(new { Id = TestIds.Assignment(), TenantId = "default", RoleId = roleId, PrincipalType = "User", PrincipalId = owner.UserId, ResourceType = "Object", ResourceId = protectedId, Active = true }), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.Created, assignment.StatusCode, "create object-scoped assignment");

            await S3CompatibilityContext.ExpectErrorAsync(() => client.DeleteObjectAsync(bucket, "protected.txt", token), HttpStatusCode.Forbidden, "single DeleteObject denied").ConfigureAwait(false);

            RawResponse raw = await PostDeleteAsync(server, bucket, new List<string[]> { new[] { "protected.txt", null! }, new[] { "ordinary.txt", null! } }, false, token, owner.AccessKey).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, raw.Status, "status");
            Check.True(HasDeleteError(raw.Text, "protected.txt", null, "AccessDenied"), "protected key denied in " + raw.Text);
            Check.Contains(raw.Text, "<Deleted><Key>ordinary.txt</Key></Deleted>", "other key deleted");
            Check.Equal("keep", await S3CompatibilityContext.GetTextAsync(client, bucket, "protected.txt", null, token).ConfigureAwait(false), "protected object survives");
        }

        private static async Task UnauthorizedCallerAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            TestPrincipal owner = await S3CompatibilityContext.CreateUserAsync(server, true, token).ConfigureAwait(false);
            TestPrincipal stranger = await S3CompatibilityContext.CreateUserAsync(server, token).ConfigureAwait(false);
            using IAmazonS3 ownerClient = S3CompatibilityContext.Client(server, owner.AccessKey, owner.SecretKey);
            string bucket = await S3CompatibilityContext.NewBucketAsync(ownerClient, "delstranger", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(ownerClient, bucket, "a.txt", "keep", token).ConfigureAwait(false);

            RawResponse raw = await PostDeleteAsync(server, bucket, new List<string[]> { new[] { "a.txt", null! } }, false, token, stranger.AccessKey).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.Forbidden, raw.Status, "stranger denied");
            Check.Equal("keep", await S3CompatibilityContext.GetTextAsync(ownerClient, bucket, "a.txt", null, token).ConfigureAwait(false), "object survives");
        }

        private static async Task TagAndGrantAsync(IAmazonS3 client, string bucket, string key, CancellationToken token)
        {
            await client.PutObjectTaggingAsync(new PutObjectTaggingRequest
            {
                BucketName = bucket,
                Key = key,
                Tagging = new Tagging { TagSet = new List<Tag> { new Tag { Key = "color", Value = "red" } } }
            }, token).ConfigureAwait(false);

            await client.PutACLAsync(new PutACLRequest { BucketName = bucket, Key = key, CannedACL = S3CannedACL.PublicRead }, token).ConfigureAwait(false);
        }

        private static async Task ExpectRowCountsAsync(Less3TestServer server, string objectId, bool present, string description, CancellationToken token)
        {
            List<JsonElement> tags = await S3CompatibilityContext.RestItemsAsync(server, "objecttags?tenantId=default&objectId=" + objectId, token).ConfigureAwait(false);
            List<JsonElement> acls = await S3CompatibilityContext.RestItemsAsync(server, "objectacls?tenantId=default&objectId=" + objectId, token).ConfigureAwait(false);

            if (present)
            {
                Check.True(tags.Count > 0, "tag rows exist " + description);
                Check.True(acls.Count > 0, "ACL rows exist " + description);
            }
            else
            {
                Check.Equal(0, tags.Count, "tag rows removed " + description);
                Check.Equal(0, acls.Count, "ACL rows removed " + description);
            }
        }

        private static bool HasDeleteError(string xml, string key, string? versionId, string code)
        {
            // Element order inside <Error> is not significant to S3 clients, so match by name.
            XDocument doc = XDocument.Parse(xml);
            return doc.Root!.Elements().Where(e => e.Name.LocalName == "Error").Any(e =>
                ChildValue(e, "Key") == key
                && ChildValue(e, "VersionId") == versionId
                && ChildValue(e, "Code") == code);
        }

        private static string? ChildValue(XElement parent, string name)
        {
            XElement? child = parent.Elements().FirstOrDefault(e => e.Name.LocalName == name);
            return child?.Value;
        }

        private static async Task<RawResponse> PostDeleteAsync(Less3TestServer server, string bucket, List<string[]> keys, bool quiet, CancellationToken token, string? accessKey = null)
        {
            byte[] body = Encoding.UTF8.GetBytes(DeleteXml(keys, quiet));
            return await S3CompatibilityContext.SendAsync(server, HttpMethod.Post, "/" + bucket + "?delete", null, body, token, accessKey).ConfigureAwait(false);
        }

        private static string DeleteXml(List<string[]> keys, bool quiet)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<Delete xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\">");
            if (quiet) sb.Append("<Quiet>true</Quiet>");

            foreach (string[] key in keys)
            {
                sb.Append("<Object><Key>").Append(SecurityElement.Escape(key[0])).Append("</Key>");
                if (key.Length > 1 && key[1] != null) sb.Append("<VersionId>").Append(SecurityElement.Escape(key[1])).Append("</VersionId>");
                sb.Append("</Object>");
            }

            sb.Append("</Delete>");
            return sb.ToString();
        }

        #endregion
    }
}
