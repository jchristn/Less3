namespace Less3.Classes
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Identifies which version of an object key a request refers to: the latest version (no version ID
    /// supplied), the null version (version ID "null"), or a specific numbered version.
    /// Immutable and thread-safe.
    /// </summary>
    public class ObjectVersionReference
    {
        #region Public-Members

        /// <summary>
        /// Version ID string Amazon S3 uses for the null version, i.e. the version written while
        /// versioning was not enabled.
        /// </summary>
        public const string NullVersionId = "null";

        /// <summary>
        /// True when no version ID was supplied and the request refers to the latest version.
        /// </summary>
        public bool IsLatest { get; }

        /// <summary>
        /// True when the version ID "null" was supplied.
        /// </summary>
        public bool IsNullVersion { get; }

        /// <summary>
        /// Numbered version. Only meaningful when IsLatest and IsNullVersion are both false. Minimum value is 1.
        /// </summary>
        public long Version { get; }

        /// <summary>
        /// Reference to the latest version.
        /// </summary>
        public static ObjectVersionReference Latest { get; } = new ObjectVersionReference(true, false, 0);

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        private ObjectVersionReference(bool isLatest, bool isNullVersion, long version)
        {
            IsLatest = isLatest;
            IsNullVersion = isNullVersion;
            Version = version;
        }

        /// <summary>
        /// Create a reference to a specific numbered version.
        /// </summary>
        /// <param name="version">Version number. Minimum value is 1.</param>
        /// <returns>Version reference.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when version is less than 1.</exception>
        public static ObjectVersionReference ForVersion(long version)
        {
            if (version < 1) throw new ArgumentOutOfRangeException(nameof(version), "Version must be one or greater.");
            return new ObjectVersionReference(false, false, version);
        }

        /// <summary>
        /// Parse a version ID supplied by a client.
        /// Null or empty refers to the latest version, "null" refers to the null version, and a positive
        /// integer refers to that numbered version. Any other value is invalid.
        /// </summary>
        /// <param name="versionId">Version ID as supplied by the client; may be null.</param>
        /// <param name="reference">Parsed reference, or null when the value is invalid.</param>
        /// <returns>True if the value is valid.</returns>
        public static bool TryParse(string versionId, out ObjectVersionReference reference)
        {
            reference = null;

            if (String.IsNullOrEmpty(versionId))
            {
                reference = Latest;
                return true;
            }

            if (String.Equals(versionId, NullVersionId, StringComparison.Ordinal))
            {
                reference = new ObjectVersionReference(false, true, 0);
                return true;
            }

            if (Int64.TryParse(versionId, NumberStyles.None, CultureInfo.InvariantCulture, out long version) && version >= 1)
            {
                reference = new ObjectVersionReference(false, false, version);
                return true;
            }

            return false;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Version ID string for this reference: null for the latest version, "null" for the null
        /// version, otherwise the version number.
        /// </summary>
        /// <returns>Version ID string, or null.</returns>
        public override string ToString()
        {
            if (IsLatest) return null;
            if (IsNullVersion) return NullVersionId;
            return Version.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
