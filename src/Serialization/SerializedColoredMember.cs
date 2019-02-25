using System.ComponentModel;
using System.Xml.Serialization;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerializedColoredMember
    {
        [XmlAttribute]
        [DefaultValue(null)]
        public string Key { get; set; }

        [XmlAttribute]
        [DefaultValue(null)]
        public string Value { get; set; }
    }
}