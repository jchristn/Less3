using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Less3.S3Responses
{
    [XmlRoot(ElementName = "Object")]
    public class Object
    {
        [XmlElement(ElementName = "Key")]
        public string Key { get; set; }
        [XmlElement(ElementName = "VersionId")]
        public string VersionId { get; set; }
    }

    [XmlRoot(ElementName = "Delete")]
    public class DeleteMultiple
    {
        [XmlElement(ElementName = "Quiet")]
        public bool Quiet { get; set; }
        [XmlElement(ElementName = "Object")]
        public List<Object> Object { get; set; }
    }
}
