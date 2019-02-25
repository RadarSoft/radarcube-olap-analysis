using System;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the abstract ancestor for all toolbox buttons
    /// </summary>
    public abstract class CommonToolboxButtonBase
    {
        internal OlapToolboxBase fOwner = null;

        /// <summary>
        ///     The unique indentifier for the given Toolbox item.
        /// </summary>
        /// <remarks>
        ///     Make sure that the value of this property is unique for each
        ///     button and remain unchanged from session to session.
        /// </remarks>
        public virtual string ButtonID { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);

        /// <summary>
        ///     Indicates whether a separator is coming after the button.
        /// </summary>
        public abstract bool NeedSeparator { get; set; }

        /// <summary>Inner text of a button in the normal state.</summary>
        public abstract string Text { get; set; }

        /// <summary>Inner text of a button in the pressed state.</summary>
        public abstract string PressedText { get; set; }

        /// <summary>Relative path to the image displayed on the button.</summary>
        public abstract string Image { get; set; }

        /// <summary>Relative path to the image displayed on the button when it is pressed.</summary>
        public abstract string PressedImage { get; set; }

        /// <summary>Indicates whether the button is pressed.</summary>
        public abstract bool IsPressed { get; set; }

        /// <summary>
        ///     The tooltop for this button
        /// </summary>
        public abstract string Tooltip { get; set; }

        /// <summary>Indicates whether the button is visible on the toolbox.</summary>
        public abstract bool Visible { get; set; }

        public string Class { get; set; } = "";

        /// <summary>
        ///     The client script code.
        /// </summary>
        /// <example>
        ///     <code lang="CS">
        /// 		<![CDATA[
        /// myButton.ClientScript = "alert('The button is pressed')";]]>
        /// 	</code>
        /// </example>
        public abstract string ClientScript { get; set; }

        internal string GetGridID()
        {
            return fOwner.OlapControl == null ? "" : fOwner.OlapControl.ClientID;
        }

        protected virtual string RealImage()
        {
            return Image;
        }

        protected virtual string RealPressedImage()
        {
            return PressedImage;
        }

        protected virtual string RealTooltip()
        {
            return Tooltip;
        }

        internal string GetRealTooltip()
        {
            return RealTooltip();
        }

        protected virtual string GetDefaultClientScript()
        {
            var script = "var grid = RadarSoft.$('#" + GetGridID() + "').data('grid');";
            script += " var args = '" + ButtonID + "|'";
            script += " + grid.parsChartTypes();";
            script += " grid.postback(args);";
            return script;
        }

        internal string GeneratePushScript()
        {
            return GetDefaultClientScript();
        }

        internal virtual void RenderContents(HtmlTextWriter writer)
        {
            var s = GeneratePushScript();

            if (s != "")
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, s);

            if (RealTooltip() != "")
                writer.AddAttribute(HtmlTextWriterAttribute.Title, RealTooltip());

            writer.AddAttribute(HtmlTextWriterAttribute.Style, "margin-right: 1px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Height, "16px");

            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                IsPressed ? "rs_toolbox_button ui-state-active" : "rs_toolbox_button");

            writer.RenderBeginTag(HtmlTextWriterTag.Button);

            if (!IsPressed)
            {
                if (Text.IsFill())
                {
                    //writer.AddStyleAttribute(HtmlTextWriterStyle.Height, "16px");
                    //writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "block");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.Write(Text);
                    writer.RenderEndTag(); // span
                }
                else if (RealImage().IsFill())
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Src, fOwner.MapPath(RealImage()));
                    writer.RenderBeginTag(HtmlTextWriterTag.Img);
                    writer.RenderEndTag(); // img
                }
                else
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, Class);
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderEndTag(); // span
                }
            }
            else
            {
                if (PressedText.IsFill())
                {
                    writer.AddStyleAttribute(HtmlTextWriterStyle.Height, "16px");
                    //writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "block");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.Write(PressedText);
                    writer.RenderEndTag(); // span
                }
                else if (RealPressedImage().IsFill())
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Src,
                        fOwner.MapPath(RealPressedImage()));
                    writer.RenderBeginTag(HtmlTextWriterTag.Img);
                    writer.RenderEndTag(); // img
                }
                else
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, Class);
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderEndTag(); // span
                }

            }

            writer.RenderEndTag(); // button
        }
    }
}