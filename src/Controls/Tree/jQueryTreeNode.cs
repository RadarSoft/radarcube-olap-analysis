using System.Collections.Generic;
using RadarSoft.RadarCube.Html;

namespace RadarSoft.RadarCube.Controls.Tree
{
    public class jQueryTreeNode
    {
        public jQueryTreeNode(string text)
            : this(text, string.Empty, string.Empty)
        {
        }

        public jQueryTreeNode(string text, string value)
            : this(text, value, string.Empty)
        {
        }

        public jQueryTreeNode(string text, string value, string imageUrl)
        {
            Text = text;
            ChildNodes = new List<jQueryTreeNode>();
            Value = value;
            ImageUrl = imageUrl;
            CssClasses = new List<string>();
        }

        public List<string> CssClasses { get; }

        public string Text { get; set; }

        public bool? Expanded { get; set; }

        public bool Draggable { get; set; }

        public string ImageUrl { get; set; }

        public bool ShowCheckBox { get; set; }

        public bool? Checked { get; set; }

        public List<jQueryTreeNode> ChildNodes { get; }

        public string ToolTip { get; set; }

        public string Value { get; set; }

        internal void Render(HtmlTextWriter writer)
        {
            var l = new List<string>(CssClasses);
            if (Expanded != true && ChildNodes.Count > 0)
                l.Add("closed");
            if (l.Count > 0)
                writer.AddAttribute(HtmlTextWriterAttribute.Class, string.Join(" ", l.ToArray()));
            if (!string.IsNullOrEmpty(ToolTip))
                writer.AddAttribute(HtmlTextWriterAttribute.Title, ToolTip);
            if (!string.IsNullOrEmpty(Value))
                writer.AddAttribute("uid", Value);
            if (Draggable)
            {
                writer.AddAttribute("drag", "true");
                writer.AddAttribute("unselectable", "on");
            }
            if (ShowCheckBox)
                writer.AddAttribute("checked", Checked.ToString());
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            if (!string.IsNullOrEmpty(ImageUrl))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Src, ImageUrl);
                writer.RenderBeginTag(HtmlTextWriterTag.Img);
                writer.RenderEndTag(); //img
            }
            //if (Draggable)
            //{
            //    writer.AddAttribute(HtmlTextWriterAttribute.Href, "#");
            //    writer.RenderBeginTag(HtmlTextWriterTag.A);
            //}

            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(Text);
            writer.RenderEndTag(); //span
            //if (Draggable)
            //    writer.RenderEndTag(); //a
            if (ChildNodes.Count > 0)
            {
                writer.AddStyleAttribute("list-style-type", "none");
                writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                foreach (var n in ChildNodes)
                    n.Render(writer);
                writer.RenderEndTag(); //ul
            }
            writer.RenderEndTag(); //li


            //HtmlInputCheckBox cb = new HtmlInputCheckBox();
            //cb.Checked = m.Visible;

            //StringWriter cw = new StringWriter();
            //HtmlTextWriter writer = new HtmlTextWriter(cw);
            //cb.RenderControl(writer);
            //n.Text = cw.ToString() + n.Text;
        }
    }
}