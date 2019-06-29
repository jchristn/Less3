using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{ 
    [XmlRoot(ElementName = "Error", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
    public class Error
    {
        [XmlElement(ElementName = "Key", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string Key { get; set; }
        [XmlElement(ElementName = "VersionId", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string VersionId { get; set; }
        [XmlElement(ElementName = "Code", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string Code { get; set; }
        [XmlElement(ElementName = "Message", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
        public string Message { get; set; }
    } 
}
