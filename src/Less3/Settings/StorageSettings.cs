namespace Less3.Settings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using SyslogLogging;
    using DatabaseWrapper.Core;
    using Less3.Storage;
    using S3ServerLibrary;
    using Watson.ORM.Core;
    using WatsonWebserver.Core;
    using Less3;

    /// <summary>
    /// Storage settings.
    /// </summary>
    public class StorageSettings
    {
        /// <summary>
        /// Temporary storage directory.
        /// </summary>
        public string TempDirectory { get; set; } = "./temp/";

        /// <summary>
        /// Type of storage driver.
        /// </summary>
        public StorageDriverType StorageType { get; set; } = StorageDriverType.Disk;

        /// <summary>
        /// Storage directory for 'Disk' StorageType.
        /// </summary>
        public string DiskDirectory { get; set; } = "./disk/";

        /// <summary>
        /// Storage settings.
        /// </summary>
        public StorageSettings()
        {

        }
    }
}
