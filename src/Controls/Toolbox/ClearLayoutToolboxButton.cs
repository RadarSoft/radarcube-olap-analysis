using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;
using System;
using System.ComponentModel;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the "Clear layout" toolbox button
    /// </summary>
    public class ClearLayoutToolboxButton : CommonToolboxButton
    {
        private static Guid fID = new Guid("1ACC371F-F07A-4244-A83F-E3109713955E");

        public override string ButtonID
        {
            get => fID.ToString();
            set
            {
                ;
            }
        }

        /// <exclude />
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public override bool IsPressed
        {
            get => false;
            set
            {
                ;
            }
        }

        public override string Text
        {
            get
            {
                if (base.Text.IsNullOrEmpty())
                    return RealTooltip();
                return base.Text;
            }
            set => base.Text = value;
        }

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(Tooltip))
                return RadarUtils.GetResStr("hint_ClearLayout");
            return Tooltip;
        }

        protected override string RealPressedImage()
        {
            if (string.IsNullOrEmpty(PressedImage))
                return RealImage();
            return PressedImage;
        }

        internal override void RenderContents(HtmlTextWriter writer)
        {
            string s = GeneratePushScript();
            if (!IsPressed && s != "")
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, s);

            writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "left");
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon-font ui-icon-font-erase");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.RenderEndTag(); // span

            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingLeft, "4px");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc-menu-item");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(Text);
            writer.RenderEndTag(); //span
            writer.RenderEndTag(); //div
            writer.RenderEndTag(); //li
        }

    }
}