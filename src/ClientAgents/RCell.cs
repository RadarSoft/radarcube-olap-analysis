using System;
using System.ComponentModel;
using System.Drawing;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class RCell
    {
        [DefaultValue(null)] public string BackImageUrl;

        [DefaultValue(-1)] public int CellIndex;

        [DefaultValue(CellType.ctData)] public CellType CellType;

        [DefaultValue(null)] public ClientChartArea[] ChartAreas = null;

        [DefaultValue(0)] public int Col;

        [DefaultValue(-1)] public double ColorIndex = -1;

        [DefaultValue(1)] public int ColSpan;

        [DefaultValue("")] public string Comment = "";

        [DefaultValue(null)] public object Data;

        [DefaultValue(0)] public int DrillActions;

        [DefaultValue("")] public string Expression = "";

        [DefaultValue(null)] public string FontFamily;

        [DefaultValue(-1)] public int FontSize;

        [DefaultValue(null)] public string ForeColor;

        [DefaultValue("")] public string Format = "";

        [DefaultValue(HAlignment.NotSet)] public HAlignment HAlign;

        [DefaultValue(null)] public ImagePosition ImagePosition;

        [DefaultValue(0)] public byte Indent;

        [DefaultValue(false)] public bool IsBold;

        [DefaultValue(false)] public bool IsItalic;

        [DefaultValue(false)] public bool IsStrikeout;

        [DefaultValue(false)] public bool IsTotal;

        [DefaultValue(false)] public bool IsUnderline;

        [DefaultValue(-1)] public int KPIStatusImageIndex = -1;

        [DefaultValue(-1)] public int KPITrendImageIndex = -1;

        [DefaultValue(null)] public RCellLevel LevelData;

        [DefaultValue(30)] public int MaxTextLength;

        [DefaultValue(null)] public ClientMeasure Measure;

        [DefaultValue("")] public string MeasureMode = "";

        [DefaultValue("")] public string MemberBackground = "";

        [DefaultValue(null)] public RCellMember MemberData;

        [DefaultValue("")] public string MemberForeground = "";

        [DefaultValue(-1)] public double Rank = -1;

        [DefaultValue(-1)] public double RankBackground = -1;

        [DefaultValue(-1)] public double RankForeground = -1;

        [DefaultValue(0)] public int Row;

        [DefaultValue(1)] public int RowSpan;

        public string Text; // text

        [DefaultValue(null)] public string Tooltip;

        [DefaultValue("")] public string UniqueName = "";

        [DefaultValue(VAlignment.NotSet)] public VAlignment VAlign;

        public RCell()
        {
            CellType = CellType.ctData;
            RowSpan = 1;
            ColSpan = 1;
            Row = 0;
            Col = 0;
            HAlign = HAlignment.NotSet;
            VAlign = VAlignment.NotSet;
            FontSize = -1;
            IsItalic = false;
            IsBold = false;
            IsUnderline = false;
            IsStrikeout = false;
            IsTotal = false;
            DrillActions = 0;
            CellIndex = -1;
        }

        public RCell(ICell cell)
            : this()
        {
            if (cell.CellType == CellType.ctData)
                if (((IDataCell) cell).Address != null)
                    if (((IDataCell) cell).Address.Measure != null)
                        Measure = new ClientMeasure(((IDataCell) cell).Address.Measure);

            Row = cell.PagedStartRow;
            Col = cell.PagedStartColumn;
            RowSpan = cell.RowSpan;
            ColSpan = cell.ColSpan;
            CellType = cell.CellType;
            CellIndex = cell.CellSet.ColumnCount * cell.StartRow + cell.StartColumn;
            Tooltip = cell.Description;

            Text = cell.Value;

            if (cell is IDataCell)
            {
                var dc = (IDataCell) cell;
                Comment = dc.Comment;
                IsTotal = dc.IsTotal;

                if (!string.IsNullOrEmpty(dc.FontFamily))
                    FontFamily = dc.FontFamily;
                if (dc.FontSize.HasValue)
                    FontSize = Convert.ToInt32(dc.FontSize.Value);
                if (dc.FontStyle.HasValue)
                {
                    IsBold = (dc.FontStyle.Value & FontStyle.Bold) == FontStyle.Bold;
                    IsItalic = (dc.FontStyle.Value & FontStyle.Italic) == FontStyle.Italic;
                    IsUnderline = (dc.FontStyle.Value & FontStyle.Underline) == FontStyle.Underline;
                    IsStrikeout = (dc.FontStyle.Value & FontStyle.Strikeout) == FontStyle.Strikeout;
                }
                if (dc.Data != null)
                {
                    Data = dc.Data;
                    //MeasureMode = (dc.Address ==  null) ?  "" : dc.Address.MeasureMode.Mode.fUniqueName.ToString();
                    MeasureMode = dc.Address == null ? "" : dc.Address.MeasureMode.Mode.ToString();

                    if (dc.Address != null && dc.Address.Measure != null && dc.Address.Measure.IsKPI
                        && dc.Address.Measure.CubeMeasure != null
                        && dc.Address.Measure.CubeMeasure.KPIStatusImageIndex != -1)
                        KPIStatusImageIndex = dc.Address.Measure.CubeMeasure.KPIStatusImageIndex;

                    if (dc.Address != null && dc.Address.Measure != null && dc.Address.Measure.IsKPI
                        && dc.Address.Measure.CubeMeasure != null
                        && dc.Address.Measure.CubeMeasure.KPITrendImageIndex != -1)
                        KPITrendImageIndex = dc.Address.Measure.CubeMeasure.KPITrendImageIndex;

                    if (dc.Address != null)
                    {
                        var rng = cell.CellSet.Grid.GetMeasureRange(dc.Address);
                        if (rng != null)
                        {
                            var d = -1.0;

                            if (dc.Address.MeasureMode.Mode == MeasureShowModeType.smPercentRowTotal
                                || dc.Address.MeasureMode.Mode == MeasureShowModeType.smPercentColTotal
                                || dc.Address.MeasureMode.Mode == MeasureShowModeType.smPercentGrandTotal
                                || dc.Address.MeasureMode.Mode == MeasureShowModeType.smPercentParentColItem
                                || dc.Address.MeasureMode.Mode == MeasureShowModeType.smPercentParentRowItem)
                            {
                                object o = null;
                                var l = cell.CellSet.Grid.FEngine.GetMetaline(dc.Address.FLineID)
                                    .GetLine(dc.Address.FHierID, dc.Address.Measure, dc.Address.MeasureMode);

                                if (l != null)
                                    l.GetCell(dc.Address, out o);

                                if (o != null)
                                    d = double.Parse(o.ToString());
                            }

                            if (rng.Item1 == rng.Item2)
                                Rank = 1;
                            else
                                try
                                {
                                    if (dc.Address.MeasureMode.Mode == MeasureShowModeType.smPercentRowTotal
                                        || dc.Address.MeasureMode.Mode == MeasureShowModeType.smPercentColTotal
                                        || dc.Address.MeasureMode.Mode == MeasureShowModeType.smPercentGrandTotal
                                        || dc.Address.MeasureMode.Mode == MeasureShowModeType.smPercentParentColItem
                                        || dc.Address.MeasureMode.Mode == MeasureShowModeType.smPercentParentRowItem)
                                    {
                                        if (d != -1 && !double.IsNaN(d))
                                        {
                                            var val = d;
                                            Rank = (val - rng.Item1) / (rng.Item2 - rng.Item1);
                                        }
                                    }
                                    else
                                    {
                                        var val = Convert.ToDouble(dc.Data);
                                        Rank = (val - rng.Item1) / (rng.Item2 - rng.Item1);
                                    }
                                }
                                catch
                                {
                                    ;
                                }
                        }
                    }
                }

                RankBackground = ((DataCell) dc).RankBackground;
                RankForeground = ((DataCell) dc).RankForeground;
                MemberBackground = ((DataCell) dc).MemberBackground;
                MemberForeground = ((DataCell) dc).MemberForeground;
            }
            if (cell is IMemberCell)
            {
                var mc = (IMemberCell) cell;
                Indent = mc.Indent;
                Comment = mc.Comment;
                MemberData = new RCellMember(mc);
                IsTotal = mc.IsTotal;
                DrillActions = (int) mc.PossibleDrillActions;
                if (mc.Member != null)
                    UniqueName = mc.Member.UniqueName;
                if (mc.Member is CalculatedMember)
                {
                    var cm = (CalculatedMember) mc.Member;
                    if (!string.IsNullOrEmpty(cm.Expression))
                        Expression = cm.Expression;
                }
                if (mc.IsPager)
                {
                    var MC = mc as MemberCell;
                    Text = RadarUtils.GetResStr("rsPages");
                }
                if (mc.Member != null && mc.Member.MemberType == MemberType.mtMeasure)
                {
                    var m = cell.CellSet.Grid.Measures.Find(mc.Member.UniqueName);
                    if (m.AggregateFunction == OlapFunction.stCalculated)
                    {
                        Expression = m.Expression;
                        Format = m.DefaultFormat;
                    }
                }
            }
            if (cell is ILevelCell)
            {
                var lc = (ILevelCell) cell;
                Indent = lc.Indent;
                DrillActions = (int) lc.PossibleDrillActions;
                LevelData = new RCellLevel(lc);
                if (lc.Level != null) UniqueName = lc.Level.UniqueName;
            }

            cell.CellSet.Grid.InitChartAreas(this, cell);
        }
    }
}