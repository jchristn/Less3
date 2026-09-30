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
    /// Object versioning compatibility with Amazon S3: delete markers, permanent version deletes, the null
    /// version, suspended versioning, and version listing.
    /// </summary>
    public static class VersioningCompatibilityCases
    {
        #region Public-Methods

        /// <summary>
        /// Suite descriptor.
        /// </summary>
        public static TestSuiteDescriptor Suite()
        {
            const string suite = "S3CompatVersioning";
            return new TestSuiteDescriptor(
                suiteId: suite,
                displayName: "S3 Compatibility: Versioning",
                cases: new List<TestCaseDescriptor>
                {
                    S3CompatibilitySuites.Case(suite, "Delete_CreatesMarkerOnTopOfLatest", "DeleteObject without a version adds a delete marker above the latest version", DeleteCreatesMarkerAsync),
                    S3CompatibilitySuites.Case(suite, "Delete_MarkerVersion_Undeletes", "Deleting the delete marker's version restores the previous version", DeleteMarkerUndeletesAsync),
                    S3CompatibilitySuites.Case(suite, "Delete_SpecificVersion_IsPermanent", "DeleteObject with a version permanently removes only that version", DeleteSpecificVersionAsync),
                    S3CompatibilitySuites.Case(suite, "Delete_NonexistentVersion_Succeeds", "Deleting a version that does not exist succeeds and changes nothing", DeleteNonexistentVersionAsync),
                    S3CompatibilitySuites.Case(suite, "Delete_MissingKey_CreatesMarker", "Deleting a missing key in a versioned bucket still records a delete marker", DeleteMissingKeyCreatesMarkerAsync),
                    S3CompatibilitySuites.Case(suite, "Get_LatestIsMarker_404WithHeader", "GET of a key whose latest version is a delete marker returns 404 with x-amz-delete-marker", GetLatestMarkerAsync),
                    S3CompatibilitySuites.Case(suite, "Get_MarkerVersion_405", "GET or HEAD naming a delete marker's version returns 405", GetMarkerVersionAsync),
                    S3CompatibilitySuites.Case(suite, "Get_InvalidVersionId_400", "A malformed version ID is rejected with 400 InvalidArgument", GetInvalidVersionAsync),
                    S3CompatibilitySuites.Case(suite, "Get_MissingVersion_404", "A well-formed version ID that does not exist returns 404 NoSuchVersion", GetMissingVersionAsync),
                    S3CompatibilitySuites.Case(suite, "Suspend_ReportsSuspended", "PutBucketVersioning Suspended is reported back as Suspended", SuspendReportsSuspendedAsync),
                    S3CompatibilitySuites.Case(suite, "Suspend_PutOnMultiVersionKey_KeepsVersions", "A write after suspending keeps every existing version (no data loss)", SuspendPutKeepsVersionsAsync),
                    S3CompatibilitySuites.Case(suite, "Suspend_SecondPut_ReplacesNullVersion", "Repeated writes while suspended replace the single null version", SuspendReplacesNullVersionAsync),
                    S3CompatibilitySuites.Case(suite, "Suspend_Delete_CreatesNullMarker", "A delete while suspended replaces the null version with a null delete marker", SuspendDeleteAsync),
                    S3CompatibilitySuites.Case(suite, "Suspend_ThenEnable_NewVersionsAppend", "Re-enabling versioning after suspension appends new versions", SuspendThenEnableAsync),
                    S3CompatibilitySuites.Case(suite, "Suspend_KeepsBucketAclAndTags", "Changing versioning keeps the bucket's ACL and tags", VersioningChangeKeepsBucketStateAsync),
                    S3CompatibilitySuites.Case(suite, "InvalidStatus_MalformedXml", "A versioning status other than Enabled or Suspended is rejected", InvalidStatusAsync),
                    S3CompatibilitySuites.Case(suite, "Unversioned_Overwrite_KeepsOneNullVersion", "Overwriting in an unversioned bucket keeps exactly one version, reported as null", UnversionedOverwriteAsync),
                    S3CompatibilitySuites.Case(suite, "Unversioned_NoVersionHeader", "An unversioned bucket never returns x-amz-version-id", UnversionedNoVersionHeaderAsync),
                    S3CompatibilitySuites.Case(suite, "Unversioned_GetNullVersion", "GET with versionId=null reads the object in an unversioned bucket", UnversionedGetNullAsync),
                    S3CompatibilitySuites.Case(suite, "Tagging_DefaultsToLatestVersion", "PutObjectTagging without a version tags the latest version, not version 1", TaggingDefaultsToLatestAsync),
                    S3CompatibilitySuites.Case(suite, "ListVersions_OrderAndIsLatest", "ListObjectVersions orders by key then newest version and flags only the latest", ListVersionsOrderAsync),
                    S3CompatibilitySuites.Case(suite, "ListVersions_InterleavesDeleteMarkers", "Delete markers appear in version order, interleaved with versions", ListVersionsInterleavedAsync),
                    S3CompatibilitySuites.Case(suite, "ListVersions_PaginatesWithoutGapsOrDuplicates", "Paging with key-marker and version-id-marker returns every version exactly once", ListVersionsPaginationAsync),
                    S3CompatibilitySuites.Case(suite, "ListVersions_NullVersionMarkerStable", "Paging past the null version still returns the older versions after that null version is removed", ListVersionsNullMarkerAsync),
                    S3CompatibilitySuites.Case(suite, "ListVersions_DelimiterCommonPrefixes", "ListObjectVersions rolls keys up into common prefixes", ListVersionsDelimiterAsync),
                    S3CompatibilitySuites.Case(suite, "ListVersions_VersionMarkerWithoutKeyMarker_400", "version-id-marker without key-marker is rejected", ListVersionsBadMarkerAsync),
                    S3CompatibilitySuites.Case(suite, "ListVersions_AnonymousOwner_NoServerError", "Versions written anonymously list without a server error", ListVersionsAnonymousOwnerAsync),
                    S3CompatibilitySuites.Case(suite, "ListObjects_ExcludesDeletedKeys", "ListObjects hides keys whose latest version is a delete marker, without resurfacing older versions", ListObjectsExcludesDeletedAsync),
                    S3CompatibilitySuites.Case(suite, "DeleteBucket_RequiresNoVersionsOrMarkers", "A bucket holding only delete markers or old versions is not empty", DeleteBucketNotEmptyAsync)
                });
        }

        #endregion

        #region Private-Methods

        private static async Task DeleteCreatesMarkerAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vdel", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            PutObjectResponse v2 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "two", token).ConfigureAwait(false);

            DeleteObjectResponse deleted = await client.DeleteObjectAsync(bucket, "k", token).ConfigureAwait(false);
            Check.Equal("true", deleted.DeleteMarker, "x-amz-delete-marker");
            Check.False(String.IsNullOrEmpty(deleted.VersionId), "marker version reported");
            Check.True(deleted.VersionId != v1.VersionId && deleted.VersionId != v2.VersionId, "marker is a new version");

            await S3CompatibilityContext.ExpectErrorAsync(() => client.GetObjectAsync(bucket, "k", token), HttpStatusCode.NotFound, "latest hidden").ConfigureAwait(false);
            Check.Equal("one", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", v1.VersionId, token).ConfigureAwait(false), "v1 readable");
            Check.Equal("two", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", v2.VersionId, token).ConfigureAwait(false), "v2 readable");
        }

        private static async Task DeleteMarkerUndeletesAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vundel", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "two", token).ConfigureAwait(false);
            DeleteObjectResponse marker = await client.DeleteObjectAsync(bucket, "k", token).ConfigureAwait(false);

            DeleteObjectResponse removed = await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = "k", VersionId = marker.VersionId }, token).ConfigureAwait(false);
            Check.Equal("true", removed.DeleteMarker, "removing a marker reports x-amz-delete-marker");
            Check.Equal(marker.VersionId, removed.VersionId, "removed version reported");
            Check.Equal("two", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", null, token).ConfigureAwait(false), "previous version restored");
        }

        private static async Task DeleteSpecificVersionAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vdelver", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            PutObjectResponse v2 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "two", token).ConfigureAwait(false);
            PutObjectResponse v3 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "three", token).ConfigureAwait(false);

            await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = "k", VersionId = v2.VersionId }, token).ConfigureAwait(false);

            ListVersionsResponse versions = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { v3.VersionId, v1.VersionId }, versions.Versions.Select(v => v.VersionId).ToList(), "v2 permanently removed, no marker");
            Check.Equal("three", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", null, token).ConfigureAwait(false), "latest unaffected");

            // Deleting the latest version by ID makes the next one current.
            await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = "k", VersionId = v3.VersionId }, token).ConfigureAwait(false);
            Check.Equal("one", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", null, token).ConfigureAwait(false), "v1 becomes latest");
        }

        private static async Task DeleteNonexistentVersionAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vdelnone", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            DeleteObjectResponse response = await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = "k", VersionId = "424242" }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NoContent, response.HttpStatusCode, "status");
            Check.Equal("one", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", null, token).ConfigureAwait(false), "object untouched");

            await S3CompatibilityContext.ExpectErrorAsync(
                () => client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = "k", VersionId = "bogus" }, token),
                HttpStatusCode.BadRequest,
                "malformed version ID on delete").ConfigureAwait(false);
        }

        private static async Task DeleteMissingKeyCreatesMarkerAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vdelmiss", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            DeleteObjectResponse response = await client.DeleteObjectAsync(bucket, "never-existed", token).ConfigureAwait(false);
            Check.Equal("true", response.DeleteMarker, "marker created");

            ListVersionsResponse versions = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.Equal(1, versions.Versions.Count(v => v.IsDeleteMarker == true), "one delete marker listed");
        }

        private static async Task GetLatestMarkerAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vgetmark", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            DeleteObjectResponse marker = await client.DeleteObjectAsync(bucket, "k", token).ConfigureAwait(false);

            RawResponse get = await S3CompatibilityContext.SendAsync(server, HttpMethod.Get, "/" + bucket + "/k", null, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotFound, get.Status, "status");
            Check.Contains(get.Text, "NoSuchKey", "code");
            Check.Equal("true", get.Header("x-amz-delete-marker"), "delete marker header");
            Check.Equal(marker.VersionId, get.Header("x-amz-version-id"), "marker version header");
        }

        private static async Task GetMarkerVersionAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vget405", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            DeleteObjectResponse marker = await client.DeleteObjectAsync(bucket, "k", token).ConfigureAwait(false);

            RawResponse get = await S3CompatibilityContext.SendAsync(server, HttpMethod.Get, "/" + bucket + "/k?versionId=" + marker.VersionId, null, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.MethodNotAllowed, get.Status, "GET status");
            Check.Contains(get.Text, "MethodNotAllowed", "code");
            Check.Equal("true", get.Header("x-amz-delete-marker"), "delete marker header");

            RawResponse head = await S3CompatibilityContext.SendAsync(server, HttpMethod.Head, "/" + bucket + "/k?versionId=" + marker.VersionId, null, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.MethodNotAllowed, head.Status, "HEAD status");
        }

        private static async Task GetInvalidVersionAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vgetbad", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            RawResponse get = await S3CompatibilityContext.SendAsync(server, HttpMethod.Get, "/" + bucket + "/k?versionId=not-a-version", null, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, get.Status, "status");
            Check.Contains(get.Text, "InvalidArgument", "code");
        }

        private static async Task GetMissingVersionAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vgetmissv", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            AmazonS3Exception e = await S3CompatibilityContext.ExpectErrorAsync(
                () => client.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = "k", VersionId = "999" }, token),
                HttpStatusCode.NotFound,
                "missing version").ConfigureAwait(false);
            Check.Equal("NoSuchVersion", e.ErrorCode, "code");
        }

        private static async Task SuspendReportsSuspendedAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vsusp", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.SetVersioningAsync(client, bucket, VersionStatus.Suspended, token).ConfigureAwait(false);
            GetBucketVersioningResponse suspended = await client.GetBucketVersioningAsync(new GetBucketVersioningRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.Equal(VersionStatus.Suspended.Value, suspended.VersioningConfig.Status?.Value, "suspended status");

            await S3CompatibilityContext.SetVersioningAsync(client, bucket, VersionStatus.Enabled, token).ConfigureAwait(false);
            GetBucketVersioningResponse enabled = await client.GetBucketVersioningAsync(new GetBucketVersioningRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.Equal(VersionStatus.Enabled.Value, enabled.VersioningConfig.Status?.Value, "enabled status");
        }

        private static async Task SuspendPutKeepsVersionsAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vsuspput", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            PutObjectResponse v2 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "two", token).ConfigureAwait(false);
            await S3CompatibilityContext.SetVersioningAsync(client, bucket, VersionStatus.Suspended, token).ConfigureAwait(false);

            PutObjectResponse suspendedPut = await S3CompatibilityContext.PutAsync(client, bucket, "k", "three", token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, suspendedPut.HttpStatusCode, "write after suspending succeeds");
            Check.Equal("null", suspendedPut.VersionId, "write after suspending creates the null version");

            Check.Equal("three", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", null, token).ConfigureAwait(false), "latest is the new write");
            Check.Equal("one", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", v1.VersionId, token).ConfigureAwait(false), "v1 retained");
            Check.Equal("two", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", v2.VersionId, token).ConfigureAwait(false), "v2 retained");
        }

        private static async Task SuspendReplacesNullVersionAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vsuspnull", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            await S3CompatibilityContext.SetVersioningAsync(client, bucket, VersionStatus.Suspended, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "null-a", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "null-b", token).ConfigureAwait(false);

            ListVersionsResponse versions = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "null", v1.VersionId }, versions.Versions.Select(v => v.VersionId).ToList(), "one null version plus v1");
            Check.Equal("null-b", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", "null", token).ConfigureAwait(false), "null version holds the last write");
        }

        private static async Task SuspendDeleteAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vsuspdel", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            await S3CompatibilityContext.SetVersioningAsync(client, bucket, VersionStatus.Suspended, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "null-a", token).ConfigureAwait(false);

            DeleteObjectResponse deleted = await client.DeleteObjectAsync(bucket, "k", token).ConfigureAwait(false);
            Check.Equal("true", deleted.DeleteMarker, "delete marker created");
            Check.Equal("null", deleted.VersionId, "marker is the null version");

            ListVersionsResponse versions = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.Equal(1, versions.Versions.Count(v => v.IsDeleteMarker == true && v.VersionId == "null"), "null delete marker listed");
            Check.Equal(1, versions.Versions.Count(v => v.IsDeleteMarker != true), "only v1 remains as a version");
            Check.Equal("one", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", v1.VersionId, token).ConfigureAwait(false), "v1 retained");
        }

        private static async Task SuspendThenEnableAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vsuspen", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            await S3CompatibilityContext.SetVersioningAsync(client, bucket, VersionStatus.Suspended, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "null", token).ConfigureAwait(false);
            await S3CompatibilityContext.SetVersioningAsync(client, bucket, VersionStatus.Enabled, token).ConfigureAwait(false);
            PutObjectResponse v3 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "three", token).ConfigureAwait(false);
            PutObjectResponse v4 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "four", token).ConfigureAwait(false);

            Check.True(v3.VersionId != "null" && v4.VersionId != "null" && v3.VersionId != v4.VersionId, "new numbered versions");
            ListVersionsResponse versions = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.Equal(4, versions.Versions.Count, "all four versions retained");
            Check.Equal("null", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", "null", token).ConfigureAwait(false), "null version retained");
        }

        private static async Task VersioningChangeKeepsBucketStateAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vkeep", false, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await client.PutBucketTaggingAsync(new PutBucketTaggingRequest { BucketName = bucket, TagSet = new List<Tag> { new Tag { Key = "team", Value = "storage" } } }, token).ConfigureAwait(false);
            await client.PutACLAsync(new PutACLRequest { BucketName = bucket, CannedACL = S3CannedACL.PublicRead }, token).ConfigureAwait(false);

            await S3CompatibilityContext.SetVersioningAsync(client, bucket, VersionStatus.Enabled, token).ConfigureAwait(false);
            await S3CompatibilityContext.SetVersioningAsync(client, bucket, VersionStatus.Suspended, token).ConfigureAwait(false);

            GetBucketTaggingResponse tags = await client.GetBucketTaggingAsync(new GetBucketTaggingRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.True(tags.TagSet != null && tags.TagSet.Any(t => t.Key == "team" && t.Value == "storage"), "bucket tags kept");

            GetACLResponse acl = await client.GetACLAsync(new GetACLRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.True((acl.AccessControlList.Grants ?? new List<S3Grant>()).Any(g => g.Grantee?.URI != null && g.Grantee.URI.Contains("AllUsers")), "bucket ACL kept");
        }

        private static async Task InvalidStatusAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vbadstat", false, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            byte[] body = Encoding.UTF8.GetBytes("<VersioningConfiguration xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><Status>Disabled</Status></VersioningConfiguration>");
            RawResponse raw = await S3CompatibilityContext.SendAsync(server, HttpMethod.Put, "/" + bucket + "?versioning", null, body, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, raw.Status, "status");
            Check.Contains(raw.Text, "MalformedXML", "code");
        }

        private static async Task UnversionedOverwriteAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vunov", false, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "two", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "three", token).ConfigureAwait(false);

            ListVersionsResponse versions = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.Equal(1, versions.Versions.Count, "exactly one version");
            Check.Equal("null", versions.Versions[0].VersionId, "reported as the null version");
            Check.True(versions.Versions[0].IsLatest == true, "flagged latest");
            Check.Equal("three", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", null, token).ConfigureAwait(false), "last write wins");
        }

        private static async Task UnversionedNoVersionHeaderAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vunhdr", false, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            RawResponse put = await S3CompatibilityContext.SendAsync(server, HttpMethod.Put, "/" + bucket + "/k", null, Encoding.UTF8.GetBytes("x"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, put.Status, "put status");
            Check.Equal(null, put.Header("x-amz-version-id"), "PUT has no version header");

            RawResponse get = await S3CompatibilityContext.SendAsync(server, HttpMethod.Get, "/" + bucket + "/k", null, null, token).ConfigureAwait(false);
            Check.Equal(null, get.Header("x-amz-version-id"), "GET has no version header");

            RawResponse delete = await S3CompatibilityContext.SendAsync(server, HttpMethod.Delete, "/" + bucket + "/k", null, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NoContent, delete.Status, "delete status");
            Check.Equal(null, delete.Header("x-amz-version-id"), "DELETE has no version header");
            Check.Equal(null, delete.Header("x-amz-delete-marker"), "DELETE creates no marker");
        }

        private static async Task UnversionedGetNullAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vunnull", false, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "body", token).ConfigureAwait(false);
            Check.Equal("body", await S3CompatibilityContext.GetTextAsync(client, bucket, "k", "null", token).ConfigureAwait(false), "versionId=null reads the object");
        }

        private static async Task TaggingDefaultsToLatestAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vtaglat", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            PutObjectResponse v2 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "two", token).ConfigureAwait(false);

            PutObjectTaggingResponse tagged = await client.PutObjectTaggingAsync(new PutObjectTaggingRequest
            {
                BucketName = bucket,
                Key = "k",
                Tagging = new Tagging { TagSet = new List<Tag> { new Tag { Key = "stage", Value = "latest" } } }
            }, token).ConfigureAwait(false);
            Check.Equal(v2.VersionId, tagged.VersionId, "tagging reports the latest version");

            GetObjectTaggingResponse latest = await client.GetObjectTaggingAsync(new GetObjectTaggingRequest { BucketName = bucket, Key = "k", VersionId = v2.VersionId }, token).ConfigureAwait(false);
            Check.True(latest.Tagging != null && latest.Tagging.Count == 1, "latest version tagged");

            GetObjectTaggingResponse first = await client.GetObjectTaggingAsync(new GetObjectTaggingRequest { BucketName = bucket, Key = "k", VersionId = v1.VersionId }, token).ConfigureAwait(false);
            Check.True(first.Tagging == null || first.Tagging.Count == 0, "version 1 untouched");
        }

        private static async Task ListVersionsOrderAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vlistord", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            PutObjectResponse b1 = await S3CompatibilityContext.PutAsync(client, bucket, "b", "1", token).ConfigureAwait(false);
            PutObjectResponse a1 = await S3CompatibilityContext.PutAsync(client, bucket, "a", "1", token).ConfigureAwait(false);
            PutObjectResponse b2 = await S3CompatibilityContext.PutAsync(client, bucket, "b", "2", token).ConfigureAwait(false);
            PutObjectResponse a2 = await S3CompatibilityContext.PutAsync(client, bucket, "a", "2", token).ConfigureAwait(false);

            ListVersionsResponse versions = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.SequenceEqual(
                new List<string> { "a/" + a2.VersionId, "a/" + a1.VersionId, "b/" + b2.VersionId, "b/" + b1.VersionId },
                versions.Versions.Select(v => v.Key + "/" + v.VersionId).ToList(),
                "key ascending, newest version first");
            Check.SequenceEqual(
                new List<bool> { true, false, true, false },
                versions.Versions.Select(v => v.IsLatest == true).ToList(),
                "only the newest version of each key is latest");
        }

        private static async Task ListVersionsInterleavedAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vlistint", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "a", "1", token).ConfigureAwait(false);
            await client.DeleteObjectAsync(bucket, "a", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "b", "1", token).ConfigureAwait(false);

            RawResponse raw = await S3CompatibilityContext.SendAsync(server, HttpMethod.Get, "/" + bucket + "?versions", null, null, token).ConfigureAwait(false);
            int marker = raw.Text.IndexOf("<DeleteMarker>", StringComparison.Ordinal);
            int aVersion = raw.Text.IndexOf("<Version><Key>a</Key>", StringComparison.Ordinal);
            int bVersion = raw.Text.IndexOf("<Version><Key>b</Key>", StringComparison.Ordinal);
            Check.True(marker >= 0 && aVersion > marker && bVersion > aVersion, "document order: a's marker, a's version, then b");
        }

        private static async Task ListVersionsPaginationAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vlistpage", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            List<string> expected = new List<string>();
            foreach (string key in new[] { "a", "b", "c" })
            {
                List<string> keyVersions = new List<string>();
                for (int i = 0; i < 3; i++)
                {
                    PutObjectResponse put = await S3CompatibilityContext.PutAsync(client, bucket, key, key + i, token).ConfigureAwait(false);
                    keyVersions.Insert(0, key + "/" + put.VersionId);
                }

                expected.AddRange(keyVersions);
            }

            List<string> actual = new List<string>();
            string? keyMarker = null;
            string? versionIdMarker = null;
            int pages = 0;

            while (true)
            {
                ListVersionsResponse page = await client.ListVersionsAsync(new ListVersionsRequest
                {
                    BucketName = bucket,
                    MaxKeys = 2,
                    KeyMarker = keyMarker,
                    VersionIdMarker = versionIdMarker
                }, token).ConfigureAwait(false);

                pages++;
                Check.True(pages < 20, "pagination terminates");
                if (page.Versions != null) actual.AddRange(page.Versions.Select(v => v.Key + "/" + v.VersionId));
                if (page.IsTruncated != true) break;

                Check.False(String.IsNullOrEmpty(page.NextKeyMarker), "NextKeyMarker present when truncated");
                keyMarker = page.NextKeyMarker;
                versionIdMarker = page.NextVersionIdMarker;
            }

            Check.SequenceEqual(expected, actual, "every version exactly once, in order");
            Check.Equal(5, pages, "nine versions in pages of two");
        }

        private static async Task ListVersionsNullMarkerAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vlistnull", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;

            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "one", token).ConfigureAwait(false);
            await S3CompatibilityContext.SetVersioningAsync(client, bucket, VersionStatus.Suspended, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "k", "null", token).ConfigureAwait(false);
            await S3CompatibilityContext.SetVersioningAsync(client, bucket, VersionStatus.Enabled, token).ConfigureAwait(false);
            PutObjectResponse v3 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "three", token).ConfigureAwait(false);

            ListVersionsResponse page1 = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket, MaxKeys = 1 }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { v3.VersionId }, page1.Versions.Select(v => v.VersionId).ToList(), "page 1");
            ListVersionsResponse page2 = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket, MaxKeys = 1, KeyMarker = page1.NextKeyMarker, VersionIdMarker = page1.NextVersionIdMarker }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "null" }, page2.Versions.Select(v => v.VersionId).ToList(), "page 2 is the null version");

            // The null version disappears between pages; the marker must still position the next page.
            await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = "k", VersionId = "null" }, token).ConfigureAwait(false);

            ListVersionsResponse page3 = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket, MaxKeys = 1, KeyMarker = page2.NextKeyMarker, VersionIdMarker = page2.NextVersionIdMarker }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { v1.VersionId }, page3.Versions.Select(v => v.VersionId).ToList(), "version 1 is not skipped");
        }

        private static async Task ListVersionsDelimiterAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vlistdel", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "docs/a.txt", "1", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "docs/b.txt", "1", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "root.txt", "1", token).ConfigureAwait(false);

            ListVersionsResponse response = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket, Delimiter = "/" }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "docs/" }, response.CommonPrefixes ?? new List<string>(), "common prefixes");
            Check.SequenceEqual(new List<string> { "root.txt" }, response.Versions.Select(v => v.Key).ToList(), "only top-level versions listed");
        }

        private static async Task ListVersionsBadMarkerAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vlistbad", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            RawResponse raw = await S3CompatibilityContext.SendAsync(server, HttpMethod.Get, "/" + bucket + "?versions&version-id-marker=3", null, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, raw.Status, "status");
            Check.Contains(raw.Text, "InvalidArgument", "code");
        }

        private static async Task ListVersionsAnonymousOwnerAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vlistanon", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await client.PutACLAsync(new PutACLRequest { BucketName = bucket, CannedACL = S3CannedACL.PublicReadWrite }, token).ConfigureAwait(false);

            using (HttpRequestMessage anonymous = new HttpRequestMessage(HttpMethod.Put, server.BaseUrl + "/" + bucket + "/anon.txt"))
            {
                anonymous.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("anon"));
                RawResponse put = await S3CompatibilityContext.SendRequestAsync(anonymous, token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.OK, put.Status, "anonymous write allowed by ACL");
            }

            ListVersionsResponse versions = await client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, versions.HttpStatusCode, "status");
            Check.True(versions.Versions.Any(v => v.Key == "anon.txt"), "anonymous version listed");
        }

        private static async Task ListObjectsExcludesDeletedAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vlistobj", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            await S3CompatibilityContext.PutAsync(client, bucket, "gone", "1", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "gone", "2", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "kept", "1", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(client, bucket, "kept", "2", token).ConfigureAwait(false);
            await client.DeleteObjectAsync(bucket, "gone", token).ConfigureAwait(false);

            ListObjectsV2Response list = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "kept" }, list.S3Objects.Select(o => o.Key).ToList(), "each live key listed once; deleted key hidden");
            Check.Equal(1L, list.S3Objects[0].Size, "latest version's size");
        }

        private static async Task DeleteBucketNotEmptyAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("vdelbkt", true, token).ConfigureAwait(false);
            Less3TestServer server = tb.Server;
            IAmazonS3 client = tb.Client;
            string bucket = tb.Name;
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(client, bucket, "k", "1", token).ConfigureAwait(false);
            DeleteObjectResponse marker = await client.DeleteObjectAsync(bucket, "k", token).ConfigureAwait(false);

            AmazonS3Exception notEmpty = await S3CompatibilityContext.ExpectErrorAsync(() => client.DeleteBucketAsync(bucket, token), HttpStatusCode.Conflict, "bucket with a version and a marker").ConfigureAwait(false);
            Check.Equal("BucketNotEmpty", notEmpty.ErrorCode, "code");

            await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = "k", VersionId = v1.VersionId }, token).ConfigureAwait(false);
            await S3CompatibilityContext.ExpectErrorAsync(() => client.DeleteBucketAsync(bucket, token), HttpStatusCode.Conflict, "bucket with only a delete marker").ConfigureAwait(false);

            await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = "k", VersionId = marker.VersionId }, token).ConfigureAwait(false);
            DeleteBucketResponse deleted = await client.DeleteBucketAsync(bucket, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NoContent, deleted.HttpStatusCode, "empty bucket deleted");
        }

        #endregion
    }
}
