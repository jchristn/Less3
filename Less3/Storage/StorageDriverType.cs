using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace Less3.Storage
{
    /// <summary>
    /// Type of storage driver.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StorageDriverType
    {
        /// <summary>
        /// Disk.
        /// </summary>
        [EnumMember(Value = "Disk")]
        Disk
    } 
}
