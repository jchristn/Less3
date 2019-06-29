using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{   
    [XmlRoot(ElementName = "ListVersionsResult", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
    public class ListVersionsResult
    {
        [XmlElement(ElementName = "Name", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public string Name { get; set; }
        [XmlElement(ElementName = "Prefix", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public string Prefix { get; set; }
        [XmlElement(ElementName = "KeyMarker", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public string KeyMarker { get; set; }
        [XmlElement(ElementName = "VersionIdMarker", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public string VersionIdMarker { get; set; }
        [XmlElement(ElementName = "MaxKeys", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public long MaxKeys { get; set; }
        [XmlElement(ElementName = "IsTruncated", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public bool IsTruncated { get; set; }
        [XmlElement(ElementName = "Version", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public List<Version> Version { get; set; }
        [XmlElement(ElementName = "DeleteMarker", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public List<DeleteMarker> DeleteMarker { get; set; }
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }

        public ListVersionsResult()
        {
            DeleteMarker = new List<DeleteMarker>();
            Version = new List<Version>();
        }
    }
}
