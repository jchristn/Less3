namespace Test.Shared.S3Compatibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Touchstone.Core;

    /// <summary>
    /// Wire details Less3 inherits from S3Server 8, each matching Amazon S3: HEAD ranges, Content-Range for
    /// the bytes returned, 304 and HEAD error framing, per-operation parameter validation, ListObjects v1/v2
    /// shapes, millisecond timestamps, the S3 XML namespace, and delete-marker entries in version listings.
    /// </summary>
    public static class ProtocolCompatibilityCases
    {
        #region Private-Members

        private static readonly XNamespace _S3 = "http://s3.amazonaws.com/doc/2006-03-01/";
        private static readonly Regex _MillisecondTimestamp = new Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$");
        private const string Eleven = "hello world";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Suite descriptor.
        /// </summary>
        public static TestSuiteDescriptor Suite()
        {
            const string suite = "S3CompatProtocol";
            return new TestSuiteDescriptor(
                suiteId: suite,
                displayName: "S3 Compatibility: Protocol details",
                cases: new List<TestCaseDescriptor>
                {
                    S3CompatibilitySuites.Case(suite, "Head_Range", "HEAD honors Range: 206 with Content-Range and the range length, 416 when unsatisfiable", HeadRangeAsync),
                    S3CompatibilitySuites.Case(suite, "Range_ContentRangeDescribesBytesReturned", "Content-Range describes the bytes returned when the requested end is past the object", ContentRangeReturnedAsync),
                    S3CompatibilitySuites.Case(suite, "NotModified_HeadersOnly", "304 carries ETag and Last-Modified and no body or Content-Type, for GET and HEAD", NotModifiedAsync),
                    S3CompatibilitySuites.Case(suite, "Errors_HeadHasNoBody", "Errors to HEAD have no body; the same error to GET names the key or bucket", HeadErrorsAsync),
                    S3CompatibilitySuites.Case(suite, "Params_ValidatedPerOperation", "Query parameters are validated only by the operations that use them", ParamsPerOperationAsync),
                    S3CompatibilitySuites.Case(suite, "ListShape_V1AndV2", "ListObjects v1 always has Marker and no KeyCount; v2 has KeyCount and no Marker", ListShapeAsync),
                    S3CompatibilitySuites.Case(suite, "Timestamps_Milliseconds", "Listing, version and copy timestamps have exactly three fractional digits", TimestampsAsync),
                    S3CompatibilitySuites.Case(suite, "Xml_S3Namespace", "Response bodies are in the S3 namespace; a top-level Error is not", NamespacesAsync),
                    S3CompatibilitySuites.Case(suite, "VersionListing_DeleteMarkerShape", "Delete markers in a version listing carry no ETag, Size or StorageClass; versions do", DeleteMarkerShapeAsync),
                    S3CompatibilitySuites.Case(suite, "DeleteResult_ErrorShape", "DeleteObjects per-key errors list Key, VersionId, Code and Message, without request-level elements", DeleteErrorShapeAsync),
                    S3CompatibilitySuites.Case(suite, "VersionListing_EncodingTypeUrl", "encoding-type=url encodes version listing keys, prefix and key marker", VersionListingEncodingAsync),
                    S3CompatibilitySuites.Case(suite, "Headers_NoRequestHeadersEchoed", "Responses do not echo request-only headers such as Host and Accept", NoEchoedHeadersAsync)
                });
        }

        #endregion

        #region Private-Methods

        private static async Task<TestBucket> BucketWithObjectAsync(string prefix, bool versioned, CancellationToken token)
        {
            TestBucket tb = await TestBucket.CreateAsync(prefix, versioned, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "k", Eleven, token).ConfigureAwait(false);
            return tb;
        }

        private static async Task<RawResponse> SendAsync(TestBucket tb, HttpMethod method, string pathAndQuery, Dictionary<string, string>? headers, CancellationToken token)
        {
            return await S3CompatibilityContext.SendAsync(tb.Server, method, "/" + tb.Name + pathAndQuery, headers, null, token).ConfigureAwait(false);
        }

        private static Dictionary<string, string> Header(string name, string value)
        {
            return new Dictionary<string, string> { { name, value } };
        }

        private static async Task HeadRangeAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithObjectAsync("phead", false, token).ConfigureAwait(false);

            RawResponse middle = await SendAsync(tb, HttpMethod.Head, "/k", Header("Range", "bytes=2-4"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PartialContent, middle.Status, "bytes=2-4");
            Check.Equal("bytes 2-4/11", middle.Headers["Content-Range"], "Content-Range");
            Check.Equal("3", middle.Headers["Content-Length"], "Content-Length is the range length");

            RawResponse suffix = await SendAsync(tb, HttpMethod.Head, "/k", Header("Range", "bytes=-3"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PartialContent, suffix.Status, "bytes=-3");
            Check.Equal("bytes 8-10/11", suffix.Headers["Content-Range"], "suffix Content-Range");

            RawResponse past = await SendAsync(tb, HttpMethod.Head, "/k", Header("Range", "bytes=50-"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, past.Status, "bytes=50-");
            Check.Equal(0, past.Body.Length, "no body");

            RawResponse whole = await SendAsync(tb, HttpMethod.Head, "/k", null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, whole.Status, "no Range");
            Check.Equal("11", whole.Headers["Content-Length"], "full length");
            Check.False(whole.Headers.ContainsKey("Content-Range"), "no Content-Range without Range");
        }

        private static async Task ContentRangeReturnedAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithObjectAsync("pcrange", false, token).ConfigureAwait(false);

            RawResponse past = await SendAsync(tb, HttpMethod.Get, "/k", Header("Range", "bytes=6-100"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.PartialContent, past.Status, "status");
            Check.Equal("world", past.Text, "body");
            Check.Equal("bytes 6-10/11", past.Headers["Content-Range"], "Content-Range names the last byte returned, not the requested end");

            RawResponse exact = await SendAsync(tb, HttpMethod.Get, "/k", Header("Range", "bytes=0-4"), token).ConfigureAwait(false);
            Check.Equal("bytes 0-4/11", exact.Headers["Content-Range"], "an in-bounds range is echoed");

            RawResponse unsatisfiable = await SendAsync(tb, HttpMethod.Get, "/k", Header("Range", "bytes=11-"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, unsatisfiable.Status, "bytes=11-");
            Check.Contains(unsatisfiable.Text, "<ActualObjectSize>11</ActualObjectSize>", "416 reports the object size");
            Check.Contains(unsatisfiable.Text, "<RangeRequested>bytes=11-</RangeRequested>", "416 echoes the range");
        }

        private static async Task NotModifiedAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithObjectAsync("p304", false, token).ConfigureAwait(false);
            RawResponse head = await SendAsync(tb, HttpMethod.Head, "/k", null, token).ConfigureAwait(false);
            string etag = head.Headers["ETag"];

            foreach (HttpMethod method in new[] { HttpMethod.Get, HttpMethod.Head })
            {
                RawResponse raw = await SendAsync(tb, method, "/k", Header("If-None-Match", etag), token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.NotModified, raw.Status, method + " status");
                Check.Equal(0, raw.Body.Length, method + " no body");
                Check.False(raw.Headers.ContainsKey("Content-Type"), method + " no Content-Type");
                Check.Equal(etag, raw.Headers["ETag"], method + " ETag");
                Check.True(raw.Headers.ContainsKey("Last-Modified"), method + " Last-Modified");
            }

            RawResponse changed = await SendAsync(tb, HttpMethod.Get, "/k", Header("If-None-Match", "\"00000000000000000000000000000000\""), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, changed.Status, "a different ETag is served");
            Check.Equal(Eleven, changed.Text, "body");
        }

        private static async Task HeadErrorsAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("pheaderr", false, token).ConfigureAwait(false);

            RawResponse head = await SendAsync(tb, HttpMethod.Head, "/missing", null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotFound, head.Status, "HEAD status");
            Check.Equal(0, head.Body.Length, "HEAD error has no body");

            RawResponse get = await SendAsync(tb, HttpMethod.Get, "/missing", null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotFound, get.Status, "GET status");
            Check.Contains(get.Text, "<Code>NoSuchKey</Code>", "code");
            Check.Contains(get.Text, "<Key>missing</Key>", "the error names the key");

            string noBucket = "pnobucket" + Guid.NewGuid().ToString("N").Substring(0, 10);
            RawResponse bucket = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Get, "/" + noBucket + "?list-type=2", null, null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.NotFound, bucket.Status, "missing bucket");
            Check.Contains(bucket.Text, "<BucketName>" + noBucket + "</BucketName>", "the error names the bucket");
        }

        private static async Task ParamsPerOperationAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithObjectAsync("pparams", false, token).ConfigureAwait(false);

            // GetObject does not use max-keys, so a bad value is ignored, as in Amazon S3.
            RawResponse get = await SendAsync(tb, HttpMethod.Get, "/k?max-keys=abc", null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, get.Status, "GetObject ignores max-keys");
            Check.Equal(Eleven, get.Text, "body");

            foreach (string bad in new[] { "abc", "-1" })
            {
                RawResponse list = await SendAsync(tb, HttpMethod.Get, "?list-type=2&max-keys=" + bad, null, token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.BadRequest, list.Status, "ListObjectsV2 max-keys=" + bad);
                Check.Contains(list.Text, "<Code>InvalidArgument</Code>", "code for max-keys=" + bad);
                // Amazon S3 names the argument max-keys or maxKeys depending on the failure; both echo the value.
                Check.Contains(list.Text, "<ArgumentValue>" + bad + "</ArgumentValue>", "argument value echoed for max-keys=" + bad);
            }

            RawResponse part = await SendAsync(tb, HttpMethod.Get, "/k?partNumber=0", null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.BadRequest, part.Status, "partNumber=0");
            Check.Contains(part.Text, "<Code>InvalidArgument</Code>", "partNumber code");
        }

        private static async Task ListShapeAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithObjectAsync("pshape", false, token).ConfigureAwait(false);

            XElement v1 = XElement.Parse((await SendAsync(tb, HttpMethod.Get, "", null, token).ConfigureAwait(false)).Text);
            Check.Equal("", v1.Element(_S3 + "Marker")?.Value, "v1 Marker present and empty");
            Check.True(v1.Element(_S3 + "KeyCount") == null, "v1 has no KeyCount");
            Check.True(v1.Element(_S3 + "ContinuationToken") == null && v1.Element(_S3 + "StartAfter") == null, "v1 has no v2 elements");

            XElement v2 = XElement.Parse((await SendAsync(tb, HttpMethod.Get, "?list-type=2", null, token).ConfigureAwait(false)).Text);
            Check.Equal("1", v2.Element(_S3 + "KeyCount")?.Value, "v2 KeyCount");
            Check.True(v2.Element(_S3 + "Marker") == null && v2.Element(_S3 + "NextMarker") == null, "v2 has no Marker or NextMarker");
            Check.True(v2.Element(_S3 + "Contents")!.Element(_S3 + "Owner") == null, "v2 omits Owner without fetch-owner");
        }

        private static async Task TimestampsAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithObjectAsync("ptime", true, token).ConfigureAwait(false);

            XElement list = XElement.Parse((await SendAsync(tb, HttpMethod.Get, "?list-type=2", null, token).ConfigureAwait(false)).Text);
            string listed = list.Element(_S3 + "Contents")!.Element(_S3 + "LastModified")!.Value;
            Check.True(_MillisecondTimestamp.IsMatch(listed), "listing LastModified " + listed);

            XElement versions = XElement.Parse((await SendAsync(tb, HttpMethod.Get, "?versions", null, token).ConfigureAwait(false)).Text);
            string versioned = versions.Element(_S3 + "Version")!.Element(_S3 + "LastModified")!.Value;
            Check.True(_MillisecondTimestamp.IsMatch(versioned), "version listing LastModified " + versioned);

            RawResponse copy = await SendAsync(tb, HttpMethod.Put, "/copy", Header("x-amz-copy-source", "/" + tb.Name + "/k"), token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, copy.Status, "copy status");
            string copied = XElement.Parse(copy.Text).Element(_S3 + "LastModified")!.Value;
            Check.True(_MillisecondTimestamp.IsMatch(copied), "CopyObjectResult LastModified " + copied);
        }

        private static async Task NamespacesAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithObjectAsync("pns", true, token).ConfigureAwait(false);
            await tb.Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest { BucketName = tb.Name, Key = "mp" }, token).ConfigureAwait(false);

            Dictionary<string, RawResponse> responses = new Dictionary<string, RawResponse>
            {
                { "ListBucketResult", await SendAsync(tb, HttpMethod.Get, "?list-type=2", null, token).ConfigureAwait(false) },
                { "ListVersionsResult", await SendAsync(tb, HttpMethod.Get, "?versions", null, token).ConfigureAwait(false) },
                { "ListMultipartUploadsResult", await SendAsync(tb, HttpMethod.Get, "?uploads", null, token).ConfigureAwait(false) },
                { "CopyObjectResult", await SendAsync(tb, HttpMethod.Put, "/copy", Header("x-amz-copy-source", "/" + tb.Name + "/k"), token).ConfigureAwait(false) }
            };

            byte[] deleteBody = Encoding.UTF8.GetBytes("<Delete><Object><Key>copy</Key></Object></Delete>");
            responses["DeleteResult"] = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Post, "/" + tb.Name + "?delete",
                Header("Content-MD5", S3CompatibilityContext.ContentMd5(deleteBody)), deleteBody, token).ConfigureAwait(false);

            foreach (KeyValuePair<string, RawResponse> response in responses)
            {
                Check.Equal(HttpStatusCode.OK, response.Value.Status, response.Key + " status");
                XElement root = XElement.Parse(response.Value.Text);
                Check.Equal(_S3 + response.Key, root.Name, response.Key + " root in the S3 namespace");
                Check.False(root.DescendantsAndSelf().Any(e => e.Name.Namespace == XNamespace.None), response.Key + " has no element outside the namespace");
            }

            RawResponse error = await SendAsync(tb, HttpMethod.Get, "/missing", null, token).ConfigureAwait(false);
            Check.Equal(XNamespace.None, XElement.Parse(error.Text).Name.Namespace, "a top-level Error has no namespace");
        }

        private static async Task DeleteMarkerShapeAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithObjectAsync("pdmshape", true, token).ConfigureAwait(false);
            await tb.Client.DeleteObjectAsync(tb.Name, "k", token).ConfigureAwait(false);

            RawResponse raw = await SendAsync(tb, HttpMethod.Get, "?versions", null, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, raw.Status, "status");
            Check.NotContains(raw.Text, "nil", "no xsi:nil elements");

            XElement root = XElement.Parse(raw.Text);
            XElement marker = root.Element(_S3 + "DeleteMarker")!;
            Check.SequenceEqual(new List<string> { "Key", "VersionId", "IsLatest", "LastModified", "Owner" },
                marker.Elements().Select(e => e.Name.LocalName).ToList(), "delete marker elements");
            Check.Equal("true", marker.Element(_S3 + "IsLatest")!.Value, "marker is latest");

            XElement version = root.Element(_S3 + "Version")!;
            foreach (string name in new[] { "ETag", "Size", "StorageClass", "Owner" })
                Check.True(version.Element(_S3 + name) != null, "version has " + name);
            Check.Equal("11", version.Element(_S3 + "Size")!.Value, "version size");
        }

        private static async Task DeleteErrorShapeAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithObjectAsync("pdelerr", false, token).ConfigureAwait(false);

            byte[] body = Encoding.UTF8.GetBytes("<Delete><Object><Key>k</Key></Object><Object><Key>bad</Key><VersionId>not-a-version</VersionId></Object></Delete>");
            RawResponse raw = await S3CompatibilityContext.SendAsync(tb.Server, HttpMethod.Post, "/" + tb.Name + "?delete",
                Header("Content-MD5", S3CompatibilityContext.ContentMd5(body)), body, token).ConfigureAwait(false);
            Check.Equal(HttpStatusCode.OK, raw.Status, "status");

            XElement root = XElement.Parse(raw.Text);
            XElement error = root.Element(_S3 + "Error")!;
            Check.SequenceEqual(new List<string> { "Key", "VersionId", "Code", "Message" },
                error.Elements().Select(e => e.Name.LocalName).ToList(), "per-key error elements, in Amazon S3's order");
            Check.Equal("bad", error.Element(_S3 + "Key")!.Value, "error key");
            Check.Equal("NoSuchVersion", error.Element(_S3 + "Code")!.Value, "error code");

            XElement deleted = root.Element(_S3 + "Deleted")!;
            Check.SequenceEqual(new List<string> { "Key" }, deleted.Elements().Select(e => e.Name.LocalName).ToList(), "a plain delete reports only the key");

            // A top-level error still starts with Code and carries the request identifiers.
            RawResponse missing = await SendAsync(tb, HttpMethod.Get, "/missing", null, token).ConfigureAwait(false);
            List<string> topLevel = XElement.Parse(missing.Text).Elements().Select(e => e.Name.LocalName).ToList();
            Check.Equal("Code", topLevel.First(), "a top-level error starts with Code");
            Check.True(topLevel.Contains("RequestId") && topLevel.Contains("HostId"), "a top-level error carries RequestId and HostId");
        }

        private static async Task VersionListingEncodingAsync(CancellationToken token)
        {
            using TestBucket tb = await TestBucket.CreateAsync("pvenc", true, token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "dir/a b+c.txt", "x", token).ConfigureAwait(false);
            await S3CompatibilityContext.PutAsync(tb.Client, tb.Name, "dir/z.txt", "x", token).ConfigureAwait(false);

            XElement encoded = XElement.Parse((await SendAsync(tb, HttpMethod.Get, "?versions&encoding-type=url&prefix=dir%2F&max-keys=1", null, token).ConfigureAwait(false)).Text);
            Check.Equal("url", encoded.Element(_S3 + "EncodingType")?.Value, "EncodingType echoed");
            Check.Equal("dir/", encoded.Element(_S3 + "Prefix")?.Value, "prefix");
            Check.Equal("dir/a+b%2Bc.txt", encoded.Element(_S3 + "Version")!.Element(_S3 + "Key")!.Value, "key encoded");
            Check.Equal("dir/a+b%2Bc.txt", encoded.Element(_S3 + "NextKeyMarker")?.Value, "next key marker encoded");
            Check.Equal("dir/a b+c.txt", WebUtility.UrlDecode(encoded.Element(_S3 + "Version")!.Element(_S3 + "Key")!.Value), "form-decodes to the key");

            XElement plain = XElement.Parse((await SendAsync(tb, HttpMethod.Get, "?versions&prefix=dir%2F&max-keys=1", null, token).ConfigureAwait(false)).Text);
            Check.True(plain.Element(_S3 + "EncodingType") == null, "no EncodingType without encoding-type");
            Check.Equal("dir/a b+c.txt", plain.Element(_S3 + "Version")!.Element(_S3 + "Key")!.Value, "key unencoded");
        }

        private static async Task NoEchoedHeadersAsync(CancellationToken token)
        {
            using TestBucket tb = await BucketWithObjectAsync("pechohdr", false, token).ConfigureAwait(false);

            foreach (HttpMethod method in new[] { HttpMethod.Get, HttpMethod.Head })
            {
                RawResponse raw = await SendAsync(tb, method, "/k", null, token).ConfigureAwait(false);
                Check.Equal(HttpStatusCode.OK, raw.Status, method + " status");
                foreach (string name in new[] { "Host", "Accept", "Accept-Language", "Accept-Charset" })
                    Check.False(raw.Headers.ContainsKey(name), method + " response has no " + name + " header");
            }
        }

        #endregion
    }
}
