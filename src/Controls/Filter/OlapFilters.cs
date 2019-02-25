using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;
using Microsoft.AspNetCore.Hosting;

namespace RadarSoft.RadarCube.Controls.Filter
{
    public class OlapFilters : WebControl
    {
        internal OlapFilters(HttpContext context, IHostingEnvironment hosting): 
            base(context, hosting)
        {

        }
        private readonly int _FilterDescriptionLength = 100;

        internal OlapControl Grid { get; set; }

        /// <summary>
        ///     The maximal length of a string displayed as a filter
        ///     description. If 0, the string is displayed without cutting.
        /// </summary>
        /// <remarks>
        ///     If the length of a string exceeds the value defined by this property, the string is
        ///     cut.
        /// </remarks>
        public virtual int FilterDescriptionLength
        {
            get => Grid.Session.GetInt32("FLimit") == null
                ? _FilterDescriptionLength
                : Grid.Session.GetInt32("FLimit").Value;
            set
            {
                if (value == 100)
                    Grid.Session.Remove("FLimit");
                else
                    Grid.Session.SetInt32("FLimit", value);
            }
        }

        private void WriteCutString(string s, HtmlTextWriter writer)
        {
            if (FilterDescriptionLength > 10 && s.Length - FilterDescriptionLength > 0)
            {
                var s1 = s.Substring(0, FilterDescriptionLength - 3);
                var i = s1.LastIndexOf(' ');
                if (s1.Length - i < 7)
                    s1 = s.Substring(0, i);
                s1 += "...";
                writer.AddAttribute(HtmlTextWriterAttribute.Title, s);
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.Write(s1);
                writer.RenderEndTag();
            }
            else
            {
                writer.Write(s);
            }
        }

        public void DoRenderContents(HtmlTextWriter writer)
        {
            RenderContents(writer);
        }

        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            writer.AddAttribute("class", "rc_filtergrid rc_olapgrid");
            writer.RenderBeginTag("div");
        }

        protected override void RenderContents(HtmlTextWriter writer)
        {
            if (Grid == null)
                return;

            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "2");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_container ui-widget-content");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_pivotheader ui-widget ui-widget-header rc_levelcell");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.Write(RadarUtils.GetResStr("rsItem"));
            writer.RenderEndTag(); //td
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_pivotheader ui-widget ui-widget-header rc_levelcell");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.Write(RadarUtils.GetResStr("rsFilterDescription"));
            writer.RenderEndTag(); //td
            writer.RenderEndTag(); //tr

            if (Grid.Active)
            {
                var filtered = new Dictionary<string, int>();
                var possible_m = new SortedList<string, string>();

                foreach (var m in Grid.Measures)
                    if (m.Filter != null)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, "");
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_datacell ui-widget ui-widget-content");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(m.DisplayName);
                        writer.RenderEndTag(); //td
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_datacell ui-widget ui-widget-content");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);

                        writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
                        writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
                        writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                        writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                        writer.RenderBeginTag(HtmlTextWriterTag.Table);
                        writer.AddAttribute("uid", m.UniqueName);
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
                        writer.AddAttribute(HtmlTextWriterAttribute.Align, "left");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        WriteCutString(m.Filter.Description, writer);
                        writer.RenderEndTag(); //td
                        writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "1px");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);

                        writer.AddAttribute(HtmlTextWriterAttribute.Title,
                            RadarUtils.GetResStr("hint_ClickTopEditMeasFilter"));
                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                            "rs_icon_cover rs_icon_cover_filter ui-state-default ui-corner-all");
                        writer.AddAttribute("measure", "true");
                        writer.RenderBeginTag(HtmlTextWriterTag.Div);
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_filter ui-icon ui-icon-volume-off");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.RenderEndTag(); //span
                        writer.RenderEndTag(); //div


                        writer.RenderEndTag(); //td
                        writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "1px");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);

                        writer.AddAttribute(HtmlTextWriterAttribute.Title, RadarUtils.GetResStr("repResetFilter"));
                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                            "rs_icon_cover ui-state-default ui-corner-all");
                        writer.RenderBeginTag(HtmlTextWriterTag.Div);
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_del2 ui-icon ui-icon-close");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.RenderEndTag(); //span
                        writer.RenderEndTag(); //div


                        writer.RenderEndTag(); //td
                        writer.RenderEndTag(); //tr
                        writer.RenderEndTag(); //table

                        writer.RenderEndTag(); //td
                        writer.RenderEndTag(); //tr
                    }
                    else
                    {
                        if (m.Visible && m.VisibleInTree) possible_m.Add(m.UniqueName, m.DisplayName);
                    }
                var possible_h = new SortedList<string, string>();
                foreach (var d in Grid.Dimensions)
                foreach (var h in d.Hierarchies)
                {
                    var fd = h.FilterDescription;
                    if (fd != null)
                    {
                        writer.AddAttribute("uid", h.UniqueName);
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, "");
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_datacell ui-widget ui-widget-content");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(h.DisplayName);
                        writer.RenderEndTag(); //td
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_datacell ui-widget ui-widget-content");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);

                        writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
                        writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
                        writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                        writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                        writer.RenderBeginTag(HtmlTextWriterTag.Table);
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
                        writer.AddAttribute(HtmlTextWriterAttribute.Align, "left");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        WriteCutString(fd, writer);
                        writer.RenderEndTag(); //td
                        writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "1px");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);

                        writer.AddAttribute(HtmlTextWriterAttribute.Title,
                            RadarUtils.GetResStr("hint_ClickTopEditMeasFilter"));
                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                            "rs_icon_cover rs_icon_cover_filter ui-state-default ui-corner-all");
                        writer.AddAttribute("measure", "false");
                        writer.RenderBeginTag(HtmlTextWriterTag.Div);
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_filter ui-icon ui-icon-volume-off");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.RenderEndTag(); //span
                        writer.RenderEndTag(); //div

                        writer.RenderEndTag(); //td
                        writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
                        writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "1px");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);

                        writer.AddAttribute(HtmlTextWriterAttribute.Title, RadarUtils.GetResStr("repResetFilter"));
                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                            "rs_icon_cover ui-state-default ui-corner-all");
                        writer.RenderBeginTag(HtmlTextWriterTag.Div);
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_del2 ui-icon ui-icon-close");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.RenderEndTag(); //span
                        writer.RenderEndTag(); //div

                        writer.RenderEndTag(); //td
                        writer.RenderEndTag(); //tr
                        writer.RenderEndTag(); //table

                        writer.RenderEndTag(); //td
                        writer.RenderEndTag(); //tr
                    }
                    else
                    {
                        if (Grid.AxesLayout.fColumnAxis.Contains(h) ||
                            Grid.AxesLayout.fRowAxis.Contains(h) ||
                            Grid.AxesLayout.fPageAxis.Contains(h))
                            possible_h.Add(h.UniqueName, h.DisplayName);
                    }
                }
                if (possible_h.Count > 0 || possible_m.Count > 0)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_datacell ui-widget-content");
                    writer.AddStyleAttribute("padding", "1px;");
                    writer.AddStyleAttribute("border-bottom-width", "0px !important");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);

                    writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapfilter_select");
                    writer.RenderBeginTag(HtmlTextWriterTag.Select);
                    writer.AddAttribute(HtmlTextWriterAttribute.Selected, "selected");
                    writer.AddAttribute(HtmlTextWriterAttribute.Value, "...");
                    writer.RenderBeginTag(HtmlTextWriterTag.Option);
                    writer.Write("...");
                    writer.RenderEndTag(); //option

                    foreach (var m in possible_m)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Value, "m:" + m.Key);
                        writer.RenderBeginTag(HtmlTextWriterTag.Option);
                        writer.Write(m.Value);
                        writer.RenderEndTag(); //option
                    }

                    foreach (var h in possible_h)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Value, "h:" + h.Key);
                        writer.RenderBeginTag(HtmlTextWriterTag.Option);
                        writer.Write(h.Value);
                        writer.RenderEndTag(); //option
                    }

                    writer.RenderEndTag(); //select

                    writer.RenderEndTag(); //td

                    writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_datacell ui-widget-content");
                    writer.AddStyleAttribute("border-bottom-width", "0px !important");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);

                    //HtmlImage img = new HtmlImage();
                    //img.Src = ImageUrl("filterover.png");
                    //img.Attributes.Add("title", RadarUtils.GetResStr("rsEditFilter"));
                    //img.ID = "olapfilter_editnew";
                    //img.RenderControl(writer);

                    writer.AddAttribute(HtmlTextWriterAttribute.Title, RadarUtils.GetResStr("rsEditFilter"));
                    writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapfilter_editnew_cover");
                    writer.AddAttribute(HtmlTextWriterAttribute.Class,
                        "rs_icon_cover rs_icon_cover_filter ui-state-default ui-corner-all");
                    writer.AddStyleAttribute("display", "none");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapfilter_editnew");
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_filter ui-icon ui-icon-volume-off");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderEndTag(); //span
                    writer.RenderEndTag(); //div

                    writer.RenderEndTag(); //td
                    writer.RenderEndTag(); //tr
                }
            }
            writer.RenderEndTag(); //table
        }

        public override void RenderEndTag(HtmlTextWriter writer)
        {
            writer.RenderEndTag(); //div
        }
    }
}