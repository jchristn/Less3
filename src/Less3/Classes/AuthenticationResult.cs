namespace Less3.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Runtime.Serialization;

    /// <summary>
    /// Authentication result.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
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
