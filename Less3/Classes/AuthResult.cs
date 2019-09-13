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
    internal enum AuthResult
    {
        [EnumMember(Value = "AdminAuthorized")]
        AdminAuthorized,
        [EnumMember(Value = "Authenticated")]
        Authenticated,
        [EnumMember(Value = "AuthenticationRequired")]
        AuthenticationRequired,
        [EnumMember(Value = "PermitBucketGlobalConfig")]
        PermitBucketGlobalConfig,
        [EnumMember(Value = "PermitBucketAllUsersAcl")]
        PermitBucketAllUsersAcl,
        [EnumMember(Value = "PermitBucketAuthUserAcl")]
        PermitBucketAuthUserAcl,
        [EnumMember(Value = "PermitBucketUserAcl")]
        PermitBucketUserAcl,
        [EnumMember(Value = "PermitBucketOwnership")]
        PermitBucketOwnership,
        [EnumMember(Value = "PermitObjectAllUsersAcl")]
        PermitObjectAllUsersAcl,
        [EnumMember(Value = "PermitObjectAuthUserAcl")]
        PermitObjectAuthUserAcl,
        [EnumMember(Value = "PermitObjectUserAcl")]
        PermitObjectUserAcl,
        [EnumMember(Value = "PermitObjectOwnership")]
        PermitObjectOwnership,
        [EnumMember(Value = "Denied")]
        Denied
    }
}
