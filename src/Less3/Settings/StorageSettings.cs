namespace Less3.Settings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using SyslogLogging;
    using Less3.Storage;
    using S3ServerLibrary;
    using WatsonWebserver.Core;

    /// <summary>
    /// Storage settings.
    /// </summary>
    public class StorageSettings
    {
        /// <summary>
        /// Temporary storage directory used for single-object write staging.
        /// In a multi-node cluster this must point at storage mounted identically on every node
        /// so a write that is retried on another node can be reasoned about consistently.
        /// </summary>
        public string TempDirectory { get; set; } = "./temp/";

        /// <summary>
        /// Directory used to stage multipart upload parts before assembly. Null or empty means
        /// a "parts" subdirectory of <see cref="TempDirectory"/> is used. In a multi-node cluster
        /// this must be shared storage so any node can complete or abort any upload.
        /// </summary>
        public string PartsDirectory { get; set; } = null;

        /// <summary>
        /// Type of storage driver.
        /// </summary>
        public StorageDriverType StorageType { get; set; } = StorageDriverType.Disk;

        /// <summary>
        /// Storage directory for 'Disk' StorageType. In a multi-node cluster this must be shared
        /// storage mounted identically on every node so any node can read any object's blob.
        /// </summary>
        public string DiskDirectory { get; set; } = "./disk/";

        /// <summary>
        /// Storage settings.
        /// </summary>
        public StorageSettings()
        {

        }

        /// <summary>
        /// Resolve the effective directory used to stage multipart upload parts. Returns
        /// <see cref="PartsDirectory"/> when set, otherwise a "parts" subdirectory of
        /// <see cref="TempDirectory"/>. The returned path always ends with a trailing slash.
        /// </summary>
        /// <returns>Effective parts directory path.</returns>
        public string GetEffectivePartsDirectory()
        {
            if (!String.IsNullOrEmpty(PartsDirectory))
            {
                return PartsDirectory.EndsWith("/") || PartsDirectory.EndsWith("\\") ? PartsDirectory : PartsDirectory + "/";
            }

            string temp = String.IsNullOrEmpty(TempDirectory) ? "./temp/" : TempDirectory;
            if (!temp.EndsWith("/") && !temp.EndsWith("\\")) temp += "/";
            return temp + "parts/";
        }
    }
}
