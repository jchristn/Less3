namespace Less3.Helpers
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;

    /// <summary>
    /// Evaluates the conditional headers Amazon S3 honors on GetObject, HeadObject and CopyObject
    /// (If-Match, If-None-Match, If-Modified-Since, If-Unmodified-Since) using the precedence in
    /// RFC 7232 section 6, which matches Amazon S3's documented behavior. Thread-safe.
    /// </summary>
    internal static class ConditionalRequestEvaluator
    {
        #region Internal-Methods

        /// <summary>
        /// Evaluate conditional headers against an object.
        /// </summary>
        /// <param name="headers">Request headers.</param>
        /// <param name="headerPrefix">Header name prefix: empty for GET/HEAD, "x-amz-copy-source-" for CopyObject.</param>
        /// <param name="etag">Object ETag.</param>
        /// <param name="lastModifiedUtc">Object last-modified timestamp.</param>
        /// <returns>Outcome.</returns>
        internal static ConditionalRequestOutcome Evaluate(NameValueCollection headers, string headerPrefix, string etag, DateTime lastModifiedUtc)
        {
            if (headers == null) return ConditionalRequestOutcome.Proceed;
            string prefix = headerPrefix ?? "";

            // HTTP dates have one-second resolution.
            DateTime lastModified = TruncateToSeconds(UtcTimestamp.AsUtc(lastModifiedUtc));

            string ifMatch = headers[prefix + "if-match"];
            string ifUnmodifiedSince = headers[prefix + "if-unmodified-since"];
            string ifNoneMatch = headers[prefix + "if-none-match"];
            string ifModifiedSince = headers[prefix + "if-modified-since"];

            if (!String.IsNullOrWhiteSpace(ifMatch))
            {
                if (!EtagMatcher.Matches(ifMatch, etag)) return ConditionalRequestOutcome.PreconditionFailed;
            }
            else if (TryParseHttpDate(ifUnmodifiedSince, out DateTime unmodifiedSince))
            {
                if (lastModified > unmodifiedSince) return ConditionalRequestOutcome.PreconditionFailed;
            }

            if (!String.IsNullOrWhiteSpace(ifNoneMatch))
            {
                if (EtagMatcher.Matches(ifNoneMatch, etag)) return ConditionalRequestOutcome.NotModified;
            }
            else if (TryParseHttpDate(ifModifiedSince, out DateTime modifiedSince))
            {
                if (lastModified <= modifiedSince) return ConditionalRequestOutcome.NotModified;
            }

            return ConditionalRequestOutcome.Proceed;
        }

        /// <summary>
        /// Parse an HTTP date (RFC 1123, RFC 850 or asctime) or an ISO 8601 timestamp as UTC.
        /// </summary>
        /// <param name="value">Header value.</param>
        /// <param name="result">Parsed UTC timestamp.</param>
        /// <returns>True if parsed.</returns>
        internal static bool TryParseHttpDate(string value, out DateTime result)
        {
            result = DateTime.MinValue;
            if (String.IsNullOrWhiteSpace(value)) return false;

            string[] formats = new string[]
            {
                "r",
                "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                "dddd, dd-MMM-yy HH:mm:ss 'GMT'",
                "ddd MMM d HH:mm:ss yyyy",
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                "yyyy-MM-dd'T'HH:mm:ss.fff'Z'"
            };

            DateTimeStyles styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces;
            if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, styles, out DateTime parsed)
                || DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, styles, out parsed))
            {
                result = TruncateToSeconds(parsed);
                return true;
            }

            return false;
        }

        #endregion

        #region Private-Methods

        private static DateTime TruncateToSeconds(DateTime value)
        {
            return new DateTime(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
        }

        #endregion
    }
}
