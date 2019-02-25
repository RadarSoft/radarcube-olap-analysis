using System.ComponentModel;
using System.Xml.Serialization;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerializedGradientStop
    {
        [XmlAttribute]
        [DefaultValue(0.0)]
        public double Offset { get; set; }

        [XmlAttribute]
        [DefaultValue(null)]
        public string Color { get; set; }
    }
}