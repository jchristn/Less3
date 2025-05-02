namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Type of object retention.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
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
