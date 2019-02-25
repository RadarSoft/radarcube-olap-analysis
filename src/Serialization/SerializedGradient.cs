using System.ComponentModel;
using System.Xml.Serialization;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerializedGradient
    {
        [DefaultValue(null)]
        [XmlElement(ElementName = "Stop")]
        public SerializedGradientStop[] GradientStops { get; set; }

        [XmlAttribute]
        [DefaultValue(false)]
        public bool IsDiscrete { get; set; }
    }
}