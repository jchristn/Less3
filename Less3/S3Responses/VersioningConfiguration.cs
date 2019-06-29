using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{
    [XmlRoot(ElementName = "VersioningConfiguration")]
    public class VersioningConfiguration
    {
        [XmlElement(ElementName = "Status")]
        public string Status { get; set; }
        [XmlElement(ElementName = "MfaDelete")]
        public string MfaDelete { get; set; }

        public VersioningConfiguration()
        {

        }
    }
}
