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
        /// The credential is inactive.
        /// </summary>
        [EnumMember(Value = "CredentialInactive")]
        CredentialInactive,
        /// <summary>
        /// The user is inactive.
        /// </summary>
        [EnumMember(Value = "UserInactive")]
        UserInactive,
        /// <summary>
        /// The tenant is inactive.
        /// </summary>
        [EnumMember(Value = "TenantInactive")]
        TenantInactive,
        /// <summary>
        /// The credential and user do not belong to the same tenant.
        /// </summary>
        [EnumMember(Value = "TenantMismatch")]
        TenantMismatch,
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
