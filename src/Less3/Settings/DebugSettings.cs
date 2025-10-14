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
    /// Debug settings.
    /// </summary>
    public class DebugSettings
    {
        /// <summary>
        /// Enable or disable debugging of authentication logic.
        /// </summary>
        public bool Authentication { get; set; } = false;

        /// <summary>
        /// Enable or disable debugging of S3 request parsing.
        /// </summary>
        public bool S3Requests { get; set; } = false;

        /// <summary>
        /// Enable or disable debugging of exceptions.
        /// </summary>
        public bool Exceptions { get; set; } = false;

        /// <summary>
        /// Debug settings.
        /// </summary>
        public DebugSettings()
        {

        }
    }
}
