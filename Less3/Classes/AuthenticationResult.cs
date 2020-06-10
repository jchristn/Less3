using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Less3.Classes
{
    /// <summary>
    /// Authentication result.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AuthenticationResult
    {
        /// <summary>
        /// No authentication material was supplied.
        /// </summary>
        [EnumMember(Value = "NoMaterialSupplied")]
        NoMaterialSupplied,
        /// <summary>
        /// The user was not found.
        /// </summary>
        [EnumMember(Value = "UserNotFound")]
        UserNotFound,
        /// <summary>
        /// The supplied access key was not found.
        /// </summary>
        [EnumMember(Value = "AccessKeyNotFound")]
        AccessKeyNotFound,
        /// <summary>
        /// Authentication was successful.
        /// </summary>
        [EnumMember(Value = "Authenticated")]
        Authenticated,
        /// <summary>
        /// Authentication was not successful.
        /// </summary>
        [EnumMember(Value = "NotAuthenticated")]
        NotAuthenticated
    }
}
