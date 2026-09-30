namespace Test.Shared.S3Compatibility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
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
    /// GetObject, HeadObject and PutObject compatibility with Amazon S3: byte ranges, conditional requests,
    /// ETags, stored system metadata, response overrides, Content-MD5, conditional writes and tagging headers.
    /// </summary>
    public static class ObjectCompatibilityCases
    {
        #region Public-Methods

        /// <summary>
        /// Suite descriptor.
        /// </summary>
        public static TestSuiteDescriptor Suite()
        {
            const string suite = "S3CompatObjects";
            return new TestSuiteDescriptor(
                suiteId: suite,
                displayName: "S3 Compatibility: Object reads and writes",
                cases: new List<TestCaseDescriptor>
                {
                    S3CompatibilitySuites.Case(suite, "Range_EndPastLength_Clamped", "A range ending past the object is clamped to the last byte", RangeClampedAsync),
                    S3CompatibilitySuites.Case(suite, "Range_LastByte", "bytes=N- for the last byte returns that byte", RangeLastByteAsync),
                    S3CompatibilitySuites.Case(suite, "Range_Suffix", "A suffix range bytes=-N returns the last N bytes with 206", RangeSuffixAsync),
                    S3CompatibilitySuites.Case(suite, "Range_SuffixLargerThanObject", "A suffix larger than the object returns the whole object with 206", RangeSuffixLargeAsync),
                    S3CompatibilitySuites.Case(suite, "Range_Middle", "A range in the middle returns exactly those bytes", RangeMiddleAsync),
                    S3CompatibilitySuites.Case(suite, "Range_StartPastEnd_416", "A range starting at or past the end returns 416", RangeUnsatisfiableAsync),
                    S3CompatibilitySuites.Case(suite, "Range_EmptyObject_416", "A range on an empty object returns 416, but a suffix range returns 200 and no bytes", RangeEmptyObjectAsync),
                    S3CompatibilitySuites.Case(suite, "Range_OnVersion", "A range read of an older version reads that version", RangeOnVersionAsync),
                    S3CompatibilitySuites.Case(suite, "Conditional_IfNoneMatch", "If-None-Match returns 304 on match and 200 otherwise", IfNoneMatchAsync),
                    S3CompatibilitySuites.Case(suite, "Conditional_IfMatch", "If-Match returns 412 on mismatch and 200 on match", IfMatchAsync),
                    S3CompatibilitySuites.Case(suite, "Conditional_IfModifiedSince", "If-Modified-Since returns 304 when unmodified and 200 when modified", IfModifiedSinceAsync),
                    S3CompatibilitySuites.Case(suite, "Conditional_IfUnmodifiedSince", "If-Unmodified-Since returns 412 when modified and 200 otherwise", IfUnmodifiedSinceAsync),
                    S3CompatibilitySuites.Case(suite, "Conditional_Head", "HEAD honors If-None-Match and If-Match", ConditionalHeadAsync),
                    S3CompatibilitySuites.Case(suite, "Conditional_SdkEtagToMatch", "The SDK's EtagToNotMatch and EtagToMatch behave as in S3", ConditionalSdkAsync),
                    S3CompatibilitySuites.Case(suite, "ETag_QuotedAndConsistent", "GET, HEAD, PUT and listings report the same quoted ETag", EtagConsistentAsync),
                    S3CompatibilitySuites.Case(suite, "Timestamps_UtcAndConsistent", "LastModified is the same real UTC time in listings, version listings, HEAD, GET, 304 and range responses", TimestampsAsync),
                    S3CompatibilitySuites.Case(suite, "ConcurrentReads_SameObject", "Many concurrent GETs of one object all succeed", ConcurrentReadsAsync),
                    S3CompatibilitySuites.Case(suite, "ReadsDuringOverwrites_NeverFail", "GETs racing overwrites always return a complete version, never a server error", ReadsDuringOverwritesAsync),
                    S3CompatibilitySuites.Case(suite, "IfMatchRange_NeverServesAnotherVersion", "A ranged GET with If-Match racing overwrites returns 412 or bytes of the matching version, never another version", IfMatchRangeRaceAsync),
                    S3CompatibilitySuites.Case(suite, "SystemMetadata_Persisted", "Cache-Control, Content-Disposition, Content-Encoding, Content-Language and Expires are stored and returned", SystemMetadataAsync),
                    S3CompatibilitySuites.Case(suite, "ResponseOverrides", "response-content-type and related query parameters override response headers", ResponseOverridesAsync),
                    S3CompatibilitySuites.Case(suite, "UserMetadata_KeysLowercased", "User metadata keys are stored and returned in lowercase", UserMetadataLowercaseAsync),
                    S3CompatibilitySuites.Case(suite, "ContentType_DefaultsToBinaryOctetStream", "An object written without Content-Type is served as binary/octet-stream", DefaultContentTypeAsync),
                    S3CompatibilitySuites.Case(suite, "ContentMd5_Validated", "Content-MD5 is enforced: match accepted, mismatch BadDigest, malformed InvalidDigest", ContentMd5Async),
                    S3CompatibilitySuites.Case(suite, "ConditionalWrite_IfNoneMatchStar", "If-None-Match: * creates only when the key does not exist", ConditionalWriteIfNoneMatchAsync),
                    S3CompatibilitySuites.Case(suite, "ConditionalWrite_IfMatch", "If-Match writes only when the current ETag matches", ConditionalWriteIfMatchAsync),
                    S3CompatibilitySuites.Case(suite, "PutTaggingHeader", "x-amz-tagging on PutObject applies tags; an invalid tag set is rejected without writing", PutTaggingHeaderAsync),
                    S3CompatibilitySuites.Case(suite, "LargeUnsignedPut_Streams", "A 24 MB unsigned PUT streams to disk and round-trips intact", LargePutAsync),
                    S3CompatibilitySuites.Case(suite, "GrantHeaders_QuotedValues", "x-amz-grant-* headers with quoted id= and emailAddress= values grant access", GrantHeadersAsync),
                    S3CompatibilitySuites.Case(suite, "GrantHeaders_UnknownGrantee_400", "A grant for an unknown user is rejected and no object is written", GrantHeadersUnknownAsync),
                    S3CompatibilitySuites.Case(suite, "CannedAcl_BucketOwnerFullControl", "bucket-owner-full-control is accepted and grants the bucket owner", CannedAclBucketOwnerAsync),
                    S3CompatibilitySuites.Case(suite, "CannedAcl_Unknown_400", "An unknown canned ACL is rejected", CannedAclUnknownAsync)
                });
        }

        #endregion

        #region Private-Methods

        private const string Ten = "0123456789";

        private static async Task<TestBucket> BucketWithTenAsync(string prefix, CancellationToken token)
        {
            TestBucket tb = await TestBucket.CreateAsync(prefix, false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "ten", Ten, token).ConfigureAwait(false);
            return tb;
        }

        private static async Task<RawResponse> GetAsync(TestBucket tb, string key, Dictionary<string, string>? headers, CancellationToken token, HttpMethod? method = null)
        {
            return await S3CompatibilityContext.SendAsync(tb.Server, method ?? HttpMethod.Get, "/" + tb.Name + "/" + key, headers, null, token).ConfigureAwait(false);
        }

        private static async Task<RawResponse> RangeAsync(TestBucket tb, string range, CancellationToken token)
        {
            return await GetAsync(tb, "ten", new Dictionary<string, string> { { "Range", range } }, token).ConfigureAwait(false);
        }

        private static async Task RangeClampedAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("orclamp", token).ConfigureAwait(false);
            RawResponse raw = await RangeAsync(tb, "bytes=5-100", token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PartialContent, raw.Status, "status");
            Check.Equal("56789", raw.Text, "body");
            Check.Equal("bytes 5-9/10", raw.Header("Content-Range"), "Content-Range");
        }

        private static async Task RangeLastByteAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("orlast", token).ConfigureAwait(false);
            RawResponse raw = await RangeAsync(tb, "bytes=9-", token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PartialContent, raw.Status, "status");
            Check.Equal("9", raw.Text, "body");
            Check.Equal("bytes 9-9/10", raw.Header("Content-Range"), "Content-Range");
        }

        private static async Task RangeSuffixAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("orsuffix", token).ConfigureAwait(false);
            RawResponse raw = await RangeAsync(tb, "bytes=-3", token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PartialContent, raw.Status, "status");
            Check.Equal("789", raw.Text, "body");
            Check.Equal("bytes 7-9/10", raw.Header("Content-Range"), "Content-Range");
            Check.Equal("3", raw.Header("Content-Length"), "Content-Length");

            GetObjectResponse sdk = await tb.Client.GetObjectAsync(new GetObjectRequest { BucketName = tb.Name, Key = "ten", ByteRange = new ByteRange("bytes=-4") }, token).ConfigureAwait(false);
            using (StreamReader reader = new StreamReader(sdk.ResponseStream))
            {
                Check.Equal("6789", await reader.ReadToEndAsync(token).ConfigureAwait(false), "SDK suffix range");
            }
        }

        private static async Task RangeSuffixLargeAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("orsuflg", token).ConfigureAwait(false);
            RawResponse raw = await RangeAsync(tb, "bytes=-50", token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PartialContent, raw.Status, "status");
            Check.Equal(Ten, raw.Text, "body");
            Check.Equal("bytes 0-9/10", raw.Header("Content-Range"), "Content-Range");
        }

        private static async Task RangeMiddleAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("ormid", token).ConfigureAwait(false);
            RawResponse raw = await RangeAsync(tb, "bytes=2-4", token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PartialContent, raw.Status, "status");
            Check.Equal("234", raw.Text, "body");
            Check.Equal("bytes 2-4/10", raw.Header("Content-Range"), "Content-Range");
        }

        private static async Task RangeUnsatisfiableAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("orpast", token).ConfigureAwait(false);
            foreach (string range in new[] { "bytes=10-", "bytes=10-20", "bytes=-0" })
            {
                RawResponse raw = await RangeAsync(tb, range, token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, raw.Status, range);
                Check.Contains(raw.Text, "InvalidRange", "code for " + range);
            }
        }

        private static async Task RangeEmptyObjectAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("orempty", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "empty", "", token).ConfigureAwait(false);
            foreach (string range in new[] { "bytes=0-", "bytes=0-0" })
            {
                RawResponse raw = await GetAsync(tb, "empty", new Dictionary<string, string> { { "Range", range } }, token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, raw.Status, range);
                Check.Contains(raw.Text, "<ActualObjectSize>0</ActualObjectSize>", range + " reports the object size");
            }

            // Amazon S3 answers a suffix range on an empty object with 200 and an empty body.
            RawResponse suffix = await GetAsync(tb, "empty", new Dictionary<string, string> { { "Range", "bytes=-1" } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, suffix.Status, "bytes=-1 on an empty object");
            Check.Equal(0, suffix.Body.Length, "empty body");

            RawResponse whole = await GetAsync(tb, "empty", null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, whole.Status, "unranged GET of an empty object");
        }

        private static async Task RangeOnVersionAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("orver", true, token).ConfigureAwait(false);
            PutObjectResponse v1 = await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "k", "abcdef", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "k", "uvwxyz", token).ConfigureAwait(false);

            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get, "/" + tb.Name + "/k?versionId=" + v1.VersionId,
                new Dictionary<string, string> { { "Range", "bytes=1-2" } }, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PartialContent, raw.Status, "status");
            Check.Equal("bc", raw.Text, "older version's bytes");
            Check.Equal(v1.VersionId, raw.Header("x-amz-version-id"), "version header");
        }

        private static async Task<string> EtagAsync(TestBucket tb, string key, CancellationToken token)
        {
            RawResponse head = await GetAsync(tb, key, null, token, HttpMethod.Head).ConfigureAwait(false);
            return head.Header("ETag")!;
        }

        private static async Task IfNoneMatchAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("ocinm", token).ConfigureAwait(false);
            string etag = await EtagAsync(tb, "ten", token).ConfigureAwait(false);

            RawResponse match = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-None-Match", etag } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotModified, match.Status, "matching ETag");
            Check.Equal(0, match.Body.Length, "no body");
            Check.Equal(etag, match.Header("ETag"), "ETag on 304");

            RawResponse star = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-None-Match", "*" } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotModified, star.Status, "star");

            RawResponse other = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-None-Match", "\"00000000000000000000000000000000\"" } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, other.Status, "different ETag");
            Check.Equal(Ten, other.Text, "body served");
        }

        private static async Task IfMatchAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("ocim", token).ConfigureAwait(false);
            string etag = await EtagAsync(tb, "ten", token).ConfigureAwait(false);

            RawResponse mismatch = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-Match", "\"00000000000000000000000000000000\"" } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PreconditionFailed, mismatch.Status, "mismatch");
            Check.Contains(mismatch.Text, "PreconditionFailed", "code");

            RawResponse match = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-Match", etag } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, match.Status, "match");

            RawResponse ranged = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-Match", "\"nope\"" }, { "Range", "bytes=0-1" } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PreconditionFailed, ranged.Status, "conditions apply to range reads");
        }

        private static async Task IfModifiedSinceAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("ocims", token).ConfigureAwait(false);
            string future = DateTime.UtcNow.AddDays(1).ToString("r", CultureInfo.InvariantCulture);
            string past = DateTime.UtcNow.AddDays(-1).ToString("r", CultureInfo.InvariantCulture);

            RawResponse unmodified = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-Modified-Since", future } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotModified, unmodified.Status, "not modified since tomorrow");

            RawResponse modified = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-Modified-Since", past } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, modified.Status, "modified since yesterday");

            RawResponse garbage = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-Modified-Since", "not a date" } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, garbage.Status, "an unparseable date is ignored");
        }

        private static async Task IfUnmodifiedSinceAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("ocius", token).ConfigureAwait(false);
            string future = DateTime.UtcNow.AddDays(1).ToString("r", CultureInfo.InvariantCulture);
            string past = DateTime.UtcNow.AddDays(-1).ToString("r", CultureInfo.InvariantCulture);

            RawResponse failed = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-Unmodified-Since", past } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PreconditionFailed, failed.Status, "modified after yesterday");

            RawResponse ok = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-Unmodified-Since", future } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, ok.Status, "unmodified before tomorrow");
        }

        private static async Task ConditionalHeadAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("ochead", token).ConfigureAwait(false);
            string etag = await EtagAsync(tb, "ten", token).ConfigureAwait(false);

            RawResponse notModified = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-None-Match", etag } }, token, HttpMethod.Head).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotModified, notModified.Status, "HEAD If-None-Match");

            RawResponse failed = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-Match", "\"nope\"" } }, token, HttpMethod.Head).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PreconditionFailed, failed.Status, "HEAD If-Match");
        }

        private static async Task ConditionalSdkAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithTenAsync("ocsdk", token).ConfigureAwait(false);
            GetObjectMetadataResponse meta = await tb.Client.GetObjectMetadataAsync(tb.Name, "ten", token).ConfigureAwait(false);

            await S3CompatibilityContext.ExpectErrorAsync(
                () => tb.Client.GetObjectAsync(new GetObjectRequest { BucketName = tb.Name, Key = "ten", EtagToMatch = "\"nope\"" }, token),
                HttpStatusCode.PreconditionFailed,
                "EtagToMatch mismatch").ConfigureAwait(false);

            using (GetObjectResponse ok = await tb.Client.GetObjectAsync(new GetObjectRequest { BucketName = tb.Name, Key = "ten", EtagToMatch = meta.ETag }, token).ConfigureAwait(false))
            {
                Check.Equal(HttpStatusCode.OK, ok.HttpStatusCode, "EtagToMatch match");
            }
        }

        private static async Task EtagConsistentAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("oetag", false, token).ConfigureAwait(false);
            PutObjectResponse put = await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "k", "etag-body", token).ConfigureAwait(false);
            string expected = "\"" + Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("etag-body"))).ToLowerInvariant() + "\"";

            Check.Equal(expected, put.ETag, "PUT ETag");
            Check.Equal(expected, (await GetAsync(tb, "k", null, token).ConfigureAwait(false)).Header("ETag"), "GET ETag");
            Check.Equal(expected, await EtagAsync(tb, "k", token).ConfigureAwait(false), "HEAD ETag");

            RawResponse ranged = await GetAsync(tb, "k", new Dictionary<string, string> { { "Range", "bytes=0-1" } }, token).ConfigureAwait(false);
            Check.Equal(expected, ranged.Header("ETag"), "single ETag header on range reads");

            ListObjectsV2Response list = await tb.Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = tb.Name }, token).ConfigureAwait(false);
            Check.Equal(expected, list.S3Objects[0].ETag, "listing ETag");
        }

        private static async Task TimestampsAsync(CancellationToken token)
        {
            // Stored timestamps must not be shifted by the server's UTC offset. On a server whose clock is
            // not UTC, treating stored UTC values as local time moves them by hours; clients such as mc then
            // skip "future" versions when removing them.
            using TestBucket tb = await TestBucket.CreateAsync("otime", true, token).ConfigureAwait(false);
            DateTime before = DateTime.UtcNow.AddMinutes(-2);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "ten", Ten, token).ConfigureAwait(false);
            DateTime after = DateTime.UtcNow.AddMinutes(2);

            ListObjectsV2Response list = await tb.Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = tb.Name }, token).ConfigureAwait(false);
            CheckWithin(list.S3Objects[0].LastModified, before, after, "ListObjectsV2 LastModified");

            ListVersionsResponse versions = await tb.Client.ListVersionsAsync(new ListVersionsRequest { BucketName = tb.Name }, token).ConfigureAwait(false);
            CheckWithin(versions.Versions[0].LastModified, before, after, "ListObjectVersions LastModified");

            RawResponse head = await GetAsync(tb, "ten", null, token, HttpMethod.Head).ConfigureAwait(false);
            DateTime headTime = ParseHttpDate(head.Header("Last-Modified"), "HEAD Last-Modified");
            CheckWithin(headTime, before, after, "HEAD Last-Modified");

            RawResponse notModified = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-None-Match", head.Header("ETag")! } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotModified, notModified.Status, "304");
            Check.Equal(head.Header("Last-Modified"), notModified.Header("Last-Modified"), "304 Last-Modified matches HEAD");

            RawResponse suffix = await GetAsync(tb, "ten", new Dictionary<string, string> { { "Range", "bytes=-2" } }, token).ConfigureAwait(false);
            Check.Equal(head.Header("Last-Modified"), suffix.Header("Last-Modified"), "suffix range Last-Modified matches HEAD");

            RawResponse modifiedSince = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-Modified-Since", headTime.AddMinutes(-30).ToString("r", CultureInfo.InvariantCulture) } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, modifiedSince.Status, "modified since 30 minutes before Last-Modified");

            RawResponse unmodifiedSince = await GetAsync(tb, "ten", new Dictionary<string, string> { { "If-Modified-Since", headTime.AddMinutes(30).ToString("r", CultureInfo.InvariantCulture) } }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotModified, unmodifiedSince.Status, "not modified since 30 minutes after Last-Modified");
        }

        private static void CheckWithin(DateTime? value, DateTime lowerUtc, DateTime upperUtc, string description)
        {
            Check.True(value != null, description + " present");
            DateTime utc = value!.Value.ToUniversalTime();
            Check.True(utc >= lowerUtc && utc <= upperUtc, description + " is " + utc.ToString("o", CultureInfo.InvariantCulture) + ", expected between " + lowerUtc.ToString("o", CultureInfo.InvariantCulture) + " and " + upperUtc.ToString("o", CultureInfo.InvariantCulture));
        }

        private static DateTime ParseHttpDate(string? value, string description)
        {
            Check.True(DateTime.TryParseExact(value, "r", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime parsed), description + " parses: " + value);
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        private static async Task ConcurrentReadsAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("oconc", false, token).ConfigureAwait(false);
            string body = new string('x', 256 * 1024);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "shared", body, token).ConfigureAwait(false);

            List<Task<RawResponse>> reads = Enumerable.Range(0, 16).Select(_ => GetAsync(tb, "shared", null, token)).ToList();
            RawResponse[] results = await Task.WhenAll(reads).ConfigureAwait(false);

            foreach (RawResponse result in results)
            {
                Check.Equal(HttpStatusCode.OK, result.Status, "concurrent read status");
                Check.Equal(body.Length, result.Body.Length, "concurrent read length");
            }
        }

        private static async Task ReadsDuringOverwritesAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("orace", false, token).ConfigureAwait(false);
            string[] bodies = Enumerable.Range(0, 4).Select(i => new string((char)('a' + i), 64 * 1024)).ToArray();
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "hot", bodies[0], token).ConfigureAwait(false);

            using CancellationTokenSource stop = new CancellationTokenSource();
            Task writer = Task.Run(async () =>
            {
                int i = 0;
                while (!stop.IsCancellationRequested)
                {
                    await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "hot", bodies[++i % bodies.Length], token).ConfigureAwait(false);
                }
            });

            try
            {
                for (int round = 0; round < 40; round++)
                {
                    RawResponse read = await GetAsync(tb, "hot", null, token).ConfigureAwait(false);
                    Check.Equal(HttpStatusCode.OK, read.Status, "read during overwrite (round " + round + ")");
                    Check.True(bodies.Contains(read.Text), "read returned one complete version (round " + round + ")");
                }
            }
            finally
            {
                stop.Cancel();
                await writer.ConfigureAwait(false);
            }
        }

        private static async Task IfMatchRangeRaceAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("orangerace", false, token).ConfigureAwait(false);
            string bodyA = new string('a', 32 * 1024);
            string bodyB = new string('b', 32 * 1024);
            PutObjectResponse a = await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "hot", bodyA, token).ConfigureAwait(false);

            using CancellationTokenSource stop = new CancellationTokenSource();
            Task writer = Task.Run(async () =>
            {
                int i = 0;
                while (!stop.IsCancellationRequested)
                {
                    await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "hot", (++i % 2 == 0) ? bodyA : bodyB, token).ConfigureAwait(false);
                }
            });

            try
            {
                for (int round = 0; round < 60; round++)
                {
                    RawResponse read = await GetAsync(tb, "hot", new Dictionary<string, string> { { "Range", "bytes=100-199" }, { "If-Match", a.ETag } }, token).ConfigureAwait(false);
                    Check.True(read.Status == HttpStatusCode.PartialContent || read.Status == HttpStatusCode.PreconditionFailed, "status is 206 or 412 (round " + round + ", got " + read.Status + ")");
                    if (read.Status == HttpStatusCode.PartialContent)
                    {
                        Check.Equal(new string('a', 100), read.Text, "206 carries the version named by If-Match (round " + round + ")");
                    }
                }
            }
            finally
            {
                stop.Cancel();
                await writer.ConfigureAwait(false);
            }
        }

        private static async Task SystemMetadataAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("osysmeta", false, token).ConfigureAwait(false);
            PutObjectRequest put = new PutObjectRequest { BucketName = tb.Name, Key = "k", ContentBody = "body", ContentType = "text/plain" };
            put.Headers.CacheControl = "max-age=3600";
            put.Headers.ContentDisposition = "attachment; filename=\"report.txt\"";
            put.Headers.ContentEncoding = "identity";
            put.Headers["Content-Language"] = "en-US";
            put.Headers["Expires"] = "Tue, 01 Jan 2030 00:00:00 GMT";
            await tb.Client.PutObjectAsync(put, token).ConfigureAwait(false);

            foreach (HttpMethod method in new[] { HttpMethod.Head, HttpMethod.Get })
            {
                RawResponse raw = await GetAsync(tb, "k", null, token, method).ConfigureAwait(false);
                Check.Equal("max-age=3600", raw.Header("Cache-Control"), method + " Cache-Control");
                Check.Equal("attachment; filename=\"report.txt\"", raw.Header("Content-Disposition"), method + " Content-Disposition");
                Check.Equal("identity", raw.Header("Content-Encoding"), method + " Content-Encoding");
                Check.Equal("en-US", raw.Header("Content-Language"), method + " Content-Language");
                Check.True(raw.Header("Expires") != null && raw.Header("Expires")!.Contains("2030"), method + " Expires");
                Check.Equal(null, raw.Header("x-amz-meta-:cache-control"), method + " system metadata not leaked as user metadata");
            }
        }

        private static async Task ResponseOverridesAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("oresp", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "k", "body", token).ConfigureAwait(false);

            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get,
                "/" + tb.Name + "/k?response-content-type=application%2Fjson&response-content-disposition=" + Uri.EscapeDataString("attachment; filename=\"x.json\"") + "&response-cache-control=no-cache",
                null, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, raw.Status, "status");
            Check.True(raw.Header("Content-Type")!.StartsWith("application/json", StringComparison.Ordinal), "content type overridden");
            Check.Equal("attachment; filename=\"x.json\"", raw.Header("Content-Disposition"), "disposition overridden");
            Check.Equal("no-cache", raw.Header("Cache-Control"), "cache control overridden");
        }

        private static async Task UserMetadataLowercaseAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("ousermeta", false, token).ConfigureAwait(false);
            RawResponse put = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/k",
                new Dictionary<string, string> { { "X-Amz-Meta-MixedCase", "Value" } }, Encoding.UTF8.GetBytes("x"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, put.Status, "put");

            GetObjectMetadataResponse meta = await tb.Client.GetObjectMetadataAsync(tb.Name, "k", token).ConfigureAwait(false);
            Check.Equal("Value", meta.Metadata["x-amz-meta-mixedcase"], "lowercased key, value preserved");
        }

        private static async Task DefaultContentTypeAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("octype", false, token).ConfigureAwait(false);
            using (HttpRequestMessage request = tb.Server.CreateS3Request(HttpMethod.Put, "/" + tb.Name + "/k", S3CompatibilityContext.DefaultAccessKey))
            {
                ByteArrayContent content = new ByteArrayContent(Encoding.UTF8.GetBytes("x"));
                content.Headers.ContentType = null;
                request.Content = content;
                RawResponse put = await S3CompatibilityContext.SendRequestAsync(request, token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.OK, put.Status, "put");
            }

            RawResponse head = await GetAsync(tb, "k", null, token, HttpMethod.Head).ConfigureAwait(false);
            Check.Equal("binary/octet-stream", head.Header("Content-Type"), "default content type");
        }

        private static async Task ContentMd5Async(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("omd5", false, token).ConfigureAwait(false);
            byte[] body = Encoding.UTF8.GetBytes("digest me");

            RawResponse good = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/good",
                new Dictionary<string, string> { { "Content-MD5", S3CompatibilityContext.ContentMd5(body) } }, body, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, good.Status, "matching digest accepted");

            RawResponse bad = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/bad",
                new Dictionary<string, string> { { "Content-MD5", S3CompatibilityContext.ContentMd5(Encoding.UTF8.GetBytes("other")) } }, body, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, bad.Status, "mismatched digest");
            Check.Contains(bad.Text, "BadDigest", "code");
            await S3CompatibilityContext.ExpectErrorAsync(() => tb.Client.GetObjectMetadataAsync(tb.Name, "bad", token), HttpStatusCode.NotFound, "no object written on BadDigest").ConfigureAwait(false);

            RawResponse malformed = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/malformed",
                new Dictionary<string, string> { { "Content-MD5", "not-base64" } }, body, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, malformed.Status, "malformed digest");
            Check.Contains(malformed.Text, "InvalidDigest", "code");

            PutObjectResponse sdk = await tb.Client.PutObjectAsync(new PutObjectRequest { BucketName = tb.Name, Key = "sdk", ContentBody = "digest me", MD5Digest = S3CompatibilityContext.ContentMd5(body) }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, sdk.HttpStatusCode, "SDK MD5Digest accepted");
        }

        private static async Task ConditionalWriteIfNoneMatchAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("ocwinm", false, token).ConfigureAwait(false);
            Dictionary<string, string> ifNoneMatch = new Dictionary<string, string> { { "If-None-Match", "*" } };

            RawResponse created = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/once", ifNoneMatch, Encoding.UTF8.GetBytes("first"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, created.Status, "create when absent");

            RawResponse rejected = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/once", ifNoneMatch, Encoding.UTF8.GetBytes("second"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PreconditionFailed, rejected.Status, "reject when present");
            Check.Equal("first", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "once", null, token).ConfigureAwait(false), "original kept");

            RawResponse unsupported = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/once",
                new Dictionary<string, string> { { "If-None-Match", "\"abc\"" } }, Encoding.UTF8.GetBytes("third"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotImplemented, unsupported.Status, "only * is supported on writes");
        }

        private static async Task ConditionalWriteIfMatchAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("ocwim", false, token).ConfigureAwait(false);
            PutObjectResponse original = await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "k", "v1", token).ConfigureAwait(false);

            RawResponse wrong = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/k",
                new Dictionary<string, string> { { "If-Match", "\"00000000000000000000000000000000\"" } }, Encoding.UTF8.GetBytes("v2"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PreconditionFailed, wrong.Status, "stale ETag rejected");
            Check.Equal("v1", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "k", null, token).ConfigureAwait(false), "object unchanged");

            RawResponse right = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/k",
                new Dictionary<string, string> { { "If-Match", original.ETag } }, Encoding.UTF8.GetBytes("v2"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, right.Status, "current ETag accepted");
            Check.Equal("v2", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "k", null, token).ConfigureAwait(false), "object replaced");

            RawResponse missing = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/absent",
                new Dictionary<string, string> { { "If-Match", original.ETag } }, Encoding.UTF8.GetBytes("x"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotFound, missing.Status, "If-Match on a missing key");
        }

        private static async Task PutTaggingHeaderAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("otaghdr", false, token).ConfigureAwait(false);
            RawResponse put = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/k",
                new Dictionary<string, string> { { "x-amz-tagging", "team=storage&cost%20center=a%26b" } }, Encoding.UTF8.GetBytes("x"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, put.Status, "put with tags");

            GetObjectTaggingResponse tags = await tb.Client.GetObjectTaggingAsync(new GetObjectTaggingRequest { BucketName = tb.Name, Key = "k" }, token).ConfigureAwait(false);
            Check.True(tags.Tagging.Any(t => t.Key == "team" && t.Value == "storage"), "first tag");
            Check.True(tags.Tagging.Any(t => t.Key == "cost center" && t.Value == "a&b"), "decoded tag");

            RawResponse invalid = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/bad",
                new Dictionary<string, string> { { "x-amz-tagging", "=nokey" } }, Encoding.UTF8.GetBytes("x"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, invalid.Status, "invalid tag set rejected");
            await S3CompatibilityContext.ExpectErrorAsync(() => tb.Client.GetObjectMetadataAsync(tb.Name, "bad", token), HttpStatusCode.NotFound, "nothing written").ConfigureAwait(false);
        }

        private static async Task LargePutAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("olarge", false, token).ConfigureAwait(false);
            byte[] data = new byte[24 * 1024 * 1024];
            new Random(42).NextBytes(data);

            RawResponse put = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/big.bin",
                new Dictionary<string, string> { { "x-amz-content-sha256", "UNSIGNED-PAYLOAD" } }, data, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, put.Status, "put");
            Check.Equal("\"" + Convert.ToHexString(MD5.HashData(data)).ToLowerInvariant() + "\"", put.Header("ETag"), "ETag is the MD5 of the full body");

            RawResponse get = await GetAsync(tb, "big.bin", null, token).ConfigureAwait(false);
            Check.True(get.Body.AsSpan().SequenceEqual(data), "content round-trips");
        }

        private static async Task GrantHeadersAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            TestPrincipal owner = await S3CompatibilityContext.CreateUserAsync(server, true, token).ConfigureAwait(false);
            TestPrincipal byId = await S3CompatibilityContext.CreateUserAsync(server, token).ConfigureAwait(false);
            TestPrincipal byEmail = await S3CompatibilityContext.CreateUserAsync(server, token).ConfigureAwait(false);
            TestPrincipal stranger = await S3CompatibilityContext.CreateUserAsync(server, token).ConfigureAwait(false);

            using IAmazonS3 ownerClient = S3CompatibilityContext.Client(server, owner.AccessKey, owner.SecretKey);
            string bucket = await S3CompatibilityContext.NewBucketAsync(ownerClient, "ogrant", false, token).ConfigureAwait(false);

            RawResponse put = await S3CompatibilityContext.SendAsync(server, HttpMethod.Put, "/" + bucket + "/shared.txt",
                new Dictionary<string, string> { { "x-amz-grant-read", "id=\"" + byId.UserId + "\", emailAddress=\"" + byEmail.Email + "\"" } },
                Encoding.UTF8.GetBytes("shared"), token, owner.AccessKey).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, put.Status, "put with grant headers");

            foreach (TestPrincipal reader in new[] { byId, byEmail })
            {
                using IAmazonS3 client = S3CompatibilityContext.Client(server, reader.AccessKey, reader.SecretKey);
                Check.Equal("shared", await S3CompatibilityContext.GetTextAsync(client, bucket, "shared.txt", null, token).ConfigureAwait(false), "granted reader " + reader.UserId);
            }

            using IAmazonS3 strangerClient = S3CompatibilityContext.Client(server, stranger.AccessKey, stranger.SecretKey);
            await S3CompatibilityContext.ExpectErrorAsync(() => strangerClient.GetObjectAsync(bucket, "shared.txt", token), HttpStatusCode.Forbidden, "ungranted user denied").ConfigureAwait(false);
        }

        private static async Task GrantHeadersUnknownAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("ograntbad", false, token).ConfigureAwait(false);
            RawResponse put = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/k",
                new Dictionary<string, string> { { "x-amz-grant-read", "id=\"usr_does_not_exist\"" } }, Encoding.UTF8.GetBytes("x"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, put.Status, "unknown grantee rejected");
            await S3CompatibilityContext.ExpectErrorAsync(() => tb.Client.GetObjectMetadataAsync(tb.Name, "k", token), HttpStatusCode.NotFound, "no object written").ConfigureAwait(false);
        }

        private static async Task CannedAclBucketOwnerAsync(CancellationToken token)
        {
            Less3TestServer server = await S3CompatibilityContext.ServerAsync(token).ConfigureAwait(false);
            TestPrincipal owner = await S3CompatibilityContext.CreateUserAsync(server, true, token).ConfigureAwait(false);
            TestPrincipal writer = await S3CompatibilityContext.CreateUserAsync(server, token).ConfigureAwait(false);

            using IAmazonS3 ownerClient = S3CompatibilityContext.Client(server, owner.AccessKey, owner.SecretKey);
            string bucket = await S3CompatibilityContext.NewBucketAsync(ownerClient, "ocannedbo", false, token).ConfigureAwait(false);
            await ownerClient.PutACLAsync(new PutACLRequest { BucketName = bucket, CannedACL = S3CannedACL.PublicReadWrite }, token).ConfigureAwait(false);

            using IAmazonS3 writerClient = S3CompatibilityContext.Client(server, writer.AccessKey, writer.SecretKey);
            PutObjectResponse put = await writerClient.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = "delivered.txt",
                ContentBody = "x",
                CannedACL = S3CannedACL.BucketOwnerFullControl
            }, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, put.HttpStatusCode, "bucket-owner-full-control accepted");

            GetACLResponse acl = await writerClient.GetACLAsync(new GetACLRequest { BucketName = bucket, Key = "delivered.txt" }, token).ConfigureAwait(false);
            Check.True((acl.AccessControlList.Grants ?? new List<S3Grant>()).Any(g => g.Grantee?.CanonicalUser == owner.UserId && g.Permission?.Value == "FULL_CONTROL"), "bucket owner granted full control");
        }

        private static async Task CannedAclUnknownAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("ocannedbad", false, token).ConfigureAwait(false);
            RawResponse put = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Put, "/" + tb.Name + "/k",
                new Dictionary<string, string> { { "x-amz-acl", "everyone-gets-everything" } }, Encoding.UTF8.GetBytes("x"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, put.Status, "unknown canned ACL rejected");
            await S3CompatibilityContext.ExpectErrorAsync(() => tb.Client.GetObjectMetadataAsync(tb.Name, "k", token), HttpStatusCode.NotFound, "no object written").ConfigureAwait(false);
        }

        #endregion
    }
}
