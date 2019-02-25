using System.Collections.Generic;
using RadarSoft.RadarCube.Html;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace RadarSoft.RadarCube.Controls.Tree
{
    public class jQueryTree : WebControl
    {
        public jQueryTree(HttpContext context, IHostingEnvironment hosting)
            :base(context, hosting)
        {
            Nodes = new List<jQueryTreeNode>();
        }

        public List<jQueryTreeNode> Nodes { get; }

        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            writer.AddAttribute("class", "rs_treeview");
            writer.RenderBeginTag(HtmlTextWriterTag.Ul);
        }

        protected override void RenderContents(HtmlTextWriter writer)
        {
            foreach (var n in Nodes)
                n.Render(writer);
        }

        public override void RenderEndTag(HtmlTextWriter writer)
        {
            writer.RenderEndTag(); //ul
        }

        protected override void Render(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_tdtree");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            base.Render(writer);
            writer.RenderEndTag(); // div tree
        }
    }
}