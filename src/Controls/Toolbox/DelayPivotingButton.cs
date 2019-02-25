using System;
using System.ComponentModel;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the button that switches the Delay pivoting mode.
    /// </summary>
    public class DelayPivotingButton : CommonToolboxButton
    {
        private static Guid fID = new Guid("7D509AFE-2175-4AD2-8C28-A2B4E063AC98");

        /// <exclude />
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
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
            get => fOwner.OlapControl != null && fOwner.OlapControl.DelayPivoting;
            set
            {
                ;
            }
        }

        //protected override string RealImage()
        //{
        //    if (string.IsNullOrEmpty(Image))
        //        return fOwner.ImageUrl("unlocked.gif");
        //    return Image;
        //}

        //protected override string RealPressedImage()
        //{
        //    if (string.IsNullOrEmpty(PressedImage))
        //        return fOwner.ImageUrl("locked.gif");
        //    return PressedImage;
        //}

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(Tooltip))
                return " delay pivoting";
            return Tooltip;
        }

        internal override void RenderContents(HtmlTextWriter writer)
        {
            string s = GeneratePushScript();

            writer.AddAttribute("type", "checkbox");
            writer.AddAttribute("id", "rc_delay_pivoting_of_" + ButtonID);
            writer.AddAttribute("name", "rc_delay_pivoting_of_" + ButtonID);

            if (IsPressed)
                writer.AddAttribute("checked", "checked");

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_delay_pivoting");
            writer.RenderBeginTag("input");
            writer.RenderEndTag();//input

            writer.AddAttribute(HtmlTextWriterAttribute.Onclick, s);

            if (IsPressed)
                writer.AddAttribute(HtmlTextWriterAttribute.Title, "Switch off" + RealTooltip());
            else
                writer.AddAttribute(HtmlTextWriterAttribute.Title, "Switch on" + RealTooltip());


            writer.AddAttribute("for", "rc_delay_pivoting_of_" + ButtonID);
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
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon-font ui-icon-font-lock-open");
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
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon-font ui-icon-font-lock");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderEndTag(); // span
                }

            }

            writer.RenderEndTag();//label
        }
    }
}