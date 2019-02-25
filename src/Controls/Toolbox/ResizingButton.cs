using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;
using System;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the button that switches the possibility of resizing the cells.
    /// </summary>
    public class ResizingButton : CommonToolboxButton
    {
        private static Guid fID = new Guid("587D286D-96F5-47F6-80A8-FB502B462189");

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
            get => fOwner.OlapControl.AllowResizing;
            set
            {
                ;
            }
        }

        //protected override string RealImage()
        //{
        //    if (string.IsNullOrEmpty(Image))
        //        return fOwner.ImageUrl("resize.gif");
        //    return Image;
        //}

        //protected override string RealPressedImage()
        //{
        //    if (string.IsNullOrEmpty(PressedImage))
        //        return fOwner.ImageUrl("resizePressed.gif");
        //    return PressedImage;
        //}

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(Tooltip))
                return " cells resizing";
            return Tooltip;
        }

        internal override void RenderContents(HtmlTextWriter writer)
        {
            string s = GeneratePushScript();

            writer.AddAttribute("type", "checkbox");
            writer.AddAttribute("id", "resize_of_" + ButtonID);
            writer.AddAttribute("class", "rc_cell_resizing");
            writer.AddAttribute("name", "resize_of_" + ButtonID);

            if (IsPressed)
                writer.AddAttribute("checked", "checked");

            writer.RenderBeginTag("input");
            writer.RenderEndTag();//input

            writer.AddAttribute(HtmlTextWriterAttribute.Onclick, s);

            if (IsPressed)
                writer.AddAttribute(HtmlTextWriterAttribute.Title, "Desable" + RealTooltip());
            else
                writer.AddAttribute(HtmlTextWriterAttribute.Title, "Enable" + RealTooltip());


            writer.AddAttribute("for", "resize_of_" + ButtonID);
            //writer.AddStyleAttribute("font-size", "0px !important");
            writer.AddStyleAttribute("display", "inline-block");
            writer.RenderBeginTag("label");

            if (!IsPressed)
            {
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
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon-font ui-icon-font-arrow-4-diag");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderEndTag(); // span
                }
            }
            else
            {

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
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon-font ui-icon-font-arrow-4-diag");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderEndTag(); // span
                }

            }

            writer.RenderEndTag();//label
        }

    }
}