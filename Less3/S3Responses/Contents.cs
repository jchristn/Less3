using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{
    [XmlRoot(ElementName = "Contents", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class Contents
    {
        [XmlElement(ElementName = "Key", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string Key { get; set; }
        [XmlElement(ElementName = "LastModified", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public DateTime LastModified { get; set; }
        [XmlElement(ElementName = "ETag", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string ETag { get; set; }
        [XmlElement(ElementName = "Size", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public long Size { get; set; }
        [XmlElement(ElementName = "StorageClass", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string StorageClass { get; set; }
    } 
}
