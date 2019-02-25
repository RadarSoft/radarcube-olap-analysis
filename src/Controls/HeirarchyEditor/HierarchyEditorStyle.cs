using System.IO;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Controls.HeirarchyEditor
{
    /// <summary>
    ///     Defines Hierarchy Editor settings, such as height, width and the number of items
    ///     per page
    /// </summary>
    public class HierarchyEditorStyle : IStreamedObject
    {
        /// <summary>Defines the number of items per page.</summary>
        public int ItemsInPage { get; set; } = 10;

        /// <summary>
        ///     Defines the width of Hierarchy Editor Tree (in pixels).
        /// </summary>
        public int Width { get; set; } = 346;

        /// <summary>
        ///     Defines the height of Hierarchy Editor Tree (in pixels).
        /// </summary>
        public int TreeHeight { get; set; } = 225;

        /// <summary>
        ///     Defines if the Hierarchy Editor window is resizable.
        /// </summary>
        public bool Resizable { get; set; } = true;


        public void WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgHierarchyEditorStyle);
            if (!Resizable)
                StreamUtils.WriteTag(writer, Tags.tgHierarchyEditorStyle_Resizable);

            StreamUtils.WriteTag(writer, Tags.tgHierarchyEditorStyle_ItemsInPage);
            StreamUtils.WriteInt32(writer, ItemsInPage);

            StreamUtils.WriteTag(writer, Tags.tgHierarchyEditorStyle_TreeHeight);
            StreamUtils.WriteInt32(writer, TreeHeight);

            StreamUtils.WriteTag(writer, Tags.tgHierarchyEditorStyle_Width);
            StreamUtils.WriteInt32(writer, Width);

            StreamUtils.WriteTag(writer, Tags.tgHierarchyEditorStyle_EOT);
        }

        public void ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgHierarchyEditorStyle);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgHierarchyEditorStyle_Resizable:
                        Resizable = false;
                        break;
                    case Tags.tgHierarchyEditorStyle_ItemsInPage:
                        ItemsInPage = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgHierarchyEditorStyle_TreeHeight:
                        TreeHeight = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgHierarchyEditorStyle_Width:
                        Width = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgHierarchyEditorStyle_EOT:
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