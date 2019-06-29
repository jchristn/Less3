using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{  
    [XmlRoot(ElementName = "Buckets", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
    public class Buckets
    {
        [XmlElement(ElementName = "Bucket", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public List<Bucket> Bucket { get; set; }
    }

    [XmlRoot(ElementName = "ListAllMyBucketsResult", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
    public class ListAllMyBucketsResult
    {
        [XmlElement(ElementName = "Owner", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public Owner Owner { get; set; }
        [XmlElement(ElementName = "Buckets", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public Buckets Buckets { get; set; }
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }
    }
}
