using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>Represents a collection of the custom toolbox buttons.</summary>
    /// <moduleiscollection />
    public class CustomToolboxButtonCollection : List<CustomToolboxButton>, IStreamedObject
    {
        public void WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteList(writer, this);
        }

        public void ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgList);
            Clear();
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgList_Count:
                        var c = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgList_Item:
                        var b = new CustomToolboxButton();
                        // BeforeRead
                        StreamUtils.ReadStreamedObject(reader, b);
                        // AfterRead
                        Add(b);
                        break;
                    case Tags.tgList_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }
    }
}