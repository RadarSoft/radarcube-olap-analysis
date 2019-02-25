using System.IO;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    public class CommonToolboxButton : CommonToolboxButtonBase, IStreamedObject
    {
        public CommonToolboxButton()
        {
            Visible = true;
        }

        public override bool NeedSeparator { get; set; }

        public override string Text { get; set; }

        public override string PressedText { get; set; }

        public override string Image { get; set; }

        public override string PressedImage { get; set; }

        public override bool IsPressed { get; set; }

        public override string Tooltip { get; set; }

        public override bool Visible { get; set; }

        public override string ClientScript { get; set; }

        public void WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgToolboxButton);

            StreamUtils.WriteTag(writer, Tags.tgToolboxButton_Text);
            StreamUtils.WriteString(writer, Text);

            StreamUtils.WriteTag(writer, Tags.tgToolboxButton_ButtonID);
            StreamUtils.WriteString(writer, ButtonID);

            StreamUtils.WriteTag(writer, Tags.tgToolboxButton_PressedText);
            StreamUtils.WriteString(writer, PressedText);

            StreamUtils.WriteTag(writer, Tags.tgToolboxButton_Image);
            StreamUtils.WriteString(writer, Image);

            StreamUtils.WriteTag(writer, Tags.tgToolboxButton_PressedImage);
            StreamUtils.WriteString(writer, PressedImage);

            StreamUtils.WriteTag(writer, Tags.tgToolboxButton_Tooltip);
            StreamUtils.WriteString(writer, Tooltip);

            StreamUtils.WriteTag(writer, Tags.tgToolboxButton_ClientScript);
            StreamUtils.WriteString(writer, ClientScript);

            if (IsPressed)
                StreamUtils.WriteTag(writer, Tags.tgToolboxButton_IsPressed);

            if (!Visible)
                StreamUtils.WriteTag(writer, Tags.tgToolboxButton_Visible);

            if (NeedSeparator)
                StreamUtils.WriteTag(writer, Tags.tgToolboxButton_NeedSeparator);

            StreamUtils.WriteTag(writer, Tags.tgToolboxButton_EOT);
        }

        public void ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgToolboxButton);
            IsPressed = false;
            Visible = true;
            NeedSeparator = false;
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgToolboxButton_Text:
                        Text = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgToolboxButton_ButtonID:
                        ButtonID = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgToolboxButton_PressedImage:
                        PressedImage = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgToolboxButton_PressedText:
                        PressedText = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgToolboxButton_Tooltip:
                        Tooltip = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgToolboxButton_Image:
                        Image = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgToolboxButton_IsPressed:
                        IsPressed = true;
                        break;
                    case Tags.tgToolboxButton_Visible:
                        Visible = false;
                        break;
                    case Tags.tgToolboxButton_NeedSeparator:
                        NeedSeparator = true;
                        break;
                    case Tags.tgToolboxButton_ClientScript:
                        ClientScript = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgToolboxButton_EOT:
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