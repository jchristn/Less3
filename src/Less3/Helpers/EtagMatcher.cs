namespace Less3.Helpers
{
    using System;

    /// <summary>
    /// Evaluates HTTP entity-tag conditions (If-Match, If-None-Match) against an object's ETag.
    /// Thread-safe.
    /// </summary>
    internal static class EtagMatcher
    {
        #region Internal-Methods

        /// <summary>
        /// Determine whether an If-Match or If-None-Match header value matches an ETag.
        /// The header may be "*", a single entity tag, or a comma-separated list; quotes and weak
        /// validator prefixes (W/) are ignored.
        /// </summary>
        /// <param name="headerValue">Header value.</param>
        /// <param name="etag">Object ETag, with or without quotes.</param>
        /// <returns>True if any listed tag matches, or the header is "*".</returns>
        internal static bool Matches(string headerValue, string etag)
        {
            if (String.IsNullOrWhiteSpace(headerValue)) return false;

            string normalizedEtag = Normalize(etag);

            foreach (string candidate in headerValue.Split(','))
            {
                string trimmed = candidate.Trim();
                if (trimmed == "*") return true;
                if (normalizedEtag != null && String.Equals(Normalize(trimmed), normalizedEtag, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        /// <summary>
        /// Strip quotes and a weak-validator prefix from an entity tag.
        /// </summary>
        /// <param name="etag">Entity tag.</param>
        /// <returns>Normalized entity tag, or null.</returns>
        internal static string Normalize(string etag)
        {
            if (String.IsNullOrWhiteSpace(etag)) return null;
            string value = etag.Trim();
            if (value.StartsWith("W/", StringComparison.Ordinal)) value = value.Substring(2);
            return value.Trim('"');
        }

        #endregion
    }
}
