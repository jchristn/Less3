namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Shared bucket-name validation for all bucket creation paths.
    /// </summary>
    internal static class BucketNameValidator
    {
        private static readonly HashSet<string> ReservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "admin",
            "api",
            "favicon.ico",
            "openapi.json",
            "robots.txt",
            "swagger"
        };

        /// <summary>
        /// Check if a bucket name is invalid for Less3.
        /// </summary>
        /// <param name="name">Bucket name.</param>
        /// <returns>True if invalid.</returns>
        internal static bool IsInvalid(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return true;
            if (name.Length < 3 || name.Length > 63) return true;
            if (ReservedNames.Contains(name)) return true;
            if (!IsLowercaseLetterOrNumber(name[0])) return true;
            if (!IsLowercaseLetterOrNumber(name[name.Length - 1])) return true;

            bool hasDot = false;
            for (int i = 0; i < name.Length; i++)
            {
                char current = name[i];
                bool valid = IsLowercaseLetterOrNumber(current) || current == '-' || current == '.';
                if (!valid) return true;
                if (current == '.') hasDot = true;

                if (i > 0)
                {
                    char previous = name[i - 1];
                    if (previous == '.' && current == '.') return true;
                    if (previous == '-' && current == '.') return true;
                    if (previous == '.' && current == '-') return true;
                }
            }

            if (hasDot && LooksLikeIpv4Address(name)) return true;
            return false;
        }

        private static bool IsLowercaseLetterOrNumber(char value)
        {
            return (value >= 'a' && value <= 'z') || (value >= '0' && value <= '9');
        }

        private static bool LooksLikeIpv4Address(string value)
        {
            return Regex.IsMatch(value, @"^\d{1,3}(\.\d{1,3}){3}$");
        }
    }
}
