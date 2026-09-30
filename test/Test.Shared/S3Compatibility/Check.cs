namespace Test.Shared.S3Compatibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Assertions for the S3 compatibility suites. Each throws with a descriptive message on failure.
    /// </summary>
    public static class Check
    {
        #region Public-Methods

        /// <summary>
        /// Assert a condition.
        /// </summary>
        public static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Check failed: " + message);
        }

        /// <summary>
        /// Assert a condition is false.
        /// </summary>
        public static void False(bool condition, string message)
        {
            if (condition) throw new InvalidOperationException("Check failed: " + message);
        }

        /// <summary>
        /// Assert equality.
        /// </summary>
        public static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Check failed: " + message + " (expected [" + expected + "], actual [" + actual + "])");
        }

        /// <summary>
        /// Assert two sequences are equal element by element.
        /// </summary>
        public static void SequenceEqual<T>(IList<T> expected, IList<T> actual, string message)
        {
            bool equal = expected.Count == actual.Count;
            for (int i = 0; equal && i < expected.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(expected[i], actual[i])) equal = false;
            }

            if (!equal)
                throw new InvalidOperationException("Check failed: " + message + " (expected [" + String.Join(", ", expected) + "], actual [" + String.Join(", ", actual) + "])");
        }

        /// <summary>
        /// Assert a string contains a substring (ordinal).
        /// </summary>
        public static void Contains(string? haystack, string needle, string message)
        {
            if (haystack == null || haystack.IndexOf(needle, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Check failed: " + message + " (missing [" + needle + "] in [" + Truncate(haystack) + "])");
        }

        /// <summary>
        /// Assert a string does not contain a substring (ordinal).
        /// </summary>
        public static void NotContains(string? haystack, string needle, string message)
        {
            if (haystack != null && haystack.IndexOf(needle, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("Check failed: " + message + " (unexpected [" + needle + "] in [" + Truncate(haystack) + "])");
        }

        #endregion

        #region Private-Methods

        private static string Truncate(string? value)
        {
            if (value == null) return "<null>";
            return value.Length > 600 ? value.Substring(0, 600) + "..." : value;
        }

        #endregion
    }
}
