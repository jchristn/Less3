namespace Less3.Database.PostgreSql
{
    using System;

    internal static class Sanitizer
    {
        internal static readonly string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static string SanitizeString(string input)
        {
            if (String.IsNullOrEmpty(input)) return "";
            string result = input.Replace("\0", "");
            result = result.Replace("'", "''");
            return result;
        }
    }
}
