using System;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the "Show only Data area" toolbox button
    /// </summary>
    public class DataAreaToolboxButton : CommonToolboxButton
    {
        private static Guid fID = new Guid("F473FABF-8F15-4ec9-B0D8-7FF0D74CB94C");

        public override string ButtonID
        {
            get => fID.ToString();
            set
            {
                ;
            }
        }

        public override bool IsPressed
        {
            get => fOwner.OlapControl == null
                ? false
                : fOwner.OlapControl.ShowAreasMode == rsShowAreasOlapGrid.rsDataOnly;
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

        protected override string RealPressedImage()
        {
            if (string.IsNullOrEmpty(PressedImage))
                return RealImage();
            return PressedImage;
        }

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(Tooltip))
                return RadarUtils.GetResStr("rsShowDataOnly");
            return Tooltip;
        }

        internal override void RenderContents(HtmlTextWriter writer)
        {
            string s = GeneratePushScript();
            if (!IsPressed && s != "")
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, s);
            else
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-state-disabled");

            writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "left");
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon-font ui-icon-font-blank");
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