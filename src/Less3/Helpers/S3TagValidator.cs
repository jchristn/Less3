namespace Less3.Helpers
{
    using System;
    using System.Collections.Generic;

    using S3ServerLibrary.S3Objects;

    /// <summary>
    /// Shared S3 tag validation.
    /// </summary>
    internal static class S3TagValidator
    {
        internal static bool IsInvalid(Tagging tagging)
        {
            if (tagging == null) return false;
            if (tagging.Tags == null) return false;
            return IsInvalid(tagging.Tags.Tags);
        }

        internal static bool IsInvalid(IEnumerable<Tag> tags)
        {
            if (tags == null) return false;

            int count = 0;
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);

            foreach (Tag tag in tags)
            {
                count++;
                if (count > 50) return true;
                if (tag == null) return true;
                if (String.IsNullOrEmpty(tag.Key)) return true;
                if (tag.Key.Length > 128) return true;
                if (tag.Value != null && tag.Value.Length > 256) return true;
                if (tag.Key.StartsWith("aws:", StringComparison.OrdinalIgnoreCase)) return true;
                if (!keys.Add(tag.Key)) return true;
            }

            return false;
        }
    }
}
