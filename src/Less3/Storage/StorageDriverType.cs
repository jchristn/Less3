namespace Less3.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

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
