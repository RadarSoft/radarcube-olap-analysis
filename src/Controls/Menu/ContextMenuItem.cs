using System.Collections.Generic;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Menu
{
    public class ContextMenuItem
    {
        public ContextMenuItem(string text)
            : this()
        {
            Text = text;
        }

        public ContextMenuItem()
        {
            IsSeparator = false;
            ChildItems = new List<ContextMenuItem>();
        }

        public ContextMenuItem(string text, string value, string imageUrl, string navigateUrl)
            : this()
        {
            Text = text;
            Value = value;
            ImageUrl = imageUrl;
            NavigateUrl = navigateUrl;
        }

        public string Text { get; set; }

        public string ImageUrl { get; set; }

        public string Target { get; set; }

        public bool Selectable { get; set; }

        public string NavigateUrl { get; set; }

        public bool IsSeparator { get; set; }

        public List<ContextMenuItem> ChildItems { get; }

        public string Value { get; set; }

        internal void RenderContents(HtmlTextWriter writer, string childmenu)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            if (ImageUrl.IsFill())
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon rs_menu_icon");
                writer.AddAttribute(HtmlTextWriterAttribute.Src, ImageUrl);
                writer.RenderBeginTag(HtmlTextWriterTag.Img);
                writer.RenderEndTag(); //img
            }
            if (!string.IsNullOrEmpty(NavigateUrl))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Href, NavigateUrl);
                writer.RenderBeginTag(HtmlTextWriterTag.A);
            }
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(Text);
            writer.RenderEndTag(); //span
            if (!string.IsNullOrEmpty(NavigateUrl))
                writer.RenderEndTag(); //a

            writer.RenderEndTag(); //div

            if (ChildItems.Count > 0)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_shadow");
                writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                foreach (var item in ChildItems)
                    item.RenderContents(writer, null);
                writer.RenderEndTag(); //ul
            }

            writer.RenderEndTag(); //li
            if (IsSeparator)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.RenderEndTag(); //li
            }
        }
    }
}