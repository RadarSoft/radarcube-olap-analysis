using System.IO;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Engine
{
    internal class LineData : IStreamedObject
    {
        internal object Value;
        internal string FormattedValue;
        internal CellColorSettings Colors;
        internal CellFontSettings Fonts;

        public LineData()
        {

        }

        internal LineData(object value, string formattedValue, CellColorSettings colors, CellFontSettings fonts)
        {
            Value = value;
            FormattedValue = formattedValue;
            Colors = colors;
            Fonts = fonts;
        }

        public void ReadStream(BinaryReader reader, object options)
        {
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgLineData_FormattedValue:
                        FormattedValue = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgLineData_Value:
                        Value = StreamUtils.ReadObject(reader);
                        break;
                    case Tags.tgLineData_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        public void WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgLineData_FormattedValue);
            StreamUtils.WriteString(writer, FormattedValue);

            StreamUtils.WriteTag(writer, Tags.tgLineData_Value);
            StreamUtils.WriteObject(writer, Value);

            StreamUtils.WriteTag(writer, Tags.tgLineData_EOT);
        }
    }
}