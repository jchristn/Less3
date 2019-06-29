using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{
    [XmlRoot(ElementName = "AccessControlPolicy")]
    public class AccessControlPolicy
    {
        [XmlElement(ElementName = "Owner")]
        public Owner Owner { get; set; }
        [XmlElement(ElementName = "AccessControlList")]
        public AccessControlList AccessControlList { get; set; }
    }
}
