using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{  
    [XmlRoot(ElementName = "Grant")]
    public class Grant
    {
        [XmlElement(ElementName = "Grantee")]
        public Grantee Grantee { get; set; }
        [XmlElement(ElementName = "Permission")]
        public string Permission { get; set; }
    } 
}
