using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{  
    [XmlRoot(ElementName = "DeleteMarker", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
    public class DeleteMarker
    {
        [XmlElement(ElementName = "Key", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public string Key { get; set; }
        [XmlElement(ElementName = "VersionId", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public long VersionId { get; set; }
        [XmlElement(ElementName = "IsLatest", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public bool IsLatest { get; set; }
        [XmlElement(ElementName = "LastModified", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public DateTime LastModified { get; set; }
        [XmlElement(ElementName = "Owner", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public Owner Owner { get; set; }
    }
}
