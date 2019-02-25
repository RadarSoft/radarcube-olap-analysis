using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CellSet
{
    internal class DataCell : IDataCell, ICommentable
    {
        private readonly CellFormattingProperties _fFormattingProperties = new CellFormattingProperties();
        private readonly object _fValue;

        private List<CubeAction> fActions;
        internal string MemberBackground = "";
        internal string MemberForeground = "";

        internal double RankBackground = -1;
        internal double RankForeground = -1;

        internal DataCell(CellSet acs, int aRow, int aCol)
        {
            CellSet = acs;
            StartRow = aRow;
            StartColumn = aCol;

#if DEBUG
            if (StartColumn == 1 && StartRow == 21)
            {
            }
#endif

            var rm = acs.FRowMembersArray[acs.FFixedColumns - 1 + aRow * acs.FFixedColumns] as CellsetMember;
            if (rm != null && rm.FIsPager)
            {
                Address = new ICubeAddress(acs.FGrid);
                return;
            }
            var cm = acs.FColMembersArray[aCol + (acs.FFixedRows - 1) * acs.ColumnCount] as CellsetMember;
            if (cm != null && cm.FIsPager)
            {
                Address = new ICubeAddress(acs.FGrid);
                return;
            }
            var memberlist = new List<Member>();

            Hierarchy H = null;
            var C = rm;
            var mid = -1;
            while (C != null)
            {
                IsTotal |= C.FIsTotal;
                IsTotalHorizontal |= C.FIsTotal;
                if (C.FMember != null)
                    if (C.FMember.MemberType == MemberType.mtMeasure)
                    {
                        mid = C.FMember.ID;
                    }
                    else
                    {
                        if (C.FMember.MemberType == MemberType.mtMeasureMode)
                        {
                            memberlist.Add(C.FMember);
                        }
                        else if (C.FMember.FLevel.FHierarchy != H)
                        {
                            memberlist.Add(C.FMember);
                            H = C.FMember.FLevel.FHierarchy;
                        }
                        ;
                    }
                C = C.FParent;
            }
            C = cm;
            while (C != null)
            {
                IsTotal |= C.FIsTotal;
                IsTotalVertical |= C.FIsTotal;
                if (C.FMember != null)
                    if (C.FMember.MemberType == MemberType.mtMeasure)
                    {
                        mid = C.FMember.ID;
                    }
                    else
                    {
                        if (C.FMember.MemberType == MemberType.mtMeasureMode)
                        {
                            memberlist.Add(C.FMember);
                        }
                        else if (C.FMember.FLevel.FHierarchy != H)
                        {
                            memberlist.Add(C.FMember);
                            H = C.FMember.FLevel.FHierarchy;
                        }
                        ;
                    }
                C = C.FParent;
            }
#if DEBUG
            if (IsTotal)
            {
            }
#endif
            switch (acs.FGrid.Mode)
            {
                case OlapGridMode.gmStandard:
                    Address = acs.FGrid.FEngine.CreateCubeAddress(memberlist);
                    if (mid >= 0)
                    {
                        Address.Measure = acs.Grid.fMeasures[mid];
                    }
                    else
                    {
                        if (rm != null && rm.FMeasureID != string.Empty)
                            Address.Measure = acs.Grid.fMeasures[rm.FMeasureID];
                        if (cm != null && cm.FMeasureID != string.Empty)
                            Address.Measure = acs.Grid.fMeasures[cm.FMeasureID];
                    }

                    if (Address.Measure != null)
                    {
                        var m = Address.Measure.DefaultMode;
                        if (m != null)
                            Address.MeasureMode = m;
                    }

                    acs.FGrid.FEngine.GetCellFormattedValue(this, out _fValue, out _fFormattingProperties);
                    var olapgrid = acs.FGrid;
                    if (olapgrid != null &&
                        olapgrid.AxesLayout.ColorBackAxisItem != null &&
                        olapgrid.AxesLayout.ColorBackAxisItem is Measure)
                    {
                        var aOld = Address;

                        var aTmp = acs.FGrid.FEngine.CreateCubeAddress(memberlist);
                        Address = aTmp;

                        var mcolor = olapgrid.AxesLayout.ColorBackAxisItem as Measure;
                        Address.Measure = mcolor;
                        object backvalue;
                        CellFormattingProperties backformat;
                        if (acs.FGrid.FEngine.GetCellFormattedValue(this, out backvalue, out backformat))
                            RankBackground = GetColorRank(backvalue);
                        Address = aOld;
                    }

                    if (olapgrid != null &&
                        olapgrid.AxesLayout.fColorForeAxisItem != null &&
                        olapgrid.AxesLayout.fColorForeAxisItem is Measure
                        //&& olapgrid.VM_GridAxisLayout.ColorForeAxisItem.AsMeasure == _a.Measure
                    )
                    {
                        var aOld = Address;
                        var aTmp = acs.FGrid.FEngine.CreateCubeAddress(memberlist);
                        Address = aTmp;

                        var mcolor = olapgrid.AxesLayout.fColorForeAxisItem as Measure;

                        Address.Measure = mcolor;
                        object forevalue;
                        CellFormattingProperties foreformat;
                        if (acs.FGrid.FEngine.GetCellFormattedValue(this, out forevalue, out foreformat))
                            RankForeground = GetColorRank(forevalue);
                        Address = aOld;
                    }
                    if (olapgrid != null && olapgrid.AxesLayout.fColorAxisItem is Hierarchy)
                    {
                        string un = null;
                        var h = olapgrid.AxesLayout.fColorAxisItem as Hierarchy;
                        un = FindByAHierarchy(ColumnMember, h);
                        if (string.IsNullOrEmpty(un))
                            un = FindByAHierarchy(RowMember, h);


                        if (un.IsFill())
                            MemberBackground = un;
                    }
                    if (olapgrid != null && olapgrid.AxesLayout.fColorForeAxisItem is Hierarchy)
                    {
                        string un = null;
                        var h = olapgrid.AxesLayout.fColorForeAxisItem as Hierarchy;
                        un = FindByAHierarchy(ColumnMember, h);
                        if (string.IsNullOrEmpty(un))
                            un = FindByAHierarchy(RowMember, h);

                        if (un.IsFill())
                            MemberForeground = un;
                    }

                    break;
                default:
                    acs.FGrid.Cube.RestoreQueryData(
                        aCol - acs.FFixedColumns + (aRow - acs.FFixedRows) * (acs.ColumnCount - acs.FFixedColumns),
                        out _fValue,
                        out _fFormattingProperties);

                    if (string.IsNullOrEmpty(_fFormattingProperties.FormattedValue) && _fValue != null)
                        _fFormattingProperties.FormattedValue = _fValue.ToString();
                    break;
            }
        }

        public CellType CellType => CellType.ctData;

        public CellSet CellSet { get; }

        public string Description
        {
            get
            {
                if (Data == null) return "";
                return Data.ToString().StartsWith("#ERROR!") ? Data.ToString().Substring(7) : "";
            }
        }

        public int StartRow { get; }

        public int StartColumn { get; }

        public int RowSpan => 1;

        public int ColSpan => 1;

        public string Value =>
            _fFormattingProperties.FormattedValue == null ? "" : _fFormattingProperties.FormattedValue;

        public void Drillthrough(DataTable dataTable, int RowsToFetch)
        {
            CellSet.Grid.Engine.Drillthrough(Address, dataTable, RowsToFetch, null);
        }

        public void Drillthrough(DataTable dataTable, int RowsToFetch, DrillThroughMethod DrillThroughMethod)
        {
            CellSet.Grid.Engine.Drillthrough(Address, dataTable, RowsToFetch, null, DrillThroughMethod);
        }

        public void Drillthrough(DataTable dataTable, int RowsToFetch, ICollection<string> columns)
        {
            CellSet.Grid.Engine.Drillthrough(Address, dataTable, RowsToFetch, columns);
        }

        public void Drillthrough(DataTable dataTable, int RowsToFetch, ICollection<string> columns,
            DrillThroughMethod DrillThroughMethod)
        {
            CellSet.Grid.Engine.Drillthrough(Address, dataTable, RowsToFetch, columns, DrillThroughMethod);
        }

        public void Drillthrough(DataTable ADataSet, string mdx)
        {
            CellSet.Grid.Engine.Drillthrough(ADataSet, mdx);
        }

        public object Data => _fValue;

        public ICubeAddress Address { get; }

        public IMemberCell RowMember => CellSet.Cells(CellSet.FFixedColumns - 1, StartRow) as IMemberCell;

        public IMemberCell ColumnMember => CellSet.Cells(StartColumn, CellSet.FFixedRows - 1) as IMemberCell;

        public bool IsTotal { get; }

        public bool IsTotalVertical { get; }

        public bool IsTotalHorizontal { get; }

        /// <summary>The font color of the cell</summary>
        public Color ForeColor => _fFormattingProperties.ForeColor;

        /// <summary>The backround color of the cell</summary>
        public Color BackColor => _fFormattingProperties.BackColor;

        public FontStyle? FontStyle => _fFormattingProperties.FontStyle;

        public double? FontSize => _fFormattingProperties.FontSize;

        public string FontFamily => _fFormattingProperties.FontFamily;

        public string Comment
        {
            get
            {
                var s = string.Empty;
                if (Address != null)
                    CellSet.fComments.TryGetValue(Address, out s);
                return s;
            }
            set
            {
                CellSet.fComments.Remove(Address);
                CellSet.fComments.Add(Address, value);
            }
        }

        public void Writeback(object NewValue, WritebackMethod Method)
        {
            CellSet.Grid.Engine.Writeback(Address, NewValue, Method);
        }

        public List<CubeAction> CubeActions
        {
            get
            {
                if (fActions != null)
                    return fActions;
                fActions = CellSet.FGrid.Cube.RetrieveActions(this);
                return fActions;
            }
        }

        public int PagedStartColumn
        {
            get
            {
                int i;
                if (CellSet.AdjustedColsHelper.TryGetValue(StartColumn, out i))
                    return i;
                throw new Exception("Invalid paging index conversion");
            }
        }

        public int PagedStartRow
        {
            get
            {
                int i;
                if (CellSet.AdjustedRowsHelper.TryGetValue(StartRow, out i))
                    return i;
                throw new Exception("Invalid paging index conversion");
            }
        }

        private string FindByAHierarchy(IMemberCell rm, Hierarchy AHierarchy)
        {
            var current = rm;
            if (AHierarchy.FirstVisibleLevel() == null)
                return null;

            var unfvl = AHierarchy.FirstVisibleLevel().UniqueName;

            while (current != null && current.Member != null && current.Parent != null &&
                   current.Member.Level.UniqueName != unfvl)
                current = current.Parent;

            if (current != null &&
                current.Member != null &&
                current.Member.Level != null &&
                //current.Member.Level.Hierarchy != null &&
                current.Member.Level.UniqueName == unfvl)
                return current.Member.UniqueName;

            return null;
        }

        private string FindByUniqueName(IMemberCell rm, string AUniqueName)
        {
            var current = rm;

            while (current != null &&
                   current.Member != null &&
                   current.Parent != null &&
                   (current.Member.MemberType != MemberType.mtCommon ||
                    current.Member.Level != null && current.Member.Level.UniqueName != AUniqueName ||
                    current.Member.Level.Hierarchy.Origin == HierarchyOrigin.hoParentChild
                    && current.Member.Level != null))
                current = current.Parent;

            if (current != null &&
                current.Member != null &&
                current.Member.Level != null &&
                current.Member.Level.Hierarchy != null &&
                current.Member.Level.Hierarchy.UniqueName == AUniqueName)
                return current.Member.UniqueName;

            return null;
        }

        private double GetColorRank()
        {
            double rank = -1;
            if (Address != null)
            {
                var rng = CellSet.Grid.GetMeasureRange(Address);
                if (rng != null)
                {
                    var d = -1.0;

                    if (Address.MeasureMode.Mode == MeasureShowModeType.smPercentRowTotal
                        || Address.MeasureMode.Mode == MeasureShowModeType.smPercentColTotal
                        || Address.MeasureMode.Mode == MeasureShowModeType.smPercentGrandTotal
                        || Address.MeasureMode.Mode == MeasureShowModeType.smPercentParentColItem
                        || Address.MeasureMode.Mode == MeasureShowModeType.smPercentParentRowItem)
                    {
                        object o = null;
                        var l = CellSet.Grid.FEngine.GetMetaline(Address.FLineID)
                            .GetLine(Address.FHierID, Address.Measure, Address.MeasureMode);

                        if (l != null)
                            l.GetCell(Address, out o);

                        if (o != null)
                            d = double.Parse(o.ToString());
                    }

                    if (rng.Item1 == rng.Item2)
                        rank = 1;
                    else
                        try
                        {
                            if (Address.MeasureMode.Mode == MeasureShowModeType.smPercentRowTotal
                                || Address.MeasureMode.Mode == MeasureShowModeType.smPercentColTotal
                                || Address.MeasureMode.Mode == MeasureShowModeType.smPercentGrandTotal
                                || Address.MeasureMode.Mode == MeasureShowModeType.smPercentParentColItem
                                || Address.MeasureMode.Mode == MeasureShowModeType.smPercentParentRowItem)
                            {
                                if (d != -1 && !double.IsNaN(d))
                                {
                                    var val = d;
                                    rank = (val - rng.Item1) / (rng.Item2 - rng.Item1);
                                }
                            }
                            else
                            {
                                var val = Convert.ToDouble(Data);
                                rank = (val - rng.Item1) / (rng.Item2 - rng.Item1);
                            }
                        }
                        catch
                        {
                            ;
                        }
                }
            }

            return rank;
        }

        private double GetColorRank(object valuecolor)
        {
            double rankColor = -1;
            var tuple = CellSet.Grid.GetMeasureRange(Address);
            if (tuple == null)
            {
#warning TODO(VOLJ) only  for KPI dragging
                return rankColor;
            }

            var d = Convert.ToDouble(valuecolor);

            if (tuple.Item2 > tuple.Item1)
                rankColor = (d - tuple.Item1) / (tuple.Item2 - tuple.Item1);

            if (tuple.Item2 == tuple.Item1)
                rankColor = 1;

            return rankColor;
        }
    }
}