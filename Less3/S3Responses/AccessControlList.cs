using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{   
    [XmlRoot(ElementName = "AccessControlList")]
    public class AccessControlList
    {
        [XmlElement(ElementName = "Grant")]
        public List<Grant> Grant { get; set; }
    } 
}
