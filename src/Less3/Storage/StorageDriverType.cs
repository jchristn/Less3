using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Less3.Storage
{
    /// <summary>
    /// Type of storage driver.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StorageDriverType
    {
        /// <summary>
        /// Disk.
        /// </summary>
        [EnumMember(Value = "Disk")]
        Disk
    } 
}
