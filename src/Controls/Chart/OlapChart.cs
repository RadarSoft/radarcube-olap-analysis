using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.ClientAgents;
using RadarSoft.RadarCube.Controls.Grid;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Events;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace RadarSoft.RadarCube.Controls.Chart
{
    public abstract class OlapChart : OlapGrid
    {
        internal ChartDefinitions chartDefinitions = new ChartDefinitions();

        internal OlapChart(HttpContext contextBase, IHostingEnvironment hosting, IMemoryCache cache)
            : base(contextBase, hosting, cache)
        {
        }

        public override AnalysisType AnalysisType
        {
            get => AnalysisType.Chart;
            set => throw new Exception(
                "The AnalysisType property is not available for change in the OlapChart control. To be able to change the mode of display data use the OlapAnalysis control.")
            ;
        }

        internal Tuple<double, double> Scale { get; set; } = new Tuple<double, double>(1D, 1D);

        internal override CellsetMode CellsetMode
        {
            get
            {
                if (AnalysisType == AnalysisType.Grid)
                    return CellsetMode.cmGrid;

                return CellsetMode.cmChart;
            }
        }

        protected override void CreateAxesLayout()
        {
            FLayout = new ChartAxesLayout(this);
        }

        internal override void RaiseCallback(string eventArgument, string data)
        {
            if (AnalysisType == AnalysisType.Grid)
            {
                base.RaiseCallback(eventArgument, data);
                return;
            }

            if (callbackException != null) return;
#if !DEBUG
            try
            {
#endif
            var args = eventArgument.Split('|');

            if (args[0] == "createpopup3")
            {
                legendId = args[1] + "|" + args[2];
                callbackData = CallbackData.Popup;
                return;
            }

            //if (args[0] == "filterselection")
            //{
            //    for (int i = 0; i < CellSet.FixedColumns; i++)
            //        for (int j = FSelection.Top; j < FSelection.Bottom; j++)
            //        {
            //            IMemberCell mc = CellSet.Cells(i, j) as IMemberCell;
            //            if ((mc != null) && (mc.Member != null) &&
            //                (mc.Member.MemberType != MemberType.mtMeasure) &&
            //                (mc.Member.MemberType != MemberType.mtMeasureMode) && (!mc.IsTotal) && (!mc.IsPager))
            //            {
            //                if (mc.ChildrenCount > 0)
            //                {
            //                    IMemberCell mc1 = mc.Children(0);
            //                    if (mc1.Member != null)
            //                    {
            //                        if ((mc1.Member.MemberType != MemberType.mtMeasure) && (mc1.Member.MemberType != MemberType.mtMeasureMode))
            //                            continue;
            //                    }
            //                }

            //                string s = mc.Member.Level.UniqueName + "|" + mc.Member.UniqueName;
            //                list.Add(s);
            //            }
            //        }

            //    for (int i = FSelection.Left; i < FSelection.Right; i++)
            //        for (int j = 0; j < CellSet.FixedRows; j++)
            //        {
            //            IMemberCell mc = CellSet.Cells(i, j) as IMemberCell;
            //            if ((mc != null) && (mc.Member != null) &&
            //                (mc.Member.MemberType != MemberType.mtMeasure) &&
            //                (mc.Member.MemberType != MemberType.mtMeasureMode) && (!mc.IsTotal) && (!mc.IsPager))
            //            {
            //                if (mc.ChildrenCount > 0)
            //                {
            //                    IMemberCell mc1 = mc.Children(0);
            //                    if (mc1.Member != null)
            //                    {
            //                        if ((mc1.Member.MemberType != MemberType.mtMeasure) && (mc1.Member.MemberType != MemberType.mtMeasureMode))
            //                            continue;
            //                    }
            //                }

            //                string s = mc.Member.Level.UniqueName + "|" + mc.Member.UniqueName;
            //                list.Add(s);
            //            }
            //        }

            //    ApplyChartMemberFilter(list.ToArray());
            //    return true;
            //}


            if (args[0] == "apllymemberfilter")
            {
                var filtersStr = args[1].Split('^').ToList().Select(x => x.Replace('=', '|')).ToArray();


                if (filtersStr.Length > 0)
                    ApplyChartMemberFilter(filtersStr);
                return;
            }

#if !DEBUG
            }
            catch (Exception E)
            {
                callbackException = E;
                callbackExceptionData = new Dictionary<string, string>(1);
                callbackExceptionData.Add("eventArgument", eventArgument);
                return;
            }
#endif
            base.RaiseCallback(eventArgument, data);
        }

        internal void ApplyChartMemberFilter(string[] members)
        {
            var ls = "";
            Level l = null;
            var hh = new List<Hierarchy>();
            foreach (var mm in members)
            {
                var args = mm.Split('|');
                if (args[0] != ls)
                {
                    ls = args[0];
                    l = Dimensions.FindLevel(ls);
                    if (!hh.Contains(l.Hierarchy))
                    {
                        l.Hierarchy.BeginUpdate();
                        l.Hierarchy.DoSetFilter(false);
                        hh.Add(l.Hierarchy);
                    }
                }

                var m = l.FindMember(args[1]);
                m.Visible = true;
            }

            foreach (var h in hh)
                h.EndUpdate();
            callbackData = CallbackData.PivotAndData;
        }

        //public MeasureGroup Pivoting(Measure Source, LayoutArea DestArea)
        //{
        //    return base.Pivoting(Source, DestArea, null, null);
        //}

        public MeasureGroup Pivoting(Measure Source, LayoutArea DestArea, MeasureGroup destMeasureGroup)
        {
            return base.Pivoting(Source, DestArea, destMeasureGroup, null);
        }

        public new void PivotingOut(Measure measure, LayoutArea from)
        {
            base.PivotingOut(measure, true, from);
        }

        /// <summary>
        ///     setting the initial chart magnification
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        public void SetScale(double X, double Y)
        {
            Scale = Tuple.Create(X, Y);
        }

        internal override CellSet.CellSet CreateCellset()
        {
            if (AnalysisType == AnalysisType.Grid)
                return base.CreateCellset();

            return new ChartCellSet(this);
        }

        internal override void InitClientCellset(RCellset rcellset)
        {
            if (AnalysisType == AnalysisType.Grid)
            {
                base.InitClientCellset(rcellset);
                return;
            }

            var ccs = CellSet as ChartCellSet;
            if (ccs == null) return;

            if (ccs.SizeAxisDescriptor != null && ccs.SizeAxisDescriptor.Axis != null)
                rcellset.SizeAxis = ccs.SizeAxisDescriptor.Axis.CreateChartAxis();

            if (ccs.ColorAxisDescriptor != null && ccs.ColorAxisDescriptor.Axis != null)
                rcellset.ColorAxis = ccs.ColorAxisDescriptor.Axis.CreateChartAxis();

            if (ccs.ShapeAxisDescriptor != null && ccs.ShapeAxisDescriptor.Axis != null)
                rcellset.ShapeAxis = ccs.ShapeAxisDescriptor.Axis.CreateChartAxis();

            if (ccs.XAxisDescriptor != null && ccs.XAxisDescriptor.Axis != null)
                rcellset.XAxis = ccs.XAxisDescriptor.Axis.CreateChartAxis();

            if (ccs.YAxesDescriptor != null && ccs.YAxesDescriptor.ChartAreas.Count > 0)
                rcellset.YAxes = ccs.YAxesDescriptor.ChartAreas.Select(item => item.Axis.CreateChartAxis()).ToArray();

            if (ccs.fRowChartMembers != null)
                rcellset.RowChartMembers = ccs.fRowChartMembers.Select(
                    item => item.Select(
                        item2 => new ClientMember(item2)).ToList()).ToList();

            if (ccs.fColumnChartMembers != null)
                rcellset.ColumnChartMembers = ccs.fColumnChartMembers.Select(
                    item => item.Select(
                        item2 => new ClientMember(item2)).ToList()).ToList();

            if (ccs.fColorChartMembers != null)
                rcellset.ColorChartMembers = ccs.fColorChartMembers.Select(item => new ClientMember(item)).ToArray();

            if (ccs.fSizeChartMembers != null)
                rcellset.SizeChartMembers = ccs.fSizeChartMembers.Select(item => new ClientMember(item)).ToArray();

            if (ccs.fShapeChartMembers != null)
                rcellset.ShapeChartMembers = ccs.fShapeChartMembers.Select(item => new ClientMember(item)).ToArray();

            var members2 = new Dictionary<string, string>();
            if (AxesLayout.DetailsAxis.Count > 0)
                foreach (var h in AxesLayout.DetailsAxis)
                foreach (var m in h.FirstVisibleLevel().Members)
                    if (!members2.ContainsKey(m.UniqueName))
                        members2.Add(m.UniqueName, m.DisplayName);

            foreach (var l in AxesLayout.fColumnLevels)
            foreach (var m in l.Members)
                if (!members2.ContainsKey(m.UniqueName))
                    members2.Add(m.UniqueName, m.DisplayName);
            foreach (var l in AxesLayout.fRowLevels)
            foreach (var m in l.Members)
                if (!members2.ContainsKey(m.UniqueName))
                    members2.Add(m.UniqueName, m.DisplayName);

            if (members2.Count > 0)
            {
                rcellset.Members2UniqueNames = members2.Keys.ToArray();
                rcellset.Members2DisplayNames = members2.Values.ToArray();
            }
        }

        internal override void MakeLegendMenu(string member)
        {
            var mi = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                RadarUtils.GetResStr("repHideMeasure"), ImageUrl("HideThis.gif"),
                "hidethis2|" + member);
            GenericMnu.Add(mi);

            mi = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                RadarUtils.GetResStr("repHideExcept"), ImageUrl("HideExcept.gif"),
                "hideexcept2|" + member);
            GenericMnu.Add(mi);

            ConvertGenericMenu(GenericMnu, null);
        }

        protected override void MakePivotMenu(IDescriptionable dim)
        {
            if (AnalysisType == AnalysisType.Grid)
            {
                base.MakePivotMenu(dim);
                return;
            }

            var h = dim as Hierarchy;
            GenericMenuItem MI = null;
            if (h != null)
            {
                var hplace = LayoutArea.laTree;

                if (AxesLayout.fColumnAxis.Contains(h)) hplace = LayoutArea.laColumn;
                if (AxesLayout.fRowAxis.Contains(h)) hplace = LayoutArea.laRow;

                if (hplace != LayoutArea.laColumn)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToCol"), ImageUrl("PivotRow.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laColumn + "|999");
                    GenericMnu.Add(MI);
                }

                if (hplace != LayoutArea.laRow)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToRow"), ImageUrl("PivotColumn.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laRow + "|999");
                    GenericMnu.Add(MI);
                }

                if (AxesLayout.fColorAxisItem != h)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToColorChanger"), ImageUrl("PivotColor.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laColor + "|999");
                    GenericMnu.Add(MI);
                }

                if (AxesLayout.fSizeAxisItem != h)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToSizeChanger"), ImageUrl("PivotSize.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laSize + "|999");
                    GenericMnu.Add(MI);
                }

                if (AxesLayout.fShapeAxisItem != h)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToShapeChanger"), ImageUrl("PivotShape.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laShape + "|999");
                    GenericMnu.Add(MI);
                }

                if (AxesLayout.fDetailsAxis.Contains(h) == false)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToDetailsChanger"), ImageUrl("PivotDetails.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laDetails + "|999");
                    GenericMnu.Add(MI);
                }

                if (hplace != LayoutArea.laTree
                    || AxesLayout.fColorAxisItem == h || AxesLayout.fSizeAxisItem == h || AxesLayout.fShapeAxisItem == h
                    || AxesLayout.fDetailsAxis.Contains(h))
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repRemoveToTree"), ImageUrl("DeleteGroup.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laTree + "|999");
                    GenericMnu.Add(MI);
                }


                if (h.AllowFilter && h.AllowHierarchyEditor && AllowFiltering)
                {
                    GenericMnu.AddSeparator();
                    MI = new GenericMenuItem(GenericMenuActionType.ExecuteFunction,
                        RadarUtils.GetResStr("rsFilter"), ImageUrl("filtr_edit.gif"),
                        "filterDialog('h:" + h.UniqueName + "')");
                    GenericMnu.Add(MI);
                }
            }

            var m = dim as Measure;
            if (m != null)
            {
                var showDeleteItem = false;

                if (m.Visible == false)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("rep_MoveMeasureToValues"), ImageUrl("PivotRow.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laRow + "|999");
                    GenericMnu.Add(MI);
                }
                else
                {
                    showDeleteItem = true;
                }

                if (AxesLayout.fXAxisMeasure != dim)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToCol"), ImageUrl("PivotColumn.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laColumn + "|999");
                    GenericMnu.Add(MI);
                }
                else
                {
                    showDeleteItem = true;
                }

                if (AxesLayout.fColorAxisItem != dim)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToColorChanger"), ImageUrl("PivotColor.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laColor + "|999");
                    GenericMnu.Add(MI);
                }
                else
                {
                    showDeleteItem = true;
                }

                if (AxesLayout.SizeAxisItem != dim)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToSizeChanger"), ImageUrl("PivotSize.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laSize + "|999");
                    GenericMnu.Add(MI);
                }
                else
                {
                    showDeleteItem = true;
                }

                if (showDeleteItem)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repHideMeasure"), ImageUrl("DeleteGroup.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laTree + "|999|laRow");
                    GenericMnu.Add(MI);
                }

                if (AllowFiltering)
                {
                    GenericMnu.AddSeparator();

                    MI = new GenericMenuItem(GenericMenuActionType.ExecuteFunction,
                        RadarUtils.GetResStr("rsFilter"), ImageUrl("filtr_edit.gif"),
                        "filterDialog('m:" + m.UniqueName + "')");
                    GenericMnu.Add(MI);
                }
            }

            ConvertGenericMenu(GenericMnu, mnu_control);
        }

        internal override void InitChartAreas(RCell rcell, ICell cell)
        {
            if (AnalysisType == AnalysisType.Grid)
            {
                base.InitChartAreas(rcell, cell);
                return;
            }

            if (cell is IChartCell)
            {
                var cc = (IChartCell) cell;
                rcell.ChartAreas = cc.Charts.Select(item => item.CreateChartArea()).ToArray();
            }
        }

        public override void RenderBeginTag(HtmlTextWriter writer)
        {
#if EVAL
            if (!IsMvc && AnalysisType == AnalysisType.Chart)
                writer.Write("RadarCube evaluation version. HTML OLAP Chart version is: " + typeof(OlapAnalysis).Assembly.GetName().Version.ToString() + 
                ". Click <a href=\"https://www.radar-soft.com/products/radarcube-asp-net-web-forms/pricing\" target=\"_blank\">here</a> to purchase the full version.");
#endif
            base.RenderBeginTag(writer);
        }

        internal override void RenderLegends(HtmlTextWriter writer)
        {
            if (AnalysisType == AnalysisType.Grid)
            {
                base.RenderLegends(writer);
                return;
            }

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

        protected override void RenderPivot(HtmlTextWriter writer)
        {
            if (AnalysisType == AnalysisType.Grid)
            {
                base.RenderPivot(writer);
                return;
            }

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
                "rs_droptarget rs_droptarget2 rc_pivotarea_leftdivider ui-accordion ui-widget-content");
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

        protected override void RenderInternalGrid(HtmlTextWriter writer)
        {
            if (Cube == null)
            {
                writer.Write("&nbsp;");
                return;
            }

            if (CellSet == null)
            {
                if (!Cube.Active)
                    writer.Write(RadarUtils.GetResStr("rsInactiveCubeWarning"));
                return;
            }

            if (DelayPivoting)
            {
                writer.Write(RadarUtils.GetResStr("rsDelayPivoting"));
                return;
            }

            if (AnalysisType == AnalysisType.Grid)
            {
                base.RenderInternalGrid(writer);
                return;
            }

            //if (IsCanvasEnable == false)
            //{
            //    writer.Write("Канвас не поддерживается!!!");//RadarUtils.GetResStr("rsInactiveCubeWarning"));
            //    return;
            //}

            writer.AddStyleAttribute(HtmlTextWriterStyle.VerticalAlign, "top");
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "50");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            //if (UseFixedHeaders)
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
                        if ((c as MemberCell).Model.Rendered) continue;
                        (c as MemberCell).Model.Rendered = true;
                    }
                    else if (c.StartColumn != j || c.StartRow != i)
                    {
                        continue;
                    }
                    altflag[j] = !altflag[j];
                    if (c.ColSpan > 1)
                        writer.AddAttribute(HtmlTextWriterAttribute.Colspan, c.ColSpan.ToString());
                    if (c.RowSpan > 1)
                        writer.AddAttribute(HtmlTextWriterAttribute.Rowspan, c.RowSpan.ToString());
                    RenderCellEventArgs e = null;

                    HandleOnRenderCell(c);

                    switch (c.CellType)
                    {
                        case CellType.ctLevel:
                            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_levelcell ui-widget-header");
                            break;
                        case CellType.ctMember:
                        case CellType.ctNone:
                            var mc = c as IMemberCell;
                            if (mc != null && (mc.IsTotal || ((MemberCell) mc).IsTotal))
                                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                    "rc_membercell rc_membercell_total ui-widget-content ui-state-default");
                            else
                                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                    "rc_membercell ui-widget-content ui-state-default");
                            break;
                        case CellType.ctData:
                            var dc = c as IDataCell;
                            if (dc != null && dc.IsTotal)
                                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                    "rc_datacell rc_datacell_total ui-widget-content");
                            else
                                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_datacell ui-widget-content");
                            break;
                    }

                    writer.AddAttribute("cellid", (FCellSet.ColumnCount * i + j).ToString());
                    writer.AddAttribute("id", "cell_" + (FCellSet.ColumnCount * i + j));
                    //if (c is IMemberCell)
                    //{
                    //    IMemberCell mc = (IMemberCell)c;
                    //    if ((mc.Area == LayoutArea.laColumn) && (mc.ChildrenCount == 0) && (!mc.IsPager) && (fMode == OlapGridMode.gmStandard))
                    //        writer.AddAttribute("valuesort", j.ToString());
                    //}

                    if (i == 0 && j < CellSet.FixedColumns)
                        writer.AddAttribute("fixedcol", "1");
                    if (j == 0 && i < CellSet.FixedRows)
                        writer.AddAttribute("fixedrow", "1");
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

                            var actions = lc.PossibleDrillActions;

                            if (AllowDrilling)
                            {
                                if ((actions & PossibleDrillActions.esCollapsed) == PossibleDrillActions.esCollapsed)
                                {
                                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                    var _lc = lc as LevelCell;
                                    var a = _lc.PossibleDrillActions;
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

                                    writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                        RadarUtils.GetResStr("hint_CollapseCell"));
                                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                    writer.RenderEndTag(); //span
                                    writer.RenderEndTag(); //td
                                }
                                if ((actions & PossibleDrillActions.esNextHierarchy) ==
                                    PossibleDrillActions.esNextHierarchy)
                                {
                                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                    writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                        RadarUtils.GetResStr("hint_DrillNextHierarchy"));
                                    writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                        "rs_nexthier ui-icon ui-icon-plus");

                                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                    writer.RenderEndTag(); //span
                                    writer.RenderEndTag(); //td
                                }
                                if ((actions & PossibleDrillActions.esNextLevel) == PossibleDrillActions.esNextLevel)
                                {
                                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                    writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                        RadarUtils.GetResStr("hint_DrillNextLevel"));
                                    writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                        "rs_nextlevel ui-icon ui-icon-triangle-1-e");
                                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                    writer.RenderEndTag(); //span
                                    writer.RenderEndTag(); //td
                                }
                                if ((actions & PossibleDrillActions.esParentChild) ==
                                    PossibleDrillActions.esParentChild)
                                {
                                    writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                    writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                        RadarUtils.GetResStr("hint_DrillParentChild"));
                                    writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                        "rs_parentchild ui-icon ui-icon-caret-1-e");
                                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                    writer.RenderEndTag(); //span
                                    writer.RenderEndTag(); //td
                                }
                            }

                            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                            writer.RenderBeginTag(HtmlTextWriterTag.Td);
                            writer.Write(HTMLPrepare(e == null ? c.Value : e.Text));
                            writer.RenderEndTag(); //td

                            if (lc.Level.FHierarchy.AllowFilter && lc.Level.FHierarchy.AllowHierarchyEditor &&
                                AllowFiltering)
                            {
                                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                    RadarUtils.GetResStr("hint_ClickTopEditMeasFilter"));
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
                                    writer.RenderBeginTag(HtmlTextWriterTag.Td);

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
                                    {
                                        var _mc = mc as MemberCell;
                                        var a = _mc.PossibleDrillActions;
                                        if ((a & PossibleDrillActions.esNextHierarchy) ==
                                            PossibleDrillActions.esNextHierarchy)
                                            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                                                "rs_collapse ui-icon ui-icon-minus");
                                        else if ((a & PossibleDrillActions.esNextLevel) ==
                                                 PossibleDrillActions.esNextLevel)
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
                                }
                                writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                    RadarUtils.GetResStr("hint_CollapseCell"));
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.RenderEndTag(); //span

                                if (mc.Area != LayoutArea.laRow)
                                    writer.RenderEndTag(); //td
                            }
                            if ((actions & PossibleDrillActions.esNextHierarchy) ==
                                PossibleDrillActions.esNextHierarchy)
                            {
                                if (mc.Area != LayoutArea.laRow) writer.RenderBeginTag(HtmlTextWriterTag.Td);
                                writer.AddAttribute(HtmlTextWriterAttribute.Title,
                                    RadarUtils.GetResStr("hint_DrillNextHierarchy"));
                                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_nexthier ui-icon ui-icon-plus");
                                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                writer.RenderEndTag(); //span
                                if (mc.Area != LayoutArea.laRow) writer.RenderEndTag(); //td
                            }
                            if ((actions & PossibleDrillActions.esNextLevel) == PossibleDrillActions.esNextLevel)
                            {
                                if (mc.Area != LayoutArea.laRow) writer.RenderBeginTag(HtmlTextWriterTag.Td);
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
                                if (mc.Area != LayoutArea.laRow) writer.RenderBeginTag(HtmlTextWriterTag.Td);
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
                            if (mc.Area != LayoutArea.laRow) writer.RenderBeginTag(HtmlTextWriterTag.Td);
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
#if WEBFORM
                        if ((mc.Area == TLayoutArea.laColumn) && (mc.ChildrenCount == 0) && (FCellSet.FValueSortedColumn == j))
                        {
                            writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "center");
                            writer.RenderBeginTag(HtmlTextWriterTag.Div);
                            string sortimgname =
                                (FCellSet.FSortingDirection == TValueSortingDirection.sdAscending) ?
                                "Menu_ScrollUp.gif" : "Menu_ScrollDown.gif";
                            writer.AddAttribute(HtmlTextWriterAttribute.Width, "15");
                            writer.AddAttribute(HtmlTextWriterAttribute.Height, "15");
                            writer.AddAttribute(HtmlTextWriterAttribute.Src,
                                ResolveUrl(Page.ClientScript.GetWebResourceUrl(typeof(TreeView),
                                sortimgname)));
                            writer.RenderBeginTag(HtmlTextWriterTag.Img);
                            writer.RenderEndTag(); //img
                            writer.RenderEndTag(); //div
                        }
#endif
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
            if (FCellSet.RowCount == FCellSet.FFixedRows) //&& UseFixedHeaders)
                writer.RenderEndTag(); //thead
            writer.RenderEndTag(); // table

            writer.AddAttribute(HtmlTextWriterAttribute.Style, "display: none");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_levelcell");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "serviceDiv");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderEndTag(); //img
        }

        internal override void RenderModifiers(HtmlTextWriter writer)
        {
            if (AnalysisType == AnalysisType.Grid)
            {
                base.RenderModifiers(writer);
                return;
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            //Color modifier
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_colorsareaheader");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_pivotheader ui-widget-header rc_pivotheader_leftdivider");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("rsColorArea"));
            writer.RenderEndTag(); // span
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            //pivotAreaStyle.AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rs_droptarget rs_droptarget2 rc_pivotarea_leftdivider ui-accordion ui-widget-content");
            writer.AddAttribute("area", "colors");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_colorsarea");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            RenderColorsArea(writer);
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr

            //Size modifier
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_sizeareaheader");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_pivotheader ui-widget-header rc_pivotheader_leftdivider");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("rsSizeArea"));
            writer.RenderEndTag(); // span
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            //pivotAreaStyle.AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rs_droptarget rs_droptarget2 rc_pivotarea_leftdivider ui-accordion ui-widget-content");
            writer.AddAttribute("area", "size");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_sizearea");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            RenderSizeArea(writer);
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr

            //Shape modifier
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_shapeareaheader");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_pivotheader ui-widget-header rc_pivotheader_leftdivider");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("rsShapeArea"));
            writer.RenderEndTag(); // span
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            //pivotAreaStyle.AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rs_droptarget rs_droptarget2 rc_pivotarea_leftdivider ui-accordion ui-widget-content");
            writer.AddAttribute("area", "shape");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_shapearea");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            RenderShapeArea(writer);
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr

            //Details modifier
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_detailareaheader");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_pivotheader ui-widget-header rc_pivotheader_leftdivider");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("rsDetailsArea"));
            writer.RenderEndTag(); // span
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            //pivotAreaStyle.AddAttributesToRender(writer);
            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rs_droptarget rs_droptarget2 rc_pivotarea_leftdivider ui-accordion ui-widget-content");
            writer.AddAttribute("area", "detail");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "pivot_detailarea");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            RenderDetailsArea(writer);
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr


            writer.RenderEndTag(); // table
        }

        internal override void RenderColorsArea(HtmlTextWriter writer)
        {
            if (AnalysisType == AnalysisType.Grid)
            {
                base.RenderColorsArea(writer);
                return;
            }

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

        protected void RenderSizeArea(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            if (AxesLayout.fSizeAxisItem == null)
                writer.AddStyleAttribute("height", "50px");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            if (AxesLayout.fSizeAxisItem is Hierarchy)
            {
                var list = new List<Hierarchy>();
                list.Add((Hierarchy) AxesLayout.fSizeAxisItem);
                RenderPivotPanels(writer, list, "size");
            }
            else if (AxesLayout.fSizeAxisItem is Measure)
            {
                var list = new List<Measure>();
                list.Add((Measure) AxesLayout.fSizeAxisItem);
                RenderPivotPanels(writer, list, "size");
            }

            writer.RenderEndTag(); // table
        }

        protected void RenderDetailsArea(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            if (AxesLayout.fDetailsAxis.Count == 0)
                writer.AddStyleAttribute("height", "50px");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            if (AxesLayout.fDetailsAxis.Count > 0)
                RenderPivotPanels(writer, AxesLayout.fDetailsAxis, "shape");

            writer.RenderEndTag(); // table
        }

        internal override void RenderPivotPanels(HtmlTextWriter writer, List<Measure> list, string area)
        {
            if (AnalysisType == AnalysisType.Grid)
            {
                base.RenderPivotPanels(writer, list, area);
                return;
            }

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

        protected void RenderShapeArea(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            if (AxesLayout.fShapeAxisItem == null)
                writer.AddStyleAttribute("height", "50px");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            if (AxesLayout.fShapeAxisItem is Hierarchy)
            {
                var list = new List<Hierarchy>();
                list.Add((Hierarchy) AxesLayout.fShapeAxisItem);
                RenderPivotPanels(writer, list, "shape");
            }

            writer.RenderEndTag(); // table
        }

        protected override void RenderMeasure(HtmlTextWriter writer)
        {
            if (AnalysisType == AnalysisType.Grid)
            {
                base.RenderMeasure(writer);
                return;
            }

            var groupIndex = -1;
            foreach (var mg in AxesLayout.fYAxisMeasures)
            {
                groupIndex++;
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.AddAttribute("areaindex", groupIndex.ToString());
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_droptarget2 rs_pivotarea_measure");
                writer.AddAttribute("drag", "true");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.AddAttribute(HtmlTextWriterAttribute.Width, "auto");
                writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                writer.AddAttribute(HtmlTextWriterAttribute.Class,
                    "rc_pivotpanel ui-accordion-header ui-state-default ui-accordion-icons ui-corner-all");
                writer.RenderBeginTag(HtmlTextWriterTag.Table);
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                foreach (var m in mg)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_pivotpanel");
                    writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                    writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
                    writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
                    writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
                    writer.AddAttribute("uid", m.UniqueName);
                    writer.RenderBeginTag(HtmlTextWriterTag.Table);
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                    writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                    var title = "";
                    var text = "";
                    var args = new RenderPivotArgs(m, m.DisplayName);
                    if (RiseOnRenderPivotPanel(args))
                    {
                        title = args.Text;
                        if (RadarUtils.CutText(args.Text, MaxTextLength, out text))
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
                }

                writer.RenderEndTag(); // td
                writer.RenderEndTag(); // tr
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.AddStyleAttribute("word-wrap", "break-word");

                writer.AddAttribute(HtmlTextWriterAttribute.Style,
                    "font-style: italic; text-align: center; word-wrap: break-word; " +
                    "font-size: 8pt; padding-top: 2px; padding-right: 2px; " +
                    "padding-bottom: 2px; padding-left: 2px;");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write(RadarUtils.GetResStr("rsDragMeasure"));
                writer.RenderEndTag(); // td
                writer.RenderEndTag(); // tr
                writer.RenderEndTag(); //table

                writer.RenderEndTag(); //td
                writer.RenderEndTag(); //tr
            }
        }

        internal override void ReadByDerivedClass(Tags tag, BinaryReader reader)
        {
            switch (tag)
            {
                case Tags.tgOLAPGrid_Layout:
                    if (AnalysisType == AnalysisType.Chart)
                    {
                        FLayout = new ChartAxesLayout(this);
                        StreamUtils.ReadStreamedObject(reader, FLayout);
                    }
                    break;
            }

            base.ReadByDerivedClass(tag, reader);
        }
    }
}