using System;
using System.Collections.Generic;
using RadarSoft.RadarCube.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace RadarSoft.RadarCube.Controls.Menu
{
    public class ContextMenu : WebControl
    {
        public ContextMenu(HttpContext context, IHostingEnvironment hosting):
            base(context, hosting)
        {
            Items = new List<ContextMenuItem>();
            Embedded = false;
        }

        public List<ContextMenuItem> Items { get; protected set; }

        public bool Embedded { get; set; }

        protected override void Render(HtmlTextWriter writer)
        {
            var childmenus = new Dictionary<ContextMenuItem, string>();
            if (Embedded)
                writer.AddAttribute("embedded", "true");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_menubox");
            RenderBeginTag(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_menu");
            writer.RenderBeginTag(HtmlTextWriterTag.Ul);
            foreach (var item in Items)
            {
                string s = null;
                if (item.ChildItems.Count > 0)
                {
                    s = "mnu" + Guid.NewGuid().ToString().Replace("-", "");
                    childmenus.Add(item, s);
                }
                item.RenderContents(writer, s);
            }
            writer.RenderEndTag(); //ul
            RenderEndTag(writer); //div
        }
    }
}