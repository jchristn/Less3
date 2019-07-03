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
    /// Type of API request.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RequestType
    {
        [EnumMember(Value = "Admin")]
        Admin,
        [EnumMember(Value = "ServiceListBuckets")]
        ServiceListBuckets,
        [EnumMember(Value = "BucketDelete")]
        BucketDelete,
        [EnumMember(Value = "BucketDeleteTags")]
        BucketDeleteTags,
        [EnumMember(Value = "BucketExists")]
        BucketExists,
        [EnumMember(Value = "BucketRead")]
        BucketRead,
        [EnumMember(Value = "BucketReadTags")]
        BucketReadTags,
        [EnumMember(Value = "BucketReadVersioning")]
        BucketReadVersioning,
        [EnumMember(Value = "BucketWrite")]
        BucketWrite,
        [EnumMember(Value = "BucketWriteTags")]
        BucketWriteTags,
        [EnumMember(Value = "BucketWriteVersioning")]
        BucketWriteVersioning,
        [EnumMember(Value = "ObjectDelete")]
        ObjectDelete,
        [EnumMember(Value = "ObjectDeleteMultiple")]
        ObjectDeleteMultiple,
        [EnumMember(Value = "ObjectDeleteTags")]
        ObjectDeleteTags,
        [EnumMember(Value = "ObjectExists")]
        ObjectExists,
        [EnumMember(Value = "ObjectRead")]
        ObjectRead,
        [EnumMember(Value = "ObjectReadAcl")]
        ObjectReadAcl,
        [EnumMember(Value = "ObjectReadLegalHold")]
        ObjectReadLegalHold,
        [EnumMember(Value = "ObjectReadRange")]
        ObjectReadRange,
        [EnumMember(Value = "ObjectReadRetention")]
        ObjectReadRetention,
        [EnumMember(Value = "ObjectReadTags")]
        ObjectReadTags,
        [EnumMember(Value = "ObjectWrite")]
        ObjectWrite,
        [EnumMember(Value = "ObjectWriteAcl")]
        ObjectWriteAcl,
        [EnumMember(Value = "ObjectWriteLegalHold")]
        ObjectWriteLegalHold,
        [EnumMember(Value = "ObjectWriteRetention")]
        ObjectWriteRetention,
        [EnumMember(Value = "ObjectWriteTags")]
        ObjectWriteTags
    } 
}
