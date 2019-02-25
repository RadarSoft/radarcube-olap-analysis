using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.ClientAgents;
using RadarSoft.RadarCube.Controls.Chart;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Events;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace RadarSoft.RadarCube.Controls.Grid
{
    /// <summary>
    ///     Meant for displaying and manipulating the Cube structure and data of the current
    ///     OLAP slice
    /// </summary>
    public abstract class OlapGrid : OlapControl
    {
        protected string contentPath = null;

        internal OlapGrid(HttpContext contextBase, IHostingEnvironment hosting, IMemoryCache cache)
            : base(contextBase, hosting, cache)
        {
            CreateAxesLayout();
        }

        internal override CellsetMode CellsetMode => CellsetMode.cmGrid;

        protected override void CreateAxesLayout()
        {
            FLayout = new GridAxesLayout(this);
        }

        internal override CellSet.CellSet CreateCellset()
        {
            return new GridCellSet(this);
        }

        internal override void InitClientCellset(RCellset rcellset)
        {
            var ccs = CellSet as GridCellSet;
            if (ccs == null) return;

            if (ccs.ColorAxisDescriptor != null && ccs.ColorAxisDescriptor.Axis != null)
                rcellset.ColorAxis = ccs.ColorAxisDescriptor.Axis.CreateChartAxis();

            if (ccs.ForeColorAxisDescriptor != null && ccs.ForeColorAxisDescriptor.Axis != null)
                rcellset.ForeColorAxis = ccs.ForeColorAxisDescriptor.Axis.CreateChartAxis();

            if (ccs.fColorGridMembers != null)
                rcellset.ColorChartMembers = ccs.fColorGridMembers.Select(item => new ClientMember(item)).ToArray();

            if (ccs.fForeColorGridMembers != null)
                rcellset.ForeColorGridMembers =
                    ccs.fForeColorGridMembers.Select(item => new ClientMember(item)).ToArray();
        }

        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            writer.AddAttribute("id", ID);
            writer.AddStyleAttribute("width", Width);
            writer.AddStyleAttribute("height", Height);
            writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "hidden");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "relative");
            writer.RenderBeginTag("div");

            //Adding overlay while the Grid is loading
            writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "absolute");
            writer.AddStyleAttribute("top", "0");
            writer.AddStyleAttribute("left", "0");
            writer.AddStyleAttribute(HtmlTextWriterStyle.ZIndex, "1000");
            writer.AddStyleAttribute("box-sizing", "border-box !important");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Height, Height);
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, Width);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "loading_overlay ui-widget-content");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "relative");
            writer.AddStyleAttribute("top", "50%");
            writer.AddStyleAttribute("left", "50%");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.AddStyleAttribute("font-size", "2em");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon-font ui-icon-font-loading-status-circle rotate");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.RenderEndTag(); //span
            writer.RenderEndTag(); //div
            writer.RenderEndTag(); //div
        }

        public override void RenderEndTag(HtmlTextWriter writer)
        {
            writer.RenderEndTag(); //table
            writer.RenderEndTag(); // div.rc_olapgrid_container
        }


        protected override void RenderContents(HtmlTextWriter writer)
        {
            //            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            //            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            //            writer.Write("Content here");
            //            writer.RenderEndTag(); // td
            //            writer.RenderEndTag(); // tr
            //            return;


            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            if (ShowAreasMode == rsShowAreasOlapGrid.rsAll)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_leftarea");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.AddStyleAttribute(HtmlTextWriterStyle.Width, StructureTreeWidth);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_leftarea_container");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_tdtree_header");
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rc_pivotheader ui-widget-header rc_pivotheader_leftdivider");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.Write(RadarUtils.GetResStr("rsCubeTree"));
                writer.RenderEndTag(); // span
                writer.RenderEndTag(); // div

                writer.AddStyleAttribute("min-height", "100px");
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_tdtree");
                writer.AddAttribute("area", "tree");
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rs_droptarget rs_droptarget2 rc_container ui-widget-content");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_cubestructuretree_container");
                writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
                writer.AddStyleAttribute(HtmlTextWriterStyle.Height, "100%");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                _FTree.RenderControl(writer);
                writer.RenderEndTag(); // div scroll tree
                writer.RenderEndTag(); // td tree

                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_container");
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_PIVOT");
                writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
                writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                RenderPivot(writer);
                writer.RenderEndTag(); // div
                writer.RenderEndTag(); // div rc_leftarea_container
                writer.RenderEndTag(); // td rc_leftarea
            }
            if (ShowAreasMode == rsShowAreasOlapGrid.rsPivot)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_leftarea");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.AddStyleAttribute(HtmlTextWriterStyle.Width, StructureTreeWidth);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_leftarea_container");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_PIVOT");
                writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                RenderPivot(writer);
                writer.RenderEndTag(); // div
                writer.RenderEndTag(); // div rc_leftarea_container
                writer.RenderEndTag(); // td rc_leftarea
            }
            //Ravish Begin
            if (ShowAreasMode == rsShowAreasOlapGrid.rsTreeOnly)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_leftarea");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.AddStyleAttribute(HtmlTextWriterStyle.Width, StructureTreeWidth);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_leftarea_container");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_tdtree_header");
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rc_pivotheader ui-widget-header rc_pivotheader_leftdivider");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.Write(RadarUtils.GetResStr("rsCubeTree"));
                writer.RenderEndTag(); // span
                writer.RenderEndTag(); // div

                writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_droptarget rs_droptarget2 ui-widget-content");
                writer.AddAttribute("area", "tree");
                writer.AddStyleAttribute("min-height", "100px");
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_tdtree");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                _FTree.RenderControl(writer);
                writer.RenderEndTag(); // div tree
                writer.AddStyleAttribute("min-height", "150px");
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_PIVOT");
                writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
                writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                RenderPivot(writer);
                writer.RenderEndTag(); // div
                writer.RenderEndTag(); // div rc_leftarea_container
                writer.RenderEndTag(); // td rc_leftarea

                writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");
            }
            else
            {
                //Ravish End
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
                writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            }
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_gridarea");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_droptarget rs_droptarget2 ui-widget-content");
            writer.AddAttribute("area", "grid");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_IG");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            RenderInternalGrid(writer);
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td

            if ((ShowAreasMode == rsShowAreasOlapGrid.rsAll || ShowAreasMode == rsShowAreasOlapGrid.rsPivot)
                && (ShowModificationAreas || ShowLegends))
                RenderRightPanel(writer);

            writer.RenderEndTag(); // tr
        }

        internal virtual void RenderRightPanel(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_rightarea");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            if (AnalysisType == AnalysisType.Chart)
                writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "hidden");

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_container rc_rightarea_container");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            if (ShowModificationAreas)
            {
                //Modificators area
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_modifiers");
                writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                RenderModifiers(writer);
                writer.RenderEndTag(); // div
            }

            if (ShowLegends)
            {
                //Legend area
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_legends");
                writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "visible");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "hidden");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                RenderLegends(writer);
                writer.RenderEndTag(); // div
                writer.RenderEndTag(); // div
            }


            //            writer.RenderEndTag(); // table
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td
        }

        internal virtual void RenderModifiers(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            //Background modifier
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_colorsareaheader");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_pivotheader ui-widget-header rc_container rc_pivotheader_leftdivider");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("rsBackgroundArea"));
            writer.RenderEndTag(); // span
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            //pivotAreaStyle.AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rs_droptarget rs_droptarget2 rc_container ui-accordion ui-widget-content");
            writer.AddAttribute("area", "colors");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_colorsarea");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            RenderColorsArea(writer);
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr

            //Foreground modifier
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_forecolorsareaheader");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_pivotheader ui-widget-header rc_container rc_pivotheader_leftdivider");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("rsForegroundArea"));
            writer.RenderEndTag(); // span
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            //pivotAreaStyle.AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rs_droptarget rs_droptarget2 rc_container ui-accordion ui-widget-content");
            writer.AddAttribute("area", "colorfore");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_forecolorsarea");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            RenderForegroundArea(writer);
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr

            writer.RenderEndTag(); // table
        }

        internal virtual void RenderColorsArea(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            if (AxesLayout.fColorAxisItem == null)
                writer.AddStyleAttribute("height", "50px");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            if (AxesLayout.fColorAxisItem is Hierarchy)
            {
                var list = new List<Hierarchy>();
                list.Add((Hierarchy) AxesLayout.fColorAxisItem);
                RenderPivotPanels(writer, list, "color");
            }
            else if (AxesLayout.fColorAxisItem is Measure)
            {
                var list = new List<Measure>();
                list.Add((Measure) AxesLayout.fColorAxisItem);
                RenderPivotPanels(writer, list, "color");
            }


            writer.RenderEndTag(); // table
        }

        internal void RenderForegroundArea(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            if (AxesLayout.fColorForeAxisItem == null)
                writer.AddStyleAttribute("height", "50px");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            if (AxesLayout.fColorForeAxisItem is Hierarchy)
            {
                var list = new List<Hierarchy>();
                list.Add((Hierarchy) AxesLayout.fColorForeAxisItem);
                RenderPivotPanels(writer, list, "colorfore");
            }
            else if (AxesLayout.fColorForeAxisItem is Measure)
            {
                var list = new List<Measure>();
                list.Add((Measure) AxesLayout.fColorForeAxisItem);
                RenderPivotPanels(writer, list, "colorfore");
            }


            writer.RenderEndTag(); // table
        }

        internal virtual void RenderPivotPanels(HtmlTextWriter writer, List<Measure> list, string area)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, "center");
                writer.AddAttribute("uid", m.UniqueName);
                writer.AddAttribute("areaindex", i.ToString());
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_droptarget2");
                writer.AddAttribute("drag", "true");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                //pivotPanelStyle.AddAttributesToRender(writer);
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rc_pivotpanel ui-accordion-header ui-state-default ui-accordion-icons ui-corner-all");
                //writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                writer.RenderBeginTag(HtmlTextWriterTag.Table);
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");

                var title = "";
                var text = "";
                var E = new RenderPivotArgs(m, m.DisplayName);
                if (RiseOnRenderPivotPanel(E))
                {
                    title = E.Text;
                    if (RadarUtils.CutText(E.Text, MaxTextLength, out text))
                        writer.AddAttribute(HtmlTextWriterAttribute.Title, title);
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(MarkMeasuresForTree(text, true));
                }
                else
                {
                    title = m.DisplayName;
                    if (RadarUtils.CutText(m.DisplayName, MaxTextLength, out text))
                        writer.AddAttribute(HtmlTextWriterAttribute.Title, title);
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(MarkMeasuresForTree(text, true));
                }

                writer.RenderEndTag(); //td
                if (AllowFiltering)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.AddAttribute(HtmlTextWriterAttribute.Title,
                        RadarUtils.GetResStr("hint_ClickTopEditMeasFilter"));
                    if (m.Filter != null && m.Filter.Description.IsFill())
                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                            "rs_icon_cover rs_icon_cover_filter ui-state-active ui-corner-all");
                    else
                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                            "rs_icon_cover rs_icon_cover_filter ui-state-default ui-corner-all");
                    writer.AddAttribute("measure", "true");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_filter ui-icon ui-icon-volume-off");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderEndTag(); //span
                    writer.RenderEndTag(); //div
                    writer.RenderEndTag(); //td
                }
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.AddAttribute(HtmlTextWriterAttribute.Title, RadarUtils.GetResStr("hint_RemoveItem"));
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_icon_cover ui-state-default ui-corner-all");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_del ui-icon ui-icon-close");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.RenderEndTag(); //span
                writer.RenderEndTag(); //div
                writer.RenderEndTag(); //td
                writer.RenderEndTag(); //tr
                writer.RenderEndTag(); //table
                writer.RenderEndTag(); //td
                writer.RenderEndTag(); //tr
            }

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.AddStyleAttribute(HtmlTextWriterStyle.Height, "35px");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.Write("&nbsp;");
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr
        }

        internal virtual void RenderLegends(HtmlTextWriter writer)
        {
            var url_legend = ImageUrl("legend.gif");

            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_pivotheader ui-widget-header rc_container rc_pivotheader_leftdivider");

            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("exprt_ModificatorContainer"));
            writer.RenderEndTag(); // span
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td

            writer.RenderEndTag(); // tr

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            //pivotAreaStyle.AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");

            writer.AddAttribute("area", "legends");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-widget-content rc_container");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "all_legends");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "color_legend");
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderEndTag(); // div

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "size_legend");
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderEndTag(); // div

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "shape_legend");
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderEndTag(); // div

            writer.RenderEndTag(); // div

            writer.RenderEndTag(); // td

            writer.RenderEndTag(); // tr

            writer.RenderEndTag(); // table
        }

        protected virtual void RenderPivot(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_PIVOT_inner");
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            if (FilterAreaVisible)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rc_pivotheader rc_pivotheader_leftdivider ui-widget-header");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.Write(RadarUtils.GetResStr("rsFilterArea"));
                writer.RenderEndTag(); // span
                writer.RenderEndTag(); // div
                writer.RenderEndTag(); // td
            }
            else
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Colspan, "2");
            }

            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            if (FilterAreaVisible)
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rc_pivotheader rc_pivotheader_leftdivider ui-widget-header");
            else
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_pivotheader ui-widget-header");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("rsColumnArea"));
            writer.RenderEndTag(); // span
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td

            writer.RenderEndTag(); // tr

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            if (FilterAreaVisible)
            {
                //pivotAreaStyle.AddAttributesToRender(writer);
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_filterarea");
                writer.AddAttribute("area", "page");
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rs_droptarget rs_droptarget2 rc_pivotarea_leftdivider ui-accordion ui-widget-content");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                RenderPageArea(writer);
                writer.RenderEndTag(); // td
            }
            else
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Colspan, "2");
            }

            //pivotAreaStyle.AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            if (FilterAreaVisible)
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rs_droptarget rs_droptarget2 rc_pivotarea_rightdivider ui-accordion ui-widget-content");
            else
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rs_droptarget rs_droptarget2 ui-accordion ui-widget-content");
            writer.AddAttribute("area", "col");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_columnarea");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            RenderColumnArea(writer);
            writer.RenderEndTag(); // td

            writer.RenderEndTag(); // tr

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_pivotheader rc_pivotheader_leftdivider ui-widget-header");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("rsRowArea"));
            writer.RenderEndTag(); // span
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td

            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_pivotheader rc_pivotheader_leftdivider ui-widget-header");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("rsValues"));
            writer.RenderEndTag(); // span
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td

            writer.RenderEndTag(); // tr

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            //pivotAreaStyle.AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "DesignModers_droptarget rs_droptarget2 rc_pivotarea_leftdivider ui-accordion ui-widget-content");
            writer.AddAttribute("area", "row");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_rowarea");
            if (ShowAreasMode == rsShowAreasOlapGrid.rsPivot)
                writer.AddStyleAttribute("border-width", "0 0 1px 1px !important");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            RenderRowArea(writer);
            writer.RenderEndTag(); // td

            //pivotAreaStyle.AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rs_droptarget rs_droptarget2 rc_pivotarea_rightdivider ui-accordion ui-widget-content");
            writer.AddAttribute("area", "row");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_valuearea");
            if (ShowAreasMode == rsShowAreasOlapGrid.rsPivot)
                writer.AddStyleAttribute("border-width", "0 0 1px 1px !important");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            RenderMeasuresArea(writer);
            writer.RenderEndTag(); // td

            writer.RenderEndTag(); // tr
            writer.RenderEndTag(); // table
        }

        protected void RenderPageArea(HtmlTextWriter writer)
        {
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            RenderPivotPanels(writer, AxesLayout.fPageAxis, "page");

            writer.RenderEndTag(); //table 
        }

        protected void RenderPivotPanels(HtmlTextWriter writer, IList<Hierarchy> list, string area)
        {
            //            string url_filtered = ImageUrl((IsIE6) ? "Filtered.png.gif" : "Filtered.png");
            //            string url_filterednon = ImageUrl((IsIE6) ? "FilteredMenu.png.gif" : "FilteredMenu.png");

            for (var i = 0; i < list.Count; i++)
            {
                var h = list[i];
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, "center");
                writer.AddAttribute("uid", h.UniqueName);
                writer.AddAttribute("areaindex", i.ToString());
                writer.AddAttribute("drag", "true");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rc_pivotpanel ui-accordion-header ui-state-default ui-accordion-icons ui-corner-all");
                writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                writer.RenderBeginTag(HtmlTextWriterTag.Table);
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                var title = "";
                var text = "";

                var E = new RenderPivotArgs(h, h.DisplayName);
                if (RiseOnRenderPivotPanel(E))
                {
                    title = E.Text;
                    if (RadarUtils.CutText(E.Text, MaxTextLength, out text))
                        writer.AddAttribute(HtmlTextWriterAttribute.Title, title);
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(MarkHierarchiesForTree(text, true));
                }
                else
                {
                    title = h.DisplayName;
                    if (RadarUtils.CutText(h.DisplayName, MaxTextLength, out text))
                        writer.AddAttribute(HtmlTextWriterAttribute.Title, title);
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(MarkHierarchiesForTree(text, true));
                }
                writer.RenderEndTag(); //td

                if (h.AllowFilter && h.AllowHierarchyEditor && AllowFiltering)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);

                    writer.AddAttribute(HtmlTextWriterAttribute.Title,
                        RadarUtils.GetResStr("hint_ClickToEditHierFilter"));

                    if (h.Filtered || h.FilteredByLevelFilters)
                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                            "rs_icon_cover rs_icon_cover_filter ui-state-active ui-corner-all");
                    else
                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                            "rs_icon_cover rs_icon_cover_filter ui-state-default ui-corner-all");

                    writer.AddAttribute("measure", "false");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_filter ui-icon ui-icon-volume-off");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderEndTag(); //span
                    writer.RenderEndTag(); //div
                    writer.RenderEndTag(); //td
                }

                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                //writer.AddAttribute(HtmlTextWriterAttribute.Title, RadarUtils.GetResStr("hint_RemoveItem"));
                //writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_del");
                //writer.RenderBeginTag(HtmlTextWriterTag.Img);
                //writer.RenderEndTag();//img
                writer.AddAttribute(HtmlTextWriterAttribute.Title, RadarUtils.GetResStr("hint_RemoveItem"));
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_icon_cover ui-state-default ui-corner-all");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_del ui-icon ui-icon-close");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.RenderEndTag(); //span
                writer.RenderEndTag(); //div

                writer.RenderEndTag(); //td
                writer.RenderEndTag(); //tr
                writer.RenderEndTag(); //table
                writer.RenderEndTag(); //td
                writer.RenderEndTag(); //tr
            }

            if (area == "col" && AxesLayout.fXAxisMeasure != null)
            {
                var m = AxesLayout.fXAxisMeasure;
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, "center");
                writer.AddAttribute("uid", m.UniqueName);
                writer.AddAttribute("areaindex", list.Count.ToString());
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_droptarget2");
                writer.AddAttribute("drag", "true");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                //pivotPanelStyle.AddAttributesToRender(writer);
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rc_pivotpanel ui-accordion-header ui-state-default ui-accordion-icons ui-corner-all");
                //writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                writer.RenderBeginTag(HtmlTextWriterTag.Table);
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                var title = "";
                var text = "";
                var E = new RenderPivotArgs(m, m.DisplayName);
                if (RiseOnRenderPivotPanel(E))
                {
                    title = E.Text;
                    if (RadarUtils.CutText(E.Text, MaxTextLength, out text))
                        writer.AddAttribute(HtmlTextWriterAttribute.Title, title);
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(MarkHierarchiesForTree(text, true));
                }
                else
                {
                    title = m.DisplayName;
                    if (RadarUtils.CutText(m.DisplayName, MaxTextLength, out text))
                        writer.AddAttribute(HtmlTextWriterAttribute.Title, title);
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.Write(MarkHierarchiesForTree(text, true));
                }
                writer.RenderEndTag(); //td
                if (AllowFiltering)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    writer.AddAttribute(HtmlTextWriterAttribute.Title,
                        RadarUtils.GetResStr("hint_ClickTopEditMeasFilter"));
                    if (m.Filter != null && m.Filter.Description.IsFill())
                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                            "rs_icon_cover rs_icon_cover_filter ui-state-active ui-corner-all");
                    else
                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                            "rs_icon_cover rs_icon_cover_filter ui-state-default ui-corner-all");
                    writer.AddAttribute("measure", "true");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_filter ui-icon ui-icon-volume-off");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderEndTag(); //span
                    writer.RenderEndTag(); //div

                    writer.RenderEndTag(); //td
                }
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                //writer.AddAttribute(HtmlTextWriterAttribute.Title, RadarUtils.GetResStr("hint_RemoveItem"));
                //writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_del");
                //writer.RenderBeginTag(HtmlTextWriterTag.Img);
                //writer.RenderEndTag();//img

                writer.AddAttribute(HtmlTextWriterAttribute.Title, RadarUtils.GetResStr("hint_RemoveItem"));
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_icon_cover ui-state-default ui-corner-all");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_del ui-icon ui-icon-close");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.RenderEndTag(); //span
                writer.RenderEndTag(); //div

                writer.RenderEndTag(); //td
                writer.RenderEndTag(); //tr
                writer.RenderEndTag(); //table
                writer.RenderEndTag(); //td
                writer.RenderEndTag(); //tr
            }

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.AddStyleAttribute(HtmlTextWriterStyle.Height, "35px");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.Write("&nbsp;");
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr
        }

        protected void RenderRowArea(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Rowspan, "3");
            writer.AddStyleAttribute(HtmlTextWriterStyle.VerticalAlign, "top");
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            RenderPivotPanels(writer, AxesLayout.fRowAxis, "row");

            writer.RenderEndTag(); // table
        }

        protected void RenderColumnArea(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            RenderPivotPanels(writer, AxesLayout.fColumnAxis, "col");

            writer.RenderEndTag(); // td
        }

        protected void RenderMeasuresArea(HtmlTextWriter writer)
        {
            var url_filterover = ImageUrl("filterover.png");
            var url_del = ImageUrl("del.png");

            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            RenderMeasure(writer);

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            if (this is OlapChart && AnalysisType == AnalysisType.Chart)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_create_chart");
                writer.AddAttribute(HtmlTextWriterAttribute.Style,
                    "text-align: center; padding-top: 2px; " +
                    "padding-right: 2px; padding-bottom: 2px; " +
                    "padding-left: 2px; font-size: 8pt; " +
                    "font-style: italic; border-top-width: 1px; " +
                    //"border-top-color: #beddf6; " +
                    "border-top-style: solid;");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write(RadarUtils.GetResStr("rsDragMeasure2"));
            }
            else
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write("&nbsp;");
            }

            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr
            writer.RenderEndTag(); //table
        }

        protected virtual void RenderMeasure(HtmlTextWriter writer)
        {
            if (Measures.Level != null)
                for (var i = 0; i < Measures.Level.Members.Count; i++)
                {
                    var m = Measures.Level.Members[i];
                    if (!m.Visible) continue;
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    writer.AddAttribute("uid", m.UniqueName);
                    writer.AddAttribute("areaindex", i.ToString());
                    //writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_droptarget2");
                    writer.AddAttribute("drag", "true");
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    //pivotPanelStyle.AddAttributesToRender(writer);
                    writer.AddAttribute(HtmlTextWriterAttribute.Class,
                        "rc_pivotpanel ui-accordion-header ui-state-default ui-accordion-icons ui-corner-all");
                    //writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                    writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                    writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
                    writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                    writer.RenderBeginTag(HtmlTextWriterTag.Table);
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                    var title = "";
                    var text = "";
                    var E = new RenderPivotArgs(m, m.DisplayName);
                    if (RiseOnRenderPivotPanel(E))
                    {
                        title = E.Text;
                        if (RadarUtils.CutText(E.Text, MaxTextLength, out text))
                            writer.AddAttribute(HtmlTextWriterAttribute.Title, title);
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(MarkHierarchiesForTree(text, true));
                    }
                    else
                    {
                        title = m.DisplayName;
                        if (RadarUtils.CutText(m.DisplayName, MaxTextLength, out text))
                            writer.AddAttribute(HtmlTextWriterAttribute.Title, title);
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.Write(MarkHierarchiesForTree(text, true));
                    }
                    writer.RenderEndTag(); //td
                    if (AllowFiltering)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);

                        writer.AddAttribute(HtmlTextWriterAttribute.Title,
                            RadarUtils.GetResStr("hint_ClickTopEditMeasFilter"));

                        var m_Measure = Measures.Find(m.UniqueName);
                        if (m_Measure != null && m_Measure.Filter != null && m_Measure.Filter.Description.IsFill())
                            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                "rs_icon_cover rs_icon_cover_filter ui-state-active ui-corner-all");
                        else
                            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                "rs_icon_cover rs_icon_cover_filter ui-state-default ui-corner-all");

                        writer.AddAttribute("measure", "true");
                        writer.RenderBeginTag(HtmlTextWriterTag.Div);
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_filter ui-icon ui-icon-volume-off");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.RenderEndTag(); //span
                        writer.RenderEndTag(); //div

                        writer.RenderEndTag(); //td
                    }
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);

                    writer.AddAttribute(HtmlTextWriterAttribute.Title, RadarUtils.GetResStr("hint_RemoveItem"));
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_icon_cover ui-state-default ui-corner-all");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_img_del ui-icon ui-icon-close");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderEndTag(); //span
                    writer.RenderEndTag(); //div

                    writer.RenderEndTag(); //td

                    writer.RenderEndTag(); //tr
                    writer.RenderEndTag(); //table
                    writer.RenderEndTag(); //td
                    writer.RenderEndTag(); //tr
                }
        }

        protected override void Render(HtmlTextWriter writer)
        {
            base.Render(writer);

            // rendering draghelper
            writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "absolute");
            writer.AddStyleAttribute(HtmlTextWriterStyle.ZIndex, "1000");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "OLAPGrid_draghelper");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-accordion ui-state-default ui-corner-all");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderEndTag(); // div

            writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "absolute");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_rsPopup");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            //writer.Write(RadarUtils.GetResStr("rsLoading"));
            writer.RenderEndTag(); //div

            //_fPopupStyle.AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_DLG_" + ClientID);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_dialog");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.Write(RadarUtils.GetResStr("rsLoading"));
            writer.RenderEndTag(); //div

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_DLG2_" + ClientID);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_dialog");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.Write(RadarUtils.GetResStr("rsLoading"));
            writer.RenderEndTag(); //div

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_DLG_wait_" + ClientID);
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "olapgrid_lable");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderEndTag(); //div
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "olapgrid_progressbar");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderEndTag(); //div
            writer.RenderEndTag(); //div
        }

        protected virtual void RenderInternalGrid(HtmlTextWriter writer)
        {
            writer.AddStyleAttribute(HtmlTextWriterStyle.VerticalAlign, "top");
            if (UseFixedHeaders)
                writer.AddAttribute(HtmlTextWriterAttribute.Width, "50");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            if (UseFixedHeaders)
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_FR");
            writer.RenderBeginTag(HtmlTextWriterTag.Thead);
            var altflag = new bool[CellSet.ColumnCount];
            for (var i = 0; i < CellSet.RowCount; i++)
            {
                if (i == FCellSet.FFixedRows)
                {
                    //if (UseFixedHeaders)
                    writer.RenderEndTag(); //thead
                    altflag = new bool[CellSet.ColumnCount];
                }
                if (!CellSet.IsRowVisible(i)) continue;
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                for (var j = 0; j < CellSet.ColumnCount; j++)
                {
                    if (!CellSet.IsColumnVisible(j)) continue;
                    var c = CellSet.Cells(j, i);
                    var comment = "";
                    if (c is IDataCell)
                        comment = ((IDataCell) c).Comment;
                    if (c is IMemberCell)
                        comment = ((IMemberCell) c).Comment;
                    if (c is MemberCell)
                    {
                        if ((c as MemberCell).Model.Rendered)
                            continue;
                        (c as MemberCell).Model.Rendered = true;
                    }
                    else
                    {
                        if (c.StartColumn != j || c.StartRow != i)
                            continue;
                    }
                    altflag[j] = !altflag[j];
                    if (c.ColSpan > 1)
                        writer.AddAttribute(HtmlTextWriterAttribute.Colspan, c.ColSpan.ToString());
                    if (c.RowSpan > 1)
                        writer.AddAttribute(HtmlTextWriterAttribute.Rowspan, c.RowSpan.ToString());
                    RenderCellEventArgs e = null;
                    HandleOnRenderCell(c);

                    writer.AddAttribute("cellid", (FCellSet.ColumnCount * i + j).ToString());
                    if (c is IMemberCell)
                    {
                        var mc = (IMemberCell) c;
                        if (mc.Area == LayoutArea.laColumn && mc.ChildrenCount == 0 && !mc.IsPager &&
                            FMode == OlapGridMode.gmStandard)
                            writer.AddAttribute("valuesort", j.ToString());

                        if (mc.Area == LayoutArea.laRow)
                            writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "left");
                    }

                    if (i == 0 && j < CellSet.FixedColumns)
                        writer.AddAttribute("fixedcol", "1");
                    if (j == 0 && i < CellSet.FixedRows)
                        writer.AddAttribute("fixedrow", "1");

                    var classes = "";
                    switch (c.CellType)
                    {
                        case CellType.ctLevel:
                            classes = "rc_levelcell ui-widget-header";
                            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_levelcell ui-widget-header");
                            break;
                        case CellType.ctMember:
                        case CellType.ctNone:
                            var mc = c as IMemberCell;
                            if (mc != null && (mc.IsTotal || ((MemberCell) mc).IsTotal))
                                classes = "rc_membercell rc_membercell_total ui-widget-content ui-state-default";
                            else
                                classes = "rc_membercell ui-widget-content ui-state-default";
                            break;
                        case CellType.ctData:
                            var dc = c as IDataCell;
                            writer.AddStyleAttribute(HtmlTextWriterStyle.WhiteSpace, "nowrap");
                            if (dc != null && dc.IsTotal)
                                classes = "rc_datacell rc_datacell_total ui-widget-content";
                            else
                                classes = "rc_datacell ui-widget-content";
                            break;
                    }


                    if (!UseFixedHeaders && (i < CellSet.FixedRows || j < CellSet.FixedColumns)
                    ) // && (j >= CellSet.FixedColumns || ((i == 0) && (j < CellSet.FixedColumns))))
                        classes = classes + " rc_th";

                    writer.AddAttribute(HtmlTextWriterAttribute.Class, classes);
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);

                    if (!string.IsNullOrEmpty(comment))
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Height, "100%");
                        writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                        writer.RenderBeginTag(HtmlTextWriterTag.Table);
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        writer.AddAttribute(HtmlTextWriterAttribute.Valign, "center");
                        writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                    }
                    if (c is ILevelCell)
                    {
                        var lc = c as ILevelCell;
                        if (lc.Indent > 0)
                        {
                            writer.AddStyleAttribute(HtmlTextWriterStyle.MarginLeft, 4 + lc.Indent * 20 + "px");
                            writer.RenderBeginTag(HtmlTextWriterTag.Div);
                        }
                        if (lc.Level.LevelType == HierarchyDataType.htMeasures)
                        {
                            writer.AddStyleAttribute(HtmlTextWriterStyle.Padding, "2px");
                            writer.RenderBeginTag(HtmlTextWriterTag.Div);
                            writer.Write(HTMLPrepare(e == null ? c.Value : e.Text));
                            writer.RenderEndTag();
                        }
                        else
                        {
                            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "2");
                            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                            writer.RenderBeginTag(HtmlTextWriterTag.Table);
                            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            writer.Write(HTMLPrepare(e == null ? c.Value : e.Text));
                            writer.RenderEndTag(); //td
                            if (lc.Level.FHierarchy.AllowFilter && lc.Level.FHierarchy.AllowHierarchyEditor &&
                                AllowFiltering)
                            {
                                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                    RadarUtils.GetResStr("hint_ClickToEditHierFilter"));
                                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                    "rs_icon_cover rs_icon_cover_filter ui-state-default ui-corner-all");
                                writer.AddAttribute("measure", "false");
                                writer.AddAttribute("uid", lc.Level.FHierarchy.UniqueName);
                                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                    "rs_img_filter ui-icon ui-icon-volume-off");
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.RenderEndTag(); //span
                                writer.RenderEndTag(); //div
                                writer.RenderEndTag(); //td
                            }
                            writer.RenderEndTag(); //tr
                            writer.RenderEndTag(); //table
                        }
                        if (lc.Indent > 0) writer.RenderEndTag(); // div
                    }
                    if (c is IMemberCell)
                    {
                        var mc = c as IMemberCell;
                        var actions = mc.PossibleDrillActions;
                        if (mc.Indent > 0)
                        {
                            writer.AddStyleAttribute(HtmlTextWriterStyle.MarginLeft, mc.Indent * 20 + "px");
                            writer.RenderBeginTag(HtmlTextWriterTag.Div);
                        }
                        writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                        writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "2");
                        writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                        writer.RenderBeginTag(HtmlTextWriterTag.Table);
                        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                        if (mc.Area == LayoutArea.laRow)
                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        if (AllowDrilling)
                        {
                            if ((actions & PossibleDrillActions.esCollapsed) == PossibleDrillActions.esCollapsed)
                            {
                                if (mc.Area != LayoutArea.laRow)
                                {
                                    writer.AddStyleAttribute(HtmlTextWriterStyle.Padding, "0");
                                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                }

                                if (mc.ChildrenCount > 0)
                                {
                                    var mmc = mc.Children(0);
                                    if (mmc.Level.Level.Hierarchy != mc.Level.Level.Hierarchy)
                                    {
                                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                            "rs_collapse ui-icon ui-icon-minus");
                                    }
                                    else
                                    {
                                        if (mmc.Level.Level != mc.Level.Level)
                                            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                                "rs_collapse ui-icon ui-icon-triangle-1-se");
                                        else
                                            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                                "rs_collapse ui-icon ui-icon-caret-1-se");
                                    }
                                }
                                else
                                {
                                    var _mc = mc as MemberCell;
                                    var a = _mc.PossibleDrillActions;
                                    if ((a & PossibleDrillActions.esNextHierarchy) ==
                                        PossibleDrillActions.esNextHierarchy)
                                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                            "rs_collapse ui-icon ui-icon-minus");
                                    else if ((a & PossibleDrillActions.esNextLevel) == PossibleDrillActions.esNextLevel)
                                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                            "rs_collapse ui-icon ui-icon-triangle-1-se");
                                    else if ((a & PossibleDrillActions.esParentChild) ==
                                             PossibleDrillActions.esParentChild)
                                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                            "rs_collapse ui-icon ui-icon-caret-1-se");
                                    else
                                        writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                            "rs_collapse ui-icon ui-icon-triangle-1-se");
                                }
                                writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                    RadarUtils.GetResStr("hint_CollapseCell"));
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.RenderEndTag(); //span
                                if (mc.Area != LayoutArea.laRow) writer.RenderEndTag(); //td
                            }
                            if ((actions & PossibleDrillActions.esNextHierarchy) ==
                                PossibleDrillActions.esNextHierarchy)
                            {
                                if (mc.Area != LayoutArea.laRow)
                                {
                                    writer.AddStyleAttribute(HtmlTextWriterStyle.Padding, "0");
                                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                }
                                writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                    RadarUtils.GetResStr("hint_DrillNextHierarchy"));
                                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_nexthier ui-icon ui-icon-plus");

                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.RenderEndTag(); //span

                                if (mc.Area != LayoutArea.laRow) writer.RenderEndTag(); //td
                            }
                            if ((actions & PossibleDrillActions.esNextLevel) == PossibleDrillActions.esNextLevel)
                            {
                                if (mc.Area != LayoutArea.laRow)
                                {
                                    writer.AddStyleAttribute(HtmlTextWriterStyle.Padding, "0");
                                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                }

                                writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                    RadarUtils.GetResStr("hint_DrillNextLevel"));
                                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                    "rs_nextlevel ui-icon ui-icon-triangle-1-e");
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.RenderEndTag(); //span
                                if (mc.Area != LayoutArea.laRow) writer.RenderEndTag(); //td
                            }
                            if ((actions & PossibleDrillActions.esParentChild) == PossibleDrillActions.esParentChild)
                            {
                                if (mc.Area != LayoutArea.laRow)
                                {
                                    writer.AddStyleAttribute(HtmlTextWriterStyle.Padding, "0");
                                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                }

                                writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                    RadarUtils.GetResStr("hint_DrillParentChild"));
                                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                    "rs_parentchild ui-icon ui-icon-caret-1-e");
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.RenderEndTag(); //span
                                if (mc.Area != LayoutArea.laRow) writer.RenderEndTag(); //td
                            }
                        }
                        if (mc.Member != null && mc.Member.MemberType == MemberType.mtMeasure)
                        {
                            if (mc.Area != LayoutArea.laRow)
                            {
                                writer.AddStyleAttribute(HtmlTextWriterStyle.Padding, "0");
                                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            }

                            var mm = Measures[mc.Member.UniqueName];
                            var url_meas = mm.IsKPI ? "KPI" : "MeasuresRoot";
                            writer.AddAttribute(HtmlTextWriterAttribute.Src,
                                ImageUrl(url_meas + ".gif"));
                            writer.AddAttribute(HtmlTextWriterAttribute.Width, "16");
                            writer.AddAttribute(HtmlTextWriterAttribute.Height, "16");
                            writer.RenderBeginTag(HtmlTextWriterTag.Img);
                            writer.RenderEndTag(); //img
                            if (mc.Area != LayoutArea.laRow) writer.RenderEndTag(); //td
                        }
                        if (mc.Area == LayoutArea.laRow) writer.RenderEndTag(); //td

                        writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, "");

                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        if (mc.IsPager)
                        {
                            WritePagerText(writer, mc, (FCellSet.ColumnCount * i + j).ToString());
                        }
                        else
                        {
                            var txt = HTMLPrepare(e == null ? c.Value : e.Text);
                            if (e != null && !string.IsNullOrEmpty(e.Tooltip))
                            {
                                writer.AddAttribute(HtmlTextWriterAttribute.Title, e.Tooltip);
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.Write(txt);
                                writer.RenderEndTag();
                            }
                            else
                            {
                                var s = mc.Member != null ? mc.Member.ExtractAttributesAsTooltip(true) : "";
                                if (!string.IsNullOrEmpty(s) && !string.IsNullOrEmpty(c.Description))
                                    s = c.Description + "<br />" + s;
                                else
                                    s = c.Description + s;

                                if (!string.IsNullOrEmpty(s))
                                {
                                    writer.AddAttribute(HtmlTextWriterAttribute.Title, c.Description);
                                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                    writer.Write(txt);
                                    writer.RenderEndTag();
                                }
                                else
                                {
                                    writer.Write(txt);
                                }
                            }
                        }
                        if (mc.Area == LayoutArea.laColumn && mc.ChildrenCount == 0 && FCellSet.FValueSortedColumn == j)
                        {
                            writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "relative");
                            writer.AddStyleAttribute(HtmlTextWriterStyle.MarginLeft, "-7px");
                            writer.AddStyleAttribute(HtmlTextWriterStyle.Left, "50%");
                            var sortimgClass =
                                FCellSet.FSortingDirection == ValueSortingDirection.sdAscending
                                    ? "ui-icon-triangle-1-n"
                                    : "ui-icon-triangle-1-s";
                            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon " + sortimgClass);
                            writer.RenderBeginTag(HtmlTextWriterTag.Div);
                            writer.RenderEndTag(); //div
                        }
                        writer.RenderEndTag(); //td
                        writer.RenderEndTag(); //tr
                        writer.RenderEndTag(); //table

                        if (mc.Indent > 0) writer.RenderEndTag(); // div
                    }
                    if (c is IDataCell || c is ErrorCell)
                        if (c is IDataCell)
                        {
                            var dc = c as IDataCell;
                            var a = dc.Address;
                            if (a != null && a.Measure != null && a.Measure.IsKPI &&
                                a.MeasureMode.Mode == MeasureShowModeType.smKPIStatus &&
                                a.Measure.CubeMeasure.KPIStatusImageIndex >= 0 && RadarUtils.IsNumeric(dc.Data))
                            {
                                var dd = Convert.ToDouble(dc.Data);
                                var ii = a.Measure.CubeMeasure.KPIStatusImageIndex;
                                var st = "";
                                if (ii == 0)
                                    if (dd < -0.33) st = "k_traf_b";
                                    else if (dd > 0.33) st = "k_traf_g";
                                    else st = "k_traf_n";
                                if (ii == 1)
                                    if (dd < -0.33) st = "k_rs_b";
                                    else if (dd > 0.33) st = "k_rs_g";
                                    else st = "k_rs_n";
                                if (ii == 2)
                                    if (dd < -0.6) st = "k_g_b";
                                    else if (dd < -0.2) st = "k_g_bn";
                                    else if (dd < 0.2) st = "k_g_n";
                                    else if (dd < 0.6) st = "k_g_ng";
                                    else st = "k_g_g";
                                if (ii == 3)
                                    if (dd < -0.6) st = "k_rg_b";
                                    else if (dd < -0.2) st = "k_rg_bn";
                                    else if (dd < 0.2) st = "k_rg_n";
                                    else if (dd < 0.6) st = "k_rg_ng";
                                    else st = "k_rh_g";
                                if (ii == 4)
                                    if (dd < -0.33) st = "k_t_b";
                                    else if (dd > 0.33) st = "k_t_g";
                                    else st = "k_t_n";
                                if (ii == 5)
                                    if (dd < -0.33) st = "k_c_b";
                                    else if (dd > 0.33) st = "k_c_g";
                                    else st = "k_c_n";
                                if (ii == 6)
                                    if (dd < -0.33) st = "k_f_b";
                                    else if (dd > 0.33) st = "k_f_g";
                                    else st = "k_f_n";
                                if (ii == 7)
                                    if (dd < -0.33) st = "k_a_b";
                                    else if (dd > 0.33) st = "k_a_g";
                                    else st = "k_a_n";
                                writer.AddAttribute(HtmlTextWriterAttribute.Src,
                                    ImageUrl(st + ".gif"));
                                writer.AddAttribute(HtmlTextWriterAttribute.Alt, dd.ToString("0.###"));
                                writer.RenderBeginTag(HtmlTextWriterTag.Img);
                                writer.RenderEndTag();
                            }
                            else
                            {
                                var txt = e == null ? c.Value : e.Text;
                                if (e != null && !string.IsNullOrEmpty(e.Tooltip))
                                {
                                    writer.AddAttribute(HtmlTextWriterAttribute.Title, e.Tooltip);
                                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                    writer.Write(txt);
                                    writer.RenderEndTag();
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(c.Description))
                                    {
                                        writer.AddAttribute(HtmlTextWriterAttribute.Title, c.Description);
                                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                        writer.Write(txt);
                                        writer.RenderEndTag();
                                    }
                                    else
                                    {
                                        writer.Write(txt);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var txt = e == null ? c.Value : e.Text;
                            if (e != null && !string.IsNullOrEmpty(e.Tooltip))
                            {
                                writer.AddAttribute(HtmlTextWriterAttribute.Title, e.Tooltip);
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.Write(txt);
                                writer.RenderEndTag();
                            }
                            else
                            {
                                writer.Write(txt);
                            }
                        }
                    if (!string.IsNullOrEmpty(comment))
                    {
                        writer.RenderEndTag(); // td
                        writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
                        writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
                        writer.RenderBeginTag(HtmlTextWriterTag.Td);
                        writer.AddAttribute(HtmlTextWriterAttribute.Title, comment);
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon ui-icon-triangle-1-ne");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.RenderEndTag(); //span
                        writer.RenderEndTag(); //td
                        writer.RenderEndTag(); //tr
                        writer.RenderEndTag(); //table
                    }
                    writer.RenderEndTag(); // td
                }
                writer.RenderEndTag(); // tr
            }
            if (FCellSet.RowCount == FCellSet.FFixedRows) // && UseFixedHeaders)
                writer.RenderEndTag(); //thead
            writer.RenderEndTag(); // table
        }

        internal void WritePagerText(HtmlTextWriter writer, IMemberCell mc, string cellid)
        {
            var MC = mc as MemberCell;
            if (MC.Level.Level.PagerSettings.LinesInPage == 0)
                return;

            writer.AddAttribute("pagercell", cellid);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-widget ui-corner-all ui-widget-header");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.Write(RadarUtils.GetResStr("rsPages"));

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-corner-all ui-widget-content");
            writer.AddStyleAttribute("display", "inline-block");
            writer.AddStyleAttribute("padding", "2px");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            var count = MC.SiblingsCount;
            var pages = (count - 1) / MC.Level.Level.PagerSettings.LinesInPage + 1;
            var current = MC.CurrentPage;
            if (current > pages)
            {
                MC.PageTo(1);
                current = 1;
            }
            if (pages < 9)
            {
                for (var i = 1; i <= pages; i++)
                {
                    if (i != current)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    }
                    writer.Write(i.ToString());
                    if (i != current) writer.RenderEndTag(); //Span
                }
            }
            else
            {
                if (current > 5 && current != pages)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.WriteInLine("1");
                    writer.RenderEndTag(); //Span

                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.WriteInLine("...");
                    writer.RenderEndTag(); //span

                    writer.Write(current.ToString());
                    if (current != pages - 1)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        writer.WriteInLine("...");
                        writer.RenderEndTag(); //span
                    }

                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.WriteInLine(pages.ToString());
                    writer.RenderEndTag(); //Span
                }
                else
                {
                    for (var i = 1; i <= 5; i++)
                    {
                        if (i != current)
                        {
                            writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                            writer.RenderBeginTag(HtmlTextWriterTag.Span);
                        }
                        writer.WriteInLine(i.ToString());
                        if (i != current) writer.RenderEndTag(); //Span
                    }
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.WriteInLine("...");
                    writer.RenderEndTag(); //Span

                    if (pages != current)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    }
                    writer.WriteInLine(pages.ToString());
                    if (pages != current) writer.RenderEndTag(); //span
                }
            }
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // div
        }


        internal override void ReadByDerivedClass(Tags tag, BinaryReader reader)
        {
            switch (tag)
            {
                case Tags.tgOLAPGrid_Layout:
                    if (AnalysisType == AnalysisType.Grid)
                    {
                        FLayout = new GridAxesLayout(this);
                        StreamUtils.ReadStreamedObject(reader, FLayout);
                    }
                    break;
            }

            base.ReadByDerivedClass(tag, reader);
        }
    }
}