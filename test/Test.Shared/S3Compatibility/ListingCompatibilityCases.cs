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
    /// ListObjects (v1) and ListObjectsV2 compatibility with Amazon S3: binary key order across pages,
    /// markers and continuation tokens, delimiter roll-up, prefixes, and parameter validation.
    /// </summary>
    public static class ListingCompatibilityCases
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static readonly XNamespace _S3 = "http://s3.amazonaws.com/doc/2006-03-01/";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Suite descriptor.
        /// </summary>
        public static TestSuiteDescriptor Suite()
        {
            const string suite = "S3CompatListing";
            return new TestSuiteDescriptor(
                suiteId: suite,
                displayName: "S3 Compatibility: Listing",
                cases: new List<TestCaseDescriptor>
                {
                    S3CompatibilitySuites.Case(suite, "V2_BinaryKeyOrderAcrossPages", "ListObjectsV2 returns keys in binary order across pages regardless of write order", V2BinaryOrderAsync),
                    S3CompatibilitySuites.Case(suite, "V2_OverwriteKeepsPosition", "An overwritten key keeps its position in the listing", V2OverwriteKeepsPositionAsync),
                    S3CompatibilitySuites.Case(suite, "V1_MarkerPagination", "ListObjects (v1) pages by marker without duplicates or gaps", V1MarkerPaginationAsync),
                    S3CompatibilitySuites.Case(suite, "V1_MarkerIsAKeyNotAnOffset", "A v1 marker starts after that key, not after one row", V1MarkerIsKeyAsync),
                    S3CompatibilitySuites.Case(suite, "V1_DelimiterNextMarker", "Truncated v1 listings with a delimiter return NextMarker and page to completion", V1DelimiterNextMarkerAsync),
                    S3CompatibilitySuites.Case(suite, "V2_DelimiterPagination_NoSkips", "Delimiter listings page through keys and prefixes without skipping or repeating", V2DelimiterPaginationAsync),
                    S3CompatibilitySuites.Case(suite, "V2_Delimiter_PageBoundaryDoesNotDropKeys", "A page that fills part-way through a batch does not drop the remaining keys", V2PageBoundaryAsync),
                    S3CompatibilitySuites.Case(suite, "V2_CommonPrefixesCountTowardMaxKeys", "Common prefixes count toward MaxKeys and KeyCount", V2PrefixesCountAsync),
                    S3CompatibilitySuites.Case(suite, "V2_StartAfter", "StartAfter lists only keys after the given key and is echoed", V2StartAfterAsync),
                    S3CompatibilitySuites.Case(suite, "V2_ContinuationTokenEchoed", "The continuation token is echoed in the next response", V2ContinuationEchoedAsync),
                    S3CompatibilitySuites.Case(suite, "V2_InvalidContinuationToken_400", "A malformed continuation token is rejected with 400", V2InvalidTokenAsync),
                    S3CompatibilitySuites.Case(suite, "Prefix_WildcardsMatchedLiterally", "Prefixes containing _ and % match literally", PrefixLiteralAsync),
                    S3CompatibilitySuites.Case(suite, "Prefix_CaseSensitive", "Prefix matching is case-sensitive", PrefixCaseSensitiveAsync),
                    S3CompatibilitySuites.Case(suite, "Keys_CaseSensitive", "Keys differing only in case are distinct objects", KeysCaseSensitiveAsync),
                    S3CompatibilitySuites.Case(suite, "Keys_TrailingSpacesDistinct", "Keys differing only by trailing spaces are distinct, and overwriting one never touches the other", KeysTrailingSpacesAsync),
                    S3CompatibilitySuites.Case(suite, "Keys_UnicodeRoundTripAndOrder", "Unicode keys round-trip and sort in UTF-8 byte order", UnicodeKeysAsync),
                    S3CompatibilitySuites.Case(suite, "MaxKeys_Zero_ReturnsEmpty", "max-keys=0 returns no keys", MaxKeysZeroAsync),
                    S3CompatibilitySuites.Case(suite, "MaxKeys_Invalid_400", "A non-numeric or negative max-keys is rejected with 400 InvalidArgument", MaxKeysInvalidAsync),
                    S3CompatibilitySuites.Case(suite, "MaxKeys_CappedAt1000", "max-keys above 1000 is capped at 1000", MaxKeysCappedAsync),
                    S3CompatibilitySuites.Case(suite, "EncodingTypeUrl", "encoding-type=url encodes keys and is echoed", EncodingTypeUrlAsync),
                    S3CompatibilitySuites.Case(suite, "EncodingType_Invalid_400", "An unknown encoding-type is rejected with 400", EncodingTypeInvalidAsync),
                    S3CompatibilitySuites.Case(suite, "V1_IncludesOwner_V2_FetchOwner", "v1 includes Owner; v2 includes it only with fetch-owner=true", OwnerInclusionAsync),
                    S3CompatibilitySuites.Case(suite, "EmptyBucket_NotTruncated", "An empty bucket lists no keys and is not truncated", EmptyBucketAsync)
                });
        }

        #endregion

        #region Private-Methods

        private static async Task PutKeysAsync(TestBucket tb, IEnumerable<string> keys, CancellationToken token)
        {
            foreach (string key in keys)
            {
                await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, key, key, token).ConfigureAwait(false);
            }
        }

        private static async Task<XElement> ListXmlAsync(TestBucket tb, string query, CancellationToken token)
        {
            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get, "/" + tb.Name + "?" + query, null, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, raw.Status, "list " + query);
            return XElement.Parse(raw.Text);
        }

        private static List<string> Values(XElement root, string container, string element)
        {
            return root.Elements(_S3 + container).Select(c => c.Element(_S3 + element)!.Value).ToList();
        }

        private static string? Value(XElement root, string element)
        {
            return root.Element(_S3 + element)?.Value;
        }

        private static async Task V2BinaryOrderAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lorder", false, token).ConfigureAwait(false);
            List<string> keys = new List<string> { "zeta", "Alpha", "alpha", "_under", "Zeta", "a-b", "ab", "a/b", "10", "9", "~tilde", "b" };
            await PutKeysAsync(tb, keys, token).ConfigureAwait(false);

            List<string> expected = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            foreach (int pageSize in new[] { 1, 2, 5, 1000 })
            {
                List<string> actual = await S3CompatibilityContext.AllKeysV2Async(tb.Client, tb.Name, pageSize, token).ConfigureAwait(false);
                Check.SequenceEqual(expected, actual, "binary order with page size " + pageSize);
            }
        }

        private static async Task V2OverwriteKeepsPositionAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lover", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "a", "b", "c" }, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "a", "rewritten", token).ConfigureAwait(false);

            List<string> actual = await S3CompatibilityContext.AllKeysV2Async(tb.Client, tb.Name, 2, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "a", "b", "c" }, actual, "overwrite does not move the key");
        }

        private static async Task V1MarkerPaginationAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lv1", false, token).ConfigureAwait(false);
            List<string> keys = Enumerable.Range(0, 7).Select(i => "key-" + (char)('g' - i)).ToList();
            await PutKeysAsync(tb, keys, token).ConfigureAwait(false);

            List<string> actual = new List<string>();
            string? marker = null;
            for (int page = 0; page < 10; page++)
            {
                ListObjectsResponse response = await tb.Client.ListObjectsAsync(new ListObjectsRequest { BucketName = tb.Name, MaxKeys = 3, Marker = marker }, token).ConfigureAwait(false);
                if (response.S3Objects != null) actual.AddRange(response.S3Objects.Select(o => o.Key));
                if (response.IsTruncated != true) break;
                marker = response.S3Objects!.Last().Key;
            }

            Check.SequenceEqual(keys.OrderBy(k => k, StringComparer.Ordinal).ToList(), actual, "v1 pagination");
        }

        private static async Task V1MarkerIsKeyAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lv1mk", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "a", "b", "c", "d" }, token).ConfigureAwait(false);

            XElement afterC = await ListXmlAsync(tb, "marker=c", token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "d" }, Values(afterC, "Contents", "Key"), "marker=c lists only d");
            Check.Equal("c", Value(afterC, "Marker"), "marker echoed");

            XElement afterMissing = await ListXmlAsync(tb, "marker=bb", token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "c", "d" }, Values(afterMissing, "Contents", "Key"), "a marker need not exist");
        }

        private static async Task V1DelimiterNextMarkerAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lv1nm", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "a/1", "a/2", "b", "c/1", "c/2", "d" }, token).ConfigureAwait(false);

            List<string> items = new List<string>();
            string marker = "";
            for (int page = 0; page < 10; page++)
            {
                XElement root = await ListXmlAsync(tb, "delimiter=%2F&max-keys=1&marker=" + Uri.EscapeDataString(marker), token).ConfigureAwait(false);
                items.AddRange(Values(root, "Contents", "Key"));
                items.AddRange(Values(root, "CommonPrefixes", "Prefix"));
                if (Value(root, "IsTruncated") != "true") break;

                string? next = Value(root, "NextMarker");
                Check.False(String.IsNullOrEmpty(next), "NextMarker present on truncated delimiter listing");
                marker = next!;
            }

            Check.SequenceEqual(new List<string> { "a/", "b", "c/", "d" }, items, "every key and prefix exactly once");
        }

        private static async Task V2DelimiterPaginationAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lv2del", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "a/1", "a/2", "a/3", "b", "c/1", "d", "e", "f/1", "f/2", "g" }, token).ConfigureAwait(false);

            foreach (int pageSize in new[] { 1, 2, 3 })
            {
                List<string> items = new List<string>();
                string? continuation = null;
                for (int page = 0; page < 20; page++)
                {
                    ListObjectsV2Response response = await tb.Client.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = tb.Name,
                        Delimiter = "/",
                        MaxKeys = pageSize,
                        ContinuationToken = continuation
                    }, token).ConfigureAwait(false);

                    if (response.S3Objects != null) items.AddRange(response.S3Objects.Select(o => o.Key));
                    if (response.CommonPrefixes != null) items.AddRange(response.CommonPrefixes);
                    if (response.IsTruncated != true) break;
                    continuation = response.NextContinuationToken;
                }

                Check.SequenceEqual(
                    new List<string> { "a/", "b", "c/", "d", "e", "f/", "g" },
                    items.OrderBy(i => i, StringComparer.Ordinal).ToList(),
                    "all keys and prefixes with page size " + pageSize);
                Check.Equal(7, items.Count, "no duplicates with page size " + pageSize);
            }
        }

        private static async Task V2PageBoundaryAsync(CancellationToken token)
        {
            // Write order matters: a rolled-up key first, then plain keys. The old enumerator read rows in
            // write order and advanced past rows it had not returned once a page filled, dropping "c".
            using TestBucket tb = await TestBucket.CreateAsync("lv2edge", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "p/1", "a", "b", "c" }, token).ConfigureAwait(false);

            List<string> items = new List<string>();
            string? continuation = null;
            for (int page = 0; page < 10; page++)
            {
                ListObjectsV2Response response = await tb.Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = tb.Name,
                    Delimiter = "/",
                    MaxKeys = 2,
                    ContinuationToken = continuation
                }, token).ConfigureAwait(false);

                if (response.S3Objects != null) items.AddRange(response.S3Objects.Select(o => o.Key));
                if (response.CommonPrefixes != null) items.AddRange(response.CommonPrefixes);
                if (response.IsTruncated != true) break;
                continuation = response.NextContinuationToken;
            }

            Check.SequenceEqual(new List<string> { "a", "b", "c", "p/" }, items.OrderBy(i => i, StringComparer.Ordinal).ToList(), "every key and prefix returned");
            Check.Equal(4, items.Count, "nothing repeated");
        }

        private static async Task V2PrefixesCountAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lv2cnt", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "a/1", "b/1", "c", "d" }, token).ConfigureAwait(false);

            XElement root = await ListXmlAsync(tb, "list-type=2&delimiter=%2F&max-keys=3", token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "a/", "b/" }, Values(root, "CommonPrefixes", "Prefix"), "prefixes");
            Check.SequenceEqual(new List<string> { "c" }, Values(root, "Contents", "Key"), "keys");
            Check.Equal("3", Value(root, "KeyCount"), "KeyCount includes prefixes");
            Check.Equal("true", Value(root, "IsTruncated"), "truncated");
        }

        private static async Task V2StartAfterAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lv2sa", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "a", "b", "c", "d" }, token).ConfigureAwait(false);

            XElement root = await ListXmlAsync(tb, "list-type=2&start-after=b", token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "c", "d" }, Values(root, "Contents", "Key"), "keys after b");
            Check.Equal("b", Value(root, "StartAfter"), "StartAfter echoed");

            ListObjectsV2Response sdk = await tb.Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = tb.Name, StartAfter = "c" }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "d" }, sdk.S3Objects.Select(o => o.Key).ToList(), "SDK StartAfter");
        }

        private static async Task V2ContinuationEchoedAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lv2echo", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "a", "b", "c" }, token).ConfigureAwait(false);

            XElement first = await ListXmlAsync(tb, "list-type=2&max-keys=1", token).ConfigureAwait(false);
            string next = Value(first, "NextContinuationToken")!;
            Check.False(String.IsNullOrEmpty(next), "token issued");

            XElement second = await ListXmlAsync(tb, "list-type=2&max-keys=1&continuation-token=" + Uri.EscapeDataString(next), token).ConfigureAwait(false);
            Check.Equal(next, Value(second, "ContinuationToken"), "token echoed");
            Check.SequenceEqual(new List<string> { "b" }, Values(second, "Contents", "Key"), "second page");
        }

        private static async Task V2InvalidTokenAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lv2tok", false, token).ConfigureAwait(false);
            foreach (string bad in new[] { "not-base64!!", Convert.ToBase64String(Encoding.UTF8.GetBytes("12")) })
            {
                RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get, "/" + tb.Name + "?list-type=2&continuation-token=" + Uri.EscapeDataString(bad), null, null, token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.BadRequest, raw.Status, "token " + bad);
                Check.Contains(raw.Text, "InvalidArgument", "code for token " + bad);
            }
        }

        private static async Task PrefixLiteralAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lprefix", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "a_b", "axb", "a%c", "abc" }, token).ConfigureAwait(false);

            ListObjectsV2Response underscore = await tb.Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = tb.Name, Prefix = "a_" }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "a_b" }, underscore.S3Objects.Select(o => o.Key).ToList(), "_ is not a wildcard");

            ListObjectsV2Response percent = await tb.Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = tb.Name, Prefix = "a%" }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "a%c" }, percent.S3Objects.Select(o => o.Key).ToList(), "% is not a wildcard");
        }

        private static async Task PrefixCaseSensitiveAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lprefcase", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "Docs/a", "docs/b" }, token).ConfigureAwait(false);

            ListObjectsV2Response lower = await tb.Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = tb.Name, Prefix = "docs/" }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "docs/b" }, lower.S3Objects.Select(o => o.Key).ToList(), "lowercase prefix");
        }

        private static async Task KeysCaseSensitiveAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lkeycase", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "Photo.jpg", "upper", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "photo.jpg", "lower", token).ConfigureAwait(false);

            Check.Equal("upper", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "Photo.jpg", null, token).ConfigureAwait(false), "uppercase key content");
            Check.Equal("lower", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "photo.jpg", null, token).ConfigureAwait(false), "lowercase key content");

            List<string> keys = await S3CompatibilityContext.AllKeysV2Async(tb.Client, tb.Name, 1000, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "Photo.jpg", "photo.jpg" }, keys, "both keys listed");

            await tb.Client.DeleteObjectAsync(tb.Name, "Photo.jpg", token).ConfigureAwait(false);
            Check.Equal("lower", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "photo.jpg", null, token).ConfigureAwait(false), "deleting one case leaves the other");
        }

        private static async Task KeysTrailingSpacesAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lspace", false, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "a", "no space", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "a ", "one space", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "a  ", "two spaces", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "a ", "one space again", token).ConfigureAwait(false);

            Check.Equal("no space", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "a", null, token).ConfigureAwait(false), "overwriting \"a \" left \"a\" intact");
            Check.Equal("one space again", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "a ", null, token).ConfigureAwait(false), "\"a \" overwritten");
            Check.Equal("two spaces", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "a  ", null, token).ConfigureAwait(false), "\"a  \" intact");

            List<string> keys = await S3CompatibilityContext.AllKeysV2Async(tb.Client, tb.Name, 1, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "a", "a ", "a  " }, keys, "all three keys listed once, in byte order");

            ListObjectsV2Response prefixed = await tb.Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = tb.Name, Prefix = "a " }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "a ", "a  " }, prefixed.S3Objects.Select(o => o.Key).ToList(), "prefix with a trailing space");

            await tb.Client.DeleteObjectAsync(tb.Name, "a ", token).ConfigureAwait(false);
            Check.Equal("no space", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "a", null, token).ConfigureAwait(false), "deleting \"a \" left \"a\" intact");
            Check.Equal("two spaces", await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, "a  ", null, token).ConfigureAwait(false), "deleting \"a \" left \"a  \" intact");
        }

        private static async Task UnicodeKeysAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lunicode", false, token).ConfigureAwait(false);
            List<string> keys = new List<string> { "café.txt", "cafe.txt", "日本/文書.txt", "z.txt", "Ångström" };
            await PutKeysAsync(tb, keys, token).ConfigureAwait(false);

            foreach (string key in keys)
            {
                Check.Equal(key, await S3CompatibilityContext.GetTextAsync(tb.Client, tb.Name, key, null, token).ConfigureAwait(false), "round trip " + key);
            }

            List<string> expected = keys.OrderBy(k => Encoding.UTF8.GetBytes(k), new ByteArrayComparer()).ToList();
            List<string> actual = await S3CompatibilityContext.AllKeysV2Async(tb.Client, tb.Name, 2, token).ConfigureAwait(false);
            Check.SequenceEqual(expected, actual, "UTF-8 byte order");

            ListObjectsV2Response prefixed = await tb.Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = tb.Name, Prefix = "café" }, token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "café.txt" }, prefixed.S3Objects.Select(o => o.Key).ToList(), "unicode prefix");
        }

        private static async Task MaxKeysZeroAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lmk0", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "a" }, token).ConfigureAwait(false);

            XElement root = await ListXmlAsync(tb, "list-type=2&max-keys=0", token).ConfigureAwait(false);
            Check.Equal(0, Values(root, "Contents", "Key").Count, "no keys");
            Check.Equal("0", Value(root, "MaxKeys"), "MaxKeys echoed");
        }

        private static async Task MaxKeysInvalidAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lmkbad", false, token).ConfigureAwait(false);
            foreach (string bad in new[] { "abc", "1.5", "-1" })
            {
                RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get, "/" + tb.Name + "?list-type=2&max-keys=" + bad, null, null, token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.BadRequest, raw.Status, "max-keys=" + bad);
                Check.Contains(raw.Text, "<Code>InvalidArgument</Code>", "an S3 XML error for max-keys=" + bad);
            }
        }

        private static async Task MaxKeysCappedAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lmkcap", false, token).ConfigureAwait(false);
            XElement root = await ListXmlAsync(tb, "list-type=2&max-keys=5000", token).ConfigureAwait(false);
            Check.Equal("1000", Value(root, "MaxKeys"), "capped");
        }

        private static async Task EncodingTypeUrlAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lenc", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "dir/a b&c.txt", "dir/p+q.txt" }, token).ConfigureAwait(false);

            // Amazon S3 form-encodes: a space becomes '+', '/' is kept, and a literal '+' becomes %2B, so
            // clients decode with form decoding.
            XElement root = await ListXmlAsync(tb, "list-type=2&encoding-type=url", token).ConfigureAwait(false);
            Check.Equal("url", Value(root, "EncodingType"), "echoed");
            List<string> encoded = Values(root, "Contents", "Key").ToList();
            Check.SequenceEqual(new List<string> { "dir/a+b%26c.txt", "dir/p%2Bq.txt" }, encoded, "encoded keys");
            Check.SequenceEqual(new List<string> { "dir/a b&c.txt", "dir/p+q.txt" }, encoded.Select(k => WebUtility.UrlDecode(k)).ToList(), "form-decode to the keys");

            XElement v1 = await ListXmlAsync(tb, "encoding-type=url&prefix=dir%2Fa%20", token).ConfigureAwait(false);
            Check.Equal("dir/a+", Value(v1, "Prefix"), "v1 echoes the prefix encoded");
            Check.Equal("dir/a+b%26c.txt", Values(v1, "Contents", "Key").Single(), "v1 key encoded");

            XElement plain = await ListXmlAsync(tb, "list-type=2", token).ConfigureAwait(false);
            Check.SequenceEqual(new List<string> { "dir/a b&c.txt", "dir/p+q.txt" }, Values(plain, "Contents", "Key").ToList(), "unencoded without encoding-type");
        }

        private static async Task EncodingTypeInvalidAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lencbad", false, token).ConfigureAwait(false);
            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get, "/" + tb.Name + "?list-type=2&encoding-type=base64", null, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, raw.Status, "status");
        }

        private static async Task OwnerInclusionAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lowner", false, token).ConfigureAwait(false);
            await PutKeysAsync(tb, new[] { "a" }, token).ConfigureAwait(false);

            XElement v1 = await ListXmlAsync(tb, "", token).ConfigureAwait(false);
            Check.True(v1.Element(_S3 + "Contents")!.Element(_S3 + "Owner") != null, "v1 includes Owner");

            XElement v2 = await ListXmlAsync(tb, "list-type=2", token).ConfigureAwait(false);
            Check.True(v2.Element(_S3 + "Contents")!.Element(_S3 + "Owner") == null, "v2 omits Owner by default");

            XElement v2Owner = await ListXmlAsync(tb, "list-type=2&fetch-owner=true", token).ConfigureAwait(false);
            Check.True(v2Owner.Element(_S3 + "Contents")!.Element(_S3 + "Owner") != null, "v2 includes Owner with fetch-owner");
        }

        private static async Task EmptyBucketAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("lempty", false, token).ConfigureAwait(false);
            XElement root = await ListXmlAsync(tb, "list-type=2", token).ConfigureAwait(false);
            Check.Equal("false", Value(root, "IsTruncated"), "not truncated");
            Check.Equal("0", Value(root, "KeyCount"), "no keys");
        }

        #endregion
    }
}
