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
    /// Authorization result.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AuthorizationResult
    { 
        /// <summary>
        /// Operation authorized due to admin privileges.
        /// </summary>
        [EnumMember(Value = "AdminAuthorized")]
        AdminAuthorized,
        /// <summary>
        /// Service operations are by default permitted for any authenticated user.
        /// </summary>
        [EnumMember(Value = "PermitService")]
        PermitService,
        /// <summary>
        /// Operation permitted due to bucket global configuration.
        /// </summary>
        [EnumMember(Value = "PermitBucketGlobalConfig")]
        PermitBucketGlobalConfig,
        /// <summary>
        /// Operation permitted due to bucket all users access control list.
        /// </summary>
        [EnumMember(Value = "PermitBucketAllUsersAcl")]
        PermitBucketAllUsersAcl,
        /// <summary>
        /// Operation permitted due to bucket authenticated users access control list.
        /// </summary>
        [EnumMember(Value = "PermitBucketAuthUserAcl")]
        PermitBucketAuthUserAcl,
        /// <summary>
        /// Operation permitted due to bucket user access control list.
        /// </summary>
        [EnumMember(Value = "PermitBucketUserAcl")]
        PermitBucketUserAcl,
        /// <summary>
        /// Operation permitted due to bucket ownership.
        /// </summary>
        [EnumMember(Value = "PermitBucketOwnership")]
        PermitBucketOwnership,
        /// <summary>
        /// Operation permitted due to object all users access control list.
        /// </summary>
        [EnumMember(Value = "PermitObjectAllUsersAcl")]
        PermitObjectAllUsersAcl,
        /// <summary>
        /// Operation permitted due to object authenticated users access control list.
        /// </summary>
        [EnumMember(Value = "PermitObjectAuthUserAcl")]
        PermitObjectAuthUserAcl,
        /// <summary>
        /// Operation permitted due to object user access control list.
        /// </summary>
        [EnumMember(Value = "PermitObjectUserAcl")]
        PermitObjectUserAcl,
        /// <summary>
        /// Operation permitted due to object ownership.
        /// </summary>
        [EnumMember(Value = "PermitObjectOwnership")]
        PermitObjectOwnership,
        /// <summary>
        /// Operation not authorized.
        /// </summary>
        [EnumMember(Value = "NotAuthorized")]
        NotAuthorized
    }
}
