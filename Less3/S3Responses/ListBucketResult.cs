using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{ 
    [XmlRoot(ElementName = "ListBucketResult", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class ListBucketResult
    {
        [XmlElement(ElementName = "Name", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string Name { get; set; }
        [XmlElement(ElementName = "Prefix", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string Prefix { get; set; }
        [XmlElement(ElementName = "KeyCount", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public long KeyCount { get; set; }
        [XmlElement(ElementName = "MaxKeys", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public long MaxKeys { get; set; }
        [XmlElement(ElementName = "IsTruncated", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public bool IsTruncated { get; set; }
        [XmlElement(ElementName = "NextContinuationToken", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string NextContinuationToken { get; set; }
        [XmlElement(ElementName = "Contents", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public List<Contents> Contents { get; set; }
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }
    }
}
