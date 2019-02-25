using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.State
{
    /// <summary>Saves/restores the state of the Grid.</summary>
    public class OlapGridSerializer : OlapAxisLayoutSerializer, IXmlSerializable
    {
        internal OlapGridSerializer()
        {
        }

        internal OlapGridSerializer(OlapControl AGrid)
            : base(AGrid, true)
        {
        }

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            reader.ReadStartElement("OlapGridSerializer");

            XmlSerializator.DeSerialize(reader, this);
            if (!reader.EOF)
                reader.ReadEndElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            XmlSerializator.ExcludeTypes.Clear();
            XmlSerializator.Serialize(writer, this);
        }

        internal override void LoadFrom(OlapControl grid)
        {
            base.LoadFrom(grid);
            ChartsType = grid.ChartsType;
        }

        internal override void LoadTo(OlapControl grid)
        {
            base.LoadTo(grid);
            grid.ChartsType = ChartsType;
        }

        internal static OlapGridSerializer GetForAppearance(OlapControl AGrid)
        {
            return new OlapGridSerializer(AGrid);
        }

        internal new void WriteXML(string p)
        {
            base.WriteXML(p);
        }
    }
}