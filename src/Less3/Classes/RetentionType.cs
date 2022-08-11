using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace Less3.Classes
{
    /// <summary>
    /// Type of object retention.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RetentionType
    {
        /// <summary>
        /// No retention specified.
        /// </summary>
        [EnumMember(Value = "NONE")]
        NONE,
        /// <summary>
        /// Governance retention.
        /// </summary>
        [EnumMember(Value = "GOVERNANCE")]
        GOVERNANCE,
        /// <summary>
        /// Compliance retention.
        /// </summary>
        [EnumMember(Value = "COMPLIANCE")]
        COMPLIANCE 
    } 
}
