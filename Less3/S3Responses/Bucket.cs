using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{ 
    [XmlRoot(ElementName = "Bucket", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
    public class Bucket
    {
        [XmlElement(ElementName = "Name", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public string Name { get; set; }
        [XmlElement(ElementName = "CreationDate", Namespace = "http://s3.amazonaws.com/doc/2006-03-01")]
        public DateTime CreationDate { get; set; }
    } 
}
