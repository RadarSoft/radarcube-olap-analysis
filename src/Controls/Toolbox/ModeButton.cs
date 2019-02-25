using System;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the button that switches to the Grid mode.
    /// </summary>
    public class ModeButton : CommonToolboxButton
    {
        private static Guid fID = new Guid("1E053B03-55C9-410F-8A69-D4E22CE13447");

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
            get => fOwner.OlapControl.AnalysisType == AnalysisType.Grid;
            set
            {
                ;
            }
        }

        //protected override string RealImage()
        //{
        //    if (string.IsNullOrEmpty(Image))
        //        return fOwner.ImageUrl("table.gif");
        //    return Image;
        //}

        //protected override string RealPressedImage()
        //{
        //    if (string.IsNullOrEmpty(PressedImage))
        //        return fOwner.ImageUrl("bars.gif");
        //    return PressedImage;
        //}

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(Tooltip))
                return "Switch on ";
            return Tooltip;
        }

        internal override void RenderContents(HtmlTextWriter writer)
        {
            writer.AddStyleAttribute("display", "inline-block");
            writer.AddStyleAttribute(HtmlTextWriterStyle.WhiteSpace, "nowrap");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "rs_toolbox_buttonset_mode_" + fOwner.OlapControl.ID);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_toolbox_buttonset");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            var s = GeneratePushScript();

            writer.AddAttribute("type", "radio");
            writer.AddAttribute("id", "grid_mode_" + ButtonID);
            writer.AddAttribute("name", "grid_mode_" + ButtonID);

            if (IsPressed)
                writer.AddAttribute("checked", "checked");

            writer.RenderBeginTag("input");
            writer.RenderEndTag(); //input

            writer.AddAttribute(HtmlTextWriterAttribute.Title, RealTooltip() + "Grid mode");

            if (!IsPressed && s != "")
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, s);

            writer.AddAttribute("for", "grid_mode_" + ButtonID);
            //writer.AddStyleAttribute("font-size", "0px !important");
            writer.AddStyleAttribute("display", "inline-block");
            writer.RenderBeginTag("label");

            if (Text.IsFill())
            {
                writer.Write(Text);
            }
            else if (RealImage().IsFill())
            {
                writer.AddAttribute("src", RealImage());
                writer.RenderBeginTag("img");
                writer.RenderEndTag(); //img                
            }
            else
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon-font ui-icon-font-table");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.RenderEndTag(); // span
            }

            writer.RenderEndTag(); //label

            writer.AddAttribute("type", "radio");
            writer.AddAttribute("id", "chart_mode_" + ButtonID);
            writer.AddAttribute("name", "chart_mode_" + ButtonID);

            if (!IsPressed)
                writer.AddAttribute("checked", "checked");

            writer.RenderBeginTag("input");
            writer.RenderEndTag(); //input  

            writer.AddAttribute(HtmlTextWriterAttribute.Title, RealTooltip() + "Chart mode");

            if (IsPressed && s != "")
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, s);

            writer.AddAttribute("for", "chart_mode_" + ButtonID);
            //writer.AddStyleAttribute("font-size", "0px !important");
            writer.AddStyleAttribute("display", "inline-block");
            writer.RenderBeginTag("label");

            if (PressedText.IsFill())
            {
                writer.Write(PressedText);
            }
            else if (RealPressedImage().IsFill())
            {
                writer.AddAttribute("src", RealPressedImage());
                writer.RenderBeginTag("img");
                writer.RenderEndTag(); //img                
            }
            else
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon-font ui-icon-font-chart-bars");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.RenderEndTag(); // span
            }

            writer.RenderEndTag(); //label

            writer.RenderEndTag(); //div 
        }
    }
}